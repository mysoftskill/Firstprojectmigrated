namespace Microsoft.PrivacyServices.AzureFunctions.Common.DataAccessors
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.AzureFunctions.Common.Models;
    using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

    /// <summary>
    /// Defines contracts to manage VariantRequest workItems
    /// </summary>
    public interface IVariantRequestWorkItemService
    {
        /// <summary>
        /// Creates a VariantRequest WorkItem
        /// </summary>
        /// <param name="variantRequest">Work Item to be created</param>
        /// <returns>WorkItem created</returns>
        Task<WorkItem> CreateVariantRequestWorkItemAsync(ExtendedVariantRequest variantRequest);

        /// <summary>
        /// Gets all VariantRequest WorkItems in the CELA Approved or GC Approved on behalf of CELA state
        /// </summary>
        /// <returns>List of WorkItems that need to be approved</returns>
        Task<List<WorkItem>> GetPendingVariantRequestWorkItemsAsync();

        /// <summary>
        /// Updates a VariantRequest WorkItem to the Approved state
        /// </summary>
        /// <param name="workItemId">Id to be approved</param>
        /// <returns>Async Task</returns>
        Task ApproveVariantRequestWorkItemAsync(int workItemId);

        /// <summary>
        /// Updates a VariantRequestWorkItem to the Removed state
        /// </summary>
        /// <param name="workItemId">Id to be removed</param>
        /// <returns>Async Task</returns>
        Task RemoveVariantRequestWorkItemAsync(int workItemId);

        /// <summary>
        /// Gets all VariantRequest WorkItems in the Rejected state
        /// </summary>
        /// <returns>List of WorkItems that have been rejected</returns>
        Task<List<WorkItem>> GetRejectedVariantRequestWorkItemsAsync();

        /// <summary>
        /// Update the state of a work item
        /// </summary>
        /// <param name="workItemId">work item id</param>
        /// <param name="newState">The new state that the WorkItem will be updated to</param>
        /// <returns>WorkItem that has been updated</returns>
        Task<WorkItem> UpdateWorkItemStateAsync(int workItemId, string newState);

        /// <summary>
        /// Update the AssignedTo field of a work item
        /// </summary>
        /// <param name="workItemId">work item id</param>
        /// <param name="assignedTo">The new owner that the WorkItem.</param>
        /// <returns>WorkItem that has been updated</returns>
        Task<WorkItem> UpdateWorkItemAssignedToAsync(int workItemId, string assignedTo);

        /// <summary>
        /// Deletes a VariantRequest WorkItem
        /// ONLY to be used in the NON-Prod test tenant
        /// </summary>
        /// <param name="workItemId">Id of the workitem to be deleted</param>
        /// <param name="deleteFromRecycleBin">Determines if an item is permanently deleted</param>
        /// <returns>Deleted work Item if deleteFromRecycleBin is false</returns>
        Task<WorkItemDelete> DeleteVariantRequestWorkItemAsync(int workItemId, bool deleteFromRecycleBin);

        /// <summary>
        /// Get all workitems in the project, Added for testing purposes only.
        /// </summary>
        /// <returns>List of WorkItems Ids</returns>
        Task<List<int>> GetAllWorkItemsIdsAsync();

        /// <summary>
        /// Get WorkItem.
        /// </summary>
        /// <param name="ids">List containing workitem ids.</param>
        /// <returns>WorkItem</returns>
        Task<List<WorkItem>> GetWorkItemsWithIdsAsync(IEnumerable<int> ids);
    }
}
