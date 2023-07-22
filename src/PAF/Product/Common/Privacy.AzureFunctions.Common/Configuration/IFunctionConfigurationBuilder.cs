namespace Microsoft.PrivacyServices.AzureFunctions.Common.Configuration
{
    /// <summary>
    /// Builder for function configuration settings.
    /// </summary>
    public interface IFunctionConfigurationBuilder
    {
        /// <summary>
        /// Build configuration.
        /// </summary>
        /// <returns>Function configuration.</returns>
        IFunctionConfiguration Build();
    }
}
