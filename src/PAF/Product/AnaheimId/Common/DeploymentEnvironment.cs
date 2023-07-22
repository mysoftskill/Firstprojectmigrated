namespace Microsoft.PrivacyServices.AnaheimId
{
    /// <summary>
    /// Service deployment environment.
    /// </summary>
    public enum DeploymentEnvironment
    {
        /// <summary>
        /// OneBox, DevBox, Local environment.
        /// </summary>
        ONEBOX,

        /// <summary>
        /// CI1 Environment.
        /// </summary>
        CI1,

        /// <summary>
        /// CI2 Environment.
        /// </summary>
        CI2,

        /// <summary>
        /// CI1 Environment.
        /// </summary>
        INT,

        /// <summary>
        /// PPE Environment.
        /// </summary>
        PPE,

        /// <summary>
        /// PROD Environment.
        /// </summary>
        PROD,
    }
}
