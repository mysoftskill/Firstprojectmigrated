namespace Microsoft.PrivacyServices.DataManagement.DataAccess.ChangeFeedReader.Models
{
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// The service tree hierarchy level.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ServiceTreeLevel
    {
        /// <summary>
        /// Represents the Service node.
        /// </summary>
        Service = 0,

        /// <summary>
        /// Represents the TeamGroup node.
        /// </summary>
        TeamGroup = 1,

        /// <summary>
        /// Represents the ServiceGroup node.
        /// </summary>
        ServiceGroup = 2
    }

    /// <summary>
    /// Defines meta data for service returned from the service tree.
    /// </summary>
    public class ServiceTree
    {
        /// <summary>
        /// Gets or sets the service admin list.
        /// </summary>
        [JsonProperty(PropertyName = "serviceAdmins")]
        public IEnumerable<string> ServiceAdmins { get; set; }

        /// <summary>
        /// Gets or sets the service division ID.
        /// </summary>
        [JsonProperty(PropertyName = "divisionId")]
        public string DivisionId { get; set; }

        /// <summary>
        /// Gets or sets the service division name.
        /// </summary>
        [JsonProperty(PropertyName = "divisionName")]
        public string DivisionName { get; set; }

        /// <summary>
        /// Gets or sets the service organization ID.
        /// </summary>
        [JsonProperty(PropertyName = "organizationId")]
        public string OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the service organization name.
        /// </summary>
        [JsonProperty(PropertyName = "organizationName")]
        public string OrganizationName { get; set; }

        /// <summary>
        /// Gets or sets the service group ID.
        /// </summary>
        [JsonProperty(PropertyName = "serviceGroupId")]
        public string ServiceGroupId { get; set; }

        /// <summary>
        /// Gets or sets the service group name.
        /// </summary>
        [JsonProperty(PropertyName = "serviceGroupName")]
        public string ServiceGroupName { get; set; }

        /// <summary>
        /// Gets or sets the service team group ID.
        /// </summary>
        [JsonProperty(PropertyName = "teamGroupId")]
        public string TeamGroupId { get; set; }

        /// <summary>
        /// Gets or sets the service team group name.
        /// </summary>
        [JsonProperty(PropertyName = "teamGroupName")]
        public string TeamGroupName { get; set; }

        /// <summary>
        /// Gets or sets the service ID.
        /// </summary>
        [JsonProperty(PropertyName = "serviceId")]
        public string ServiceId { get; set; }

        /// <summary>
        /// Gets or sets the service name.
        /// </summary>
        [JsonProperty(PropertyName = "serviceName")]
        public string ServiceName { get; set; }

        /// <summary>
        /// Gets or sets the level of the hierarchy that is set.
        /// All ids below this level will be empty strings.
        /// </summary>
        [JsonProperty(PropertyName = "level")]
        public ServiceTreeLevel Level { get; set; }
    }
}