// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    internal class ResetNextVisibleTimeActionResult : BaseHttpActionResult
    {
        private readonly HttpRequestMessage request;
        private readonly AuthenticationScope authenticationScope;
        private readonly CommandId commandId;
        private readonly AgentId agentId;
        private readonly AssetGroupId assetGroupId;

        private readonly IDataAgentMap dataAgentMap;
        private readonly ICommandHistoryRepository commandHistoryRepository;
        private readonly IAuthorizer authorizer;
        private readonly ICommandQueueFactory commandQueueFactory;

        public ResetNextVisibleTimeActionResult(
            HttpRequestMessage requestMessage,
            CommandId commandId,
            AgentId agentId,
            AssetGroupId assetGroupId,
            IDataAgentMap dataAgentMap,
            ICommandHistoryRepository commandHistoryRepository,
            IAuthorizer authorizer,
            AuthenticationScope authenticationScope,
            ICommandQueueFactory commandQueueFactory)
        {
            this.request = requestMessage;
            this.commandId = commandId;
            this.agentId = agentId;
            this.assetGroupId = assetGroupId;
            this.dataAgentMap = dataAgentMap;
            this.commandHistoryRepository = commandHistoryRepository;
            this.authorizer = authorizer;
            this.authenticationScope = authenticationScope;
            this.commandQueueFactory = commandQueueFactory;
        }

        protected override async Task<HttpResponseMessage> ExecuteInnerAsync(CancellationToken cancellationToken)
        {
            await this.authorizer.CheckAuthorizedAsync(this.request, this.authenticationScope);

            // Start by querying command history
            CommandHistoryRecord record = await this.commandHistoryRepository.QueryAsync(this.commandId, CommandHistoryFragmentTypes.Core | CommandHistoryFragmentTypes.Status);
            if (record?.Core == null)
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            if (!new QueryCommandVisitor().Process(record.Core.QueueStorageType))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            if (!record.StatusMap.TryGetValue((this.agentId, this.assetGroupId), out CommandHistoryAssetGroupStatusRecord statusRecord))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent($"No status record exists. AgentId: {this.agentId}. AssetGroupId: {this.assetGroupId}") };
            }

            // This agent+assetGroup has NOT completed the command.
            string assetGroupQualifier = string.Empty;

            if (this.dataAgentMap.TryGetAgent(this.agentId, out IDataAgentInfo dataAgentInfo) &&
                dataAgentInfo.TryGetAssetGroupInfo(this.assetGroupId, out IAssetGroupInfo assetGroupInfo))
            {
                assetGroupQualifier = assetGroupInfo.AssetGroupQualifier;
            }

            // Create a fake lease receipt we can use to query with.
            // This code is similar to 'QueryCommandByIdActionResult' where we don't have the lease receipt of the command.
            var subjectType = record.Core.Subject.GetSubjectType();
            LeaseReceipt virtualLeaseReceipt = new LeaseReceipt(
                databaseMoniker: statusRecord.StorageAccountMoniker,
                commandId: this.commandId,
                token: string.Empty,   // etag is null since we don't have one.
                assetGroupId: this.assetGroupId,
                agentId: this.agentId,
                subjectType: subjectType,
                approximateExpirationTime: DateTimeOffset.MinValue,
                assetGroupQualifier: assetGroupQualifier,
                commandType: record.Core.CommandType,
                cloudInstance: string.Empty,
                commandCreatedTime: DateTimeOffset.UtcNow,
                queueStorageType: record.Core.QueueStorageType);

            ICommandQueue queue = this.commandQueueFactory.CreateQueue(this.agentId, this.assetGroupId, subjectType, record.Core.QueueStorageType);
            PrivacyCommand command = await queue.QueryCommandAsync(virtualLeaseReceipt);

            if (command == null)
            {
                // indicates command not found
                return new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent($"CommandId is not found: {this.commandId.Value}") };
            }

            command.NextVisibleTime = DateTimeOffset.UtcNow;
            await queue.ReplaceAsync(command.LeaseReceipt, command, CommandReplaceOperations.LeaseExtension);

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
