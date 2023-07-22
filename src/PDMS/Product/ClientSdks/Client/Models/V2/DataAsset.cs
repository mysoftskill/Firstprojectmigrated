[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1623:PropertySummaryDocumentationMustMatchAccessors", Justification = "Swagger documentation.")]

namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using Microsoft.PrivacyServices.Identity;

    using Newtonsoft.Json;

    /// <summary>
    /// Defines asset information retrieved from DataGrid.
    /// </summary>
    public class DataAsset
    {
        /// <summary>
        /// The id for the data asset. This is the DataGrid identifier for DataGrid point queries.
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// The set of properties that are necessary to uniquely identify a Data Asset in DataGrid.
        /// </summary>
        [JsonProperty(PropertyName = "qualifier")]
        [JsonConverter(typeof(AssetQualifierConverter))]
        public AssetQualifier Qualifier { get; set; }
    }
}