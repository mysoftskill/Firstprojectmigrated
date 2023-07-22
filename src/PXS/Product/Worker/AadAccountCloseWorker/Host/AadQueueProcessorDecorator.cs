// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.Host
{
    using System;
    using System.Diagnostics;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Worker;
    using Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker;
    using Microsoft.Membership.MemberServices.PrivacyHost;
    using Microsoft.Practices.Unity;

    /// <summary>
    ///     A host decorator for AAD queue processing.
    /// </summary>
    public class AadQueueProcessorDecorator: HostDecorator
    {
        private const string ClassName = nameof(AadQueueProcessorDecorator);

        private readonly IDependencyManager dependencyManager;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AadEventHubProcessorDecorator" /> class.
        /// </summary>
        /// <param name="dependencyManager">The dependency manager.</param>
        public AadQueueProcessorDecorator(IDependencyManager dependencyManager)
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
            Trace.TraceInformation($"{MethodName} started processing queue messages.");

            var processor = this.dependencyManager.Container.Resolve<IWorker>(DependencyManager.AadAacountCloseQueueWorker);
            processor.Start();

            // Execute the inner handler and wait for its stop signal.
            ConsoleSpecialKey? stopSignal = base.Execute();

            // Wait for the queue processor to stop.
            processor.StopAsync().GetAwaiter().GetResult();

            Trace.TraceInformation($"{MethodName} stopped processing queue messages for {DependencyManager.AadAacountCloseQueueWorker}.");

            // Bubble up the stop signal.
            return stopSignal;
        }
    }
}
