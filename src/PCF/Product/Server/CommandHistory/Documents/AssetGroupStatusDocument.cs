namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandHistory
{
    using System;
    using System.Linq;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Newtonsoft.Json;

    /// <summary>
    /// Defines the given asset group's progress in the processing of a command.
    /// </summary>
    internal class AssetGroupStatusDocument
    {
        [Obsolete("Deserializer use")]
        public AssetGroupStatusDocument()
        {
        }

        public AssetGroupStatusDocument(CommandHistoryAssetGroupStatusRecord record)
        {
            this.AgentId = record.AgentId;
            this.AssetGroupId = record.AssetGroupId;
            this.ClaimedVariants = record.ClaimedVariants?.ToArray() ?? new string[0];
            this.Delinked = record.Delinked;
            this.ForceCompleted = record.ForceCompleted;
            this.AffectedRows = record.AffectedRows;
            this.StorageAccountMoniker = record.StorageAccountMoniker;

            this.CompletionTime = record.CompletedTime?.ToUniversalTime();
            this.IngestionTime = record.IngestionTime?.ToUniversalTime();
            this.SoftDeleteTime = record.SoftDeleteTime?.ToUniversalTime();
        }

        [JsonProperty("aid")]
        public AgentId AgentId { get; set; }

        [JsonProperty("gid")]
        public AssetGroupId AssetGroupId { get; set; }

        [JsonProperty("it")]
        public DateTimeOffset? IngestionTime { get; set; }

        [JsonProperty("sdt")]
        public DateTimeOffset? SoftDeleteTime { get; set; }

        [JsonProperty("ct")]
        public DateTimeOffset? CompletionTime { get; set; }

        [JsonProperty("dl")]
        public bool? Delinked { get; set; }

        [JsonProperty("v")]
        public string[] ClaimedVariants { get; set; }

        [JsonProperty("fc")]
        public bool ForceCompleted { get; set; }

        [JsonProperty("ar")]
        public int? AffectedRows { get; set; }

        [JsonProperty("sam")]
        public string StorageAccountMoniker { get; set; }

        /// <summary>
        /// Converts to the data model.
        /// </summary>
        public CommandHistoryAssetGroupStatusRecord ToRecord()
        {
            return new CommandHistoryAssetGroupStatusRecord(this.AgentId, this.AssetGroupId)
            {
                ClaimedVariants = this.ClaimedVariants,
                Delinked = this.Delinked,
                CompletedTime = this.CompletionTime?.ToUniversalTime(),
                IngestionTime = this.IngestionTime?.ToUniversalTime(),
                SoftDeleteTime = this.SoftDeleteTime?.ToUniversalTime(),
                ForceCompleted = this.ForceCompleted,
                AffectedRows = this.AffectedRows,
                StorageAccountMoniker = this.StorageAccountMoniker,
            };
        }
    }
}
