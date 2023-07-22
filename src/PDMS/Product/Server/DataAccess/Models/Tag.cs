namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2
{
    using Newtonsoft.Json;

    /// <summary>
    /// One of a set of predefined values that can be applied to a DataAsset.
    /// </summary>
    public class Tag
    {
        /// <summary>
        /// The tag name. Must be unique across tags.
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
    }
}