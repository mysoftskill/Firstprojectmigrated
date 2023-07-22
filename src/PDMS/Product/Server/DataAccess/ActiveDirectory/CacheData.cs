namespace Microsoft.PrivacyServices.DataManagement.DataAccess.ActiveDirectory
{
    using System;
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.DocumentDB.Models;

    using Newtonsoft.Json;

    /// <summary>
    /// The cached active directory data.
    /// </summary>
    public class CacheData : DocumentBase<string>
    {
        /// <summary>
        /// Gets or sets the security group ids.
        /// </summary>
        [JsonProperty(PropertyName = "securityGroupIds")]
        public IEnumerable<Guid> SecurityGroupIds { get; set; }

        /// <summary>
        /// Gets or sets the time at which the value is expired.
        /// </summary>
        [JsonProperty(PropertyName = "expiration")]
        public DateTimeOffset Expiration { get; set; }
    }
}