// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.PrivacyVsoWorker.Host
{
    using System;
    using System.Diagnostics;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Worker;
    using Microsoft.Membership.MemberServices.PrivacyHost;
    using Microsoft.Practices.Unity;

    /// <summary>
    ///     A host decorator for item processing.
    /// </summary>
    public class ItemProcessorDecorator : HostDecorator
    {
        private const string ClassName = nameof(ItemProcessorDecorator);

        private readonly IDependencyManager dependencyManager;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ItemProcessorDecorator" /> class.
        /// </summary>
        /// <param name="dependencyManager">The dependency manager.</param>
        public ItemProcessorDecorator(IDependencyManager dependencyManager)
        {
            this.dependencyManager = dependencyManager ?? throw new ArgumentNullException(nameof(dependencyManager));
        }

        /// <summary>
        ///     Runs the host decorator.
        /// </summary>
        /// <returns>A console special key used as a stop running signal.</returns>
        public override ConsoleSpecialKey? Execute()
        {
            const string methodName = ClassName + "." + nameof(this.Execute);
            Trace.TraceInformation($"{methodName} executing.");
            Trace.TraceInformation($"{methodName} started processing items.");

            var processor = this.dependencyManager.Container.Resolve<IWorker>(DependencyManager.PrivacyVsoWorker);
            processor.Start();

            // Execute the inner handler and wait for its stop signal.
            ConsoleSpecialKey? stopSignal = base.Execute();

            // Wait for the queue processor to stop.
            processor?.StopAsync().GetAwaiter().GetResult();

            Trace.TraceInformation($"{methodName} stopped processing items for {DependencyManager.PrivacyVsoWorker}.");

            // Bubble up the stop signal.
            return stopSignal;
        }
    }
}
