namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2
{
    using System.ComponentModel.DataAnnotations;

    using Newtonsoft.Json;

    /// <summary>
    /// Defines asset information retrieved from DataGrid.
    /// </summary>
    public class DataAsset
    {
        /// <summary>
        /// The id for the data asset. This is the DataGrid identifier for DataGrid point queries.
        /// </summary>
        [Key]
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// The set of properties that are necessary to uniquely identify a Data Asset in DataGrid.
        /// </summary>
        [JsonProperty(PropertyName = "qualifier")]
        public string Qualifier { get; set; }
    }
}