// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.NgpProxy.PcfDataAgent
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.Common.Azure;

    public class PcfDataAgent : IPrivacyDataAgent
    {
        private readonly ILogger logger;
        private readonly string agentId;
        private readonly CommandFeedEndpointConfiguration config;

        public PcfDataAgent(ILogger logger, string agentId, CommandFeedEndpointConfiguration configuration)
        {
            this.logger = logger;
            this.config = configuration;
            this.agentId = agentId;
        }

        /// <inheritdoc />
        public async Task ProcessAccountClosedAsync(IAccountCloseCommand command)
        {
            await this.ProcessAsync(command);
        }

        public async Task ProcessAgeOutAsync(IAgeOutCommand command)
        {
            await this.ProcessAsync(command);
        }

        /// <inheritdoc />
        public async Task ProcessDeleteAsync(IDeleteCommand command)
        {
            await this.ProcessAsync(command);
        }

        /// <inheritdoc />
        public async Task ProcessExportAsync(IExportCommand command)
        {
            await this.ProcessAsync(command);
        }

        private async Task ProcessAsync(IPrivacyCommand command)
        {
            this.logger.Information(nameof(PcfDataAgent), $"{nameof(PcfDataAgent)}.{nameof(ProcessAsync)}, SubjectType={command.Subject.GetType().Name};CommandId={command.CommandId}");

            // Report CheckpointAsync as outgoing event
            OutgoingApiEventWrapper outgoingApiEvent = OutgoingApiEventWrapper.CreateBasicOutgoingEvent(
                partnerId: "PCF",
                operationName: nameof(this.ProcessAsync),
                operationVersion: string.Empty,
                targetUri: this.config.CommandFeedHostName,
                requestMethod: HttpMethod.Post,
                dependencyType: "PCF");

            Dictionary<string, string> extraData = outgoingApiEvent.ExtraData;
            extraData["AgentId"] = this.agentId.ToString();
            extraData["AssetGroupId"] = command.AssetGroupId;
            extraData["CommandType"] = command.GetType().Name;
            extraData["CommandId"] = command.CommandId;
            extraData["RequestBatchId"] = command.RequestBatchId;
            extraData["CorrelationVector"] = command.CorrelationVector;
            extraData["CloudInstance"] = command.CloudInstance;
            extraData["ProcessorApplicable"] = command.ProcessorApplicable.ToString();
            extraData["ControllerApplicable"] = command.ControllerApplicable.ToString();
            extraData["LeaseReceipt"] = command.LeaseReceipt;
            extraData["IssuedTimestamp"] = command.Timestamp.ToString("s");
            extraData["SubjectType"] = command.Subject.GetType().Name;
            extraData["EndpointName"] = this.config.CommandFeedHostName;

            try
            {
                outgoingApiEvent.Start();
                await command.CheckpointAsync(CommandStatus.Complete, affectedRowCount: 0);
            }
            catch (Exception e)
            {
                outgoingApiEvent.ProtocolStatusCode = "500";
                outgoingApiEvent.Success = false;
                outgoingApiEvent.ErrorMessage = e.ToString();
                throw;
            }
            finally
            {
                outgoingApiEvent?.Finish();
            }
        }
    }
}