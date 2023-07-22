namespace Microsoft.PrivacyServices.AzureFunctions.Core
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common.IfxMetric;
    using Microsoft.Azure.WebJobs;
    using Microsoft.PrivacyServices.AzureFunctions.Common;
    using Microsoft.PrivacyServices.AzureFunctions.Common.Configuration;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// Container for the Azure Functions that manage variant requests
    /// </summary>
    public class VariantRequestFunction
    {
        private const string ComponentName = nameof(VariantRequestFunction);
        private readonly IMetricContainer metricContainer;
        private readonly IFunctionConfiguration configuration;
        private readonly IAuthenticationProvider authenticationProvider;
        private readonly IVariantRequestProcessorFactory processorFactory;
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="VariantRequestFunction"/> class.
        /// </summary>
        /// <param name="configuration">Function configuration</param>
        /// <param name="metricContainer">Metric configuration</param>
        /// <param name="authenticationProvider">Authentication provider</param>
        /// <param name="processorFactory">Factory to create variant processors.</param>
        /// <param name="logger">The logger.</param>
        public VariantRequestFunction(
            IFunctionConfiguration configuration,
            IMetricContainer metricContainer,
            IAuthenticationProvider authenticationProvider,
            IVariantRequestProcessorFactory processorFactory,
            ILogger logger)
        {
            this.configuration = configuration ?? throw new ArgumentException(nameof(configuration));
            this.metricContainer = metricContainer ?? throw new ArgumentException(nameof(metricContainer));
            this.authenticationProvider = authenticationProvider ?? throw new ArgumentException(nameof(authenticationProvider));
            this.processorFactory = processorFactory ?? throw new ArgumentException(nameof(processorFactory));
            this.logger = logger ?? throw new ArgumentException(nameof(logger));
        }

        /// <summary>
        /// Creates VariantRequest Work item
        /// </summary>
        /// <param name="variantRequestMessage">A message from the queue in JSON string format.</param>
        /// <param name="processedQueue">The queue of processed items.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName("CreateVariantRequestWorkItem")]
        public async Task CreateVariantRequestWorkItemAsync(
            [QueueTrigger("%VariantRequestQueueName%"), StorageAccount("VariantRequestStorage")] string variantRequestMessage,
            [Queue("%VariantRequestQueueName%-processed"), StorageAccount("AzureWebJobsStorage")] ICollector<string> processedQueue)
        {
            IApiEvent incomingApi = new IncomingApiEventWrapper(this.metricContainer, "Variant_Request_Create", this.logger, "Variant");
            try
            {
                // Start API event
                incomingApi.Start();
                this.logger.Information(ComponentName, $"{nameof(this.CreateVariantRequestWorkItemAsync)}: Received variant request = {variantRequestMessage}");

                var processor = this.processorFactory.Create(
                    this.configuration,
                    this.authenticationProvider,
                    this.metricContainer,
                    this.logger);

                await processor.CreateVariantRequestWorkItemAsync(variantRequestMessage, processedQueue).ConfigureAwait(false);

                incomingApi.Success = true;
            }
            catch (Exception ex)
            {
                incomingApi.Success = false;

                // rethrow so that the item is not removed from the queue
                this.logger.Error(ComponentName, $"{nameof(this.CreateVariantRequestWorkItemAsync)}: Unhandled Exception: {ex}");
                throw;
            }
            finally
            {
                incomingApi.Finish();
            }
        }

        /// <summary>
        /// Creates VariantRequest alert for Poison Queue
        /// </summary>
        /// <param name="variantRequestMessage">A message from the queue in JSON string format.</param>
        /// <param name="unprocessedQueue">messages from poison queue move to this queue for reprocessing</param>
        [FunctionName("AlertForPoisonQueue")]
        public void AlertForPoisonQueueAsync(
            [QueueTrigger("%VariantRequestQueueName%-poison"), StorageAccount("VariantRequestStorage")] string variantRequestMessage,
            [Queue("%VariantRequestQueueName%-unprocessed"), StorageAccount("AzureWebJobsStorage")] ICollector<string> unprocessedQueue)
        {
            IApiEvent incomingApi = new IncomingApiEventWrapper(this.metricContainer, "Variant_Request_Alert_Poison_Queue", this.logger, "Variant");
            try
            {
                // Start API event
                incomingApi.Start();
                this.logger.Information(ComponentName, $"{nameof(this.AlertForPoisonQueueAsync)}: Variant Request found in poison queue: = {variantRequestMessage}");
                var processor = this.processorFactory.Create(
                    this.configuration,
                    this.authenticationProvider,
                    this.metricContainer,
                    this.logger);

                processor.MoveVariantRequestToUnprocessedQueueAsync("%VariantRequestQueueName%-poison", variantRequestMessage, unprocessedQueue);
                incomingApi.Success = true;
            }
            catch (Exception ex)
            {
                incomingApi.Success = false;
                this.logger.Error(ComponentName, $"{nameof(this.AlertForPoisonQueueAsync)}: Unhandled Exception = {ex}");
                throw;
            }
            finally
            {
                incomingApi.Finish();
            }
        }

        /// <summary>
        /// Updates variant requests in PDMS and ADO that have been approved
        /// </summary>
        /// <param name="myTimer">Timer </param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName("UpdateApprovedVariantRequest")]
        public async Task UpdateApprovedVariantRequestAsync(
            [TimerTrigger("%VariantUpdateTiming%")] TimerInfo myTimer)
        {
            IApiEvent incomingApi = new IncomingApiEventWrapper(this.metricContainer, "Variant_Request_Approve", this.logger, "Variant");
            try
            {
                // Start API event
                incomingApi.Start();
                this.logger.Information(ComponentName, $"{nameof(this.UpdateApprovedVariantRequestAsync)}: Scanning for approved workitems");

                var processor = this.processorFactory.Create(
                    this.configuration,
                    this.authenticationProvider,
                    this.metricContainer,
                    this.logger);

                await processor.UpdateApprovedVariantRequestWorkItemsAsync().ConfigureAwait(false);

                incomingApi.Success = true;
            }
            catch (Exception ex)
            {
                incomingApi.Success = false;
                this.logger.Error(ComponentName, $"{nameof(this.UpdateApprovedVariantRequestAsync)}: Unhandled Exception: {ex}");
                throw;
            }
            finally
            {
                incomingApi.Finish();
            }
        }

        /// <summary>
        /// Removes variant requests in PDMS and ADO that have been rejected
        /// </summary>
        /// <param name="myTimer">Timer </param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName("RemoveRejectedVariantRequest")]
        public async Task RemoveRejectedVariantRequestAsync(
            [TimerTrigger("%VariantRemoveTiming%")] TimerInfo myTimer)
        {
            IApiEvent incomingApi = new IncomingApiEventWrapper(this.metricContainer, "Variant_Request_Reject", this.logger, "Variant");
            try
            {
                // Start API event
                incomingApi.Start();
                this.logger.Information(ComponentName, $"{nameof(this.RemoveRejectedVariantRequestAsync)}: Scanning rejected workitems");

                var processor = this.processorFactory.Create(
                    this.configuration,
                    this.authenticationProvider,
                    this.metricContainer,
                    this.logger);

                await processor.RemoveRejectedVariantRequestWorkItemsAsync().ConfigureAwait(false);

                incomingApi.Success = true;
            }
            catch (Exception ex)
            {
                incomingApi.Success = false;
                this.logger.Error(ComponentName, $"{nameof(this.UpdateApprovedVariantRequestAsync)}: Unhandled Exception: {ex}");
                throw;
            }
            finally
            {
                incomingApi.Finish();
            }
        }
    }
}
