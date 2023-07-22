namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Client.Filters;

    /// <summary>
    /// Exposes the available APIs for the service that target the DataOwner controller.
    /// This class is internal to force callers to access this through IDataManagementClient.
    /// </summary>
    internal class DataOwnerClient : IDataOwnerClient
    {
        private readonly IHttpServiceProxy httpServiceProxy;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataOwnerClient" /> class.
        /// </summary>
        /// <param name="httpServiceProxy">The <see cref="IHttpServiceProxy" /> to use for interacting with the service.</param>
        public DataOwnerClient(IHttpServiceProxy httpServiceProxy)
        {
            this.httpServiceProxy = httpServiceProxy;
        }

        /// <summary>
        /// Issues a create call for the given data owner
        /// and returns the newly created data owner.
        /// </summary>
        /// <param name="dataOwner">The data owner to create.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The service issued data owner.</returns>
        public async Task<IHttpResult<DataOwner>> CreateAsync(DataOwner dataOwner, RequestContext requestContext)
        {
            var result =
                await this.httpServiceProxy.PostAsync<DataOwner, DataOwner>(
                    "/api/v2/dataOwners",
                    dataOwner,
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get(2);
        }

        /// <summary>
        /// Issues a read call for the given data owner id.
        /// </summary>
        /// <param name="id">The id of the data owner to retrieve.</param>
        /// <param name="requestContext">The request context.</param>
        /// <param name="expandOptions">The set of expansion properties to retrieve.</param>
        /// <returns>The corresponding data owner.</returns>
        public async Task<IHttpResult<DataOwner>> ReadAsync(string id, RequestContext requestContext, DataOwnerExpandOptions expandOptions = DataOwnerExpandOptions.None)
        {
            string url = $"/api/v2/dataOwners('{id}'){GetDataOwnerExpandOptions(expandOptions)}";

            var result =
                await this.httpServiceProxy.GetAsync<DataOwner>(
                    url,
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get(2);
        }

        /// <summary>
        /// Issues a read call that retrieves all known data owners.
        /// If the number of existing data owners is larger than the configured server-side max page size,
        /// then only the first page data owners are returned.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="expandOptions">The set of expansion properties to retrieve.</param>
        /// <param name="filterCriteria">The data owner filter criteria.</param>
        /// <returns>A collection result with all the returned data owners, total number of existing data owners and possible next page link.</returns>
        public async Task<IHttpResult<Collection<DataOwner>>> ReadByFiltersAsync(RequestContext requestContext, DataOwnerExpandOptions expandOptions = DataOwnerExpandOptions.None, DataOwnerFilterCriteria filterCriteria = null)
        {
            string url = $"/api/v2/dataOwners{GetDataOwnerExpandOptions(expandOptions)}{GetDataOwnerFilterCriteria(filterCriteria)}";

            var result =
                await this.httpServiceProxy.GetAsync<Collection<DataOwner>>(
                    url,
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get(2);
        }

        /// <summary>
        /// Issues a read call that retrieves all known data owners. Automatically handles any service side paging.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="expandOptions">The set of expansion properties to retrieve.</param>
        /// <param name="filterCriteria">The data owner filter criteria.</param>
        /// <returns>All available data owners.</returns>
        public Task<IHttpResult<IEnumerable<DataOwner>>> ReadAllByFiltersAsync(RequestContext requestContext, DataOwnerExpandOptions expandOptions = DataOwnerExpandOptions.None, DataOwnerFilterCriteria filterCriteria = null)
        {
            return DataManagementClient.ReadMany<DataOwner>(
                $"/api/v2/dataOwners{GetDataOwnerExpandOptions(expandOptions)}{GetDataOwnerFilterCriteria(filterCriteria)}",
                requestContext,
                this.httpServiceProxy);
        }

        /// <summary>
        /// Returns all data owners for which the authenticated user has a matching security group.
        /// Returns an empty collection if no matching data owners are found.
        /// Automatically handles any service side paging.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="expandOptions">The set of expansion properties to retrieve.</param>
        /// <returns>All available data owners for the authenticated user.</returns>
        public Task<IHttpResult<IEnumerable<DataOwner>>> FindAllByAuthenticatedUserAsync(RequestContext requestContext, DataOwnerExpandOptions expandOptions = DataOwnerExpandOptions.None)
        {
            return DataManagementClient.ReadMany<DataOwner>(
                $"/api/v2/dataOwners/v2.findByAuthenticatedUser{GetDataOwnerExpandOptions(expandOptions)}",
                requestContext,
                this.httpServiceProxy);
        }

        /// <summary>
        /// Issues an update call for the given data owner
        /// and returns the updated data owner.
        /// </summary>
        /// <param name="dataOwner">The data owner to create.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The service issued data owner.</returns>
        public async Task<IHttpResult<DataOwner>> UpdateAsync(DataOwner dataOwner, RequestContext requestContext)
        {
            var result =
                await this.httpServiceProxy.PutAsync<DataOwner, DataOwner>(
                    $"/api/v2/dataOwners('{dataOwner.Id}')",
                    dataOwner,
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get(2);
        }

        /// <summary>
        /// Deletes the data owner that has the corresponding
        /// serviceTree.serviceId and merges that owner's properties
        /// with the provided data owner.
        /// </summary>
        /// <param name="dataOwner">The data owner to create.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The data owner with properties updated by the service.</returns>
        public async Task<IHttpResult<DataOwner>> ReplaceServiceIdAsync(DataOwner dataOwner, RequestContext requestContext)
        {
            var result =
                await this.httpServiceProxy.PostAsync<object, DataOwner>(
                    $"/api/v2/dataOwners('{dataOwner.Id}')/v2.replaceServiceId",
                    new { value = dataOwner },
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get(2);
        }

        /// <summary>
        /// Issues a delete call with the given data owner id and ETag.
        /// </summary>
        /// <param name="id">The id of the data owner to delete.</param>
        /// <param name="etag">The ETag of the data owner.</param>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The task to perform the operation.</returns>
        public async Task<IHttpResult> DeleteAsync(string id, string etag, RequestContext requestContext)
        {
            string url = $"/api/v2/dataOwners('{id}')";

            var headers = requestContext.GetHeaders();
            if (!string.IsNullOrWhiteSpace(etag))
            {
                headers.Add("If-Match", () => Task.FromResult(etag));
            }

            var result =
                await this.httpServiceProxy.DeleteAsync(
                    url,
                    headers,
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get(2);
        }

        /// <summary>
        /// Get expand options to be used in url from DataOwnerExpandOptions.
        /// </summary>
        /// <param name="expandOptions">Data owner expand options.</param>
        /// <returns>Data owner expand options in string format.</returns>
        private static string GetDataOwnerExpandOptions(DataOwnerExpandOptions expandOptions)
        {
            var queryString = "?$select=id,eTag,name,description,alertContacts,announcementContacts,sharingRequestContacts,writeSecurityGroups,tagSecurityGroups,tagApplicationIds,icm,hasInitiatedTransferRequests,hasPendingTransferRequests";

            if (expandOptions == DataOwnerExpandOptions.None)
            {
                return queryString;
            }
            else
            {
                ////bool hasAssetGroups = expandOptions.HasFlag(DataOwnerExpandOptions.AssetGroups);
                ////bool hasDataAgents = expandOptions.HasFlag(DataOwnerExpandOptions.DataAgents);

                if (expandOptions.HasFlag(DataOwnerExpandOptions.TrackingDetails))
                {
                    queryString += ",trackingDetails";
                }

                if (expandOptions.HasFlag(DataOwnerExpandOptions.ServiceTree))
                {
                    queryString += ",serviceTree";
                }

                ////if (hasAssetGroups || hasDataAgents)
                ////{
                ////    var expand = "&$expand=";

                ////    if (hasAssetGroups)
                ////    {
                ////        expand += "assetGroups";
                ////        queryString += ",assetGroups";
                ////    }

                ////    if (hasDataAgents)
                ////    {
                ////        if (hasAssetGroups)
                ////        {
                ////            expand += ",";
                ////        }

                ////        expand += "dataAgents";
                ////        queryString += ",dataAgents";
                ////    }

                ////    queryString += expand;
                ////}

                return queryString;
            }
        }

        /// <summary>
        /// Get filter criteria to be used in url from DataOwnerFilterCriteria.
        /// </summary>
        /// <param name="filterCriteria">Data owner filter criteria.</param>
        /// <returns>Data owner filter criteria in string format.</returns>
        private static string GetDataOwnerFilterCriteria(IFilterCriteria filterCriteria)
        {
            return filterCriteria == null ? string.Empty : "&" + filterCriteria.BuildRequestString();
        }
    }
}