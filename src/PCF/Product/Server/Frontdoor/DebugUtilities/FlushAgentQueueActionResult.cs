namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    /// <summary>
    /// ActionResult for Flushing Agent Queues
    /// </summary>
    internal class FlushAgentQueueActionResult : BaseHttpActionResult
    {
        private readonly HttpRequestMessage request;
        private readonly IAuthorizer authorizer;
        private readonly AuthenticationScope authenticationScope;
        private readonly string agentIdValue;
        private readonly string flushDateValue;
        private readonly IAzureWorkItemQueuePublisher<AgentQueueFlushWorkItem> publisher;

        public FlushAgentQueueActionResult(
            HttpRequestMessage requestMessage,
            string agentIdValue,
            string flushDateValue,
            IAuthorizer authorizer,
            AuthenticationScope authenticationScope,
            IAzureWorkItemQueuePublisher<AgentQueueFlushWorkItem> publisher)
        {
            this.request = requestMessage;
            this.agentIdValue = agentIdValue;
            this.flushDateValue = flushDateValue;
            this.authorizer = authorizer;
            this.authenticationScope = authenticationScope;
            this.publisher = publisher;
        }

        protected override async Task<HttpResponseMessage> ExecuteInnerAsync(CancellationToken cancellationToken)
        {
            await this.authorizer.CheckAuthorizedAsync(this.request, this.authenticationScope);

            if (string.IsNullOrEmpty(this.agentIdValue))
            {   
                return new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("AgentId is null") };
            }

            AgentId agentId = null;
            if (!AgentId.TryParse(this.agentIdValue, out agentId))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("Malformed agent ID.") };
            }

            if (!CommandFeedGlobals.DataAgentMapFactory.GetDataAgentMap().TryGetAgent(agentId, out _))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("AgentId doesn't exist in PCF") };
            }

            DateTimeOffset dateFlush = DateTimeOffset.UtcNow;
            if (!string.IsNullOrEmpty(this.flushDateValue) && !DateTimeOffset.TryParseExact(this.flushDateValue, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out dateFlush))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("Malformed FlushDate.") };
            }

            if (dateFlush > DateTimeOffset.UtcNow)
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("FlushDate can't be greater than current date/time") };
            }

            // Create the command replay work item and publish to queue
            var workItem = new AgentQueueFlushWorkItem
            {
                AgentId = agentId,
                FlushDate = dateFlush
            };

            await this.publisher.PublishAsync(workItem);

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}