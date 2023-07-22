
namespace Microsoft.Membership.MemberServices.Common.Configuration
{
    using System;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.Configuration;

    /// <summary>
    /// A helper class to create an AppConfiguration instance based on PXS environment
    /// </summary>
    public class AppConfigurationFactory
    {
        public static IAppConfiguration Create(IPrivacyConfigurationManager config)
        {
            System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(config?.AzureAppConfigurationSettings?.Endpoint));

            string labelFilter = LabelNames.None;
            switch (config.EnvironmentConfiguration.EnvironmentType)
            {
                case EnvironmentType.OneBox:
                    return new AppConfiguration(@"local.settings.json");

                case EnvironmentType.ContinuousIntegration:
                    labelFilter = LabelNames.CI;
                    break;

                case EnvironmentType.Integration:
                    labelFilter = LabelNames.INT;
                    break;

                case EnvironmentType.PreProd:
                    labelFilter = LabelNames.PPE;
                    break;
            }

            return new AppConfiguration(new Uri(config.AzureAppConfigurationSettings.Endpoint), labelFilter);
        }
    }
}
