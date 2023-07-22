namespace Microsoft.PrivacyServices.AzureFunctions.Core
{
    using Microsoft.Azure.ComplianceServices.Common.IfxMetric;
    using Microsoft.PrivacyServices.AzureFunctions.Common;
    using Microsoft.PrivacyServices.AzureFunctions.Common.Configuration;
    using Microsoft.PrivacyServices.AzureFunctions.Common.DataAccessors;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// Create variant request processors.
    /// </summary>
    public class VariantRequestProcessorFactory : IVariantRequestProcessorFactory
    {
        /// <summary>
        /// Creates a variant request processor.
        /// </summary>
        /// <param name="configuration">Function configuration.</param>
        /// <param name="authenticationProvider">Authentication provider.</param>
        /// <param name="metricContainer">Metric client.</param>
        /// <param name="logger">Trace logger.</param>
        /// <returns>IVariantRequestProcessor instance.</returns>
        public IVariantRequestProcessor Create(
            IFunctionConfiguration configuration,
            IAuthenticationProvider authenticationProvider,
            IMetricContainer metricContainer,
            ILogger logger)
        {
            var adoClientWrapper = new AdoClientWrapper(configuration, logger);

            var workItemService = new VariantRequestWorkItemService(
                configuration,
                adoClientWrapper,
                logger,
                new VariantRequestPatchSerializer());

            var httpClientWrapper = new HttpClientWrapper(logger, configuration.PdmsBaseUrl);

            var pdmsService = new PdmsService(configuration, logger, httpClientWrapper, authenticationProvider);

            MappingProfile profile = new MappingProfile();

            var mapper = new PafMapper(profile);

            return new VariantRequestProcessor(
                configuration,
                workItemService,
                pdmsService,
                logger,
                metricContainer,
                mapper);
        }
    }
}
