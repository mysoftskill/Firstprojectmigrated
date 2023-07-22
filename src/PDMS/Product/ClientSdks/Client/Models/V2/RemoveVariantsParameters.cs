[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1623:PropertySummaryDocumentationMustMatchAccessors", Justification = "Swagger documentation.")]

namespace Microsoft.PrivacyServices.DataManagement.Client.V2
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
