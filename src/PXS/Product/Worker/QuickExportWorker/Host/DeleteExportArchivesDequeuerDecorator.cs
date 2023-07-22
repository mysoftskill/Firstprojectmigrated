// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.QuickExportWorker.Host
{
    using System;
    using System.Diagnostics;

    using Microsoft.Membership.MemberServices.Privacy.QuickExportWorker.Tasks;
    using Microsoft.Membership.MemberServices.PrivacyHost;
    
    public class DeleteExportArchivesDequeuerDecorator : HostDecorator
    {
        private readonly DependencyManager dependencyManager;

        public DeleteExportArchivesDequeuerDecorator(DependencyManager dependencyManager)
        {
            this.dependencyManager = dependencyManager;
        }

        public override ConsoleSpecialKey? Execute()
        {
            Trace.TraceInformation("DeleteExportArchivesDequeuerDecorator executing");

            var dequeuer = (DeleteExportArchivesDequeuer)this.dependencyManager.GetService(typeof(DeleteExportArchivesDequeuer));
            dequeuer.Initialize(this.dependencyManager);

            // This task is not supposed to stop. If it does it should be an unhandled exception by design.
#pragma warning disable 4014
            dequeuer.ProcessAsync();
#pragma warning restore 4014

            Trace.TraceInformation("DeleteExportArchivesDequeuerDecorator kicked off DeleteExportArchivesDequeuer thread");
            return base.Execute();
        }
    }
}
