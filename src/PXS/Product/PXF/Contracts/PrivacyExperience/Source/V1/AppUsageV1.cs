// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    /// <summary>
    /// AppUsage V1
    /// </summary>
    public class AppUsageV1 : ResourceV1
    {
        /// <summary>
        /// Gets or sets the application id. 
        /// </summary>
        [JsonProperty("appId")]
        [JsonRequired]
        public string AppId { get; set; }

        /// <summary>
        /// Gets or sets the application name. 
        /// </summary>
        [JsonProperty("appName")]
        public string AppName { get; set; }

        /// <summary>
        /// Gets or sets the application publisher. 
        /// </summary>
        [JsonProperty("appPublisher")]
        public string AppPublisher { get; set; }

        /// <summary>
        /// Gets or sets the application icon url. 
        /// </summary>
        [JsonProperty("appIconUrl")]
        public string AppIconUrl { get; set; }

        /// <summary>
        /// Gets or sets the application icon background color in html code format (e.g. #334455). 
        /// </summary>
        [JsonProperty("appIconBackground")]
        public string AppIconBackground { get; set; }
    }
}