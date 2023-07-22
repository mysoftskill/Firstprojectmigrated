// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.VortexDeviceDeleteWorker.Host
{
    using System;
    using System.Diagnostics;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Worker;
    using Microsoft.Membership.MemberServices.Privacy.VortexDeviceDeleteWorker;
    using Microsoft.Membership.MemberServices.PrivacyHost;
    using Microsoft.Practices.Unity;

    /// <summary>
    ///     A host decorator for vortex queue processing.
    /// </summary>
    public class VortexQueueProcessorDecorator : HostDecorator
    {
        private const string ClassName = nameof(VortexQueueProcessorDecorator);

        private readonly IDependencyManager dependencyManager;

        /// <summary>
        ///     Initializes a new instance of the <see cref="VortexQueueProcessorDecorator" /> class.
        /// </summary>
        /// <param name="dependencyManager">The dependency manager.</param>
        public VortexQueueProcessorDecorator(IDependencyManager dependencyManager)
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

            var processor = this.dependencyManager.Container.Resolve<IWorker>(DependencyManager.VortexDeviceDeleteQueueWorker);
            processor.Start();

            // Execute the inner handler and wait for its stop signal.
            ConsoleSpecialKey? stopSignal = base.Execute();

            // Wait for the queue processor to stop.
            processor.StopAsync().GetAwaiter().GetResult();

            Trace.TraceInformation($"{MethodName} stopped processing queue messages for {DependencyManager.VortexDeviceDeleteQueueWorker}.");

            // Bubble up the stop signal.
            return stopSignal;
        }
    }
}
