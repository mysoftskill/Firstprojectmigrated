namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandHistory
{
    using System;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Newtonsoft.Json;
    
    internal class ExportDestinationDocument
    {
        [Obsolete("Deserializer use")]
        public ExportDestinationDocument()
        {
        }

        /// <summary>
        /// Copy constructor from the given record.
        /// </summary>
        public ExportDestinationDocument(CommandHistoryExportDestinationRecord record)
        {
            this.AgentId = record.AgentId;
            this.AssetGroupId = record.AssetGroupId;
            this.ExportDestinationUri = record.ExportDestinationUri;
            this.ExportDestinationPath = record.ExportDestinationPath;
        }

        [JsonProperty("aid")]
        public AgentId AgentId { get; set; }

        [JsonProperty("gid")]
        public AssetGroupId AssetGroupId { get; set; }

        [JsonProperty("edu")]
        public Uri ExportDestinationUri { get; set; }

        [JsonProperty("edp")]
        public string ExportDestinationPath { get; set; }

        /// <summary>
        /// Converts to the data model.
        /// </summary>
        public CommandHistoryExportDestinationRecord ToRecord()
        {
            return new CommandHistoryExportDestinationRecord(
                this.AgentId,
                this.AssetGroupId,
                this.ExportDestinationUri,
                this.ExportDestinationPath);
        }
    }
}
