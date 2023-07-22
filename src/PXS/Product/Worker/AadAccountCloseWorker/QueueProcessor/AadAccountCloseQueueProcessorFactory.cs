// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.QueueProcessor
{
    using System;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.TableStorage;
    using Microsoft.Membership.MemberServices.Privacy.Core.AadAccountClose;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     AadAccountCloseQueueProcessorFactory
    /// </summary>
    public class AadAccountCloseQueueProcessorFactory : IAadAccountCloseQueueProcessorFactory
    {
        /// <inheritdoc />
        public AadAccountCloseQueueProcessor Create(
            ILogger logger,
            IPrivacyConfigurationManager configuration,
            IAccountCloseQueueManager queueManager,
            IAadAccountCloseService aadAccountCloseService,
            ITable<AccountCloseDeadLetterStorage> deadLetterTable,
            ICounterFactory counterFactory,
            IAppConfiguration appConfiguration)
        {
            if (configuration?.AadAccountCloseWorkerConfiguration?.QueueProccessorConfig == null)
            {
                throw new ArgumentNullException(nameof(configuration.AadAccountCloseWorkerConfiguration.QueueProccessorConfig));
            }

            return new AadAccountCloseQueueProcessor(
                logger,
                configuration.AadAccountCloseWorkerConfiguration.QueueProccessorConfig,
                queueManager,
                aadAccountCloseService,
                deadLetterTable,
                counterFactory,
                appConfiguration);
        }
    }
}
