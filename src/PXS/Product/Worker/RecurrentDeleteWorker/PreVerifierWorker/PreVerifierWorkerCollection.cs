namespace Microsoft.Membership.MemberServices.Privacy.RecurrentDeleteWorker.PreVerifierWorker
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Worker;
    using Microsoft.Membership.MemberServices.Privacy.RecurrentDeleteWorker.Common;

    /// <summary>
    /// PreVerifierWorkersCollection
    /// </summary>
    public class PreVerifierWorkerCollection : IWorker
    {
        private readonly List<IWorker> workers;

        /// <summary>
        /// A collection of Schedule Workers
        /// </summary>
        /// <param name="workerFactory"></param>
        /// <param name="configuration"></param>
        public PreVerifierWorkerCollection(
            IRecurrentDeleteWorkerFactory workerFactory,
            IPrivacyConfigurationManager configuration)
        {
            this.workers = new List<IWorker>();
            var config = configuration?.RecurringDeleteWorkerConfiguration ?? throw new ArgumentNullException(nameof(configuration.RecurringDeleteWorkerConfiguration));
            var preVerifierWorkerEnabled = config.EnablePreVerifierWorker;

            if (preVerifierWorkerEnabled)
            {
                foreach (var queueConfig in config.RecurringDeleteQueueProccessorConfig.AzureQueueStorageConfigurations)
                {
                    workers.Add(workerFactory.CreatePreVerifierWorker(queueConfig));
                }
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
