namespace Microsoft.ComplianceServices.Common.Queues
{
    /// <summary>
    ///     Cloud queue interface.
    /// </summary>
    /// <typeparam name="T">type of queue item</typeparam>
    public interface ICloudQueue<T>: ICloudQueueBase<T>
    {
        /// <summary>
        ///     Storage account name.
        /// </summary>
        string StorageAccountName { get; }

        /// <summary>
        ///     Gets the storage account name.
        /// </summary>
        string QueueName { get; }
    }
}
