using Microsoft.PrivacyServices.UX.Models.Pdms;

namespace Microsoft.PrivacyServices.UX.Core.Search
{
    /// <summary>
    /// Search result that corresponds to an asset group PDMS entity.
    /// </summary>
    public class SearchAssetGroupResult : SearchResultBase
    {
        /// <summary>
        /// Gets or sets entity ID.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets ID of the entity's owner. Not set, if entity doesn't have an owner.
        /// </summary>
        public string OwnerId { get; set; }

        /// <summary>
        /// Gets or sets delete agent Id.
        /// </summary>
        public string DeleteAgentId { get; set; }

        /// <summary>
        /// Gets or sets export agent Id.
        /// </summary>
        public string ExportAgentId { get; set; }

        /// <summary>
        /// Gets or sets asset group qualifier.
        /// </summary>
        public AssetGroupQualifier Qualifier { get; set; }
    }
}
