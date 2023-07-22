namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.Identity;

    /// <summary>
    /// Exposes the available APIs for the service that target the DataAsset controller.
    /// </summary>
    internal class DataAssetClient : IDataAssetClient
    {
        private readonly IHttpServiceProxy httpServiceProxy;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataAssetClient" /> class.
        /// </summary>
        /// <param name="httpServiceProxy">The <see cref="IHttpServiceProxy" /> to use for interacting with the service.</param>
        public DataAssetClient(IHttpServiceProxy httpServiceProxy)
        {
            this.httpServiceProxy = httpServiceProxy;
        }

        /// <summary>
        /// Issues a read call for the given qualifier.
        /// PDMS will call DataGrid with the specified qualifier and return all the corresponding data assets.
        /// </summary>
        /// <param name="qualifier">The asset qualifier upon which to search.</param>
        /// <param name="requestContext">The request context.</param>
        /// <param name="filterCriteria">The data asset filter criteria.</param>
        /// <param name="useSearchPropertiesAssetTypes">Comma-separated list of assets types that use the search properties when calling DataGrid.</param>
        /// <returns>The qualified data assets.</returns>
        public async Task<IHttpResult<Collection<DataAsset>>> FindByQualifierAsync(AssetQualifier qualifier, RequestContext requestContext, DataAssetFilterCriteria filterCriteria = null, string useSearchPropertiesAssetTypes = "")
        {
            var searchValue = qualifier.GetValueForSearch(useSearchPropertiesAssetTypes);
            string url = $"/api/v2/dataAssets/v2.findByQualifier(qualifier=@value)?@value='{SerializerSettings.EscapeForODataQuery(searchValue)}'{GetDataAssetFilterCriteria(filterCriteria)}";

            var result =
                await this.httpServiceProxy.GetAsync<Collection<DataAsset>>(
                    url,
                    requestContext.GetHeaders(),
                    requestContext.CancellationToken).ConfigureAwait(false);

            return result.Get(2);
        }

        /// <summary>
        /// Get filter criteria to be used in url from DataAssetFilterCriteria.
        /// </summary>
        /// <param name="filterCriteria">Data asset filter criteria.</param>
        /// <returns>Data asset filter criteria in string format.</returns>
        private static string GetDataAssetFilterCriteria(DataAssetFilterCriteria filterCriteria)
        {
            return filterCriteria == null ? string.Empty : "&" + filterCriteria.BuildRequestString();
        }
    }
}