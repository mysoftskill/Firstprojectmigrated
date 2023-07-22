// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.RecurrentDeleteWorker.Host
{
    using System;
    using System.Diagnostics;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Worker;
    using Microsoft.Membership.MemberServices.PrivacyHost;
    using Microsoft.Practices.Unity;

    /// <summary>
    /// Host decorator
    /// </summary>
    public class RecurringDeleteHostDecorator : HostDecorator
    {
        private readonly IWorker worker;
        private readonly string hostName;

        /// <summary>
        /// Create host
        /// </summary>
        /// <param name="dependencyManager">Dependency manager</param>
        /// <param name="hostName">Host name</param>
        public RecurringDeleteHostDecorator(IDependencyManager dependencyManager, string hostName)
        {
            this.worker = dependencyManager.Container.Resolve<IWorker>(hostName);
            this.hostName = hostName;
        }

        /// <summary>
        /// Runs the host decorator.
        /// </summary>
        /// <returns>A console special key used as a stop running signal.</returns>
        public override ConsoleSpecialKey? Execute()
        {
            string MethodName = this.hostName + "." + nameof(this.Execute);
            Trace.TraceInformation($"{MethodName} executing.");
            Trace.TraceInformation($"{MethodName} started processing.");

            this.worker.Start();

            // Execute the inner handler and wait for its stop signal.
            ConsoleSpecialKey? stopSignal = base.Execute();

            // Wait for the queue processor to stop.
            this.worker.StopAsync().GetAwaiter().GetResult();

            Trace.TraceInformation($"{MethodName} stopped processing.");

            // Bubble up the stop signal.
            return stopSignal;
        }
    }
}
