using System;
using System.Collections.Generic;
using Microsoft.PrivacyServices.DataManagement.Client.V2;

namespace Microsoft.PrivacyServices.UX.Models.Pdms
{
    /// <summary>
    /// Represents an asset group variant.
    /// </summary>
    public class AssetGroupVariant
    {
        /// <summary>
        /// Gets or sets variant ID.
        /// </summary>
        public string VariantId { get; set; }

        /// <summary>
        /// Gets or sets variant name. This is static metadata and not kept in sync by the service.
        /// </summary>
        public string VariantName { get; set; }

        /// <summary>
        /// Gets or sets the list of tracking TFS item URIs which relate to this variant.
        /// </summary>
        public IEnumerable<string> TfsTrackingUris { get; set; }

        /// <summary>
        /// Gets or sets the variant state. 
        /// </summary>
        public VariantState VariantState { get; set; }

        /// <summary>
        /// Gets or sets the date at which this variant can no longer be used.
        /// </summary>
        public DateTimeOffset? VariantExpiryDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether signals should be filtered for this variant.
        /// </summary>
        public bool DisabledSignalFiltering { get; set; }

    }
}
