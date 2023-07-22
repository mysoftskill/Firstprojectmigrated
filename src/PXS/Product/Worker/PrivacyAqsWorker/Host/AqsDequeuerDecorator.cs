// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.Host
{
    using System;
    using System.Diagnostics;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Worker;
    using Microsoft.Membership.MemberServices.PrivacyHost;
    using Microsoft.Practices.Unity;

    /// <summary>
    ///     A host decorator for AQS event processing.
    /// </summary>
    public class AqsDequeuerDecorator : HostDecorator
    {
        /// <summary>
        ///     This is the amount of time to wait on queue processor when there is no work to do, or if an error is encountered.
        /// </summary>
        private static readonly TimeSpan QueueProcessorDelayNoWork = TimeSpan.FromSeconds(1);

        /// <summary>
        ///     The dependency manager.
        /// </summary>
        private readonly IDependencyManager dependencyManager;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AqsDequeuerDecorator" /> class.
        /// </summary>
        /// <param name="dependencyManager">The dependency manager.</param>
        public AqsDequeuerDecorator(IDependencyManager dependencyManager)
        {
            this.dependencyManager = dependencyManager;
        }

        /// <summary>
        ///     Runs the host decorator.
        /// </summary>
        /// <returns>A console special key used as a stop running signal.</returns>
        public override ConsoleSpecialKey? Execute()
        {
            Trace.TraceInformation($"{nameof(AqsDequeuerDecorator)} executing");

            IWorker aqsProcessor = this.dependencyManager.Container.Resolve<IWorker>(DependencyManager.CdpWorker);
            aqsProcessor.Start();
            Trace.TraceInformation($"{nameof(AqsDequeuerDecorator)} started processing threads for {DependencyManager.CdpWorker}");

            IWorker azureQueueProcessor = this.dependencyManager.Container.Resolve<IWorker>(DependencyManager.MsaAccountDeleteQueueProcessorWorker);
            azureQueueProcessor.Start(QueueProcessorDelayNoWork);
            Trace.TraceInformation($"{nameof(AqsDequeuerDecorator)} started processing threads for {DependencyManager.MsaAccountDeleteQueueProcessorWorker}");

            // Execute the inner handler and wait for its stop signal.
            ConsoleSpecialKey? stopSignal = base.Execute();

            // Wait for the processor to stop.
            aqsProcessor.StopAsync().GetAwaiter().GetResult();
            Trace.TraceInformation($"{nameof(AqsDequeuerDecorator)} stopped processing threads for {DependencyManager.CdpWorker}.");

            // Wait for the processor to stop.
            azureQueueProcessor.StopAsync().GetAwaiter().GetResult();
            Trace.TraceInformation($"{nameof(AqsDequeuerDecorator)} stopped processing threads for {DependencyManager.MsaAccountDeleteQueueProcessorWorker}.");

            // Bubble up the stop signal.
            return stopSignal;
        }
    }
}
