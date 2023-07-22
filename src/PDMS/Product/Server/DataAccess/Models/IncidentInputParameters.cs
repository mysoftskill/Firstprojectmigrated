namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2
{
    using Newtonsoft.Json;

    /// <summary>
    /// Defines additional input parameters for altering the behavior of the API.
    /// If not provided, then defaults are used for all fields.
    /// </summary>
    public class IncidentInputParameters
    {
        /// <summary>
        /// Gets or sets a value indicating whether or not string substitutions are disabled for the title.
        /// </summary>
        [JsonProperty(PropertyName = "disableTitleSubstitutions")]
        public bool DisableTitleSubstitutions { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not string substitutions are disabled for the body.
        /// </summary>
        [JsonProperty(PropertyName = "disableBodySubstitutions")]
        public bool DisableBodySubstitutions { get; set; }
    }
}
