namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using Newtonsoft.Json;

    /// <summary>
    /// An incident for ICM.
    /// </summary>
    public class Incident
    {
        /// <summary>
        /// Gets or sets the incident id.
        /// The is the only value returned in the response for the CreateIncident API.
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public long Id { get; set; }
        
        /// <summary>
        /// Gets or sets the incident alert source id.
        /// This value is returned in the response for the CreateIncident API.
        /// </summary>
        [JsonProperty(PropertyName = "alertSourceId")]
        public string AlertSourceId { get; set; }

        /// <summary>
        /// Gets or sets the routing information. 
        /// This is the set of properties we offer to partners for establishing routing rules in their ICM tenants.
        /// </summary>
        [JsonProperty(PropertyName = "routing")]
        public RouteData Routing { get; set; }

        /// <summary>
        /// Gets or sets the severity of the incident.
        /// </summary>
        [JsonProperty(PropertyName = "severity")]
        public int Severity { get; set; }

        /// <summary>
        /// Gets or sets a title for the incident.
        /// </summary>
        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the body of the incident. Must be valid XHTML.
        /// It may also contain string.Format parameters for simple substitutions.
        /// The supported substitutions are:
        /// {0}: OwnerName.
        /// {1}: AgentName.
        /// {2}: AgentOwnerName.
        /// {3}: AssetGroupQualifier.
        /// {4}: AssetGroupOwnerName.
        /// {5}: OwnerId.
        /// {6}: AgentId.
        /// {7}: AssetGroupId.
        /// </summary>
        [JsonProperty(PropertyName = "body")]
        public string Body { get; set; }

        /// <summary>
        /// Gets or sets the keywords.
        /// </summary>
        [JsonProperty(PropertyName = "keywords")]
        public string Keywords { get; set; }

        /// <summary>
        /// Gets or sets custom input parameters.
        /// </summary>
        [JsonProperty(PropertyName = "inputParameters")]
        public IncidentInputParameters InputParameters { get; set; }

        /// <summary>
        /// Gets or sets additional response metadata from ICM.
        /// </summary>
        [JsonProperty(PropertyName = "responseMetadata")]
        public IncidentResponseMetadata ResponseMetadata { get; set; }
    }
}