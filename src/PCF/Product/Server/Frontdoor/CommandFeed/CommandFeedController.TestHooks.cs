namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Web.Http;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    // For security, don't even compile this part of the partial class when building for production.
#if INCLUDE_TEST_HOOKS

    // Provides test-hook additions to the command feed controller.
    public partial class CommandFeedController
    {
        /// <summary>
        /// A test-only method to insert a batch of commands on behalf of an agent. This method is only in non-production
        /// envrionements.
        /// </summary>
        [HttpPost]
        [Route("{agentId}/commands")]
        [IncomingRequestActionFilter("API", "InsertCommands", "1.0")]
        [DisallowPort80ActionFilter]
        public IHttpActionResult InsertCommands(string agentId)
        {
            ProductionSafetyHelper.EnsureNotInProduction();

            if (!AgentId.TryParse(agentId, out AgentId id))
            {
                return this.BadRequest("Invalid agent ID.");
            }

            return new InsertCommandsActionResult(
                id, 
                this.Request, 
                CommandFeedGlobals.CommandQueueFactory, 
                CommandFeedGlobals.DataAgentMapFactory.GetDataAgentMap(),
                CommandFeedGlobals.ServiceAuthorizer,
                CommandFeedGlobals.CommandValidationService,
                CommandFeedGlobals.CommandHistory,
                CommandFeedGlobals.AzureQueueStorageCommandContext);
        }
    }

#endif
}
