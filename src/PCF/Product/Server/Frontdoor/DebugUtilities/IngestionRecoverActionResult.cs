namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    /// <summary>
    /// Defines the <see cref="IngestionRecoveryActionResult" />
    /// </summary>
    internal class IngestionRecoveryActionResult : BaseHttpActionResult
    {
        /// <summary>
        /// Defines the authenticationScope
        /// </summary>
        private readonly AuthenticationScope authenticationScope;

        /// <summary>
        /// Defines the authorizer
        /// </summary>
        private readonly IAuthorizer authorizer;

        /// <summary>
        /// Defines the request
        /// </summary>
        private readonly HttpRequestMessage request;

        /// <summary>
        /// Defines the baselineAzureQueues
        /// </summary>
        private readonly IAzureWorkItemQueuePublisher<IngestionRecoveryWorkItem> IngestionRecoveryWorkItemPublisher;

        /// <summary>
        /// Start Date for the ingestion recovery
        /// </summary>
        private DateTime startDate;

        // End date of ingestion recovery
        private DateTime endDate;

        // Run ingestion only for export
        private bool exportOnly;

        // Run ingestion only for non export commands
        private bool nonExportOnly;

        /// <summary>
        /// Initializes a new instance of the <see cref="IngestionRecoveryActionResult"/> class.
        /// </summary>
        /// <param name="startDate">The start date for ingestion recovery<see cref="HttpRequestMessage"/></param>
        /// <param name="endDate">The end date for ingestion recovery<see cref="HttpRequestMessage"/></param>
        /// <param name="request">The request<see cref="HttpRequestMessage"/></param>
        /// <param name="authorizer">The authorizer<see cref="IAuthorizer"/></param>
        /// <param name="authenticationScope">The allowedCallers<see cref="AuthenticationScope"/></param>
        /// <param name="publisher">Publisher for publishing to ingestion recovery queue</param>
        /// <param name="exportOnly">limit ingestion recovery to exports only</param>
        /// <param name="nonExportOnly">limit ingestion recovery to non-exports only</param>
        public IngestionRecoveryActionResult(
            DateTime startDate,
            DateTime endDate,
            HttpRequestMessage request,
            IAuthorizer authorizer,
            AuthenticationScope authenticationScope,
            IAzureWorkItemQueuePublisher<IngestionRecoveryWorkItem> publisher,
            bool exportOnly,
            bool nonExportOnly)
        {
            this.startDate = startDate;
            this.endDate = endDate;
            this.authorizer = authorizer;
            this.authenticationScope = authenticationScope;
            this.request = request;
            this.IngestionRecoveryWorkItemPublisher = publisher;
            this.exportOnly = exportOnly;
            this.nonExportOnly = nonExportOnly;
        }

        /// <summary>
        /// The ExecuteInnerAsync
        /// </summary>
        /// <param name="cancellationToken">The cancellationToken<see cref="CancellationToken"/></param>
        /// <returns>The <see cref="Task{HttpResponseMessage}"/></returns>
        protected override async Task<HttpResponseMessage> ExecuteInnerAsync(CancellationToken cancellationToken)
        {
            await this.authorizer.CheckAuthorizedAsync(this.request, this.authenticationScope);
            IncomingEvent.Current?.SetProperty("IngestionRecoveryStartDate", this.startDate.ToString());
            IncomingEvent.Current?.SetProperty("IngestionRecoveryEndDate", this.endDate.ToString());

            var splitWindowInHours = FlightingUtilities.GetConfigValue<int>(ConfigNames.PCF.IngestionRecoveryWorkItemSplitWindowInHours, 1); ;
            // Break into multiple work items for every hour.
            while (this.startDate < this.endDate)
            {
                await this.IngestionRecoveryWorkItemPublisher.PublishAsync(new IngestionRecoveryWorkItem()
                {
                    ContinuationToken = null,
                    NewestRecordCreationTime = this.endDate.Subtract(this.startDate) > TimeSpan.FromHours(splitWindowInHours) ? this.startDate.AddHours(splitWindowInHours) : this.endDate,
                    OldestRecordCreationTime = this.startDate,
                    exportOnly = this.exportOnly,
                    nonExportOnly = this.nonExportOnly,
                    isOnDemandRepairItem = true
                });
                this.startDate = this.startDate.AddHours(splitWindowInHours);
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
