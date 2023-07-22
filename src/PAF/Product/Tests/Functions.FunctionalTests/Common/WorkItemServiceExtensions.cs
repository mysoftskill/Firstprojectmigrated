namespace Microsoft.PrivacyServices.AzureFunctions.FunctionalTests.Common
{
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.AzureFunctions.Common.DataAccessors;

    public static class WorkItemServiceExtensions
    {
        // Method used for simplifying the transition process
        public static async Task SetVariantRequestStateAsync(this IVariantRequestWorkItemService workItemService, int workItemId, string finalstate)
        {
            await workItemService.UpdateWorkItemStateAsync(workItemId, "Active").ConfigureAwait(false);
            await workItemService.UpdateWorkItemStateAsync(workItemId, "GC Approved").ConfigureAwait(false);
            await workItemService.UpdateWorkItemStateAsync(workItemId, finalstate).ConfigureAwait(false);
        }
    }
}
