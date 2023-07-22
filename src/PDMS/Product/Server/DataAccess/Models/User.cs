namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    /// <summary>
    /// Defines information for a user (including authenticated user).
    /// </summary>
    public class User : Entity
    {
        /// <summary>
        /// Security groups that the user is part of.
        /// </summary>
        [JsonProperty(PropertyName = "securityGroups")]
        public IEnumerable<Guid> SecurityGroups { get; set; }
    }
}