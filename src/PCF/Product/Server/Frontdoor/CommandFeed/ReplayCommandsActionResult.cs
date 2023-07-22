namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts;
    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.Identity;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    internal class ReplayCommandsActionResult : BaseHttpActionResult
    {
        internal const int MaximumReplayCommands = 50;

        private readonly AgentId agentId;
        private readonly HttpRequestMessage request;
        private readonly IDataAgentMap dataAgentMap;
        private readonly IAuthorizer authorizer;
        private readonly AuthenticationScope authenticationScope;
        private readonly IAzureWorkItemQueuePublisher<ReplayRequestWorkItem> requestPublisher;
        private readonly IAzureWorkItemQueuePublisher<EnqueueBatchReplayCommandsWorkItem> batchCommandsPublisher;
        private readonly ICommandHistoryRepository commandHistory;
        private readonly IAppConfiguration appConfiguration;

        public ReplayCommandsActionResult(
            AgentId agentId,
            HttpRequestMessage request,
            IDataAgentMap dataAgentMap,
            IAuthorizer authorizer,
            AuthenticationScope authenticationScope,
            IAzureWorkItemQueuePublisher<ReplayRequestWorkItem> requestPublisher,
            IAzureWorkItemQueuePublisher<EnqueueBatchReplayCommandsWorkItem> batchCommandsPublisher,
            ICommandHistoryRepository commandHistory,
            IAppConfiguration appConfiguration)
        {
            this.agentId = agentId;
            this.request = request;
            this.dataAgentMap = dataAgentMap;
            this.requestPublisher = requestPublisher;
            this.batchCommandsPublisher = batchCommandsPublisher;
            this.commandHistory = commandHistory;

            this.authorizer = authorizer;
            this.authenticationScope = authenticationScope;
            this.appConfiguration = appConfiguration;
        }

        protected override async Task<HttpResponseMessage> ExecuteInnerAsync(CancellationToken cancellationToken)
        {
            IncomingEvent.Current?.SetProperty("AgentId", this.agentId.Value);

            PcfAuthenticationContext authContext;
            if (this.authenticationScope == AuthenticationScope.Agent)
            {
                authContext = await this.authorizer.CheckAuthorizedAsync(this.request, this.agentId);
            }
            else
            {
                authContext = await this.authorizer.CheckAuthorizedAsync(this.request, this.authenticationScope);
            }

            string requestBody = await this.request.Content.ReadAsStringAsync();
            StressRequestForwarder.Instance.SendForwardedRequest(authContext, this.request, new StringContent(requestBody), this.agentId);

            if (FlightingUtilities.IsAgentIdEnabled(FlightingNames.CommandReplayDisallowedAgentIds, this.agentId))
            {
                // the agent is blocked from calling Replay
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new JsonContent(new ReplayCommandsError(ReplayCommandsErrorCode.AgentNotAllowed))
                };
            }

            ReplayCommandsRequest replayRequest;
            try
            {
                replayRequest = JsonConvert.DeserializeObject<ReplayCommandsRequest>(requestBody);
            }
            catch (JsonReaderException ex)
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent($"Request cannot be parsed: {ex.GetType().Name}/{ex.Message}") };
            }

            if (replayRequest == null)
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("The request content is empty.") };
            }

            var assetQualifiers = replayRequest.AssetQualifiers;
            IDataAgentInfo agentInfo = this.dataAgentMap[this.agentId];
            var assetGroupIds = new List<AssetGroupId>();

            if (assetQualifiers == null || assetQualifiers.Length < 1)
            {
                // if assetGroupQualifiers are null, apply replay for all asset groups belong to this agent
                // do not replay when agent readiness state is TIP
                assetGroupIds = agentInfo.AssetGroupInfos.Where(x => x.AgentReadinessState != AgentReadinessState.TestInProd).Select(x => x.AssetGroupId).ToList();
            }
            else
            {
                // remove duplicate assetQualifiers
                HashSet<string> distinctAssetQualifiers = new HashSet<string>(assetQualifiers);

                // Parse and validate each assetGroupQualifiers
                // If valid, convert to assetGroupId
                foreach (var aq in distinctAssetQualifiers)
                {
                    AssetQualifier assetQualifier;
                    try
                    {
                        assetQualifier = AssetQualifier.Parse(aq);
                    }
                    catch
                    {
                        return new HttpResponseMessage(HttpStatusCode.BadRequest)
                        {
                            Content = new JsonContent(new ReplayCommandsError(ReplayCommandsErrorCode.MalformedAssetQualifier))
                        };
                    }

                    IAssetGroupInfo assetGroup;

                    try
                    {
                        assetGroup = agentInfo.AssetGroupInfos.Single(x => x.AssetQualifier == assetQualifier && !x.IsDeprecated);
                    }
                    catch (InvalidOperationException)
                    {
                        return new HttpResponseMessage(HttpStatusCode.BadRequest)
                        {
                            Content = new JsonContent(new ReplayCommandsError(ReplayCommandsErrorCode.AssetQualifierNotFound))
                        };
                    }

                    assetGroupIds.Add(assetGroup.AssetGroupId);
                }
            }

            IncomingEvent.Current?.SetProperty("AssetGroupCount", assetGroupIds.Count.ToString());

            if (replayRequest.CommandIds != null && replayRequest.CommandIds.Length > 0)
            {
                return await this.ReplayByCommandIds(replayRequest.CommandIds, assetGroupIds);
            }
            else
            {
                if (replayRequest.ReplayFromDate == null || replayRequest.ReplayToDate == null)
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new JsonContent(new ReplayCommandsError(ReplayCommandsErrorCode.InvalidReplayDates))
                    };
                }

                return await this.ReplayByDates(
                    replayRequest.ReplayFromDate.Value.ToUniversalTime().GetDate(), replayRequest.ReplayToDate.Value.ToUniversalTime().GetDate(), assetGroupIds, replayRequest.IncludeExportCommands, replayRequest.SubjectType);
            }
        }

        private async Task<HttpResponseMessage> ReplayByDates(DateTimeOffset replayFromDate,
                                                                DateTimeOffset replayToDate,
                                                                IEnumerable<AssetGroupId> assetGroupIds,
                                                                bool? replayExportCommandsAlso,
                                                                string subjectType = default)
        {
            IncomingEvent.Current?.SetProperty("ReplayFromDate", replayFromDate.ToString());
            IncomingEvent.Current?.SetProperty("ReplayToDate", replayToDate.ToString());
            IncomingEvent.Current?.SetProperty("ReplayExportsAlso", replayExportCommandsAlso.ToString());

            // Validate replay dates
            bool isReplayDatesValid = true;
            if (replayFromDate < DateTimeOffset.UtcNow.AddDays(-Config.Instance.CommandReplay.MaxReplayDays).GetDate())
            {
                isReplayDatesValid = false;

                List<ICustomOperatorContext> flightContext = new List<ICustomOperatorContext>
                {
                    FlightingContext.FromAgentId(this.agentId),
                    FlightingContext.FromIntegerValue((DateTimeOffset.UtcNow.GetDate() - replayFromDate).Days)
                };
                // Use flight to temporarily allow agents to replay back to X days.
                if (FlightingUtilities.IsEnabledAll(
                    FlightingNames.CommandReplayExtendedMaxReplayDates, flightContext))
                {
                    isReplayDatesValid = true;
                }
            }
            else if (replayToDate > DateTimeOffset.UtcNow.GetDate() || replayFromDate > replayToDate)
            {
                isReplayDatesValid = false;
            }

            if (!isReplayDatesValid)
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new JsonContent(new ReplayCommandsError(ReplayCommandsErrorCode.InvalidReplayDates))
                };
            }

            // Create the command replay work item and publish to queue
            var workItem = new ReplayRequestWorkItem
            {
                AssetGroupIds = assetGroupIds,
                ReplayFromDate = replayFromDate,
                ReplayToDate = replayToDate,
                SubjectType = subjectType,
                IncludeExportCommands = replayExportCommandsAlso
            };

            await this.requestPublisher.PublishAsync(workItem);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        private async Task<HttpResponseMessage> ReplayByCommandIds(string[] commandIds, IEnumerable<AssetGroupId> assetGroupIds)
        {
            if (commandIds.Length > MaximumReplayCommands)
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new JsonContent(new ReplayCommandsError(ReplayCommandsErrorCode.CommandsExceedsMaxNumberAllowed))
                };
            }

            var dedupCommandIds = new HashSet<string>(commandIds);
            var queryCommandTasks = new List<Task<CommandHistoryRecord>>();
            var errors = new ConcurrentDictionary<string, string>();

            var isExportReplayEnabled = false;
            if (await appConfiguration.IsFeatureFlagEnabledAsync(FeatureNames.PCF.EnableExportCommandReplay).ConfigureAwait(false))
            {
                isExportReplayEnabled = true;
            }

            foreach (var cid in dedupCommandIds)
            {
                queryCommandTasks.Add(this.QueryCommandForReplay(cid, isExportReplayEnabled, errors));
            }

            await Task.WhenAll(queryCommandTasks).ConfigureAwait(false);

            if (errors.Count != 0)
            {
                var errorMessage = new StringBuilder();
                foreach (var error in errors)
                {
                    errorMessage.Append($"[CommandId:{error.Key},Error:{error.Value}];");
                }

                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new JsonContent(new ReplayCommandsError(ReplayCommandsErrorCode.InvalidCommandIds, errorMessage.ToString()))
                };
            }

            var replayItems = new List<ReplayCommandDestinationPair>();
            List<(AgentId, AssetGroupId)> destinations = assetGroupIds.Select(x => (this.agentId, x)).ToList();
            foreach (var task in queryCommandTasks)
            {
                var command = task.Result;
                replayItems.Add(new ReplayCommandDestinationPair(JsonConvert.DeserializeObject<JObject>(command.Core.RawPxsCommand), destinations));
            }

            await this.batchCommandsPublisher.PublishWithSplitAsync(
                replayItems,
                batch =>
                {
                    return new EnqueueBatchReplayCommandsWorkItem
                    {
                        ReplayCommandsBatch = batch.ToList(),
                        IsApplicabilityVerified = false,
                    };
                },
                x => TimeSpan.Zero);

            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        private async Task<CommandHistoryRecord> QueryCommandForReplay(string commandId, bool isExportReplayEnabled, IDictionary<string, string> errors)
        {
            CommandId cmdId;
            try
            {
                cmdId = new CommandId(commandId);
            }
            catch
            {
                errors[commandId] = "InvalidFormat";
                return null;
            }

            var record = await this.commandHistory.QueryAsync(cmdId, CommandHistoryFragmentTypes.Core);
            if (record == null)
            {
                errors[commandId] = "NotFound";
                return null;
            }

            if (record.Core.CommandType == Client.PrivacyCommandType.Export)
            {
                if (!isExportReplayEnabled)
                {
                    errors[commandId] = "ExportNotSupported";
                    return null;
                }
                else if (record.Core.IsGloballyComplete)
                {
                    errors[commandId] = "Command Globally Complete";
                    return null;
                }
            }

            return record;
        }

        internal enum ReplayCommandsErrorCode
        {
            InvalidReplayDates = 1,
            MalformedAssetQualifier = 2,
            AssetQualifierNotFound = 3,
            AgentNotAllowed = 4,
            CommandsExceedsMaxNumberAllowed = 5,
            InvalidCommandIds = 6
        }

        internal class ReplayCommandsError
        {
            public ReplayCommandsError(ReplayCommandsErrorCode errorCode, string message = null)
            {
                this.Message = message ?? errorCode.ToString();
                this.ErrorCode = errorCode;
            }

            public string Message { get; set; }

            public ReplayCommandsErrorCode ErrorCode { get; set; }
        }
    }
}
