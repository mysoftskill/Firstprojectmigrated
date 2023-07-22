namespace Microsoft.PrivacyServices.CommandFeed.Service.Common.ConfigGen
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using System.Xml.Linq;

    using Microsoft.Azure.ComplianceServices.Common.SecretClient;
    using Microsoft.Windows.Services.ConfigGen;

    /// <summary>
    /// Parses strings with kvsecret=true. 
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal class StringKvSecretParser : IValueParser
    {
        private static readonly XName KvSecretAttributeName = XName.Get("kvsecret", string.Empty);

        /// <summary>
        /// A custom attribute that allows overriding which key vault we read from. This is primarily useful
        /// in onebox scenarios where it is valuable to redirect certain resources to a "common" key vault.
        /// </summary>
        private static readonly XName KeyVaultAttribute = XName.Get("keyVault", string.Empty);

        private readonly IAzureKeyVaultClientFactory kvClientFactory;

        public StringKvSecretParser(IAzureKeyVaultClientFactory kvClientFactory)
        {
            this.kvClientFactory = kvClientFactory;
        }

        public Type TargetType => typeof(string);

        /// <summary>
        /// Returns true if KvSecret flag is set on the xml node
        /// </summary>
        public bool CanParse(string value, XElement element)
        {
            var attribute = element.Attribute(KvSecretAttributeName);
            if (attribute != null)
            {
                return bool.Parse(attribute.Value) && !string.IsNullOrWhiteSpace(value);
            }

            return false;
        }

        /// <summary>
        /// Get the secret from the secret service.
        /// </summary>
        public object Parse(string value, XElement element)
        {
            IAzureKeyVaultClient client;

            var attribute = element.Attribute(KeyVaultAttribute);
            if (attribute != null && !string.IsNullOrWhiteSpace(attribute.Value))
            {
                client = this.kvClientFactory.CreateKeyVaultClient(attribute.Value);
            }
            else
            {
                client = this.kvClientFactory.CreateDefaultKeyVaultClient();
            }

            return client.GetSecretAsync(value).GetAwaiter().GetResult();
        }
    }
}
