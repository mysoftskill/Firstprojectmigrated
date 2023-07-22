namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandLifecycleNotifications;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.Applicability;
    using Microsoft.PrivacyServices.SignalApplicability;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using PrivacyCommand = Microsoft.PrivacyServices.CommandFeed.Service.Common.PrivacyCommand;

    /// <summary>
    /// Implements the QueryCommand API.
    /// 
    /// The behavior of this class is to do the following:
    /// - Retrieve an existing command given a specific lease receipt
    /// - If the command is still in a pending state, return it
    /// </summary>
    internal class QueryCommandActionResult : BaseHttpActionResult
    {
        private readonly HttpRequestMessage requestMessage;
        private readonly AgentId agentId;
        private readonly ICommandQueue queue;
        private readonly ICommandHistoryRepository commandHistory;
        private readonly IDataAgentMap dataAgentMap;
        private readonly IAuthorizer authorizer;
        private readonly ICommandLifecycleEventPublisher publisher;
        
        public QueryCommandActionResult(
            AgentId agentId,
            HttpRequestMessage requestMessage,
            ICommandQueue queue,
            ICommandHistoryRepository commandHistory,
            IDataAgentMap dataAgentMap,
            ICommandLifecycleEventPublisher publisher,
            IAuthorizer authorizer)
        {
            this.requestMessage = requestMessage;
            this.agentId = agentId;
            this.queue = queue;
            this.dataAgentMap = dataAgentMap;
            this.publisher = publisher;
            this.authorizer = authorizer;
            this.commandHistory = commandHistory;
        }

        protected override async Task<HttpResponseMessage> ExecuteInnerAsync(CancellationToken cancellationToken)
        {
            var authContext = await this.authorizer.CheckAuthorizedAsync(this.requestMessage, this.agentId);

            string requestBody = await this.requestMessage.Content.ReadAsStringAsync();
            var request = JsonConvert.DeserializeObject<QueryCommandRequest>(requestBody);
            
            if (!LeaseReceipt.TryParse(request.LeaseReceipt, out LeaseReceipt leaseReceipt))
            {
                return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
                {
                    Content = new JsonContent(new QueryCommandError(QueryCommandErrorCode.MalformedLeaseReceipt))
                };
            }

            StressRequestForwarder.Instance.SendForwardedRequest(authContext, this.requestMessage, new StringContent(requestBody), leaseReceipt.AgentId, leaseReceipt.AssetGroupId, leaseReceipt.CommandId);
            leaseReceipt = await LeaseReceiptUtility.LoadEnvironmentLeaseReceipt(leaseReceipt, this.commandHistory, this.queue);

            IncomingEvent.Current?.SetProperty("AgentId", leaseReceipt.AgentId.Value);
            IncomingEvent.Current?.SetProperty("AssetGroupId", leaseReceipt.AssetGroupId.Value);
            IncomingEvent.Current?.SetProperty("CommandId", leaseReceipt.CommandId.Value);
            IncomingEvent.Current?.SetProperty("DatabaseMoniker", leaseReceipt.DatabaseMoniker);
            IncomingEvent.Current?.SetProperty("QueueStorageType", leaseReceipt.QueueStorageType.ToString());

            if (leaseReceipt.AgentId != this.agentId)
            {
                return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
                {
                    Content = new JsonContent(new QueryCommandError(QueryCommandErrorCode.LeaseReceiptAgentIdMismatch))
                };
            }

            if (!new QueryCommandVisitor().Process(leaseReceipt.QueueStorageType))
            {
                return new HttpResponseMessage(System.Net.HttpStatusCode.MethodNotAllowed)
                {
                    Content = new JsonContent(new QueryCommandError(QueryCommandErrorCode.CommandNotQueryable))
                };
            }

            PrivacyCommand existingCommand;
            try
            {
                existingCommand = await this.queue.QueryCommandAsync(leaseReceipt);
            }
            catch (CommandFeedException ex)
            {
                if (ex.ErrorCode == CommandFeedInternalErrorCode.InvalidLeaseReceipt)
                {
                    return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
                    {
                        Content = new JsonContent(new QueryCommandError(QueryCommandErrorCode.LeaseReceiptNotSupported))
                    };
                }

                throw;
            }

            if (existingCommand == null)
            {
                return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
                {
                    Content = new JsonContent(new QueryCommandError(QueryCommandErrorCode.CommandNotFound))
                };
            }

            var dataAgent = this.dataAgentMap[this.agentId];
            if (dataAgent.TryGetAssetGroupInfo(existingCommand.AssetGroupId, out var assetGroupInfo))
            {
                // delete the command from queue if its is not actionable except in the cases 
                // where the applicability result is tag dependent. In such a case, 
                // lets trust the command ingestion applicability and send to agent 
                // this is to prevent accidentally completing commands on behalf of the the agent
                // if there are upstream issues in generating PcfConfig
                if (!assetGroupInfo.IsCommandActionable(existingCommand, out var applicabilityResult))
                {
                    if (!ApplicabilityHelper.IsApplicabilityResultTagDependent(applicabilityResult.ReasonCode))
                    {
                        // delete the completed command from the queue
                        try
                        {
                            await this.queue.DeleteAsync(existingCommand.LeaseReceipt);
                        }
                        catch (CommandFeedException ex)
                        {
                            if (ex.ErrorCode == CommandFeedInternalErrorCode.NotFound ||
                                ex.ErrorCode == CommandFeedInternalErrorCode.Conflict)
                            {
                                return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
                                {
                                    Content = new JsonContent(new QueryCommandError(QueryCommandErrorCode.CommandNotFound))
                                };
                            }

                            throw;
                        }

                        if (applicabilityResult.ReasonCode == ApplicabilityReasonCode.FilteredByVariant)
                        {
                            List<IAssetGroupVariantInfo> applicableVariants = applicabilityResult.GetPcfApplicableVariants(assetGroupInfo.VariantInfosAppliedByPcf).ToList();
                            await this.publisher.PublishCommandCompletedAsync(
                                existingCommand.AgentId,
                                existingCommand.AssetGroupId,
                                existingCommand.AssetGroupQualifier,
                                existingCommand.CommandId,
                                existingCommand.CommandType,
                                existingCommand.Timestamp,
                                applicableVariants.Select(vn => vn.VariantId.Value).ToArray(),
                                ignoredByVariant: true,
                                rowCount: 0,
                                delinked: false,
                                completedByPcf: true,
                                nonTransientExceptions: null);
                        }
                        else
                        {
                            await this.publisher.PublishCommandCompletedAsync(
                                existingCommand.AgentId,
                                existingCommand.AssetGroupId,
                                existingCommand.AssetGroupQualifier,
                                existingCommand.CommandId,
                                existingCommand.CommandType,
                                existingCommand.Timestamp,
                                null,
                                ignoredByVariant: false,
                                rowCount: 0,
                                delinked: false,
                                completedByPcf: true,
                                nonTransientExceptions: null);
                        }

                        return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
                        {
                            Content = new JsonContent(new QueryCommandError(QueryCommandErrorCode.CommandAlreadyCompleted))
                        };
                    }
                }

                // update command with applicable properties
                existingCommand.ApplicableVariants = existingCommand.GetCommandApplicableVariants(assetGroupInfo.VariantInfosAppliedByAgents).ToList();
                existingCommand.DataTypeIds = applicabilityResult.ApplicableDataTypes;
            }
            else
            {
                return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
                {
                    Content = new JsonContent(new QueryCommandError(QueryCommandErrorCode.LeaseReceiptAssetGroupIdInvalid))
                };
            }

            bool isMultiTenantCollaborationSupported = ClientVersionHelper.DoesClientSupportMultiTenantCollaboration(this.requestMessage) &&
                                                       dataAgent.IsOptedIntoAadSubject2();
            var serializerOption = isMultiTenantCollaborationSupported ? CommandFeedSerializerOptions.MultiTenantCollaborationSupported : CommandFeedSerializerOptions.None;
            var serializer = new CommandFeedSerializer(serializerOption);

            IncomingEvent.Current?.SetProperty("IsMultiTenantCollaborationSupported", isMultiTenantCollaborationSupported.ToString());

            QueryCommandResponse response = new QueryCommandResponse
            {
                Command = JObject.FromObject(serializer.Process(existingCommand))
            };

            await this.publisher.PublishCommandSentToAgentAsync(
                this.agentId,
                existingCommand.AssetGroupId,
                existingCommand.AssetGroupQualifier,
                existingCommand.CommandId,
                existingCommand.CommandType);

            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new JsonContent(response)
            };
        }

        internal enum QueryCommandErrorCode
        {
            LeaseReceiptAgentIdMismatch = 2,
            CommandNotFound = 3,
            CommandAlreadyCompleted = 4,
            LeaseReceiptAssetGroupIdInvalid = 6,
            MalformedLeaseReceipt = 9,
            LeaseReceiptNotSupported = 13,
            CommandNotQueryable = 14
        }

        internal class QueryCommandError
        {
            public QueryCommandError(QueryCommandErrorCode errorCode)
            {
                this.Message = errorCode.ToString();
                this.ErrorCode = errorCode;
            }

            public string Message { get; set; }

            public QueryCommandErrorCode ErrorCode { get; set; }
        }
    }
}
