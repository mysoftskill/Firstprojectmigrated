// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.VortexDeviceDeleteWorker.QueueProcessor
{
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Privacy.Core.Vortex;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     IVortexDeviceDeleteQueueProcessorFactory
    /// </summary>
    public interface IVortexDeviceDeleteQueueProcessorFactory
    {
        /// <summary>
        ///     Creates a new instance of <see cref="VortexDeviceDeleteQueueProcessor" />
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="configuration">The queue processor configuration</param>
        /// <param name="queueManager">The queue</param>
        /// <param name="vortexEventService">The vortex service</param>
        /// <param name="counterFactory">The counter factory for making perf counters</param>
        /// <param name="appConfiguration">Azure App Configuration instance</param>
        /// <returns></returns>
        VortexDeviceDeleteQueueProcessor Create(
            ILogger logger,
            IPrivacyConfigurationManager configuration,
            IVortexDeviceDeleteQueueManager queueManager,
            IVortexEventService vortexEventService,
            ICounterFactory counterFactory,
            IAppConfiguration appConfiguration);
    }
}
