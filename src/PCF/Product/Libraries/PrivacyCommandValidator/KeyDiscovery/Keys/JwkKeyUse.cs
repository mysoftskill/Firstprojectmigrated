namespace Microsoft.PrivacyServices.CommandFeed.Validator.KeyDiscovery.Keys
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Intended use of the JWK public key.
    /// </summary>
    /// <remarks>
    ///     <see ref="https://tools.ietf.org/html/rfc7517" />
    /// </remarks>
    public enum JwkKeyUse
    {
        /// <summary>
        /// Signature key use.
        /// </summary>
        [EnumMember(Value = "sig")]
        Signature,

        /// <summary>
        /// Encryption key use.
        /// </summary>
        [EnumMember(Value = "enc")]
        Encryption
    }
}
