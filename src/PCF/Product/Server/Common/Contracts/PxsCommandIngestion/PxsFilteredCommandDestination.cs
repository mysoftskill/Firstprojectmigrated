namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System.Collections.Generic;
    using Microsoft.PrivacyServices.Policy;
    using Newtonsoft.Json;

    /// <summary>
    /// Defines information about a prefiltered destination for a command, along with all relevant metadata.
    /// </summary>
    public sealed class PxsFilteredCommandDestination
    {
        /// <summary>
        /// The agent ID.
        /// </summary>
        public AgentId AgentId { get; set; }

        /// <summary>
        /// The asset group ID.
        /// </summary>
        public AssetGroupId AssetGroupId { get; set; }

        /// <summary>
        /// The asset group qualifier.
        /// </summary>
        public string AssetGroupQualifier { get; set; }

        /// <summary>
        /// The moniker that the command should be inserted into. Moniker is fixed to ensure idempotence of downstream work items.
        /// </summary>
        public string TargetMoniker { get; set; }

        /// <summary>
        /// The set of data types. Only useful for export.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public IList<DataTypeId> DataTypes { get; set; }

        /// <summary>
        /// The set of variants that can be applied by the agent.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public IList<VariantId> ApplicableVariantIds { get; set; }

        /// <summary>
        /// The queue storage type that the command should be inserted into.
        /// </summary>
        public QueueStorageType QueueStorageType { get; set; }
    }
}
