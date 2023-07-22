namespace Microsoft.Membership.MemberServices.Privacy.VortexDeviceDeleteWorker.QueueProcessor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Worker;

    /// <summary>
    /// AnaheimId processors.
    /// </summary>
    public class AnaheimIdQueueWorkersCollection : IWorker
    {
        /// <summary>
        /// Subworkers to process anaheim id requests.
        /// </summary>
        private readonly List<IWorker> workers;

        public AnaheimIdQueueWorkersCollection(
            IAnaheimIdQueueWorkerFactory anaheimIdQueueWorkerFactory,
            IPrivacyConfigurationManager configuration)
        {
            this.workers = new List<IWorker>();
            var config = configuration.VortexDeviceDeleteWorkerConfiguration;
            foreach (var queueConfig in config.QueueProccessorConfig.AzureQueueStorageConfigurations)
            {
                this.workers.Add(anaheimIdQueueWorkerFactory.Create(queueConfig));
            }
        }

        /// <inheritdoc/>>
        public void Start() => this.workers.ForEach(p => p.Start());

        /// <inheritdoc/>>
        public void Start(TimeSpan delay) => this.workers.ForEach(p => p.Start(delay));

        /// <inheritdoc/>>
        public Task StopAsync() => Task.WhenAll(this.workers.Select(p => p.StopAsync()));
    }
}
