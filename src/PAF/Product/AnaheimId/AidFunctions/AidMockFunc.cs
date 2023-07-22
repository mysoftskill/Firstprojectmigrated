namespace Microsoft.PrivacyServices.AnaheimId.AidFunctions
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using global::Azure.Core;
    using global::Azure.Messaging.EventHubs;
    using global::Azure.Messaging.EventHubs.Producer;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.ComplianceServices.Common.IfxMetric;
    using Microsoft.ComplianceServices.AnaheimIdLib.Schema;
    using Microsoft.PrivacyServices.AnaheimId.Config;
    using Microsoft.PrivacyServices.Common.Azure;
    using Newtonsoft.Json;

    /// <summary>
    /// Anaheim id mock function
    /// </summary>
    public class AidMockFunc
    {
        private const string ComponentName = nameof(AidMockFunc);
        private const int NumOfAnaheimIds = 3;
        private readonly IAidConfig config;
        private readonly IMetricContainer metricContainer;
        private readonly TokenCredential tokenCredential;
        private readonly EventHubProducerClient eventHubProducerClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="AidMockFunc"/> class.
        /// </summary>
        /// <param name="config">AiID Config.</param>
        /// <param name="metricContainer">Container to hold Metrics.</param>
        /// <param name="tokenCredential">Token credentials.</param>
        public AidMockFunc(IAidConfig config, IMetricContainer metricContainer, TokenCredential tokenCredential)
        {
            this.config = config ?? throw new ArgumentException(nameof(config));
            this.metricContainer = metricContainer ?? throw new ArgumentException(nameof(metricContainer));
            this.tokenCredential = tokenCredential ?? throw new ArgumentException(nameof(tokenCredential));
            var fullyQualifiedNamespace = $"{this.config.EventHubNamespace}.servicebus.windows.net";

            // Create a producer client that you can use to send events to an event hub
            this.eventHubProducerClient = new EventHubProducerClient(
                fullyQualifiedNamespace: fullyQualifiedNamespace,
                eventHubName: this.config.EventHubName,
                credential: tokenCredential);
        }

        /// <summary>
        /// Anaheim id mock process: create a new anaheim ids and send them to anaheim id eventhub.
        /// </summary>
        /// <param name="deleteDeviceIdRequests">Delete request.</param>
        /// <param name="logger">Logger.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task RunAsync(IEnumerable<DeleteDeviceIdRequest> deleteDeviceIdRequests, ILogger logger)
        {
            IApiEvent outgoingApi = new OutgoingApiEventWrapper(this.metricContainer, "AID_MockFunc", "AID_EventHubMock", logger, "Anaheim");
            try
            {
                // Start API Event
                outgoingApi.Start();

                // Create a batch of events
                EventDataBatch eventBatch = await this.eventHubProducerClient.CreateBatchAsync().ConfigureAwait(false);

                foreach (var request in deleteDeviceIdRequests)
                {
                    AnaheimIdRequest anaheimIdRequest = this.CreateAnaheimIdRequest(request);
                    string messageText = JsonConvert.SerializeObject(anaheimIdRequest);

                    if (!eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes(messageText))))
                    {
                        await this.eventHubProducerClient.SendAsync(eventBatch).ConfigureAwait(false);
                        eventBatch = await this.eventHubProducerClient.CreateBatchAsync().ConfigureAwait(false);
                    }
                }

                if (eventBatch.SizeInBytes > 0)
                {
                    await this.eventHubProducerClient.SendAsync(eventBatch).ConfigureAwait(false);
                }

                // If no exceptions occur the mock api is successful
                outgoingApi.Success = true;
            }
            catch (Exception ex)
            {
                logger.Error(ComponentName, $"Unhandled Exception. {ex.Message}");
                outgoingApi.Success = false;
                throw;
            }
            finally
            {
                outgoingApi.Finish();
            }
        }

        private AnaheimIdRequest CreateAnaheimIdRequest(DeleteDeviceIdRequest deleteRequest)
        {
            AnaheimIdRequest request = new AnaheimIdRequest() { DeleteDeviceIdRequest = deleteRequest };

            List<long> anaheimIds = new List<long>();

            for (int i = 0; i < NumOfAnaheimIds; i++)
            {
                anaheimIds.Add(RandomHelper.Next(0, 10));
            }

            request.AnaheimIds = anaheimIds;

            return request;
        }
    }
}
