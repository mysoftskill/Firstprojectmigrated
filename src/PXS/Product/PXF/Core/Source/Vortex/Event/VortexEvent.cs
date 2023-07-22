// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Vortex.Event
{
    using System;

    using Newtonsoft.Json;

    /// <summary>
    /// VortexEvents
    /// </summary>
    public class VortexEvents
    {
        /// <summary>
        /// Events
        /// </summary>
        [JsonProperty("Events", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public VortexEvent[] Events { get; set; }
    }

    public partial class VortexEvent
    {
        /// <summary>
        ///     Gets or sets the correlation vector. If null, check <see cref="VortexTags.CorrelationVector"/> for legacy location
        /// </summary>
        [JsonProperty("cV", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string CorrelationVector { get; set; }

        /// <summary>
        ///     Gets or sets the extension in the legacy location
        /// </summary>
        [JsonProperty("ext", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Extensions Ext { get; set; }

        /// <summary>
        ///     Gets or sets the user identifier in the legacy location
        /// </summary>
        [JsonProperty("userId", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string LegacyUserId { get; set; }

        /// <summary>
        /// Gets or sets the device identifier in the legacy location
        /// </summary>
        [JsonProperty("deviceId", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string LegacyDeviceId { get; set; }

        /// <summary>
        ///     Gets or sets the name in the legacy location
        /// </summary>
        [JsonProperty("name", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Name { get; set; }

        /// <summary>
        ///     Gets or sets the tags in the legacy location
        /// </summary>
        [JsonProperty("tags", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public VortexTags Tags { get; set; }

        /// <summary>
        ///     Gets or sets the time in the legacy location
        /// </summary>
        [JsonProperty("time", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTimeOffset Time { get; set; }

        /// <summary>
        ///     Gets or sets the version in the legacy location
        /// </summary>
        [JsonProperty("ver", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public float Version { get; set; }

        /// <summary>
        ///     Gets or sets the data in the legacy location
        /// </summary>
        [JsonProperty("data", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public VortexData Data { get; set; }
    }
}
