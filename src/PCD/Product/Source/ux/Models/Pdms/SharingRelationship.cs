using System.Collections.Generic;
using Microsoft.PrivacyServices.Policy;

namespace Microsoft.PrivacyServices.UX.Models.Pdms
{
    /// <summary>
    /// Represents a sharing relationship
    /// </summary>
    public class SharingRelationship
    {
        /// <summary>
        /// Gets or sets data asset group id
        /// </summary>
        public string AssetGroupId { get; set; }

        /// <summary>
        /// Gets or sets data asset group qualifier.
        /// </summary>
        public AssetGroupQualifier AssetGroupQualifier { get; set; }

        /// <summary>
        /// Gets or sets the request capabilities.
        /// </summary>
        public IEnumerable<CapabilityId> Capabilities { get; set; }
    }
}

