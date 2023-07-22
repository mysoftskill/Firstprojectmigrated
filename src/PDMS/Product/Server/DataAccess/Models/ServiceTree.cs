namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// The service tree hierarchy level.
    /// </summary>
    [JsonConverter(typeof(EnumTolerantConverter<ServiceTreeLevel>))]
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
        /// The service admin list.
        /// </summary>
        [JsonProperty(PropertyName = "serviceAdmins")]
        public IEnumerable<string> ServiceAdmins { get; set; }

        /// <summary>
        /// The service division ID.
        /// </summary>
        [JsonProperty(PropertyName = "divisionId")]
        public string DivisionId { get; set; }

        /// <summary>
        /// The service division name.
        /// </summary>
        [JsonProperty(PropertyName = "divisionName")]
        public string DivisionName { get; set; }

        /// <summary>
        /// The service organization ID.
        /// </summary>
        [JsonProperty(PropertyName = "organizationId")]
        public string OrganizationId { get; set; }

        /// <summary>
        /// The service organization name.
        /// </summary>
        [JsonProperty(PropertyName = "organizationName")]
        public string OrganizationName { get; set; }

        /// <summary>
        /// The service group ID.
        /// </summary>
        [JsonProperty(PropertyName = "serviceGroupId")]
        public string ServiceGroupId { get; set; }

        /// <summary>
        /// The service group name.
        /// </summary>
        [JsonProperty(PropertyName = "serviceGroupName")]
        public string ServiceGroupName { get; set; }

        /// <summary>
        /// The service team group ID.
        /// </summary>
        [JsonProperty(PropertyName = "teamGroupId")]
        public string TeamGroupId { get; set; }

        /// <summary>
        /// The service team group name.
        /// </summary>
        [JsonProperty(PropertyName = "teamGroupName")]
        public string TeamGroupName { get; set; }

        /// <summary>
        /// The service ID.
        /// </summary>
        [JsonProperty(PropertyName = "serviceId")]
        public string ServiceId { get; set; }

        /// <summary>
        /// The service name.
        /// </summary>
        [JsonProperty(PropertyName = "serviceName")]
        public string ServiceName { get; set; }

        /// <summary>
        /// The level of the hierarchy that is set.
        /// All ids below this level will be empty strings.
        /// </summary>
        [JsonProperty(PropertyName = "level")]
        public ServiceTreeLevel Level { get; set; }
    }
}