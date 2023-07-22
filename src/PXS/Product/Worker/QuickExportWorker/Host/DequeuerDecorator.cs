// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.QuickExportWorker.Host
{
    using System;
    using System.Diagnostics;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Privacy.QuickExportWorker;
    using Microsoft.Membership.MemberServices.PrivacyHost;

    public class DequeuerDecorator : HostDecorator
    {
        private readonly IDependencyManager dependencyManager;

        public DequeuerDecorator(IDependencyManager dependencyManager)
        {
            this.dependencyManager = dependencyManager ?? throw new ArgumentNullException(nameof(dependencyManager));
        }

        public override ConsoleSpecialKey? Execute()
        {
            Trace.TraceInformation("DequeuerDecorator executing");

            var dequeuer = (ExportDequeuer)this.dependencyManager.GetService(typeof(ExportDequeuer));
            dequeuer.Initialize(this.dependencyManager);

            // This task is not supposed to stop. If it does it should be an unhandled exception by design.
#pragma warning disable 4014
            dequeuer.ProcessAsync();
#pragma warning restore 4014

            Trace.TraceInformation("DequeuerDecorator kicked off ExportDequeuer thread");
            return base.Execute();
        }
    }
}
