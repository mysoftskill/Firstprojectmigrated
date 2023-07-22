using System.Collections.Generic;

namespace Microsoft.PrivacyServices.UX.Models.Pdms
{
    /// <summary>
    /// Represents a variant request.
    /// </summary>
    public class VariantRequest
    {
        /// <summary>
        /// Gets or sets the id on the request
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets owner id on the request
        /// </summary>
        public string OwnerId { get; set; }

        /// <summary>
        /// Gets or sets the owner name on the request
        /// </summary>
        public string OwnerName { get; set; }

        /// <summary>
        /// Gets or sets the tracking details on the request
        /// </summary>
        public TrackingDetails TrackingDetails { get; set; }

        /// <summary>
        /// Gets or sets the requested variant associated to the request
        /// </summary>
        public IEnumerable<AssetGroupVariant> RequestedVariants { get; set; }

        /// <summary>
        /// Gets or sets the relationships associated to the request
        /// </summary>
        public IEnumerable<VariantRelationship> VariantRelationships { get; set; }
    }
}
