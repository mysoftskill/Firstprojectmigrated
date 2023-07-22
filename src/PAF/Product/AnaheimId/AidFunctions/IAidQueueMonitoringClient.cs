namespace Microsoft.PrivacyServices.AnaheimId.AidFunctions
{
    using System.Threading.Tasks;

    /// <summary>
    /// Client for monitoring queue size.
    /// </summary>
    public interface IAidQueueMonitoringClient
    {
        /// <summary>
        ///     Gets the approximate queue size.
        /// </summary>
        /// <returns>Approximate queue size.</returns>
        Task<int> GetQueueSizeAsync();

        /// <summary>
        ///     Gets the Storage account name.
        /// </summary>
        /// <returns>Storage account name.</returns>
        string GetStorageAccountName();

        /// <summary>
        ///     Gets the queue name.
        /// </summary>
        /// <returns>queue name.</returns>
        string GetQueueName();
    }
}
