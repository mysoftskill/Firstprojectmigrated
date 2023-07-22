namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Newtonsoft.Json;

    /// <summary>
    /// Base privacy command from the command feed API.
    /// </summary>
    public abstract partial class PrivacyCommand
    {
        /// <summary>
        /// Prevent inheritance outside the assembly.
        /// </summary>
        internal PrivacyCommand(string commandType)
        {
            this.CommandType = commandType;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [JsonProperty("type")]
        private string CommandType { get; set; }

        /// <inheritdoc/>
        [JsonProperty("timestamp")]
        public DateTimeOffset Timestamp { get; set; }

        /// <inheritdoc/>
        [JsonProperty("cv")]
        public string CorrelationVector { get; set; }

        /// <inheritdoc/>
        [JsonProperty("leaseReceipt")]
        public string LeaseReceipt { get; set; }
        
        /// <inheritdoc/>
        [JsonProperty("subject")]
        public IPrivacySubject Subject { get; set; }

        /// <inheritdoc/>
        [JsonProperty("leaseExpirationTime")]
        public DateTimeOffset ApproximateLeaseExpiration { get; set; }

        /// <inheritdoc/>
        [JsonIgnore]
        public TimeSpan LeaseDuration
        {
            get { return this.ApproximateLeaseExpiration - DateTimeOffset.UtcNow; }
        }

        /// <inheritdoc/>
        [JsonProperty("verifier")]
        public string Verifier { get; set; }

        /// <inheritdoc/>
        [JsonProperty("applicableVariants")]
        public IList<Variant> ApplicableVariants { get; set; }

        /// <inheritdoc/>
        [JsonProperty("assetGroupQualifier")]
        public string AssetGroupQualifier { get; set; }

        /// <inheritdoc/>
        [JsonProperty("agentState")]
        public string AgentState { get; set; }

        /// <inheritdoc/>
        [JsonProperty("assetGroupId")]
        public string AssetGroupId { get; set; }

        /// <inheritdoc/>
        [JsonProperty("commandId")]
        public string CommandId { get; set; }

        /// <inheritdoc />
        [JsonProperty("requestBatchId")]
        public string RequestBatchId { get; set; }

        /// <inheritdoc />
        [JsonProperty("processorApplicable")]
        public bool ProcessorApplicable { get; set; }

        /// <inheritdoc />
        [JsonProperty("controllerApplicable")]
        public bool ControllerApplicable { get; set; }

        /// <inheritdoc /> 
        [JsonProperty("cloudInstance")]
        public string CloudInstance { get; set; }
    }
}
