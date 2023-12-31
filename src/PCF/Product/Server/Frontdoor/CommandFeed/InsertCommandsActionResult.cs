namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue.QueueStorageCommandQueue;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Validator;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using AgeOutCommand = Microsoft.PrivacyServices.CommandFeed.Service.Common.AgeOutCommand;
    using DeleteCommand = Microsoft.PrivacyServices.CommandFeed.Service.Common.DeleteCommand;
    using ExportCommand = Microsoft.PrivacyServices.CommandFeed.Service.Common.ExportCommand;
    using PrivacyCommand = Microsoft.PrivacyServices.CommandFeed.Service.Common.PrivacyCommand;

#if INCLUDE_TEST_HOOKS

    /// <summary>
    /// Implements the InsertCommands test API.
    /// </summary>
    internal class InsertCommandsActionResult : BaseHttpActionResult
    {
        private readonly ICommandQueueFactory queueFactory;
        private readonly IDataAgentMap dataAgentMap;
        private readonly IAuthorizer authorizer;
        private readonly IValidationService validationService;
        private readonly ICommandHistoryRepository commandHistory;
        private readonly HttpRequestMessage requestMessage;
        private readonly AgentId agentId;
        private readonly AzureQueueStorageContext azureQueueStorageCommandContext;

        public InsertCommandsActionResult(
            AgentId agentId,
            HttpRequestMessage requestMessage,
            ICommandQueueFactory queueFactory,
            IDataAgentMap dataAgentMap,
            IAuthorizer authorizer,
            IValidationService validationService,
            ICommandHistoryRepository commandHistory,
            AzureQueueStorageContext azureQueueStorageCommandContext)
        {
            this.queueFactory = queueFactory;
            this.requestMessage = requestMessage;
            this.agentId = agentId;
            this.dataAgentMap = dataAgentMap;
            this.authorizer = authorizer;
            this.validationService = validationService;
            this.commandHistory = commandHistory;
            this.azureQueueStorageCommandContext = azureQueueStorageCommandContext;
        }

        protected override async Task<HttpResponseMessage> ExecuteInnerAsync(CancellationToken cancellationToken)
        {
            // Return error when configuration disallows.
            if (!Config.Instance.Frontdoor.SyntheticCommandInsertionEnabled)
            {
                return new HttpResponseMessage(System.Net.HttpStatusCode.MethodNotAllowed);
            }

            await this.authorizer.CheckAuthorizedAsync(this.requestMessage, this.agentId);

            string content = await this.requestMessage.Content.ReadAsStringAsync();
            JObject[] requestMessage = JsonConvert.DeserializeObject<JObject[]>(content);

            CommandFeedParser parser = new CommandFeedParser(this.agentId, new RequestBatchId(Guid.NewGuid()));

            // Make a list of all of the commands we're to insert for this agent.
            List<PrivacyCommand> commands = new List<PrivacyCommand>();

            try
            {
                commands.AddRange(requestMessage.Select(parser.Parse));
            }
            catch (FormatException ex)
            {
                throw new BadRequestException($"Invalid format in request body: {ex.GetType().Name}/{ex.Message}");
            }

            IDataAgentInfo dataAgentInfo = this.dataAgentMap[this.agentId];
            var knownAssetGroups = dataAgentInfo.AssetGroupInfos.Where(agi => !agi.IsDeprecated).ToDictionary(x => x.AssetGroupQualifier, x => x);

            foreach (var command in commands)
            {
                // Check the verifier on the command.
                bool isVerifierValid = await command.IsVerifierValidAsync(this.validationService);
                if (!isVerifierValid)
                {
                    command.Verifier = string.Empty;
                    command.VerifierV3 = string.Empty;
                }

                AssetGroupId targetGroupId;

                if (knownAssetGroups.TryGetValue(command.AssetGroupQualifier, out IAssetGroupInfo assetGroupInfo))
                {
                    // If the command matches a known target for this Agent, patch the command's AssetGroupId accordingly
                    targetGroupId = assetGroupInfo.AssetGroupId;
                    command.AssetGroupId = assetGroupInfo.AssetGroupId;

                    try
                    {
                        // Enforce that commands are applicable to the given asset group.
                        if (!assetGroupInfo.IsCommandActionable(command, out var applicabilityResult))
                        {
                            throw new BadRequestException($"Command is not actionable: {applicabilityResult.ReasonDescription}.");
                        }
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        throw new BadRequestException($"Command is not actionable: {ex.Message}.");
                    }
                }
                else if (command.AssetGroupId != Config.Instance.PPEHack.FixedAssetGroupId)
                {
                    throw new BadRequestException($"Invalid asset group qualifier: {command.AssetGroupQualifier}. Valid choices are {string.Join(",", knownAssetGroups.Keys)}. Or set AssetGroupId=Guid.Empty to create a fake command.");
                }

                QueueStorageType queueStorageType = new QueueStorageTypeSelector().Process(command);

                var queue = this.queueFactory.CreateQueue(this.agentId, command.AssetGroupId, command.Subject.GetSubjectType(), queueStorageType);

                string preferredMoniker = CommandMonikerHash.GetPreferredMoniker(command.CommandId, command.AssetGroupId, CommandMonikerHash.GetCurrentWeightedMonikers(queueStorageType));

                try
                {
                    // Insert a stub record into cold storage for this command.
                    // The raw pxs command is required to do any Checkpoint operations that manage agent state
                    var record = new CommandHistoryRecord(command.CommandId);
                    record.Core.RawPxsCommand = new PcfToRawPxsCommandParser(isTestRequest: true).Process(command);
                    record.Core.QueueStorageType = queueStorageType;
                    record.Core.CreatedTime = command.Timestamp == default(DateTimeOffset) ? DateTimeOffset.UtcNow : command.Timestamp;

                    if (command is ExportCommand exportCommand)
                    {
                        var exportDestinationRecord = new CommandHistoryExportDestinationRecord(
                            this.agentId,
                            command.AssetGroupId,
                            exportCommand.AzureBlobContainerTargetUri,
                            exportCommand.AzureBlobContainerPath);
                        record.ExportDestinations[(this.agentId, command.AssetGroupId)] = exportDestinationRecord;
                    }

                    bool success = await this.commandHistory.TryInsertAsync(record);

                    // Will be handled below.
                    if (!success)
                    {
                        throw new CommandFeedException("Command History record already exists") { ErrorCode = CommandFeedInternalErrorCode.Conflict };
                    }

                    await queue.EnqueueAsync(preferredMoniker, command);
                }
                catch (CommandFeedException ex) when (ex.ErrorCode == CommandFeedInternalErrorCode.Conflict)
                {
                    IncomingEvent.Current?.SetProperty("InsertConflict", "true");
                    throw new BadRequestException($"Invalid CommandId: {command.CommandId} already exist. Try inserting command with a different ID");
                }
            }

            return new HttpResponseMessage(System.Net.HttpStatusCode.NoContent);
        }

        // This parser is only used as a test hook. In production, this conversion is never done in the reverse. This is just to support the InsertCommandsActionResult
        private class PcfToRawPxsCommandParser : ICommandVisitor<PrivacyCommand, string>
        {
            private readonly bool isTestRequest;

            public PcfToRawPxsCommandParser(bool isTestRequest)
            {
                this.isTestRequest = isTestRequest;
            }

            public PrivacyCommandType Classify(PrivacyCommand command)
            {
                return command.CommandType;
            }

            public string VisitAccountClose(PrivacyCommand command)
            {
                var request =
                    new AccountCloseRequest
                    {
                        RequestType = RequestType.AccountClose,
                        AccountCloseReason = AccountCloseReason.UserAccountClosed
                    };

                PopulateCommonProperties(command, request, this.isTestRequest);
                return JsonConvert.SerializeObject(request);
            }

            public string VisitAgeOut(PrivacyCommand command)
            {
                var ageOutCommand = (AgeOutCommand)command;

                var request =
                    new AgeOutRequest
                    {
                        RequestType = RequestType.AgeOut,
                        IsSuspended = ageOutCommand.IsSuspended ?? false,
                        LastActive = ageOutCommand.LastActive
                    };

                PopulateCommonProperties(command, request, this.isTestRequest);
                return JsonConvert.SerializeObject(request);
            }

            public string VisitDelete(PrivacyCommand command)
            {
                var deleteCommand = (DeleteCommand)command;

                var request =
                    new DeleteRequest
                    {
                        RequestType = RequestType.Delete,
                        Predicate = deleteCommand.Predicate,
                        TimeRangePredicate = deleteCommand.TimeRangePredicate,
                        PrivacyDataType = deleteCommand.DataType.Value
                    };

                PopulateCommonProperties(command, request, this.isTestRequest);

                return JsonConvert.SerializeObject(request);
            }

            public string VisitExport(PrivacyCommand command)
            {
                var exportCommand = (ExportCommand)command;

                var request =
                    new ExportRequest
                    {
                        RequestType = RequestType.Export,
                        PrivacyDataTypes = exportCommand.DataTypeIds.Select(i => i.Value),
                        StorageUri = exportCommand.AzureBlobContainerTargetUri
                    };

                PopulateCommonProperties(command, request, this.isTestRequest);
                return JsonConvert.SerializeObject(request);
            }

            private static void PopulateCommonProperties(PrivacyCommand command, PrivacyRequest request, bool isTestRequest)
            {
                // Common Command Properties
                request.RequestId = command.CommandId.GuidValue;
                request.ProcessorApplicable = command.ProcessorApplicable ?? false; // Value was nullable before onboarding period, old commands are nullable
                request.ControllerApplicable = command.ControllerApplicable ?? false; // Value was nullable before onboarding period, old commands are nullable
                request.Subject = command.Subject;
                request.CloudInstance = command.CloudInstance;
                request.Timestamp = command.Timestamp;
                request.CorrelationVector = command.CorrelationVector;
                request.IsSyntheticRequest = command.IsSyntheticTestCommand;
                request.IsTestRequest = isTestRequest;
                request.Portal = command.CommandSource;
                request.RequestGuid = command.RequestBatchId.GuidValue;
                request.VerificationToken = command.Verifier;
                request.VerificationTokenV3 = command.VerifierV3;

                // this conversion backwards doesn't happen in product code - this is just specific to this test api
                switch (command.CommandType)
                {
                    case PrivacyCommandType.None:
                        request.RequestType = RequestType.None;
                        break;
                    case PrivacyCommandType.Delete:
                        request.RequestType = RequestType.Delete;
                        break;
                    case PrivacyCommandType.Export:
                        request.RequestType = RequestType.Export;
                        break;
                    case PrivacyCommandType.AccountClose:
                        request.RequestType = RequestType.AccountClose;
                        break;
                    case PrivacyCommandType.AgeOut:
                        request.RequestType = RequestType.AgeOut;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                // Properties that don't map between contracts
                request.IsWatchdogRequest = false; // Does not map backwards because this is not a property in a PrivacyCommand
                request.AuthorizationId = string.Empty; // Does not map backwards because this is not a property in a PrivacyCommand
                request.Context = string.Empty;
                request.Requester = string.Empty;
            }
        }
    }

#endif
}
