// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.DataContracts.V2
{
    using System;

    using Newtonsoft.Json;

    /// <summary>
    ///     AppUsage resource record V2
    /// </summary>
    public class AppUsageResourceV2 : PrivacyResourceV2
    {
        /// <summary>
        ///     The kind of aggregation for the record. Currently Daily or Monthly are supported values.
        /// </summary>
        [JsonProperty("aggregation")]
        public string Aggregation { get; set; }

        /// <summary>
        ///     Gets or sets the application icon background color in html code format (e.g. #334455).
        /// </summary>
        [JsonProperty("appIconBackground")]
        public string AppIconBackground { get; set; }

        /// <summary>
        ///     Gets or sets the application icon url.
        /// </summary>
        [JsonProperty("appIconUrl")]
        public string AppIconUrl { get; set; }

        /// <summary>
        ///     Gets or sets the application id.
        /// </summary>
        [JsonProperty("appId")]
        [JsonRequired]
        public string AppId { get; set; }

        /// <summary>
        ///     Gets or sets the application name.
        /// </summary>
        [JsonProperty("appName")]
        public string AppName { get; set; }

        /// <summary>
        ///     Gets or sets the application publisher.
        /// </summary>
        [JsonProperty("appPublisher")]
        public string AppPublisher { get; set; }

        /// <summary>
        ///     Gets or sets the end time of the application usage.
        /// </summary>
        [JsonProperty("endDateTime")]
        public DateTimeOffset EndDateTime { get; set; }
    }
}
