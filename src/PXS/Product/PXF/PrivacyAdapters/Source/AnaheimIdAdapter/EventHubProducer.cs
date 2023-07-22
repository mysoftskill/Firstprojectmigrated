namespace Microsoft.Membership.MemberServices.PrivacyAdapters.AnaheimIdAdapter
{
    using System;
    using System.Text;
    using System.Threading.Tasks;

    using global::Azure.Core;
    using global::Azure.Messaging.EventHubs;
    using global::Azure.Messaging.EventHubs.Producer;

    public class EventHubProducer : IEventHubProducer
    {
        private readonly EventHubProducerClient producerClient;

        /// <summary>
        ///     Creates a new instance of <see cref="EventHubProducer" />
        /// </summary>
        /// <param name="fullyQualifiedNamespace"></param>
        /// <param name="eventHubName"></param>
        /// <param name="token"></param>
        public EventHubProducer(string fullyQualifiedNamespace, string eventHubName, TokenCredential token)
        {
            if (string.IsNullOrEmpty(fullyQualifiedNamespace) || string.IsNullOrEmpty(eventHubName) || token == null)
            {
                throw new ArgumentNullException($"{nameof(EventHubProducer)}: Invalid input for initializition.");
            }
            this.producerClient = new EventHubProducerClient(fullyQualifiedNamespace, eventHubName, token);
        }

        /// <inheritdoc/>>
        public async Task SendAsync(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentNullException($"{nameof(EventHubProducer)}: Message trying to send is empty or null.");
            }

            EventData eventData = new EventData(Encoding.UTF8.GetBytes(message));
            EventDataBatch eventDataBatch = await producerClient.CreateBatchAsync();
            if (!eventDataBatch.TryAdd(eventData))
            {
                throw new Exception($"{nameof(EventHubProducer)}: The event data could not be added into batch.");
            }
            await producerClient.SendAsync(eventDataBatch);
        }
    }
}
