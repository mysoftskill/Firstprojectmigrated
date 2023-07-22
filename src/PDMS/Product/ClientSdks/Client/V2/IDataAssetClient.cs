namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.Identity;

    /// <summary>
    /// Exposes the available APIs for the service that target the DataAsset controller.
    /// </summary>
    public interface IDataAssetClient
    {
        /// <summary>
        /// Issues a read call for the given qualifier.
        /// PDMS will call DataGrid with the specified qualifier and return all the corresponding data assets.
        /// </summary>
        /// <param name="qualifier">The asset qualifier upon which to search.</param>
        /// <param name="requestContext">The request context.</param>
        /// <param name="filterCriteria">The data asset filter criteria.</param>
        /// <param name="useSearchPropertiesAssetTypes">Comma-separated list of assets types that use the search properties when calling DataGrid.</param>
        /// <returns>The qualified data assets.</returns>
        Task<IHttpResult<Collection<DataAsset>>> FindByQualifierAsync(AssetQualifier qualifier, RequestContext requestContext, DataAssetFilterCriteria filterCriteria = null, string useSearchPropertiesAssetTypes = "");
    }
}