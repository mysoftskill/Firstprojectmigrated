namespace Microsoft.PrivacyServices.DataManagement.Client.PowerShell.TestHook
{

    using Microsoft.PrivacyServices.DataManagement.Client.AAD;
    using System;
    using System.Management.Automation;
    using System.Security;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using Microsoft.Azure.Services.AppAuthentication;
    using Microsoft.Azure.KeyVault;

    /// <summary>
    /// A <c>cmdlet</c> for creating an authentication provider for test purpose.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PdmsAuthenticationProvider")]
    public class NewAuthProviderCmdlet : Cmdlet
    {
        private const string KeyVaultConnectionString = @"RunAs=App;AppId=25862df9-0e4d-4fb7-a5c8-dfe8ac261930;TenantId=33e01921-4d64-4f8c-a055-5bdaffd5e33d;CertificateSubjectName=CN=aadclient2.ppe.dpp.microsoft.com;CertificateStoreLocation=LocalMachine";
        private const string KeyVaultBaseUrl = @"https://pdms-int-ame.vault.azure.net/";
        private const string KeyVaultCertificateName = @"aadclient2";
        private const string NgpPdmsTestId = "5fda7238-1512-46b9-9aa0-97c7fc7e576d";

        /// <summary>
        /// Processes the current record in the PowerShell pipeline.
        /// </summary>
        protected override void ProcessRecord()
        {
            var azureServiceTokenProvider = new AzureServiceTokenProvider(KeyVaultConnectionString);
            var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
            var secretBundle = keyVaultClient.GetSecretAsync(KeyVaultBaseUrl, KeyVaultCertificateName, CancellationToken.None).GetAwaiter().GetResult();


            X509Certificate2 cert = new X509Certificate2(
                Convert.FromBase64String(secretBundle.Value),
                (SecureString)null,
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);

            var authProvider = new ServiceAzureActiveDirectoryProviderFactory(NgpPdmsTestId, cert, targetProductionEnvironment: false, sendX5c: true);

            this.WriteObject(authProvider.CreateForClient());
        }
    }
}