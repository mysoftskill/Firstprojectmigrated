namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandHistory
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using Microsoft.Azure.Documents;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.PXS.Command.CommandStatus;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    internal sealed class CoreCommandDocument : Document
    {
        [Obsolete("Deserializer use")]
        public CoreCommandDocument()
        {
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        public CoreCommandDocument(CommandHistoryCoreRecord record)
        {
            JObject pxsCommand = null;
            if (!string.IsNullOrEmpty(record.RawPxsCommand))
            {
                pxsCommand = JsonConvert.DeserializeObject<JObject>(record.RawPxsCommand);
            }

            this.Id = record.CommandId.Value;
            this.CreatedTime = record.CreatedTime.ToUnixTimeSeconds();
            this.Requester = record.Requester;
            this.Context = record.Context;
            this.IsGloballyComplete = record.IsGloballyComplete;
            this.CompletedTime = record.CompletedTime?.ToUnixTimeSeconds() ?? 0;
            this.PxsCommand = pxsCommand;
            this.IsSynthetic = record.IsSynthetic;
            this.Subject = record.Subject;
            this.CommandType = record.CommandType;
            this.FinalDestinationUri = record.FinalExportDestinationUri;
            this.ExportArchivesDeleteStatus = record.ExportArchivesDeleteStatus;
            this.DeleteRequester = record.DeleteRequester;
            this.DeleteRequestedTime = record.DeleteRequestedTime;
            this.DeletedTime = record.DeletedTime;
            this.TotalCommandCount = record.TotalCommandCount;
            this.IngestedCommandCount = record.IngestedCommandCount;
            this.CompletedCommandCount = record.CompletedCommandCount;
            this.IngestionAssemblyVersion = record.IngestionAssemblyVersion;
            this.IngestionDataSetVersion = record.IngestionDataSetVersion;
            this.QueueStorageType = record.QueueStorageType;

            using (var b64Stream = new Base64Stream())
            {
                CompressionTools.Brotli.CompressJson(record.WeightedMonikerList, b64Stream);
                b64Stream.Close();

                this.BrotliEncodedWeightedMonikerlist = b64Stream.EncodedOutput.ToString();
            }
        }

        [JsonProperty("qst")]
        public QueueStorageType QueueStorageType { get; set; }

        [JsonProperty("crt")]
        public long CreatedTime { get; set; }

        [JsonProperty("r")]
        [JsonConverter(typeof(InternedStringJsonConverter))]
        public string Requester { get; set; }

        [JsonProperty("ctx")]
        public string Context { get; set; }

        [JsonProperty("c")]
        public bool IsGloballyComplete { get; set; }

        [JsonProperty("cpt")]
        public long CompletedTime { get; set; }

        [JsonProperty("tcc")]
        public long? TotalCommandCount { get; set; }

        [JsonProperty("icc")]
        public long? IngestedCommandCount { get; set; }
        
        [JsonProperty("ccc")]
        public long? CompletedCommandCount { get; set; }

        [JsonProperty("idsv")]
        public long? IngestionDataSetVersion { get; set; }

        [JsonProperty("iav")]
        [JsonConverter(typeof(InternedStringJsonConverter))]
        public string IngestionAssemblyVersion { get; set; }

        [JsonProperty("s")]
        public IPrivacySubject Subject { get; set; }

        [JsonProperty("ct")]
        public PrivacyCommandType CommandType { get; set; }

        [JsonProperty("pxs")]
        public JObject PxsCommand { get; set; }

        [JsonProperty("synth", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsSynthetic { get; set; }

        [JsonProperty("ed")]
        public Uri FinalDestinationUri { get; set; }

        [JsonProperty("ds")]
        public ExportArchivesDeleteStatus ExportArchivesDeleteStatus { get; set; }

        [JsonProperty("drqt")]
        public DateTimeOffset? DeleteRequestedTime { get; set; }

        [JsonProperty("dt")]
        public DateTimeOffset? DeletedTime { get; set; }

        [JsonProperty("drq")]
        public string DeleteRequester { get; set; }

        [JsonProperty("wml")]
        public string BrotliEncodedWeightedMonikerlist { get; set; }

        [JsonProperty("abp")]
        public BlobPointer AuditBlobPointer { get; set; }

        [JsonProperty("sbp")]
        public BlobPointer StatusBlobPointer { get; set; }

        [JsonProperty("edbp")]
        public BlobPointer ExportDestinationBlobPointer { get; set; }

        public CommandHistoryCoreRecord ToRecord()
        {
            var commandId = new CommandId(this.Id);
            var coreRecord = new CommandHistoryCoreRecord(commandId)
            {
                CreatedTime = DateTimeOffset.FromUnixTimeSeconds(this.CreatedTime),
                Requester = this.Requester,
                Context = this.Context,
                CommandType = this.CommandType,
                IsGloballyComplete = this.IsGloballyComplete,
                CompletedTime = this.CompletedTime > 0 ? DateTimeOffset.FromUnixTimeSeconds(this.CompletedTime) : (DateTimeOffset?)null,
                TotalCommandCount = this.TotalCommandCount,
                IngestedCommandCount = this.IngestedCommandCount,
                CompletedCommandCount = this.CompletedCommandCount,
                IngestionDataSetVersion = this.IngestionDataSetVersion,
                IngestionAssemblyVersion = this.IngestionAssemblyVersion,
                RawPxsCommand = JsonConvert.SerializeObject(this.PxsCommand),
                IsSynthetic = this.IsSynthetic,
                Subject = this.Subject,
                FinalExportDestinationUri = this.FinalDestinationUri,
                ExportArchivesDeleteStatus = this.ExportArchivesDeleteStatus,
                DeleteRequestedTime = this.DeleteRequestedTime,
                DeletedTime = this.DeletedTime,
                DeleteRequester = this.DeleteRequester,

                // Default for records without this property should be 'AzureCosmosDb'
                QueueStorageType = this.QueueStorageType == QueueStorageType.Undefined ? QueueStorageType.AzureCosmosDb : this.QueueStorageType
            };

            if (!string.IsNullOrEmpty(this.BrotliEncodedWeightedMonikerlist))
            {
                using (var b64Stream = new Base64Stream(this.BrotliEncodedWeightedMonikerlist))
                {
                    coreRecord.WeightedMonikerList = CompressionTools.Brotli.DecompressJson<List<string>>(b64Stream);
                }
            }

            return coreRecord;
        }
    }
}
