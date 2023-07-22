namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi.Throttling
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;

    using WebApiThrottle;

    /// <summary>
    /// Provides throttle policy information by loading it from Parallax based configuration files.
    /// </summary>
    public class ParallaxThrottlePolicyProvider : IThrottlePolicyProvider
    {
        private readonly IThrottlingConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParallaxThrottlePolicyProvider" /> class.
        /// </summary>
        /// <param name="configuration">The configuration for this component.</param>
        public ParallaxThrottlePolicyProvider(IThrottlingConfiguration configuration)
        {
            this.configuration = configuration;
        }

        /// <summary>
        /// Returns the rules.
        /// </summary>
        /// <returns>The rules.</returns>
        public IEnumerable<ThrottlePolicyRule> AllRules()
        {
            return this.configuration.Rules?.Select(x => new ThrottlePolicyRule
            {
                Entry = x.Entry,
                PolicyType = (ThrottlePolicyType)x.PolicyType,
                LimitPerSecond = x.LimitPerSecond,
                LimitPerDay = x.LimitPerDay,
                LimitPerHour = x.LimitPerHour,
                LimitPerMinute = x.LimitPerMinute,
                LimitPerWeek = x.LimitPerWeek
            }) ?? Enumerable.Empty<ThrottlePolicyRule>();
        }

        /// <summary>
        /// Returns the Allowed List policies.         
        /// We cannot change the name of this method as it is defined in an external package: WebApiThrottle
        /// </summary>
        /// <returns>The Allowed List.</returns>
        public IEnumerable<ThrottlePolicyWhitelist> AllWhitelists()
        {
            return this.configuration.Exclusions?.Select(x => new ThrottlePolicyWhitelist
            {
                Entry = x.Entry,
                PolicyType = (ThrottlePolicyType)x.PolicyType
            }) ?? Enumerable.Empty<ThrottlePolicyWhitelist>();
        }

        /// <summary>
        /// Returns the general settings.
        /// </summary>
        /// <returns>The settings.</returns>
        public ThrottlePolicySettings ReadSettings()
        {
            return new ThrottlePolicySettings
            {
                LimitPerSecond = this.configuration.LimitPerSecond,
                LimitPerDay = this.configuration.LimitPerDay,
                LimitPerHour = this.configuration.LimitPerHour,
                LimitPerMinute = this.configuration.LimitPerMinute,
                LimitPerWeek = this.configuration.LimitPerWeek,
                EndpointThrottling = this.configuration.EndpointThrottlingEnabled,
                ClientThrottling = this.configuration.ClientThrottlingEnabled,
                IpThrottling = this.configuration.IPThrottlingEnabled,
                StackBlockedRequests = this.configuration.StackBlockedRequests
            };
        }
    }
}