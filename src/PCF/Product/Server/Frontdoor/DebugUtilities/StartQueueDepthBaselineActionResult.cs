namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Telemetry.BackgroundTasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.Telemetry.Repository;

    /// <summary>
    /// Defines the <see cref="StartQueueDepthBaselineActionResult" />
    /// </summary>
    internal class StartQueueDepthBaselineActionResult : BaseHttpActionResult
    {
        /// <summary>
        /// Defines the agentId
        /// </summary>
        private readonly AgentId agentId;

        /// <summary>
        /// Defines the authenticationScope
        /// </summary>
        private readonly AuthenticationScope authenticationScope;

        /// <summary>
        /// Defines the authorizer
        /// </summary>
        private readonly IAuthorizer authorizer;

        /// <summary>
        /// Defines the dataAgentInfo
        /// </summary>
        private readonly IDataAgentInfo dataAgentInfo;

        /// <summary>
        /// Defines the request
        /// </summary>
        private readonly HttpRequestMessage request;

        /// <summary>
        /// Defines the baselineAzureQueues
        /// </summary>
        private readonly Dictionary<SubjectType, IAzureWorkItemQueuePublisher<QueueDepthWorkItem>> baselineAzureQueues;

        /// <summary>
        /// Defines the telemetryRepository
        /// </summary>
        private ITelemetryRepository telemetryRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="StartQueueDepthBaselineActionResult"/> class.
        /// </summary>
        /// <param name="agentId">The agentId<see cref="AgentId"/></param>
        /// <param name="request">The request<see cref="HttpRequestMessage"/></param>
        /// <param name="dataAgentInfo">The dataAgentMap<see cref="IDataAgentInfo"/></param>
        /// <param name="authorizer">The authorizer<see cref="IAuthorizer"/></param>
        /// <param name="authenticationScope">The allowedCallers<see cref="AuthenticationScope"/></param>
        public StartQueueDepthBaselineActionResult(
            AgentId agentId,
            HttpRequestMessage request,
            IDataAgentInfo dataAgentInfo,
            IAuthorizer authorizer,
            AuthenticationScope authenticationScope)
        {
            this.agentId = agentId;
            this.authorizer = authorizer;
            this.authenticationScope = authenticationScope;
            this.dataAgentInfo = dataAgentInfo;
            this.request = request;

            this.telemetryRepository = new KustoTelemetryRepository();
            this.baselineAzureQueues = new Dictionary<SubjectType, IAzureWorkItemQueuePublisher<QueueDepthWorkItem>>();
            foreach (var subject in (SubjectType[])Enum.GetValues(typeof(SubjectType)))
            {
                this.baselineAzureQueues.Add(
                    subject,
                    new AzureWorkItemQueue<QueueDepthWorkItem>(
                        QueueDepthWorkItem.BaselineQueueLeaseTime,
                        $"{QueueDepthWorkItem.BaselineTasksQueueName}{subject}"));
            }
        }

        /// <summary>
        /// The ExecuteInnerAsync
        /// </summary>
        /// <param name="cancellationToken">The cancellationToken<see cref="CancellationToken"/></param>
        /// <returns>The <see cref="Task{HttpResponseMessage}"/></returns>
        protected override async Task<HttpResponseMessage> ExecuteInnerAsync(CancellationToken cancellationToken)
        {
            await this.authorizer.CheckAuthorizedAsync(this.request, this.authenticationScope);
            IncomingEvent.Current?.SetProperty("AgentId", this.agentId.Value);

            var workItems = QueueDepthTaskScheduler.CreateAgentWorkItems(this.agentId, this.dataAgentInfo, DateTimeOffset.UtcNow);

            // publish Kusto task
            await this.telemetryRepository.AddTasksAsync(workItems, TaskActionName.Create);

            foreach (var workItem in workItems)
            {
                // publish queue depth task
                await this.baselineAzureQueues[workItem.SubjectType].PublishAsync(workItem);
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
