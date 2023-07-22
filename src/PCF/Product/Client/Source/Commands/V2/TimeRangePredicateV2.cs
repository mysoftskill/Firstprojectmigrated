
namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System;
    using Newtonsoft.Json;

    public class TimeRangePredicateV2
    {
        /// <summary>
        /// Start time in the time range for which to apply the signal. 
        /// </summary>
        [JsonProperty("startTime")]
        public DateTimeOffset StartTime { get; set; }

        /// <summary>
        /// End time in the time range for which to apply the signal. 
        /// </summary>
        [JsonProperty("endTime")]
        public DateTimeOffset EndTime { get; set; }
    }
}