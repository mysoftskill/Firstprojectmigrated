namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// This contains the specific metadata required by a given protocol.
    /// </summary>
    public class ConnectionDetail
    {
        /// <summary>
        /// A protocol id pulled from the privacy policy library. Once set, this value is immutable.
        /// </summary>
        [JsonProperty(PropertyName = "protocol")]
        public string Protocol { get; set; }

        /// <summary>
        /// Defines the authentication mechanism used for interacting with the agent.
        /// </summary>
        [JsonProperty(PropertyName = "authenticationType")]
        public AuthenticationType? AuthenticationType { get; set; }

        /// <summary>
        /// The MSA site id. Required if <c>MsaSiteBasedAuth</c> is the selected AuthenticationType.
        /// </summary>
        [JsonProperty(PropertyName = "msaSiteId")]
        public long? MsaSiteId { get; set; }

        /// <summary>
        /// The AAD application id. Required if <c>AadAppBasedAuth</c> is the selected AuthenticationType.
        /// If there is more than one AadAppId, this stores the most recent AadAppId.
        /// </summary>
        [JsonProperty(PropertyName = "aadAppId")]
        public Guid? AadAppId { get; set; }

        /// <summary>
        /// An enumerable group of multiple AAD application ids.
        /// </summary>
        [JsonProperty(PropertyName = "aadAppIds")]
        public IEnumerable<Guid> AadAppIds { get; set; }

        /// <summary>
        /// Identifies the readiness of the connection details. 
        /// Prod state is required before the agent can be used in that environment. 
        /// <c>Preprod</c> will only be available in the <c>preprod</c> environment. 
        /// You must promote connection details from <c>preprod</c> to prod. 
        /// That action will replace the existing prod details (if available). 
        /// If no <c>preprod</c> details exist, then the prod details are used in the <c>preprod</c> environment.
        /// </summary>
        [JsonProperty(PropertyName = "releaseState")]
        public ReleaseState ReleaseState { get; set; }

        /// <summary>
        /// Identifies the readiness of agent.
        /// </summary>
        [JsonProperty(PropertyName = "agentReadiness")]
        public AgentReadiness AgentReadiness { get; set; }
    }
}