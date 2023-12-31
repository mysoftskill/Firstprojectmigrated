namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Queries a command ID for an agent/asset group pair.
    /// </summary>
    internal class QueryCommandByIdActionResult : BaseHttpActionResult
    {
        private readonly HttpRequestMessage requestMessage;
        private readonly AuthenticationScope authenticationScope;
        private readonly IAuthorizer authorizer;
        private readonly ICommandHistoryRepository commandHistory;
        private readonly ICommandQueueFactory commandQueueFactory;
        private readonly CommandId commandId;
        private readonly AgentId agentId;
        private readonly AssetGroupId assetGroupId;
        private readonly IDataAgentMap dataAgentMap;

        public QueryCommandByIdActionResult(
            HttpRequestMessage request,
            CommandId commandId,
            AgentId agentId,
            AssetGroupId assetGroupId,
            IAuthorizer authorizer,
            IDataAgentMap dataAgentMap,
            AuthenticationScope authenticationScope,
            ICommandHistoryRepository commandHistory,
            ICommandQueueFactory commandQueueFactory)
        {
            this.requestMessage = request;
            this.commandId = commandId;
            this.agentId = agentId;
            this.assetGroupId = assetGroupId;

            this.authenticationScope = authenticationScope;
            this.authorizer = authorizer;
            this.commandHistory = commandHistory;
            this.commandQueueFactory = commandQueueFactory;
            this.dataAgentMap = dataAgentMap;
        }

        protected override async Task<HttpResponseMessage> ExecuteInnerAsync(CancellationToken cancellationToken)
        {
            var authContext = await this.authorizer.CheckAuthorizedAsync(this.requestMessage, this.authenticationScope);
            StressRequestForwarder.Instance.SendForwardedRequest(authContext, this.requestMessage, null);

            var (responseCode, command) = await this.ProcessAsync();

            QueryResponse response = new QueryResponse
            {
                ResponseCode = responseCode,
            };

            if (command != null)
            {
                bool isMultiTenantCollaborationSupported = ClientVersionHelper.DoesClientSupportMultiTenantCollaboration(this.requestMessage) &&
                                                           this.dataAgentMap[this.agentId].IsOptedIntoAadSubject2();
                var serializerOption = isMultiTenantCollaborationSupported ? CommandFeedSerializerOptions.MultiTenantCollaborationSupported : CommandFeedSerializerOptions.None;
                var serializer = new CommandFeedSerializer(serializerOption);
                response.Command = JObject.FromObject(serializer.Process(command));
            }

            if (response.ResponseCode == ResponseCode.CommandNotQueryable)
            {
                return new HttpResponseMessage(HttpStatusCode.MethodNotAllowed) { Content = new JsonContent(response) };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new JsonContent(response)
            };
        }

        private async Task<(ResponseCode response, PrivacyCommand command)> ProcessAsync()
        {
            // Start by querying command history
            CommandHistoryRecord record = await this.commandHistory.QueryAsync(this.commandId, CommandHistoryFragmentTypes.Core | CommandHistoryFragmentTypes.Status);
            if (record == null)
            {
                return (ResponseCode.CommandNotFound, null);
            }

            if (!new QueryCommandVisitor().Process(record.Core.QueueStorageType))
            {
                return (ResponseCode.CommandNotQueryable, null);
            }

            if (!record.StatusMap.TryGetValue((this.agentId, this.assetGroupId), out var statusRecord))
            {
                return (ResponseCode.CommandNotApplicable, null);
            }

            if (string.IsNullOrEmpty(statusRecord.StorageAccountMoniker))
            {
                return (ResponseCode.UnableToResolveLocation, null);
            }

            if (statusRecord.CompletedTime != null)
            {
                return (ResponseCode.CommandAlreadyCompleted, null);
            }

            if (statusRecord.IngestionTime == null)
            {
                return (ResponseCode.CommandNotYetDelivered, null);
            }

            // Create a fake lease receipt we can use to query with.
            var subjectType = record.Core.Subject.GetSubjectType();
            LeaseReceipt virtualLeaseReceipt = new LeaseReceipt(
                databaseMoniker: statusRecord.StorageAccountMoniker,
                commandId: this.commandId,
                token: string.Empty,   // etag is null since we don't have one.
                assetGroupId: this.assetGroupId,
                agentId: this.agentId,
                subjectType: subjectType,
                approximateExpirationTime: DateTimeOffset.MinValue,
                assetGroupQualifier: string.Empty,
                commandType: record.Core.CommandType,
                cloudInstance: string.Empty,
                commandCreatedTime: DateTimeOffset.UtcNow,
                queueStorageType: record.Core.QueueStorageType);

            var queue = this.commandQueueFactory.CreateQueue(this.agentId, this.assetGroupId, subjectType, record.Core.QueueStorageType);
            var command = await queue.QueryCommandAsync(virtualLeaseReceipt);

            if (command == null)
            {
                return (ResponseCode.CommandNotFoundInQueue, null);
            }

            return (ResponseCode.OK, command);
        }

        /// <summary>
        /// Don't rename these values; they are on the wire as strings.
        /// </summary>
        public enum ResponseCode
        {
            /// <summary>
            /// The command was found and has been returned.
            /// </summary>
            OK,

            /// <summary>
            /// The given command ID was not found in PCF. It may not exist or may have aged out.
            /// </summary>
            CommandNotFound,

            /// <summary>
            /// The command has been delivered, but it was not found in the agent's queue. This can happen in the case
            /// where the agent has already marked the command as completed and the command has been deleted from the queue,
            /// but PCF has not yet updated the overall state tracking map to reflect this fact. This is expected to be a temporary
            /// state and should not persist for more than a few minutes normally.
            /// </summary>
            CommandNotFoundInQueue,

            /// <summary>
            /// The command was not (and will not be) delivered to the given agent/asset group
            /// </summary>
            CommandNotApplicable,

            /// <summary>
            /// The command was applicable, but has not yet been delivered to the agent.
            /// </summary>
            CommandNotYetDelivered,

            /// <summary>
            /// The agent has already marked the command as completed.
            /// </summary>
            CommandAlreadyCompleted,

            /// <summary>
            /// This command predates the feature that tracks command insertion locations.
            /// </summary>
            UnableToResolveLocation,

            /// <summary>
            /// This command is not queryable.
            /// </summary>
            CommandNotQueryable,
        }

        internal class QueryResponse
        {
            [JsonProperty]
            [JsonConverter(typeof(StringEnumConverter))]
            public ResponseCode ResponseCode { get; set; }

            [JsonProperty]
            public JObject Command { get; set; }
        }
    }
}
