namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Text;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Newtonsoft.Json;

    /// <summary>
    /// Defines a lease receipt, which is a logical token handed to the client when they take a lease on a command.
    /// The lease receipt is serialized to a string, and contains enough information to route back to the CosmosDB
    /// that stores the command, as well as the etag used for updating the command.
    /// </summary>
    public sealed class LeaseReceipt
    {
        public const long CurrentVersion = 3;

        // Minimum versions by lease receipt field.
        public const long MinimumExpirationTimeVersion = 1;
        public const long MinimumQualifierVersion = 1;
        public const long MinimumCommandTypeVersion = 1;
        public const long MinimumCommandCreatedTimeVersion = 2;

        public const long MinimumAzureQueueStorageVersion = 3;

        public LeaseReceipt(
            string databaseMoniker,
            string token,
            PrivacyCommand command,
            QueueStorageType queueStorageType)
        {
            this.DatabaseMoniker = databaseMoniker;
            this.Token = token;

            this.CommandId = command.CommandId;
            this.AssetGroupId = command.AssetGroupId;
            this.AgentId = command.AgentId;
            this.SubjectType = command.Subject.GetSubjectType();
            this.ApproximateExpirationTime = command.NextVisibleTime;
            this.AssetGroupQualifier = command.AssetGroupQualifier;
            this.CommandType = command.CommandType;
            this.CloudInstance = command.CloudInstance;
            this.CommandCreatedTime = command.Timestamp;
            this.QueueStorageType = queueStorageType;

            // We're at version x now.
            this.Version = CurrentVersion;
        }

        public LeaseReceipt(
            string databaseMoniker,
            CommandId commandId, 
            string token,
            AssetGroupId assetGroupId,
            AgentId agentId,
            SubjectType subjectType,
            DateTimeOffset approximateExpirationTime,
            string assetGroupQualifier,
            PrivacyCommandType commandType,
            string cloudInstance,
            DateTimeOffset? commandCreatedTime,
            QueueStorageType queueStorageType)
        {
            this.DatabaseMoniker = databaseMoniker;
            this.CommandId = commandId;
            this.Token = token;
            this.AssetGroupId = assetGroupId;
            this.AgentId = agentId;
            this.SubjectType = subjectType;
            this.ApproximateExpirationTime = approximateExpirationTime;
            this.AssetGroupQualifier = assetGroupQualifier;
            this.CommandType = commandType;
            this.CloudInstance = cloudInstance;
            this.CommandCreatedTime = commandCreatedTime;
            this.QueueStorageType = queueStorageType;

            // We're at version x now.
            this.Version = CurrentVersion;
        }

        [JsonConstructor]
        private LeaseReceipt()
        {
        }

        #region Version 0 Fields

        /// <summary>
        /// Indicates the schema version of this receipt. A quick way to test whether a lease receipt
        /// contains the necessary fields for a given operation or not.
        /// </summary>
        [JsonProperty("v")]
        public long Version { get; set; }

        /// <summary>
        /// The moniker of the database which hosts the command.
        /// </summary>
        [JsonProperty("dm")]
        public string DatabaseMoniker { get; set; }

        /// <summary>
        /// The unique ID of the command.
        /// </summary>
        [JsonProperty("cid")]
        public CommandId CommandId { get; set; }

        /// <summary>
        /// The update token (etag).
        /// </summary>
        [JsonProperty("tk")]
        public string Token { get; set; }

        /// <summary>
        /// The asset ID.
        /// </summary>
        [JsonProperty("gid")]
        public AssetGroupId AssetGroupId { get; set; }

        /// <summary>
        /// The agent ID.
        /// </summary>
        [JsonProperty("aid")]
        public AgentId AgentId { get; set; }

        /// <summary>
        /// The subject type.
        /// </summary>
        [JsonProperty("st")]
        public SubjectType SubjectType { get; set; }

        #endregion

        #region Version 1 Fields

        [JsonProperty("agq")]
        private string assetGroupQualifier;

        [JsonProperty("et")]
        private DateTimeOffset approximateExpirationTime;

        [JsonProperty("ct")]
        private PrivacyCommandType commandType;

        [JsonProperty("ci")]
        private string cloudInstance;

        /// <summary>
        /// The asset group qualifier.
        /// </summary>
        [JsonIgnore]
        public string AssetGroupQualifier
        {
            get
            {
                this.CheckVersion(MinimumQualifierVersion);
                return this.assetGroupQualifier;
            }

            set
            {
                this.assetGroupQualifier = value;
            }
        }
        
        /// <summary>
        /// The approximate time at which this lease receipt expires.
        /// </summary>
        [JsonIgnore]
        public DateTimeOffset ApproximateExpirationTime
        {
            get
            {
                this.CheckVersion(MinimumExpirationTimeVersion);
                return this.approximateExpirationTime;
            }

            set
            {
                this.approximateExpirationTime = value;
            }
        }

        /// <summary>
        /// The type of command.
        /// </summary>
        [JsonIgnore]
        public PrivacyCommandType CommandType
        {
            get
            {
                this.CheckVersion(MinimumCommandTypeVersion);
                return this.commandType;
            }

            set
            {
                this.commandType = value;
            }
        }

        /// <summary>
        /// The cloud instance Public vs SC.
        /// </summary>
        [JsonIgnore]
        public string CloudInstance
        {
            get => this.cloudInstance;

            set => this.cloudInstance = value;
        }

        #endregion

        #region Version 2 Fields

        /// <summary>
        /// The command created time.
        /// </summary>
        [JsonProperty("cts")]
        private DateTimeOffset? commandCreatedTime;

        [JsonIgnore]
        public DateTimeOffset? CommandCreatedTime
        {
            get => this.commandCreatedTime;

            set => this.commandCreatedTime = value;
        }

        #endregion

        #region Version 3 Fields

        [JsonProperty("qst", DefaultValueHandling = DefaultValueHandling.Ignore)]
        private QueueStorageType queueStorageType;

        [JsonIgnore]
        public QueueStorageType QueueStorageType
        {
            // Default all versions older than 'MinimumAzureQueueStorageVersion' to CosmosDB
            get => this.Version < MinimumAzureQueueStorageVersion ? QueueStorageType.AzureCosmosDb : this.queueStorageType;

            set => this.queueStorageType = value;
        }

        public AzureQueueMessageToken DeserializeToken()
        {
            this.CheckVersion(MinimumAzureQueueStorageVersion);
            this.CheckQueueStorageType(QueueStorageType.AzureQueueStorage);
            return JsonConvert.DeserializeObject<AzureQueueMessageToken>(this.Token);
        }

        #endregion

        /// <summary>
        /// Serializes to a string.
        /// </summary>
        public string Serialize()
        {
            string value = JsonConvert.SerializeObject(this);
            byte[] rawBytes = Encoding.UTF8.GetBytes(value);
            byte[] compressedBytes = CompressionTools.Gzip.Compress(rawBytes);
            return Convert.ToBase64String(compressedBytes);
        }

        /// <summary>
        /// Parses from a string.
        /// </summary>
        public static LeaseReceipt Parse(string leaseReceipt)
        {
            if (string.IsNullOrEmpty(leaseReceipt))
            {
                throw new ArgumentNullException(nameof(leaseReceipt));
            }

            byte[] compressedBytes = Convert.FromBase64String(leaseReceipt);
            string json = Encoding.UTF8.GetString(CompressionTools.Gzip.Decompress(compressedBytes));
            return JsonConvert.DeserializeObject<LeaseReceipt>(json);
        }

        /// <summary>
        /// Attempts to parse the given string as LeaseReceipt.
        /// </summary>
        public static bool TryParse(string value, out LeaseReceipt item)
        {
            if (string.IsNullOrEmpty(value))
            {
                item = null;
                Logger.Instance?.LogLeaseReceiptFailedToParse(value ?? "null");
                return false;
            }

            try
            {
                item = Parse(value);
                return true;
            }
            catch (Exception ex) when (ex is FormatException || ex is InvalidDataException)
            {
                item = null;
                Logger.Instance?.LogLeaseReceiptFailedToParse(value);
                IncomingEvent.Current?.SetProperty("LeaseParsingException", ex.ToString());
                return false;
            }
        }

        private void CheckVersion(long minimumVersion, [CallerMemberName] string callerName = "")
        {
            if (this.Version < minimumVersion)
            {
                throw new InvalidOperationException($"Unable to read property '{callerName}' from lease receipt version = '{this.Version}'. Minimum version = '{minimumVersion}'.");
            }
        }

        private void CheckQueueStorageType(QueueStorageType storageType)
        {
            if (storageType != this.queueStorageType)
            {
                throw new InvalidOperationException($"Queue storage type:  {this.queueStorageType} is incompatible with this lease receipt.");
            }
        }

        public sealed class AzureQueueMessageToken
        {
            [JsonProperty("mi")]
            private string messageId;

            [JsonIgnore]
            public string MessageId
            {
                get => this.messageId;

                set => this.messageId = value;
            }

            [JsonProperty("pr")]
            private string popReceipt;

            [JsonIgnore]
            public string PopReceipt
            {
                get => this.popReceipt;

                set => this.popReceipt = value;
            }

            public AzureQueueMessageToken(string messageId, string popReceipt)
            {
                this.messageId = messageId;
                this.popReceipt = popReceipt;
            }

            [JsonConstructor]
            private AzureQueueMessageToken()
            {
            }
        }
    }
}
