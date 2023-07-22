namespace Microsoft.PrivacyServices.DataManagement.DataGridService
{
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Models.Filters;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;
    using Microsoft.PrivacyServices.Identity;

    public interface IDataAssetProvider
    {
        /// <summary>
        /// Find data assets based on the asset qualifier.
        /// </summary>
        /// <param name="filterCriteria">Filter criteria for the data asset.</param>
        /// <param name="qualifier">The asset qualifier to search on.</param>
        /// <param name="includeTags">Whether or not tag information should be retrieved.</param>
        /// <returns>Data assets matching the qualifier.</returns>
        Task<FilterResult<DataAsset>> FindDataAssetsByQualifierAsync(DataAssetFilterCriteria filterCriteria, AssetQualifier qualifier, bool includeTags);
    }
}
