namespace Microsoft.PrivacyServices.CommandFeed.Validator.KeyDiscovery.Keys
{
    using System.Runtime.Serialization;

    /// <summary>
    /// JWK key types.
    /// </summary>
    /// <remarks>
    ///     <see ref="https://tools.ietf.org/html/rfc7517" />
    /// </remarks>
    public enum JwkKeyType
    {
        /// <summary>
        /// RSA key type.
        /// </summary>
        [EnumMember(Value = "RSA")]
        RSA,

        /// <summary>
        /// ECC key type.
        /// </summary>
        [EnumMember(Value = "EC")]
        EC
    }
}
