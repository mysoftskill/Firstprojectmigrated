// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.EventHubProcessor
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.QueueProcessor;
    using Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.TableStorage;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.Membership.MemberServices.Privacy.Core.PrivacyCommand;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.ServiceBus.Messaging;

    /// <summary>
    ///     A factory class used to create <see cref="AadEventProcessor" /> instances.
    /// </summary>
    public class AadEventProcessorFactory : IEventProcessorFactory
    {
        private readonly IClock clock;

        private readonly string cloudInstance;

        private readonly ICounterFactory counterFactory;

        private readonly ITable<NotificationDeadLetterStorage> deadLetterTable;

        private readonly string eventHubsEndpoint;

        private readonly string hubId;

        /// <summary>
        ///     The logger.
        /// </summary>
        private readonly ILogger logger;

        private readonly IAccountCloseQueueManager queueManager;

        private readonly IRequestClassifier requestClassifier;

        private readonly IList<string> tenantFilterList;

        private readonly IAppConfiguration appConfiguration;

        /// <summary>
        ///     Creates a new instance of <see cref="AadEventProcessorFactory" />.
        /// </summary>
        public AadEventProcessorFactory(
            ILogger logger,
            IAccountCloseQueueManager queueManager,
            ICounterFactory counterFactory,
            IClock clock,
            string hubId,
            IPrivacyConfigurationManager configurationManager,
            IRequestClassifier requestClassifier,
            ITable<NotificationDeadLetterStorage> deadLetterTable,
            string eventHubsEndpoint,
            IAppConfiguration appConfiguration)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.queueManager = queueManager ?? throw new ArgumentNullException(nameof(queueManager));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.counterFactory = counterFactory ?? throw new ArgumentNullException(nameof(counterFactory));
            this.hubId = hubId;
            this.cloudInstance = (configurationManager?.PrivacyExperienceServiceConfiguration?.CloudInstance).ToPcfCloudInstance();
            this.tenantFilterList = configurationManager?.AadAccountCloseWorkerConfiguration?.EventHubProcessorConfig?.TenantFilter;
            this.requestClassifier = requestClassifier;
            this.deadLetterTable = deadLetterTable;
            AadEventProcessor.ValidateFilters(this.logger, this.tenantFilterList);
            this.eventHubsEndpoint = eventHubsEndpoint;
            this.appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));
        }

        /// <inheritdoc />
        public IEventProcessor CreateEventProcessor(PartitionContext context)
        {
            const string msg = "Created a new event processor for PartitionId '{0}' and Owner '{1}'.";

            var processor = new AadEventProcessor(
                this.logger,
                this.queueManager,
                this.counterFactory,
                this.clock,
                this.hubId,
                this.cloudInstance,
                this.tenantFilterList,
                this.requestClassifier,
                this.deadLetterTable,
                this.eventHubsEndpoint,
                this.appConfiguration);
            this.logger.Information(
                nameof(AadEventProcessorFactory),
                msg,
                context?.Lease?.PartitionId,
                context?.Lease?.Owner);

            return new InstrumentedEventProcessor(processor);
        }
    }
}
