namespace Microsoft.PrivacyServices.CommandFeed.Service.Common.ConfigGen
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using Microsoft.Azure.ComplianceServices.Common.SecretClient;
    using Microsoft.Windows.Services.ConfigGen;

    /// <summary>
    /// Defines configuration parsers for reading complex values out of config.
    /// </summary>
    public static class ConfigurationValueParsers
    {
        /// <summary>
        /// Creates a list of parsers.
        /// </summary>
        public static IEnumerable<IValueParser> GetParsers(IAzureKeyVaultClientFactory clientFactory)
        {
            var parsers = new[]
            {
                new OneboxSecretParser(),
                new KeyVaultX509CertParser(clientFactory),
                new StringKvSecretParser(clientFactory),
                ValueParser.Create<Uri>((s, e) => new Uri(s)),
                ValueParser.Create<AssetGroupId>((s, e) => new AssetGroupId(s)),
                ValueParser.Create<AgentId>((s, e) => new AgentId(s)),
                ValueParser.Create<Guid>((s, e) => Guid.Parse(s)),
            };

            return parsers.Concat(ValueParser.DefaultParsers);
        }
    }
}
