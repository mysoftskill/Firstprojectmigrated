// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.VortexDeviceDeleteWorker.QueueProcessor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Worker;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.Membership.MemberServices.Privacy.Core.Vortex;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     VortexDeviceDeleteQueueProcessorCollection
    /// </summary>
    public class VortexDeviceDeleteQueueProcessorCollection : IWorker
    {
        /// <summary>
        ///     Sub processors that were created
        /// </summary>
        private readonly List<IWorker> processors;

        public VortexDeviceDeleteQueueProcessorCollection(
            ILogger logger,
            IVortexDeviceDeleteQueueProcessorFactory queueProcessorFactory,
            IPrivacyConfigurationManager configuration,
            IVortexDeviceDeleteQueueManager queueManager,
            IVortexEventService vortexEventService,
            ICounterFactory counterFactory,
            IAppConfiguration appConfiguration)
        {
            int processorCount = configuration.VortexDeviceDeleteWorkerConfiguration.QueueProccessorConfig.ProcessorCount;

            this.processors = new List<IWorker>(processorCount);
            for (int x = 0; x < processorCount; ++x)
            {
                this.processors.Add(queueProcessorFactory.Create(logger, configuration, queueManager, vortexEventService, counterFactory, appConfiguration));
            }
        }

        /// <inheritdoc />
        public void Start() => this.processors.ForEach(p => p.Start());

        /// <inheritdoc />
        public void Start(TimeSpan delay) => this.processors.ForEach(p => p.Start(delay));

        /// <inheritdoc />
        public Task StopAsync() => Task.WhenAll(this.processors.Select(p => p.StopAsync()));
    }
}
