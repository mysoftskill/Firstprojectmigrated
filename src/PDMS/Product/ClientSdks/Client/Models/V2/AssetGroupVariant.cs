namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    /// <summary>
    /// The asset group variant object that links asset groups with variant definitions.
    /// </summary>
    public class AssetGroupVariant
    {
        /// <summary>
        /// Gets or sets the variant id. Links to an approved variant definition.
        /// </summary>
        [JsonProperty(PropertyName = "variantId")]
        public string VariantId { get; set; }

        /// <summary>
        /// Name of the variant
        /// </summary>
        [JsonProperty(PropertyName = "variantName")]
        public string VariantName { get; set; }

        /// <summary>
        /// Gets or sets the variant state. The current state of the variant. Could be Requested, Approved, Deprecated or Rejected.
        /// </summary>
        [JsonProperty(PropertyName = "variantState")]
        public VariantState VariantState { get; set; }

        /// <summary>
        /// Gets or sets the date at which this variant can no longer be used.
        /// </summary>
        [JsonProperty(PropertyName = "variantExpiryDate")]
        public DateTimeOffset? VariantExpiryDate { get; set; }

        /// <summary>
        /// Gets or sets the variant TFS tracking URIs. List of TFS item URIs which relate to this variant.
        /// </summary>
        [JsonProperty(PropertyName = "tfsTrackingUris")]
        public IEnumerable<string> TfsTrackingUris { get; set; }

        /// <summary>
        /// The variant EGRC Id.
        /// </summary>
        [JsonProperty(PropertyName = "egrcId")]
        public string EgrcId { get; set; }

        /// <summary>
        /// The variant EGRC Name.
        /// </summary>
        [JsonProperty(PropertyName = "egrcName")]
        public string EgrcName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether signals should be filtered for this variant.
        /// </summary>
        [JsonProperty(PropertyName = "disableSignalFiltering")]
        public bool DisableSignalFiltering { get; set; }
    }
}