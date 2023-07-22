namespace Microsoft.PrivacyServices.DataManagement.DataAccess.ChangeFeedReader.Models
{
    using System;

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
        public Guid VariantId { get; set; }
    }
}