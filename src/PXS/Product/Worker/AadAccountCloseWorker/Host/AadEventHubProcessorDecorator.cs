// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.Host
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.EventHubProcessor;
    using Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.QueueProcessor;
    using Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.TableStorage;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.Membership.MemberServices.PrivacyHost;
    using Microsoft.Practices.Unity;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.ServiceBus.Messaging;

    /// <summary>
    ///     A host decorator for AAD event processing.
    /// </summary>
    public class AadEventHubProcessorDecorator : HostDecorator
    {
        /// <summary>
        ///     The class name used for logging annotations.
        /// </summary>
        private const string ClassName = nameof(AadEventHubProcessorDecorator);

        /// <summary>
        ///     The dependency manager.
        /// </summary>
        private readonly IDependencyManager dependencyManager;

        /// <summary>
        ///     The event hub host.
        /// </summary>
        private List<(EventProcessorHost Host, IEventProcessorFactory Factory)> hosts;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AadEventHubProcessorDecorator" /> class.
        /// </summary>
        /// <param name="dependencyManager">The dependency manager.</param>
        public AadEventHubProcessorDecorator(IDependencyManager dependencyManager)
        {
            this.dependencyManager = dependencyManager ?? throw new ArgumentNullException(nameof(dependencyManager));
        }

        /// <summary>
        ///     Runs the host decorator.
        /// </summary>
        /// <returns>A console special key used as a stop running signal.</returns>
        public override ConsoleSpecialKey? Execute()
        {
            const string MethodName = ClassName + "." + nameof(this.Execute);
            Trace.TraceInformation($"{MethodName} executing.");

            var configManager = this.dependencyManager.Container.Resolve<IPrivacyConfigurationManager>();
            var workerConfig = configManager.AadAccountCloseWorkerConfiguration;

            if (!workerConfig.EventHubProcessorConfig.EnableProcessing)
            {
                // The AAD Event Hub Processor is disabled so short-circuit everything 
                // and go to the next decorator in the pipeline.
                return base.Execute();
            }

            var eventHubProcessorConfig = workerConfig.EventHubProcessorConfig;

            var options = new EventProcessorOptions { MaxBatchSize = eventHubProcessorConfig.MaxBatchSize };
            var eventHubHelpers = this.dependencyManager.Container.Resolve<IEventHubHelpers>();

            // Get azure storage connection string for checkpoint and partitioning
            string azureStorageConnectionString = eventHubHelpers.GetAzureStorageConnectionStringAsync().GetAwaiter().GetResult();

            // Number of hosts is determined on how many connections we receive
            IEnumerable<IConnectionInformation> connectionInfos = eventHubHelpers.GetConnectionInformationsAsync().GetAwaiter().GetResult();

            // Each connection needs its own event processor host
            this.hosts = connectionInfos.Select(
                info => (Host: new EventProcessorHost(
                        $"{eventHubProcessorConfig.EventHubConfig.HostName}{Guid.NewGuid()}", // Host name must be unique for each instance
                        info.EntityPath,
                        eventHubProcessorConfig.EventHubConfig.ConsumerGroupName,
                        info.ConnectionString,
                        azureStorageConnectionString,
                        info.Name),
                    Factory: new AadEventProcessorFactory(
                        this.dependencyManager.Container.Resolve<ILogger>(),
                        this.dependencyManager.Container.Resolve<IAccountCloseQueueManager>(),
                        this.dependencyManager.Container.Resolve<ICounterFactory>(),
                        this.dependencyManager.Container.Resolve<IClock>(),
                        info.Name,
                        this.dependencyManager.Container.Resolve<IPrivacyConfigurationManager>(),
                        this.dependencyManager.Container.Resolve<IRequestClassifier>(),
                        this.dependencyManager.Container.Resolve<ITable<NotificationDeadLetterStorage>>(),
                        info.Endpoint,
                        this.dependencyManager.Container.Resolve<IAppConfiguration>()) as IEventProcessorFactory)).ToList();

            // Start the event hub processor.
            Task.WhenAll(this.hosts.Select(host => host.Host.RegisterEventProcessorFactoryAsync(host.Factory, options)).ToArray()).GetAwaiter().GetResult();

            Trace.TraceInformation($"{MethodName} started processing events from event hub.");

            // Execute the inner handler and wait for its stop signal.
            ConsoleSpecialKey? stopSignal = base.Execute();

            // Wait for the event processor to stop. 
            Task.WaitAll(this.hosts.Select(host => host.Host.UnregisterEventProcessorAsync()).ToArray());

            Trace.TraceInformation($"{MethodName} stopped processing events from event hub.");

            // Bubble up the stop signal.
            return stopSignal;
        }
    }
}
