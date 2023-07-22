namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandReplay
{
    using System;
    using Microsoft.Azure.Documents;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Newtonsoft.Json;

    public sealed class ReplayJobDocument : Document
    {
        private DateTimeOffset replayDate;
        private DateTimeOffset createdTime;
        private DateTimeOffset? lastCompletedHour;
        private DateTimeOffset? completedTime;

        /// <summary>
        /// The target replay date
        /// </summary>
        [JsonProperty("rd")]
        public DateTimeOffset ReplayDate
        {
            get
            {
                return this.replayDate;
            }

            set
            {
                this.replayDate = value.ToUniversalTime();
            }
        }

        /// <summary>
        /// The created time of this replay job
        /// </summary>
        [JsonProperty("crt")]
        public DateTimeOffset CreatedTime
        {
            get
            {
                return this.createdTime;
            }

            set
            {
                this.createdTime = value.ToUniversalTime();
            }
        }

        /// <summary>
        /// The next visible time of this job in Unix seconds format
        /// </summary>
        [JsonProperty("nvt")]
        public long UnixNextVisibleTimeSeconds { get; set; }

        /// <summary>
        /// This indicate whether the job has been completed
        /// </summary>
        [JsonProperty("c")]
        public bool IsCompleted { get; set; }

        /// <summary>
        /// The job completed time
        /// </summary>
        [JsonProperty("cpt")]
        public DateTimeOffset? CompletedTime
        {
            get
            {
                return this.completedTime;
            }

            set
            {
                this.completedTime = value?.ToUniversalTime();
            }
        }

        /// <summary>
        /// One replay job is in charge of a full day.
        /// This provides more granular status of the job
        /// </summary>
        [JsonProperty("rch")]
        public DateTimeOffset? LastCompletedHour
        {
            get
            {
                return this.lastCompletedHour;
            }

            set
            {
                this.lastCompletedHour = value?.ToUniversalTime();
            }
        }

        /// <summary>
        /// The continuationToken used for query cold storage
        /// </summary>
        [JsonProperty("ctk")]
        public string ContinuationToken { get; set; }

        /// <summary>
        /// A list of asset group ids applied to this replay job
        /// </summary>
        [JsonProperty("ags")]
        public AssetGroupId[] AssetGroupIds { get; set; }

        /// <summary>
        /// A list of asset groupIds that requested export commands
        /// </summary>
        [JsonProperty("agsex")]
        public AssetGroupId[] AssetGroupIdsForExportCommands { get; set; }

        /// <summary>
        /// [Optional] A subject type applied to this replay job
        /// Null or empty means all subject types apply.
        /// </summary>
        [JsonProperty("subject")]
        public string SubjectType { get; set; }
    }
}
