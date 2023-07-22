namespace Microsoft.PrivacyServices.AzureFunctions.Common.DataAccessors
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
    using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

    /// <summary>
    /// Defines the Wrapper for WorkItemTrackingHttpClient
    /// </summary>
    public interface IAdoClientWrapper
    {
        /// <summary>
        /// Creates WorkItem in ADO
        /// </summary>
        /// <param name="document">PatchDocument to create the workItem</param>
        /// <param name="type">type of the work Item</param>
        /// <param name="comment">Optionally any comment that needs to be added.</param>
        /// <returns>newly created workitem</returns>
        Task<WorkItem> CreateWorkItemAsync(JsonPatchDocument document, string type, string comment = null);

        /// <summary>
        /// Queries ADO and returns a set of workitems
        /// </summary>
        /// <param name="wiql">ADO query</param>
        /// <param name="fields">fields that need to be retrieved (Do we need this?)</param>
        /// <returns>List of work items</returns>
        Task<List<WorkItem>> QueryByWiqlAsync(Wiql wiql, IEnumerable<string> fields);

        /// <summary>
        /// Queries ADO and returns a set of workitems Ids
        /// </summary>
        /// <param name="wiql">ADO query</param>
        /// <returns>List of work items Ids</returns>
        Task<List<int>> QueryByWiqlAsync(Wiql wiql);

        /// <summary>
        /// Update a work item
        /// </summary>
        /// <param name="document">PatchDocument to update the workItem</param>
        /// <param name="workItemId">WorkItem Id</param>
        /// <returns>Updated workItem</returns>
        Task<WorkItem> UpdateWorkItemAsync(JsonPatchDocument document, int workItemId);

        /// <summary>
        /// Deletes a work item
        /// </summary>
        /// <param name="workItemId">WorkItem Id</param>
        /// <param name="deleteFromRecycleBin">defines soft or hard delete</param>
        /// <returns>Deleted work item if soft delete</returns>
        Task<WorkItemDelete> DeleteWorkItemAsync(int workItemId, bool deleteFromRecycleBin);

        /// <summary>
        /// Gets a WorkItem in ADO
        /// </summary>
        /// <param name="workItemId">Id of the workItem to get</param>
        /// <returns>workitem</returns>
        Task<WorkItem> GetWorkItemAsync(int workItemId);

        /// <summary>
        /// Gets a WorkItem in ADO
        /// </summary>
        /// <param name="workItemIds">Ids of the workItem to get</param>
        /// <returns>workitem</returns>
        Task<List<WorkItem>> GetWorkItemsAsync(IEnumerable<int> workItemIds);
    }
}
