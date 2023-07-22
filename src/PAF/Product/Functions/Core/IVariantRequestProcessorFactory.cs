namespace Microsoft.PrivacyServices.AzureFunctions.Core
{
    using Microsoft.Azure.ComplianceServices.Common.IfxMetric;
    using Microsoft.PrivacyServices.AzureFunctions.Common;
    using Microsoft.PrivacyServices.AzureFunctions.Common.Configuration;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// Interface to create variant request processors.
    /// </summary>
    public interface IVariantRequestProcessorFactory
    {
        /// <summary>
        /// Creates a variant request processor.
        /// </summary>
        /// <param name="configuration">Function configuration.</param>
        /// <param name="authenticationProvider">Authentication provider.</param>
        /// <param name="metricContainer">Metric configuration.</param>
        /// <param name="logger">Trace logger.</param>
        /// <returns>IVariantRequestProcessor instance.</returns>
        public IVariantRequestProcessor Create(
            IFunctionConfiguration configuration,
            IAuthenticationProvider authenticationProvider,
            IMetricContainer metricContainer,
            ILogger logger);
    }
}
