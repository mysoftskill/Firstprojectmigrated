// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.VortexDeviceDeleteWorker.QueueProcessor
{
    using System;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Privacy.Core.Vortex;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     VortexDeviceDeleteQueueProcessorFactory
    /// </summary>
    public class VortexDeviceDeleteQueueProcessorFactory : IVortexDeviceDeleteQueueProcessorFactory
    {
        /// <inheritdoc />
        public VortexDeviceDeleteQueueProcessor Create(
            ILogger logger,
            IPrivacyConfigurationManager configuration,
            IVortexDeviceDeleteQueueManager queueManager,
            IVortexEventService vortexEventService,
            ICounterFactory counterFactory,
            IAppConfiguration appConfiguration)
        {
            if (configuration?.VortexDeviceDeleteWorkerConfiguration?.QueueProccessorConfig == null)
            {
                throw new ArgumentNullException(nameof(configuration.VortexDeviceDeleteWorkerConfiguration.QueueProccessorConfig));
            }

            logger.Information(nameof(VortexDeviceDeleteQueueProcessorFactory), "Creating Processor.");

            return new VortexDeviceDeleteQueueProcessor(
                logger,
                configuration.VortexDeviceDeleteWorkerConfiguration.QueueProccessorConfig,
                queueManager,
                vortexEventService,
                counterFactory,
                appConfiguration);
        }
    }
}
