// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.QueueProcessor
{
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.TableStorage;
    using Microsoft.Membership.MemberServices.Privacy.Core.AadAccountClose;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     IAadAccountCloseQueueProcessorFactory
    /// </summary>
    public interface IAadAccountCloseQueueProcessorFactory
    {
        /// <summary>
        ///     Creates a new instance of <see cref="AadAccountCloseQueueProcessor" />
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="queueManager">The queue manager.</param>
        /// <param name="aadAccountCloseService">The aad account close service.</param>
        /// <param name="deadLetterTable">The dead letter table.</param>
        /// <param name="counterFactory">The counter factory.</param>
        /// <param name="appConfiguration">The Azure App Configuration instance</param>
        /// <returns></returns>
        AadAccountCloseQueueProcessor Create(
            ILogger logger,
            IPrivacyConfigurationManager configuration,
            IAccountCloseQueueManager queueManager,
            IAadAccountCloseService aadAccountCloseService,
            ITable<AccountCloseDeadLetterStorage> deadLetterTable,
            ICounterFactory counterFactory,
            IAppConfiguration appConfiguration);
    }
}
