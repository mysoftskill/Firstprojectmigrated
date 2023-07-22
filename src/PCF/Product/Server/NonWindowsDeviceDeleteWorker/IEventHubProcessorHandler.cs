namespace Microsoft.Azure.ComplianceServices.NonWindowsDeviceDeleteWorker
{
    using global::Azure.Messaging.EventHubs.Processor;
    using System.Threading.Tasks;

    /// <summary>
    /// EventHubProcessorHandler interface.
    /// </summary>
    public interface IEventHubProcessorHandler
    {
        /// <summary>
        /// EventHub ProcessErrorHandler.
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <returns></returns>
        Task ProcessErrorHandler(ProcessErrorEventArgs eventArgs);

        /// <summary>
        /// Process EventHub messages.
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <returns></returns>
        Task ProcessEventHandlerAsync(ProcessEventArgs eventArgs);

        /// <summary>
        /// The handler for partition initialization is responsible for beginning to track the partition.
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <returns></returns>
        Task PartitionInitializingHandlerAsync(PartitionInitializingEventArgs eventArgs);

        /// <summary>
        /// The handler for partition close will stop tracking the partition and checkpoint if an event was processed for it.
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <returns></returns>
        Task PartitionClosingHandlerAsync(PartitionClosingEventArgs eventArgs);

        /// <summary>
        /// Make sure EventHubProcessorHandler complete processing.
        /// </summary>
        /// <returns></returns>
        Task CompleteAsync();
    }
}
