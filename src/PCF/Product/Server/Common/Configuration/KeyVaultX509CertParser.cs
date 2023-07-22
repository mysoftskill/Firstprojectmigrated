namespace Microsoft.PrivacyServices.CommandFeed.Service.Common.ConfigGen
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Security.Cryptography.X509Certificates;
    using System.Xml.Linq;

    using Microsoft.Azure.ComplianceServices.Common.SecretClient;
    using Microsoft.Windows.Services.ConfigGen;

    /// <summary>
    /// Parses certificates from subject names.
    /// </summary>
    /// <remarks>
    /// This class is not unit-testable since it loads certificates installed on the local machine, which are (by design)
    /// not present in UT environments.
    /// </remarks>
    [ExcludeFromCodeCoverage]
    internal class KeyVaultX509CertParser : IValueParser
    {
        /// <summary>
        /// A custom attribute that allows overriding which key vault we read from. This is primarily useful
        /// in onebox scenarios where it is valuable to redirect certain resources to a "common" key vault.
        /// </summary>
        private static readonly XName KeyVaultAttribute = XName.Get("keyVault", string.Empty);

        private readonly IAzureKeyVaultClientFactory keyVaultClientFactory;

        public KeyVaultX509CertParser(IAzureKeyVaultClientFactory keyVaultClient)
        {
            this.keyVaultClientFactory = keyVaultClient;
        }

        /// <summary>
        /// We can parse everything!
        /// </summary>
        public bool CanParse(string value, XElement element)
        {
            return true;
        }

        /// <summary>
        /// Finds the certificate in the cert store.
        /// </summary>
        public object Parse(string value, XElement element)
        {
            IAzureKeyVaultClient client;

            var attribute = element.Attribute(KeyVaultAttribute);
            if (attribute != null && !string.IsNullOrWhiteSpace(attribute.Value))
            {
                client = this.keyVaultClientFactory.CreateKeyVaultClient(attribute.Value);
            }
            else
            {
                client = this.keyVaultClientFactory.CreateDefaultKeyVaultClient();
            }

            return client.GetCurrentCertificateAsync(value).GetAwaiter().GetResult();
        }

        public Type TargetType
        {
            get { return typeof(X509Certificate2); }
        }
    }
}