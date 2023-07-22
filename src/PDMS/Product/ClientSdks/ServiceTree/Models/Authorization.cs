namespace Microsoft.PrivacyServices.DataManagement.Client.ServiceTree
{
    using System;

    using Microsoft.PrivacyServices.DataManagement.Client;

    using Newtonsoft.Json;

    /// <summary>
    /// The authorization types.
    /// Service tree has more types than this, 
    /// but we only care about the values listed here.
    /// </summary>
    [JsonConverter(typeof(EnumTolerantConverter<AuthorizationType>))]
    public enum AuthorizationType
    {
        /// <summary>
        /// User type.
        /// </summary>
        User
    }

    /// <summary>
    /// The authorization data.
    /// </summary>
    public class Authorization
    {
        /// <summary>
        /// Gets or sets the id. For user type this is the user name.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the authorization type.
        /// </summary>
        public AuthorizationType Type { get; set; }

        /// <summary>
        /// Gets or sets the date at which this authorization can no longer be used.
        /// </summary>
        public DateTimeOffset ExpiryDate { get; set; }
    }
}