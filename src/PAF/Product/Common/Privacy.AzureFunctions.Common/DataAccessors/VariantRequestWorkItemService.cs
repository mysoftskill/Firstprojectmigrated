namespace Microsoft.PrivacyServices.AzureFunctions.Common.DataAccessors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.AzureFunctions.Common.Configuration;
    using Microsoft.PrivacyServices.AzureFunctions.Common.Models;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
    using Microsoft.VisualStudio.Services.Common;
    using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

    /// <summary>
    /// Manages VariantRequest workitem changes and access
    /// </summary>
    public class VariantRequestWorkItemService : IVariantRequestWorkItemService
    {
        private const string ComponentName = nameof(VariantRequestWorkItemService);

        private const string VariantLinkingHref = "https://aka.ms/VariantLinkingInstructions";

        private const int MaxWorkItemTitleLength = 256;

        private static readonly string[] WorkItemFields =
        {
            "System.Id",
            "System.Title",
            "System.State",
            "System.TeamProject",
            "Custom.VariantRequestId"
        };

        private readonly IFunctionConfiguration configuration;
        private readonly ILogger logger;
        private readonly IAdoClientWrapper adoClientWrapper;
        private readonly IVariantRequestPatchSerializer patchSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="VariantRequestWorkItemService"/> class.
        /// Construct a new WorkItemService
        /// </summary>
        /// <param name="configuration">Implementation of IFunctionConfiguration</param>
        /// <param name="adoClientWrapper">Implementation of IAdoClientWrapper</param>
        /// <param name="logger">Implementation of ILogger</param>
        /// <param name="patchSerializer">Implementation of IVariantRequestPatchSerializer</param>
        public VariantRequestWorkItemService(
            IFunctionConfiguration configuration,
            IAdoClientWrapper adoClientWrapper,
            ILogger logger,
            IVariantRequestPatchSerializer patchSerializer)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.adoClientWrapper = adoClientWrapper ?? throw new ArgumentNullException(nameof(adoClientWrapper));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.patchSerializer = patchSerializer ?? throw new ArgumentNullException(nameof(patchSerializer));
        }

        /// <summary>
        /// Formats the Title field for work item
        /// </summary>
        /// <param name="egrcId">EGRC Id for the variant.</param>
        /// <param name="egrcName">EGRC Name for the variant.</param>
        /// <returns>returns a string containing the new title.</returns>
        public string FormatTitle(string egrcId, string egrcName)
        {
            string formattedTitle = $"AssetGroup-Variant Link Request: EGRC ID: {egrcId}, EGRC Name: {egrcName}";

            if (formattedTitle.Length > MaxWorkItemTitleLength)
            {
                this.logger.Information(ComponentName, $"workItem formatted title '{formattedTitle}' length is greater than '{MaxWorkItemTitleLength}'. Trimming the title to '{MaxWorkItemTitleLength}' characters");
                formattedTitle = formattedTitle.Substring(0, MaxWorkItemTitleLength);
            }

            return formattedTitle;
        }

        /// <summary>
        /// Formats the Description field for work item
        /// </summary>
        /// <param name="descriptionItems">Dictionary of description items that are added to the description. Key will be the side header and value will be the paragraph</param>
        /// <returns>returns an html string containing the description with all description items.</returns>
        public static string FormatDescription(Dictionary<string, string> descriptionItems)
        {
            var instructions = $"<a href=\"{VariantLinkingHref}\">{VariantLinkingHref}</a>.";
            descriptionItems["Instructions"] = instructions;
            StringBuilder descriptionBuilder = new StringBuilder();
            foreach (var kvp in descriptionItems)
            {
                descriptionBuilder.Append($"<u><b>{kvp.Key}</b></u>: <p>{kvp.Value}</p>");
            }

            return descriptionBuilder.ToString();
        }

        /// <inheritdoc/>
        public async Task<WorkItem> CreateVariantRequestWorkItemAsync(ExtendedVariantRequest variantRequest)
        {
            var variantRequestWorkItem = BuildVariantRequestWorkItem(variantRequest);
            JsonPatchDocument patchDocument = this.patchSerializer.CreateVariantRequestPatchDocument(variantRequestWorkItem);

            // add comment to notify the contacts
            string comment = BuildComment(variantRequestWorkItem.RequesterAlias, variantRequestWorkItem.GeneralContractorAlias);

            var workItem = await this.adoClientWrapper.CreateWorkItemAsync(patchDocument, "VariantRequest", comment).ConfigureAwait(false);

            // Set Work Item state to Active to allow users to edit it;
            // attempt to assign the work item to the gc
            if (workItem.Id != null)
            {
                int workItemId = (int)workItem.Id;
                this.logger.Information(ComponentName, $"workItem url = {workItem.Url}");
                await this.UpdateWorkItemStateAsync(workItemId, "Active").ConfigureAwait(false);

                await this.UpdateWorkItemAssignedToAsync(workItemId, variantRequestWorkItem.GeneralContractorAlias).ConfigureAwait(false);
            }

            return workItem;
        }

        /// <inheritdoc/>
        public Task<List<int>> GetAllWorkItemsIdsAsync()
        {
            // Runs a query to find variant requests approved by CELA and GCs
            Wiql wiql = new Wiql()
            {
                Query = "Select [Id] From WorkItems " +
                   $"Where [System.TeamProject] = '{this.configuration.AzureDevOpsProjectName}' " +
                   "And [Work Item Type] = 'VariantRequest' "
            };

            // Gets the list of workitems
            return this.adoClientWrapper.QueryByWiqlAsync(wiql);
        }

        /// <inheritdoc/>
        public Task<List<WorkItem>> GetPendingVariantRequestWorkItemsAsync()
        {
            // Runs a query to find variant requests approved by CELA and GCs
            Wiql wiql = new Wiql()
            {
                Query = "Select [State], [Title], [Custom.VariantRequestId] " +
                   "From WorkItems " +
                   $"Where [System.TeamProject] = '{this.configuration.AzureDevOpsProjectName}' " +
                   "And [Work Item Type] = 'VariantRequest' " +
                   "And ([System.State] = 'GC on behalf of CELA Approved' Or [System.State] = 'CELA Approved') " +
                   "Order By [State] Asc, [Changed Date] Desc"
            };

            // Gets the list of workitems
            return this.adoClientWrapper.QueryByWiqlAsync(wiql, WorkItemFields);
        }

        /// <inheritdoc/>
        public Task ApproveVariantRequestWorkItemAsync(int workItemId)
        {
            return this.UpdateWorkItemStateAsync(workItemId, "Approved");
        }

        /// <inheritdoc/>
        public Task RemoveVariantRequestWorkItemAsync(int workItemId)
        {
            return this.UpdateWorkItemStateAsync(workItemId, "Removed");
        }

        /// <inheritdoc/>
        public Task<List<WorkItem>> GetRejectedVariantRequestWorkItemsAsync()
        {
            // Runs a query to find variant requests approved by CELA and GCs
            Wiql wiql = new Wiql()
            {
                Query = "Select [State], [Title], [Custom.VariantRequestId] " +
                   "From WorkItems " +
                    $"Where [System.TeamProject] = '{this.configuration.AzureDevOpsProjectName}' " +
                   "And [Work Item Type] = 'VariantRequest' " +
                   "And [System.State] = 'Rejected' " +
                   "Order By [State] Asc, [Changed Date] Desc"
            };

            // Gets the list of workitemIds
            return this.adoClientWrapper.QueryByWiqlAsync(wiql, WorkItemFields);
        }

        /// <inheritdoc/>
        public async Task<WorkItem> UpdateWorkItemStateAsync(int workItemId, string newState)
        {
            // Set up the Patch document to update the document
            Dictionary<string, string> approvalFields = new Dictionary<string, string>
            {
                { "System.State", newState }
            };

            JsonPatchDocument updateDocument = this.patchSerializer.UpdateVariantRequestPatchDocument(approvalFields);

            // Update the workItem
            WorkItem workitem = await this.adoClientWrapper.UpdateWorkItemAsync(updateDocument, workItemId).ConfigureAwait(false);

            this.logger.Information(ComponentName, "WorkItem {0} has been moved to state {1}", workitem?.Id, newState);
            return workitem;
        }

        /// <inheritdoc/>
        public async Task<WorkItem> UpdateWorkItemAssignedToAsync(int workItemId, string assignedTo)
        {
            WorkItem workitem = null;
            if (!string.IsNullOrWhiteSpace(assignedTo))
            {
                // Set up the Patch document to update the document
                Dictionary<string, string> approvalFields = new Dictionary<string, string>();

                // If no domain is specified, assume corp domain
                var domain = assignedTo.Contains('@') ? string.Empty : "@microsoft.com";
                approvalFields.Add("System.AssignedTo", $"{assignedTo}{domain}");
                JsonPatchDocument updateDocument = this.patchSerializer.UpdateVariantRequestPatchDocument(approvalFields);

                // Update the workItem
                try
                {
                    workitem = await this.adoClientWrapper.UpdateWorkItemAsync(updateDocument, workItemId).ConfigureAwait(false);
                    this.logger.Information(ComponentName, "WorkItem {0} assigned.", workitem?.Id);
                }
                catch (VssServiceException ex)
                {
                    // If this fails, it's not fatal; it can happen if the gcalias was bad.  Log it, but don't re-throw.
                    this.logger.Error(ComponentName, $"Error setting AssignedTo field of work item {workItemId}: {ex.Message}");
                }
            }

            return workitem;
        }

        /// <inheritdoc/>
        public async Task<WorkItemDelete> DeleteVariantRequestWorkItemAsync(int workItemId, bool deleteFromRecycleBin = false)
        {
            if (!this.configuration.EnableNonProdFunctionality)
            {
                this.logger.Information(ComponentName, "DeleteVariantRequestWorkItem is enabled in non-prod only");
                throw new InvalidOperationException("DeleteVariantRequestWorkItem is enabled in non-prod only");
            }

            // Deletes a specific work item
            WorkItemDelete workitem = await this.adoClientWrapper.DeleteWorkItemAsync(workItemId, deleteFromRecycleBin).ConfigureAwait(false);

            // Outputs the work item id of the deleted workitem if it remains in the recyclebin
            if (!deleteFromRecycleBin)
            {
                this.logger.Information(ComponentName, "Work Item {0} Deleted", workitem.Resource.Id);
            }

            return workitem;
        }

        /// <inheritdoc/>
        public Task<List<WorkItem>> GetWorkItemsWithIdsAsync(IEnumerable<int> id)
        {
            return this.adoClientWrapper.GetWorkItemsAsync(id);
        }

        /// <summary>
        /// Fill in the fields for the assetgroup-variant linking ADO work item.
        /// </summary>
        /// <param name="variantRequest">The variant request to use when building the work item.</param>
        /// <returns>returns an assetgroup-variant request linking work item.</returns>
        private VariantRequestWorkItem BuildVariantRequestWorkItem(ExtendedVariantRequest variantRequest)
        {
            // Include the EGRC Id and Name of the first variant in the title
            var firstVariant = variantRequest.RequestedVariants?.FirstOrDefault();
            var egrcId = firstVariant?.EgrcId ?? "Unknown";
            var egrcName = firstVariant?.EgrcName ?? "Unknown";
            Dictionary<string, string> descriptionItems = new Dictionary<string, string>
            {
                ["PCD Team Name"] = variantRequest.OwnerName ?? "Unknown",
                ["Additional Information"] = variantRequest.AdditionalInformation ?? string.Empty
            };
            return new VariantRequestWorkItem()
            {
                Variants = variantRequest.RequestedVariants,
                AssetGroups = variantRequest.VariantRelationships,
                WorkItemTitle = this.FormatTitle(egrcId, egrcName),
                WorkItemDescription = FormatDescription(descriptionItems),
                VariantRequestId = variantRequest.Id,
                GeneralContractorAlias = variantRequest.GeneralContractorAlias ?? string.Empty, // ADO model doesn't allow null
                CelaContactAlias = variantRequest.CelaContactAlias ?? string.Empty, // ADO model doesn't allow null
                RequesterAlias = variantRequest.RequesterAlias ?? string.Empty
            };
        }

        /// <summary>
        /// Formats an href to mention an user in the Comments field.
        /// </summary>
        /// <param name="contact">An email alias to mention in the comment.</param>
        /// <returns>returns an html string containing the href.</returns>
        private static string FormatContactAddress(string contact)
        {
            if (!string.IsNullOrEmpty(contact))
            {
                // If no domain is specified, assume corp domain
                string domain = contact.Contains('@') ? string.Empty : "@microsoft.com";

                return $@"<a href=""mailto:{contact}{domain}"" data-vss-mention=""version: 1.0"">@{contact}</a>, ";
            }

            return string.Empty;
        }

        /// <summary>
        /// Builds a string for the Comments field.
        /// </summary>
        /// <param name="requesterAlias">An Requester email alias to mention in the comment.</param>
        /// <param name="gcAlias">An General Contractor email alias to mention in the comment.</param>
        /// <returns>returns an string containing the comment.</returns>
        private static string BuildComment(string requesterAlias, string gcAlias)
        {
            string comment = $"{FormatContactAddress(gcAlias)}";

            // If at least one of the aliases was provided, then add the
            // rest of the text; otherwise, return string.Empty
            if (!string.IsNullOrWhiteSpace(comment))
            {
                return $"{comment}You have been added as an approver for this asset-variant request by the requestor {FormatContactAddress(requesterAlias)}. In order to make sure you receive updates for this request please select the <b>Follow</b> button on this work item.<br />For Variant Process: <a href='http://aka.ms/ngpvariant'>aka.ms/ngpvariant</a><br />For NGP support: <a href='http://aka.ms/ngpsupport'>aka.ms/ngpsupport</a>";
            }

            return string.Empty;
        }
    }
}
