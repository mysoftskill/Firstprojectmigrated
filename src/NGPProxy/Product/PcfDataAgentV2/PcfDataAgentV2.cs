// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.NgpProxy.PcfDataAgentV2
{
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts;
    using Microsoft.PrivacyServices.Common.Azure;
    using Newtonsoft.Json;

    public class PcfDataAgentV2 : IPrivacyDataAgentV2
    {
        private readonly ILogger logger;

        public PcfDataAgentV2(ILogger logger)
        {
            this.logger = logger;
        }

        /// <inheritdoc />
        public async Task ProcessWorkitemAsync(ICommandFeedClient commandFeedClient, Workitem workitem, CancellationToken cancellationToken)
        {
            var commandPage = JsonConvert.DeserializeObject<CommandPage>(workitem.CommandPage);

            this.logger.Information(nameof(PcfDataAgentV2), $"{nameof(ProcessWorkitemAsync)}, Workitem: {workitem.WorkitemId}, OperationType: {commandPage.OperationType}, CommandTypeId: {commandPage.CommandTypeId}, Number of Commands: {commandPage.Commands?.Count}");

            // Complete the workitem
            var updateWorkitemRequest = new UpdateWorkitemRequest { WorkitemStatus = WorkitemStatus.Completed };
            await commandFeedClient.UpdateWorkitemAsync(workitem.WorkitemId, updateWorkitemRequest, cancellationToken);
        }
    }
}