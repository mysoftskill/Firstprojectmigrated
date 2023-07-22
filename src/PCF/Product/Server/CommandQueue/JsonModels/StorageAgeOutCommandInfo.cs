namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue
{
    using System;
    using System.ComponentModel;

    using Newtonsoft.Json;

    /// <summary>
    /// Format of delete commands in storage.
    /// </summary>
    internal class StorageAgeOutCommandInfo
    {
        [JsonProperty("la")]
        public long? UnixLastActiveTimeSeconds { get; set; }

        [JsonProperty("is", DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(false)]
        public bool? IsSuspended { get; set; }
    }
}
