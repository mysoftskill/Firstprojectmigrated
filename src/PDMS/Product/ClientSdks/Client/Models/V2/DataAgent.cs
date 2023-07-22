[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1623:PropertySummaryDocumentationMustMatchAccessors", Justification = "Swagger documentation.")]

namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System;
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.Policy;

    using Newtonsoft.Json;

    /// <summary>
    /// Defines the metadata necessary for contacting a partner team's system.
    /// </summary>
    [DerivedType(typeof(DeleteAgent))]
    [JsonConverter(typeof(DerivedTypeConverter), "#v2")]
    public abstract class DataAgent : NamedEntity
    {
        private static readonly DictionaryConverter<ReleaseState, ConnectionDetail>.KeySelector KeySelector = c => c.ReleaseState;

        private static readonly DictionaryConverter<ReleaseState, ConnectionDetail>.KeyApplicator KeyApplicator = (c, v) => v.ReleaseState = c;

        /// <summary>
        /// The OData type. Used for <c>deserializing</c> into the proper derived type.
        /// </summary>
        [JsonProperty(PropertyName = "@odata.type", Order = -1)]
        public abstract string ODataType { get; }

        /// <summary>
        /// This contains the specific metadata required by the given protocol. Multiple values are supported so that changes can be tested in <c>preprod</c> before applying them to production.
        /// </summary>
        [JsonConverter(typeof(DictionaryConverter<ReleaseState, ConnectionDetail>), typeof(DataAgent), nameof(KeySelector), nameof(KeyApplicator))]
        [JsonProperty(PropertyName = "connectionDetails")]
        public IDictionary<ReleaseState, ConnectionDetail> ConnectionDetails { get; set; }

        /// <summary>
        /// This contains the specific metadata required for migrating from V1 agent protocol to V2 agent protocol. 
        /// Multiple values are supported so that changes can be tested in <c>preprod</c> before applying them to production.
        /// </summary>
        [JsonConverter(typeof(DictionaryConverter<ReleaseState, ConnectionDetail>), typeof(DataAgent), nameof(KeySelector), nameof(KeyApplicator))]
        [JsonProperty(PropertyName = "migratingConnectionDetails")]
        public IDictionary<ReleaseState, ConnectionDetail> MigratingConnectionDetails { get; set; }

        /// <summary>
        /// The capabilities for this agent.
        /// </summary>
        [JsonProperty(PropertyName = "capabilities")]
        public IEnumerable<CapabilityId> Capabilities { get; set; }

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