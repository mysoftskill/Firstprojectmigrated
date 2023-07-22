namespace Microsoft.PrivacyServices.AzureFunctions.Common.DataAccessors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.AzureFunctions.Common.Configuration;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
    using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
    using Microsoft.VisualStudio.Services.Client;
    using Microsoft.VisualStudio.Services.Common;
    using Microsoft.VisualStudio.Services.WebApi;
    using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

    /// <summary>
    /// Wrapper class for WorkItemTrackingHttpClient
    /// </summary>
    public class AdoClientWrapper : IAdoClientWrapper
    {
        private const string ComponentName = nameof(AdoClientWrapper);

        private readonly IFunctionConfiguration configuration;

        private readonly Uri adoProjectUri;
        private readonly string adoProjectName;
        private readonly string adoAccessToken;

        private readonly ILogger logger;
        private AadAppTokenProvider aadAppTokenProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdoClientWrapper"/> class.
        /// Construct a new AdoClientWrapper
        /// </summary>
        /// <param name="configuration">Implementation of IFunctionConfiguration.</param>
        /// <param name="logger">Implementation of ILogger.</param>
        public AdoClientWrapper(IFunctionConfiguration configuration, ILogger logger)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.logger = this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.adoProjectUri = new Uri(this.configuration.AzureDevOpsProjectUrl);
            this.adoProjectName = this.configuration.AzureDevOpsProjectName;
            this.adoAccessToken = this.configuration.AzureDevOpsAccessToken;
            if (this.configuration.ShouldUseAADToken)
            {
                try
                {
                    this.aadAppTokenProvider = this.InitializeAadAppTokenProvider(configuration);
                    logger.Information(ComponentName, "Successfully initialized AadTokenProvider");
                }
                catch (Exception ex)
                {
                    logger.Error(ComponentName, $"Unable to initialize AadAppTokenProvider: {ex.Message}");
                }
            }
        }

        /// <inheritdoc/>
        public async Task<WorkItem> CreateWorkItemAsync(JsonPatchDocument patchDocument, string type, string comment = null)
        {
            try
            {
                WorkItem workItem;
                using (var workItemTrackingHttpClient = await this.GetWorkItemTrackingHttpClientAsync().ConfigureAwait(false))
                {
                    workItem = await workItemTrackingHttpClient.CreateWorkItemAsync(patchDocument, this.adoProjectName, type).ConfigureAwait(false);

                    int workItemId = (int)workItem.Id;
                    if (!string.IsNullOrWhiteSpace(comment) && workItemId > 0)
                    {
                        var workItemComment = new CommentCreate()
                        {
                            Text = comment
                        };
                        try
                        {
                            var commentAdded = await workItemTrackingHttpClient.AddCommentAsync(workItemComment, this.adoProjectName, workItemId).ConfigureAwait(false);
                            if (commentAdded != null)
                            {
                                this.logger.Information(ComponentName, $"workItem {workItemId}: comment = {comment}");
                            }
                        }
                        catch (Exception ex)
                        {
                            // Since the work item was successfully created, we catch any exceptions when adding the comment,
                            // but do not rethrow them
                            this.logger.Warning(ComponentName, $"Error adding comment to workItem {workItemId}: {ex.Message}");
                        }
                    }
                }

                return workItem;
            }
            catch (VssServiceException vssex)
            {
                this.logger.Error(ComponentName, "Error creating workitem: {0}", vssex.Message);
                throw vssex;
            }
        }

        /// <inheritdoc/>
        public async Task<WorkItemDelete> DeleteWorkItemAsync(int workItemId, bool deleteFromRecycleBin)
        {
            if (this.configuration.EnableNonProdFunctionality)
            {
                try
                {
                    WorkItemDelete workitem;
                    using (var workItemTrackingHttpClient = await this.GetWorkItemTrackingHttpClientAsync().ConfigureAwait(false))
                    {
                        // Deletes a specific work item
                        workitem = await workItemTrackingHttpClient.DeleteWorkItemAsync(this.adoProjectName, workItemId, deleteFromRecycleBin).ConfigureAwait(false);

                        // Outputs the work item id of the deleted workitem if it remains in the recyclebin
                        if (!deleteFromRecycleBin)
                        {
                            this.logger.Information(ComponentName, "Work Item {0} Deleted", workitem.Resource.Id);
                        }
                    }

                    return workitem;
                }
                catch (VssServiceException vssex)
                {
                    this.logger.Error(ComponentName, "Work Item deletion failed: {0}", vssex.Message);
                    throw vssex;
                }
            }

            this.logger.Error(ComponentName, "Should not be able to delete in production environment");
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<List<int>> QueryByWiqlAsync(Wiql wiql)
        {
            try
            {
                List<int> workItemIds;
                using (var workItemTrackingHttpClient = await this.GetWorkItemTrackingHttpClientAsync().ConfigureAwait(false))
                {
                    var workItemQueryResult = await workItemTrackingHttpClient.QueryByWiqlAsync(wiql).ConfigureAwait(false);

                    // some error handling
                    if (workItemQueryResult.WorkItems.Count() != 0)
                    {
                        // parses the workitem ids into an array
                        workItemIds = (from workItem in workItemQueryResult.WorkItems
                                             select workItem.Id).ToList();
                    }
                    else
                    {
                        workItemIds = null;
                    }
                }

                return workItemIds;
            }
            catch (VssServiceException vssex)
            {
                this.logger.Error(ComponentName, vssex.Message);
                throw vssex;
            }
        }

        /// <inheritdoc/>
        public async Task<List<WorkItem>> QueryByWiqlAsync(Wiql wiql, IEnumerable<string> fields)
        {
            try
            {
                List<WorkItem> workItems;
                using (var workItemTrackingHttpClient = await this.GetWorkItemTrackingHttpClientAsync().ConfigureAwait(false))
                {
                    var workItemQueryResult = await workItemTrackingHttpClient.QueryByWiqlAsync(wiql).ConfigureAwait(false);

                    // some error handling
                    if (workItemQueryResult.WorkItems.Count() != 0)
                    {
                        // parses the workitem ids into an array
                        int[] workItemIds = (from workItem in workItemQueryResult.WorkItems
                                             select workItem.Id).ToArray();

                        workItems = await workItemTrackingHttpClient.GetWorkItemsAsync(workItemIds, fields, workItemQueryResult.AsOf).ConfigureAwait(false);
                    }
                    else
                    {
                        workItems = null;
                    }
                }

                return workItems;
            }
            catch (VssServiceException vssex)
            {
                this.logger.Error(ComponentName, vssex.Message);
                throw vssex;
            }
        }

        /// <inheritdoc/>
        public async Task<WorkItem> UpdateWorkItemAsync(JsonPatchDocument document, int workItemId)
        {
            try
            {
                WorkItem workItem;
                using (var workItemTrackingHttpClient = await this.GetWorkItemTrackingHttpClientAsync().ConfigureAwait(false))
                {
                    if (!await this.ValidWorkItemProjectAsync(workItemId).ConfigureAwait(false))
                    {
                        throw new InvalidOperationException($"Tried to update a workitem in the wrong ADO project: Workitem {workItemId} is not in project {this.adoProjectName} ");
                    }
                    else
                    {
                        workItem = await workItemTrackingHttpClient.UpdateWorkItemAsync(document, workItemId).ConfigureAwait(false);
                    }
                }

                return workItem;
            }
            catch (VssServiceException vssex)
            {
                this.logger.Error(ComponentName, vssex.Message);

                throw vssex;
            }
        }

        /// <inheritdoc/>
        public async Task<WorkItem> GetWorkItemAsync(int workItemId)
        {
            try
            {
                WorkItem workItem;
                using (var workItemTrackingHttpClient = await this.GetWorkItemTrackingHttpClientAsync().ConfigureAwait(false))
                {
                    workItem = await workItemTrackingHttpClient.GetWorkItemAsync(workItemId).ConfigureAwait(false);
                }

                return workItem;
            }
            catch (VssServiceException vssex)
            {
                this.logger.Error(ComponentName, vssex.Message);
                throw vssex;
            }
        }

        /// <inheritdoc/>
        public async Task<List<WorkItem>> GetWorkItemsAsync(IEnumerable<int> workItemIds)
        {
            try
            {
                List<WorkItem> workItems;
                using (var workItemTrackingHttpClient = await this.GetWorkItemTrackingHttpClientAsync().ConfigureAwait(false))
                {
                    workItems = await workItemTrackingHttpClient.GetWorkItemsAsync(workItemIds).ConfigureAwait(false);
                }

                return workItems;
            }
            catch (VssServiceException vssex)
            {
                this.logger.Error(ComponentName, vssex.Message);
                throw vssex;
            }
        }

        /// <summary>
        /// Determines if the workitem is a part of this project
        /// </summary>
        /// <param name="workItemId">the workitem id evaluted</param>
        /// <returns>true if this is a valid project </returns>
        private async Task<bool> ValidWorkItemProjectAsync(int workItemId)
        {
            var workItem = await this.GetWorkItemAsync(workItemId).ConfigureAwait(false);

            var adoProject = workItem.Fields.GetValueOrDefault("System.TeamProject");

            if (adoProject.Equals(this.adoProjectName))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private async Task<WorkItemTrackingHttpClient> GetWorkItemTrackingHttpClientAsync()
        {
            VssConnection connection;
            if (this.configuration.ShouldUseAADToken)
            {
                var authResult = await this.aadAppTokenProvider.GetAuthenticationResultAsync().ConfigureAwait(false);
                var aadToken = new VssAadToken(authResult.TokenType, authResult.AccessToken);
                connection = new VssConnection(this.adoProjectUri, new VssAadCredential(aadToken));
            }
            else
            {
                connection = new VssConnection(this.adoProjectUri, new VssBasicCredential(null, this.adoAccessToken));
            }

            return connection.GetClient<WorkItemTrackingHttpClient>();
        }


        private AadAppTokenProvider InitializeAadAppTokenProvider(IFunctionConfiguration config)
        {
            // Set the authentication authority
            string authority = $"https://login.microsoftonline.com/{config.MSTenantId}/v2.0";

            // Set the resource as Azure DevOps
            string resource = "499b84ac-1321-427f-aa17-267ca6975798";
            if (config.AadClientId == null)
            {
                throw new ArgumentNullException("AadClientId");
            }

            if (config.AadClientSecret == null && config.AadClientCert == null)
            {
                throw new ArgumentException("Either AadClientSecret or AadClientCert must be specified.");
            }

            if (config.AadClientSecret != null)
            {
                return new AadAppTokenProvider(authority, config.AadClientId, resource, config.AadClientSecret);
            }
            else
            {
                return new AadAppTokenProvider(authority, config.AadClientId, resource, config.AadClientCert);
            }
        }
    }
}