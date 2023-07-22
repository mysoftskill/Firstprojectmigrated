namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    /// <summary>
    /// Defines the metadata necessary for contacting a partner team's system.
    /// </summary>
    [DerivedType(typeof(DeleteAgent))]
    [JsonConverter(typeof(DerivedTypeConverter), "#v2")]
    public abstract class DataAgent : NamedEntity
    {
        /// <summary>
        /// This contains the specific metadata required by the given protocol. Multiple values are supported so that changes can be tested in <c>preprod</c> before applying them to production.
        /// </summary>
        [JsonProperty(PropertyName = "connectionDetails")]
        public IEnumerable<ConnectionDetail> ConnectionDetails { get; set; }

        /// <summary>
        /// This contains the specific metadata required for migrating from V1 agent protocol to V2 agent protocol. 
        /// Multiple values are supported so that changes can be tested in <c>preprod</c> before applying them to production.
        /// </summary>
        [JsonProperty(PropertyName = "migratingConnectionDetails")]
        public IEnumerable<ConnectionDetail> MigratingConnectionDetails { get; set; }

        /// <summary>
        /// The capabilities for this agent.
        /// </summary>
        [JsonProperty(PropertyName = "capabilities")]
        public IEnumerable<string> Capabilities { get; set; }

        /// <summary>
        /// The id of the associated agent owner.
        /// </summary>
        [JsonProperty(PropertyName = "ownerId")]
        public string OwnerId { get; set; }

        /// <summary>
        /// The associated agent owner. Must use $expand to retrieve these.
        /// </summary>
        [JsonProperty(PropertyName = "owner")]
        public DataOwner Owner { get; set; }

        /// <summary>
        /// OperationalReadinessLow and OperationalReadinessHigh combined to specify up to 128
        /// booleans representing agent operational readiness.
        /// </summary>
        [JsonProperty(PropertyName = "operationalReadinessLow")]
        public long OperationalReadinessLow { get; set; }

        /// <summary>
        /// OperationalReadinessLow and OperationalReadinessHigh combined to specify up to 128
        /// booleans representing agent operational readiness.
        /// </summary>
        [JsonProperty(PropertyName = "operationalReadinessHigh")]
        public long OperationalReadinessHigh { get; set; }

        /// <summary>
        /// ICM meta data.
        /// </summary>
        [JsonProperty(PropertyName = "icm")]
        public Icm Icm { get; set; }

        /// <summary>
        /// The date agent was Prod-Ready.
        /// </summary>
        [JsonProperty(PropertyName = "inProdDate")]
        public DateTimeOffset? InProdDate { get; set; }
    }
}