namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2
{
    using System.Collections.Generic;

    using Newtonsoft.Json;

    /// <summary>
    /// This object defines the payload for the V2.AssetGroup.RemoveVariants API.
    /// </summary>
    public class RemoveVariantsParameters
    {
        /// <summary>
        /// The list of VariantIds to remove from the AssetGroup.
        /// </summary>
        [JsonProperty(PropertyName = "variantIds")]
        public IEnumerable<string> VariantIds { get; set; }
    }
}
