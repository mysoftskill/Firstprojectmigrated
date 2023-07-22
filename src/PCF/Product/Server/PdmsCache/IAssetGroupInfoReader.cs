namespace Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache
{
    using System.Threading.Tasks;

    /// <summary>
    /// Defines an interface capabile of reading asset group information from an asynchronous source.
    /// </summary>
    public interface IAssetGroupInfoReader
    {
        /// <summary>
        /// Gets the latest version of the data set.
        /// </summary>
        Task<long> GetLatestVersionAsync();

        /// <summary>
        /// Retreives the latest set of AssetGroupInfo objects from the source of this collection.
        /// </summary>
        Task<AssetGroupInfoCollectionReadResult> ReadAsync();

        /// <summary>
        /// Retrieves the set of asset group information for the given version.
        /// </summary>
        Task<AssetGroupInfoCollectionReadResult> ReadVersionAsync(long version);
    }
}
