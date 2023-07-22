namespace Microsoft.Membership.MemberServices.Privacy.RecurrentDeleteWorker.ScheduleScanner
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Worker;
    using Microsoft.Membership.MemberServices.Privacy.RecurrentDeleteWorker.Common;

    /// <summary>
    /// RecurrentDeleteScheduleScannersCollection
    /// </summary>
    public class RecurrentDeleteScheduleScannersCollection : IWorker
    {
        private readonly List<IWorker> workers;

        /// <summary>
        /// A collection of Schedule Scanners
        /// </summary>
        /// <param name="workerFactory"></param>
        /// <param name="configuration"></param>
        public RecurrentDeleteScheduleScannersCollection(
            IRecurrentDeleteWorkerFactory workerFactory,
            IPrivacyConfigurationManager configuration)
        {
            this.workers = new List<IWorker>();
            var config = configuration.RecurringDeleteWorkerConfiguration;
            foreach (var queueConfig in config.RecurringDeleteQueueProccessorConfig.AzureQueueStorageConfigurations)
            {
                this.workers.Add(workerFactory.CreateRecurrentDeleteScheduleScanner(queueConfig));
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
