namespace Microsoft.PrivacyServices.CommandFeed.Validator.TokenValidation
{
    /// <summary>
    /// Permissible signing algorithms
    /// </summary>
    public enum SigningAlgorithm
    {
        /// <summary>
        /// RS256
        /// </summary>
        Rs256,

        /// <summary>
        /// RS384
        /// </summary>
        Rs384,

        /// <summary>
        /// RS512
        /// </summary>
        Rs512,

        /// <summary>
        /// ES256
        /// </summary>
        Es256,

        /// <summary>
        /// ES384
        /// </summary>
        Es384,

        /// <summary>
        /// ES512
        /// </summary>
        Es512
    }
}
