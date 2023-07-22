namespace Microsoft.PrivacyServices.AzureFunctions.Common.Models
{
    using Newtonsoft.Json;

    /// <summary>
    /// Defines a message passed from PDMS to indicate that a new request has been created.
    /// </summary>
    public class VariantRequestMessage
    {
        /// <summary>
        /// The unique Id for this variant request.
        /// </summary>
        [JsonProperty(PropertyName = "variantRequestId")]
        public string VariantRequestId { get; set; }
    }
}