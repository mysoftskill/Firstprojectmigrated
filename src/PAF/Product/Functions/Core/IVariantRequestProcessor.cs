namespace Microsoft.PrivacyServices.AzureFunctions.Core
{
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;

    /// <summary>
    /// Interface for processing variant requests.
    /// </summary>
    public interface IVariantRequestProcessor
    {
        /// <summary>
        /// Creates VariantRequest Work item
        /// </summary>
        /// <param name="variantRequestMessageJson">Message from the queue</param>
        /// <param name="processedQueue">processedQueue</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task CreateVariantRequestWorkItemAsync(
            string variantRequestMessageJson,
            ICollector<string> processedQueue);

        /// <summary>
        /// Updates PDMS with all approved workitems and sets the workitems to approved
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task UpdateApprovedVariantRequestWorkItemsAsync();

        /// <summary>
        /// Removes all rejected variant requests from PDMS and sets the workitem state to Removed
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task RemoveRejectedVariantRequestWorkItemsAsync();

        /// <summary>
        /// Move variant requests to unprocessed queue and set metric dimensions
        /// </summary>
        /// <param name="variantRequestPoisonQueue">Poison queue name</param>
        /// <param name="variantRequestMessageJson">Message from the queue</param>
        /// <param name="unprocessedQueue">messages from poison queue move to this queue for reprocessing</param>
        void MoveVariantRequestToUnprocessedQueueAsync(
            string variantRequestPoisonQueue,
            string variantRequestMessageJson,
            ICollector<string> unprocessedQueue);
    }
}
