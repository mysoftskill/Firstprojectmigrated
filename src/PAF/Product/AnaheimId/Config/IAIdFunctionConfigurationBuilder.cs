namespace Microsoft.PrivacyServices.AnaheimId.Config
{
    /// <summary>
    /// Builder for function configuration settings.
    /// </summary>
    public interface IAIdFunctionConfigurationBuilder
    {
        /// <summary>
        /// Build configuration.
        /// </summary>
        /// <returns>Function configuration.</returns>
        IAIdFunctionConfiguration Build();
    }
}
