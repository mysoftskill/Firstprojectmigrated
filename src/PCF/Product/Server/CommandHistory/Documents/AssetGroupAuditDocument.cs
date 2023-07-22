namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandHistory
{
    using System;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Newtonsoft.Json;
    
    /// <summary>
    /// Tracks internal PCF audit records for commands.
    /// </summary>
    internal class AssetGroupAuditDocument
    {
        public AssetGroupAuditDocument(AgentId agentId, AssetGroupId assetGroupId, CommandIngestionAuditRecord record)
        {
            this.AssetGroupId = assetGroupId;
            this.AgentId = agentId;
            this.AuditRecord = record;
        }

        [Obsolete("Deserializer use")]
        public AssetGroupAuditDocument()
        {
        }

        [JsonProperty("aid")]
        public AgentId AgentId { get; set; }

        [JsonProperty("gid")]
        public AssetGroupId AssetGroupId { get; set; }

        [JsonProperty("r")]
        public CommandIngestionAuditRecord AuditRecord { get; set; }
    }
}
