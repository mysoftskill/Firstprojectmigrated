namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor.TestHooks
{
    using Microsoft.Azure.ComplianceServices.NonWindowsDeviceDeleteWorker;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// NoP Mock EventHubProcessorHandler
    /// </summary>
    public class FakeEventHubProcessorHandler : IEventHubProcessorHandler
    {
        public Task CompleteAsync()
        {
            return Task.CompletedTask;
        }

        public Task PartitionClosingHandlerAsync(global::Azure.Messaging.EventHubs.Processor.PartitionClosingEventArgs eventArgs)
        {
            return Task.CompletedTask;
        }

        public Task PartitionInitializingHandlerAsync(global::Azure.Messaging.EventHubs.Processor.PartitionInitializingEventArgs eventArgs)
        {
            return Task.CompletedTask;
        }

        public Task ProcessErrorHandler(global::Azure.Messaging.EventHubs.Processor.ProcessErrorEventArgs eventArgs)
        {
            return Task.CompletedTask;
        }

        public Task ProcessEventHandlerAsync(global::Azure.Messaging.EventHubs.Processor.ProcessEventArgs eventArgs)
        {
            return Task.CompletedTask;
        }
    }
}
