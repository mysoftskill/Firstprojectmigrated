namespace Microsoft.PrivacyServices.AzureFunctions.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using global::Azure.Messaging.EventHubs;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.ComplianceServices.Common.IfxMetric;
    using Microsoft.Azure.WebJobs;
    using Microsoft.ComplianceServices.AnaheimIdLib.Schema;
    using Microsoft.PrivacyServices.AnaheimId;
    using Microsoft.PrivacyServices.AnaheimId.AidFunctions;
    using Microsoft.PrivacyServices.Common.Azure;
    using Newtonsoft.Json;

    /// <summary>
    /// AnaheimId Azure Function Container.
    /// </summary>
    public class AnaheimIdFunction
    {
        private const string ComponentName = nameof(AnaheimIdFunction);
        private readonly IAidFunctionsFactory aidFunctionsFactory;
        private readonly IAppConfiguration appConfig;
        private readonly IMetricContainer metricContainer;
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnaheimIdFunction"/> class.
        /// </summary>
        /// <param name="aidFunctionsFactory">Functions implementation.</param>
        /// <param name="appConfig">App config.</param>
        /// <param name="metricContainer">Metric container.</param>
        /// <param name="logger">Implementation of ILogger.</param>

        public AnaheimIdFunction(
            IAidFunctionsFactory aidFunctionsFactory,
            IAppConfiguration appConfig,
            IMetricContainer metricContainer,
            ILogger logger)
        {
            this.aidFunctionsFactory = aidFunctionsFactory ?? throw new ArgumentNullException(nameof(aidFunctionsFactory));
            this.appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
            this.metricContainer = metricContainer ?? throw new ArgumentNullException(nameof(metricContainer));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// This function is triggered when a new blob is created in the missing signals container.
        /// The blob name will be used to read a blob filename and content stream.
        /// </summary>
        [FunctionName("RunAidBlobStorage")]
        public void RunAidBlobStorage(
            [BlobTrigger("missingsignalscontainer/{name}", Connection = "PAF_AID_BLOBSTORAGE_HOST")] Stream inBlob, string name)
        {
            // Function was called without a blob stream
            if (inBlob == null)
            {
                throw new ArgumentNullException(nameof(inBlob));
            }

            IApiEvent incomingApi = new IncomingApiEventWrapper(this.metricContainer, "AID_BlobStorage", this.logger, "Anaheim");

            try
            {
                // Start API event
                incomingApi.Start();

                // Check if the file is a test output file
                if (name.Contains(".test", StringComparison.InvariantCulture)) 
                {
                    this.logger.Information(ComponentName, $"Test File Detected={name}");
                }

                // Check that the name does not match an expected filename (trigger files must contain "MissingSignal")
                else if (!name.Contains("MissingSignal", StringComparison.InvariantCulture))
                {
                    this.logger.Information(ComponentName, $"Other File Detected={name}");

                }

                // The file is a missing signal file that we will trigger on
                else
                {
                    this.logger.Information(ComponentName, $"New Missing Signals File Detected={name}");
                    
                    // Get the blob storage processing function and run it on the blob stream
                    var aidFunction = this.aidFunctionsFactory.GetAidBlobStorageFunc();
                    aidFunction.Run(inBlob, name, this.logger);
                }
                
                // Stop API Event
                incomingApi.Success = true;
            }
            catch (Exception exception)
            {
                incomingApi.Success = false;
                this.logger.Error(ComponentName, $"{nameof(this.RunAidBlobStorage)}: Unhandled Exception. {exception}");
                throw;
            }
            finally
            {
                incomingApi.Finish();
            }
        }

        /// <summary>
        /// EventHub Trigger.
        /// </summary>
        /// <param name="events">Events.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName("RunAidEventHubAsync")]
        public async Task RunAidEventHubAsync(
            [EventHubTrigger("%PAF_AID_EVENTHUB_NAME%", Connection = "PAF_AID_EVENTHUB_HOST")] EventData[] events)
        {
            var aidFunction = this.aidFunctionsFactory.GetAidEventHubFunc();
            var exceptions = new List<Exception>();

            this.logger.Information(ComponentName, $"number of messages received={events.Length}");

            foreach (EventData eventData in events)
            {
                IApiEvent incomingApi = new IncomingApiEventWrapper(this.metricContainer, "AID_EventHub", this.logger, "Anaheim");
                try
                {
                    incomingApi.Start();

                    string messageBody = Encoding.UTF8.GetString(eventData.EventBody.ToArray());
                    var anaheimIdRequest = AidHelpers.ParseAnaheimIdRequest(messageBody);
                    bool isEventsToQueueEnabled = await appConfig.IsFeatureFlagEnabledAsync(FeatureNames.PAF.PAF_AID_EventsToQueue_Enabled).ConfigureAwait(false);

                    this.logger.Information(ComponentName, $"{nameof(this.RunAidEventHubAsync)}: Received request with RequestId={anaheimIdRequest.DeleteDeviceIdRequest.RequestId}, PublishedToEventHubTime={eventData.EnqueuedTime}, PAF_AID_EventsToQueue_Enabled={isEventsToQueueEnabled}");

                    if (isEventsToQueueEnabled)
                    {
                        await aidFunction.RunAsync(anaheimIdRequest, this.logger).ConfigureAwait(false);
                    }

                    incomingApi.Success = true;
                }
                catch (Exception e)
                {
                    // We need to keep processing the rest of the batch - capture this exception and continue.
                    // Also, consider capturing details of the message that failed processing so it can be processed again later.
                    exceptions.Add(e);
                    incomingApi.Success = false;
                }
                finally
                {
                    incomingApi.Finish();
                }
            }

            // Once processing of the batch is complete, if any messages in the batch failed processing throw an exception so that there is a record of the failure.
            if (exceptions.Count > 1)
            {
                this.logger.Error(ComponentName, $"AID Unhandled Exception. {exceptions}");
                throw new AggregateException(exceptions);
            }

            if (exceptions.Count == 1)
            {
                this.logger.Error(ComponentName, $"{nameof(this.RunAidEventHubAsync)}: Unhandled Exception. {exceptions}");
                throw exceptions.Single();
            }
        }

        /// <summary>
        /// Anaheim Id mock service.
        /// Receive signals from eventhub and create synth anaheim ids.
        /// </summary>
        /// <param name="events">Events.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName("RunMockAidEventHubAsync")]
        public async Task RunMockAidEventHubAsync(
            [EventHubTrigger("%PXS_DEVICE_DELETE_EVENTHUB_NAME%", Connection = "PXS_DEVICE_DELETE_EVENTHUB_HOST", ConsumerGroup = "%PXS_DEVICE_DELETE_EVENTHUB_CONSUMER_GROUP%")] EventData[] events)
        {
            var aidFunction = this.aidFunctionsFactory.GetAidMockFunc();
            var exceptions = new List<Exception>();

            this.logger.Information(ComponentName, $"MOCK AID EventHub Azure Function: number of messages received={events.Length}");

            bool runMockAidEventHubAsyncEnabled = await this.appConfig.IsFeatureFlagEnabledAsync(FeatureNames.PAF.PAF_AID_RunMockAidEventHubAsync_Enabled).ConfigureAwait(false);

            if (!runMockAidEventHubAsyncEnabled)
            {
                // no-op. note: this will delete items from eventhub
                return;
            }

            List<DeleteDeviceIdRequest> deleteDeviceIdRequests = new List<DeleteDeviceIdRequest>();

            IApiEvent incomingApi = new IncomingApiEventWrapper(this.metricContainer, "AID_MockEventHub", this.logger, "Anaheim");
            try
            {
                incomingApi.Start();

                foreach (EventData eventData in events)
                {
                    string messageBody = Encoding.UTF8.GetString(eventData.EventBody.ToArray());

                    var deleteRequest = JsonConvert.DeserializeObject<DeleteDeviceIdRequest>(messageBody);
                    deleteRequest.TestSignal = true;

                    this.logger.Information(ComponentName, $"{nameof(this.RunMockAidEventHubAsync)}: RequestId={deleteRequest.RequestId}, EnqueuedTime={eventData.EnqueuedTime}");
                    deleteDeviceIdRequests.Add(deleteRequest);
                }

                this.logger.Information(ComponentName, $"NumOfRequests: {deleteDeviceIdRequests.Count}");
                this.logger.Information(ComponentName, $"RequestIds: {string.Join(", ", deleteDeviceIdRequests.Select(x => x.RequestId))}");

                await aidFunction.RunAsync(deleteDeviceIdRequests, this.logger).ConfigureAwait(false);

                incomingApi.Success = true;
            }
            catch (Exception e)
            {
                // We need to keep processing the rest of the batch - capture this exception and continue.
                // Also, consider capturing details of the message that failed processing so it can be processed again later.
                exceptions.Add(e);
                incomingApi.Success = false;
            }
            finally
            {
                incomingApi.Finish();
            }

            // Once processing of the batch is complete, if any messages in the batch failed processing throw an exception so that there is a record of the failure.
            if (exceptions.Count > 1)
            {
                this.logger.Error(ComponentName, $"{nameof(this.RunAidEventHubAsync)}: AID Unhandled Exception. {exceptions}");
                throw new AggregateException(exceptions);
            }

            if (exceptions.Count == 1)
            {
                this.logger.Error(ComponentName, $"{nameof(this.RunAidEventHubAsync)}: Unhandled Exception. {exceptions}");
                throw exceptions.Single();
            }
        }

        /// <summary>
        /// Send AID telemetry.
        /// </summary>
        /// <param name="timer">Timer trigger every minute.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName("RunTelemetryTimer")]
        public Task RunTelemetryTimerAsync([TimerTrigger("0 */5 * * * *")] TimerInfo timer)
        {
            IApiEvent incomingApi = new IncomingApiEventWrapper(this.metricContainer, "AID_TelemetryTimer", this.logger, "Anaheim");
            Task task = null;

            try
            {
                incomingApi.Start();
                
                if (timer.IsPastDue)
                {
                    this.logger.Information(ComponentName, $"{nameof(this.RunTelemetryTimerAsync)}: Timer is running late!");
                }

                task = this.aidFunctionsFactory.GetAidTelemetryFunc().RunAsync(this.logger);
                incomingApi.Success = true;
            }
            catch (Exception exception)
            {
                this.logger.Error(ComponentName, $"{nameof(this.RunTelemetryTimerAsync)}: Unhandled Exception. {exception}");
                incomingApi.Success = false;
                throw;
            }
            finally
            {
                incomingApi.Finish();
            }

            return task;
        }
    }
}
