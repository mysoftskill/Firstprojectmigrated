namespace Microsoft.Membership.MemberServices.PrivacyExperience.SyntheticsTests.Common
{
    using System;
    using System.Threading.Tasks;
    using global::Azure.Core;
    using Microsoft.Azure.ComplianceServices.Common;

    public class SyntheticsAppSettings
    {
        private readonly IAppConfiguration appConfiguration;

        /// <summary>
        /// Setup the app configuration manager for accessing the non-prod configuration manager.
        /// <param name="endpoint">The app configuration endpoint url.</param>
        /// <param name="credential">The the token credential for authentication.</param>
        /// </summary>
        public SyntheticsAppSettings(string endpoint, TokenCredential credential)
        {
            appConfiguration = new AppConfiguration(new Uri(endpoint), LabelNames.PPE, null, credential);
        }
    

        /// <summary>
        /// Collect an integer value for a given key from the configuration manager
        /// </summary>
        /// <param name="configurationName">The key to lookup in the configuration manager</param>
        /// <returns>the integer value</returns>
        public int GetConfigIntValue(string configurationName)
        {
            return appConfiguration.GetConfigValue<int>(configurationName);
        }


        /// <summary>
        /// Retrieve the enabled state by looking up a feature flag name.
        /// </summary>
        /// <param name="featureFlagName">The feature flag name.</param>
        /// <returns>true if flag is enabled</returns>
        public ValueTask<bool> IsFeatureFlagEnabledAsync(string featureFlagName)
        {
            return appConfiguration.IsFeatureFlagEnabledAsync(featureFlagName, false);
        }
    }
}
