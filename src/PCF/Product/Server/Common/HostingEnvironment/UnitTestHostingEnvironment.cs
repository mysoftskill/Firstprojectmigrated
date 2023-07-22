namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common.SecretClient;

    /// <summary>
    /// Contains constants and methods related to running PCF in a unit test environment.
    /// </summary>
    public class UnitTestHostingEnvironment : OneBoxHostingEnvironment
    {
        public override IAzureKeyVaultClientFactory CreateKeyVaultClientFactory(string keyVaultBaseUrl, string clientId)
        {
            return new NoOpKeyVaultClient();
        }
    }
}
