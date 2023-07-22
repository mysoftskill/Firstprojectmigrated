namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// The set of routing data that is available to partners.
    /// Not all fields will be available in every incident.
    /// OwnerId will always be present, if it is not set by the caller,
    /// then it will be filled in by the service based on the AgentId or AssetGroupId.
    /// The Agent owner will take precedence over the AssetGroup owner.
    /// </summary>
    public class RouteData
    {
        /// <summary>
        /// Gets or sets the event name. This is mapped into the correlation ID in ICM.
        /// This must be set from a predefined list of all known alerts that NGP teams will issue.
        /// A second incident to the same event name will be correlated together.
        /// </summary>
        [JsonProperty(PropertyName = "eventName")]
        public string EventName { get; set; }

        /// <summary>
        /// Gets or sets the owner ID. This is mapped into the routing id in ICM.
        /// This is expected to be the primary routing value for partner teams.
        /// All partners are encouraged to use this value to route the corresponding 
        /// ICM team.
        /// </summary>
        [JsonProperty(PropertyName = "ownerId")]
        public Guid? OwnerId { get; set; }

        /// <summary>
        /// Gets or sets the agent ID. This is mapped into the data center in ICM (a.k.a. DC).
        /// This may or may not always be present depending on the type of incident.
        /// Partners should consider using this if the team in PCD represents many teams in ICM.
        /// </summary>
        [JsonProperty(PropertyName = "agentId")]
        public Guid? AgentId { get; set; }

        /// <summary>
        /// Gets or sets the asset group ID. This is mapped into the device group in ICM (a.k.a. role).
        /// This may or may not always be present depending on the type of incident.
        /// Partners should consider using this if the team in PCD represents many teams in ICM.
        /// Partners that are acting on behalf of other teams may also want to use this as a way to
        /// redirect incidents to the appropriate asset group owner.
        /// </summary>
        [JsonProperty(PropertyName = "assetGroupId")]
        public Guid? AssetGroupId { get; set; }
    }
}