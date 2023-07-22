// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.CommandFeed.Client.Test
{
    using System;

    using Newtonsoft.Json;

    public class RawJsonDataRecord
    {
        [JsonProperty(PropertyName = "i")]
        public int Data { get; set; }
    }

    /// <summary>
    ///     RawJsonExampleRecord class
    /// </summary>
    public class RawJsonExampleRecord
    {
        [JsonProperty("correlationId", Order = 1)]
        public string CorrelationId { get; set; }

        [JsonProperty("properties", Order = 2)]
        public RawJsonDataRecord Data { get; set; }

        [JsonProperty("time", Order = 0)]
        public DateTimeOffset Timestamp { get; set; }
    }
}
