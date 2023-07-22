// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Vortex
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    ///     Contains information for logging as well as if request is for watchdog or other factors
    /// </summary>
    public class VortexRequestInformation
    {
        /// <summary>
        ///     Gets or sets a value indicating whether headers had vortex server name.
        /// </summary>
        [JsonProperty("hadServerName", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool HadServerName { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether had user agent information.
        /// </summary>
        [JsonProperty("hadUserAgent", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool HadUserAgent { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether this instance is watchdog request.
        ///     Watch dog requests come from the watch dog machine to check if we're up and
        ///     running. Based on our response it will try to restart us. We should process
        ///     everything up to sending the requests to PCF and Delete Feed.
        /// </summary>
        [JsonProperty("isWatchdogRequest", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsWatchdogRequest { get; set; }

        /// <summary>
        ///     Gets or sets the name of the vortex machine serving us the event.
        /// </summary>
        [JsonProperty("servedBy", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string ServedBy { get; set; }

        /// <summary>
        ///     Gets or sets the user agent.
        /// </summary>
        [JsonProperty("userAgent", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string UserAgent { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether payload was compressed. Compression
        ///     is expected to be marked in the request headers coming from Vortex.
        /// </summary>
        [JsonProperty("wasCompressed", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool WasCompressed { get; set; }

        /// <summary>
        /// Use the requestTime set by ourself insetad of the Time parsed from Vortex Event 
        /// since the Time parsed from Vortex Event is corrupted and has future dates
        /// </summary>
        [JsonProperty("requestTime")]
        public DateTimeOffset RequestTime { get; set; }

        /// <summary>
        ///     Gets the information as a message string
        /// </summary>
        /// <returns>A string of the information</returns>
        public string ToMessage()
        {
            return
                $"WasCompressed: {this.WasCompressed}, IsWatchDog: {this.IsWatchdogRequest}, ServedBy: {this.ServedBy}, UserAgent: {this.UserAgent}, HadServerName: {this.HadServerName}, HadUserAgent: {this.HadUserAgent}, RequestTime: {this.RequestTime}";
        }
    }
}
