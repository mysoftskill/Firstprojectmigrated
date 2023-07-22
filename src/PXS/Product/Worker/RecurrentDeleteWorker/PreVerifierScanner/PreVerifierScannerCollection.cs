namespace Microsoft.Membership.MemberServices.Privacy.RecurrentDeleteWorker.PreVerifierScanner
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Worker;
    using Microsoft.Membership.MemberServices.Privacy.RecurrentDeleteWorker.Common;

    /// <summary>
    /// PreVerifierScannerCollection
    /// </summary>
    public class PreVerifierScannerCollection : IWorker
    {
        private readonly List<IWorker> workers;

        /// <summary>
        /// A collection of Schedule Scanners
        /// </summary>
        /// <param name="workerFactory"></param>
        /// <param name="configuration"></param>
        public PreVerifierScannerCollection(
            IRecurrentDeleteWorkerFactory workerFactory,
            IPrivacyConfigurationManager configuration)
        {
            this.workers = new List<IWorker>();
            var config = configuration?.RecurringDeleteWorkerConfiguration ?? throw new ArgumentNullException(nameof(configuration.RecurringDeleteWorkerConfiguration));
            var preVerifierScannerEnabled = config.EnablePreVerifierScanner;
            
            if (preVerifierScannerEnabled)
            {
                foreach (var queueConfig in config.RecurringDeleteQueueProccessorConfig.AzureQueueStorageConfigurations)
                {
                    this.workers.Add(workerFactory.CreatePreVerifierScanner(queueConfig));
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
