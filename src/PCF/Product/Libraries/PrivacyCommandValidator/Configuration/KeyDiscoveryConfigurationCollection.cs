namespace Microsoft.PrivacyServices.CommandFeed.Validator.Configuration
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Hard coded configuration values for AAD Cloud Instances
    /// </summary>
    internal class KeyDiscoveryConfigurationCollection
    {
        public static readonly KeyDiscoveryConfiguration Public = new KeyDiscoveryConfiguration(
            new List<string> { CloudInstance.Public },
            new Uri("https://aadrvs.msidentity.com/"),
            new Uri("https://aadrvs.msidentity.com/api/keydiscovery/"),
            false);

        public static readonly KeyDiscoveryConfiguration MoonCake = new KeyDiscoveryConfiguration(
            new List<string> { CloudInstance.AzureMoonCake },
            new Uri("https://aadrvs.msidentity.cn/"),
            new Uri("https://aadrvs.msidentity.cn/api/keydiscovery/"),
            false);

        public static readonly KeyDiscoveryConfiguration Fairfax = new KeyDiscoveryConfiguration(
            new List<string> { CloudInstance.AzureFairfax },
            new Uri("https://aadrvs.msidentity.us/"),
            new Uri("https://aadrvs.msidentity.us/api/keydiscovery/"),
            false);

        public static readonly KeyDiscoveryConfiguration PublicPpe = new KeyDiscoveryConfiguration(
            new List<string> { CloudInstance.Public },
            new Uri("https://aadrvs-ppe.msidentity.com/"),
            new Uri("https://aadrvs-ppe.msidentity.com/api/keydiscovery/"),
            false);

        public static readonly Dictionary<string, KeyDiscoveryConfiguration> KeyDiscoveryConfigurations =
            new Dictionary<string, KeyDiscoveryConfiguration>(StringComparer.OrdinalIgnoreCase)
            {
                { CloudInstance.Public, Public },
                { CloudInstance.AzureMoonCake, MoonCake },
                { CloudInstance.AzureFairfax, Fairfax },
                { CloudInstance.Public + "Ppe", PublicPpe }
            };
    }
}
