// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.PXS.Command.Contracts.V1
{
    using System;
    using System.ComponentModel;

    using Newtonsoft.Json;

    /// <summary>
    ///     Age Out Request
    /// </summary>
    public class AgeOutRequest : PrivacyRequest
    {
        /// <summary>
        ///     Gets or sets the last time the account was active
        /// </summary>
        [JsonProperty("lastActive", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTimeOffset? LastActive { get; set; }

        /// <summary>
        ///     Gets or sets a flag that indicates that the account was suspended due to user behavoir
        /// </summary>
        [JsonProperty("isSuspended", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue(false)]
        public bool IsSuspended { get; set; }
    }
}