namespace Microsoft.PrivacyServices.DataManagement.FunctionalTests.Setup
{
    using System;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.DataManagement.Client;
    using Microsoft.PrivacyServices.DataManagement.Client.AAD;
    using Microsoft.PrivacyServices.DataManagement.Client.ServiceTree;
    using Microsoft.PrivacyServices.DataManagement.Client.V2;

    public static class TestSetup
    {

        private const string AppId = "25862df9-0e4d-4fb7-a5c8-dfe8ac261930";
        private const string TenantId = "33e01921-4d64-4f8c-a055-5bdaffd5e33d";
        private const string CertificateSubjectName = "aadclient2.ppe.dpp.microsoft.com";
        private const string KeyVaultBaseUrl = @"https://pdms-int-ame.vault.azure.net/";

        // AME Test Tenant App Id
        private const string NgpPdmsTestId = "5fda7238-1512-46b9-9aa0-97c7fc7e576d";

        // PDMS Test Client Setup
        public static IDataManagementClient PdmsClientInstance { get; }
        public static RequestContext RequestContext { get; }
        public static Uri PdmsBaseUri { get; }
        public static IAuthenticationProvider AuthenticationProvider { get; }

        // Service Tree Client Setup
        public static IServiceTreeClient ServiceTreeClientInstance { get; }
        public static RequestContext ServiceTreeRequestContext { get; }
        public static IAuthenticationProvider ServiceTreeAuthenticationProvider { get; }

        static TestSetup()
        {
            PdmsBaseUri = GetBaseUri();
            BaseHttpServiceProxy httpServiceProxy = new HttpServiceProxy(
                PdmsBaseUri,
                defaultTimeout: TimeSpan.FromSeconds(100));
            PdmsClientInstance = new DataManagementClient(httpServiceProxy);

            var cert = GetClientCertificateForAadAuthenticationAsync().GetAwaiter().GetResult();

            // NGP PDMS Test AppId in AME Tenant, used for all the tests, uses a certs.
            var authProvider = new ServiceAzureActiveDirectoryProviderFactory(NgpPdmsTestId, cert, targetProductionEnvironment: false, sendX5c: true);

            AuthenticationProvider = authProvider.CreateForClient();

            RequestContext = new RequestContext
            {
                AuthenticationProvider = AuthenticationProvider
            };

            // Service Tree Client setup
            var serviceTreeUri = new Uri(Defaults.ServiceTreeUrl);
            var serviceTreeHttpServiceProxy = new HttpServiceProxy(serviceTreeUri, defaultTimeout: System.Threading.Timeout.InfiniteTimeSpan);

            var clientId = Defaults.PdmsIntResourceId_FP;

            var serviceTreeAuthenticationProviderFactory = new ServiceAzureActiveDirectoryProviderFactory(clientId, cert, targetProductionEnvironment: false, sendX5c: true);
            serviceTreeAuthenticationProviderFactory.ResourceId = Defaults.ServiceTreeResourceId;

            ServiceTreeClientInstance = new ServiceTreeClient(serviceTreeHttpServiceProxy);

            ServiceTreeRequestContext = new RequestContext
            {
                AuthenticationProvider = serviceTreeAuthenticationProviderFactory.CreateForClient()
            };
        }

        private static async Task<string> GetClientSecretForAadAuthenticationAsync()
        {
            var secretsReader = new SecretsReader(KeyVaultBaseUrl, TenantId, AppId, CertificateSubjectName);
            var secret = await secretsReader.GetSecretByNameAsync("pdms-test").ConfigureAwait(false);

            return secret;
        }

        private static async Task<X509Certificate2> GetClientCertificateForAadAuthenticationAsync()
        {
            var secretsReader = new SecretsReader(KeyVaultBaseUrl, TenantId, AppId, CertificateSubjectName);

            var secret = await secretsReader.GetCertificateByNameAsync("aadclient2").ConfigureAwait(false);

            return secret;
        }

        private static Uri GetBaseUri(bool debug = false)
        {
            string testEnvironment = Environment.GetEnvironmentVariable("PDMS_TestEnvironmentName");
            if (testEnvironment == null)
            {
                testEnvironment = "devbox";
            }

            string pdmsEndpoint;
            switch (testEnvironment)
            {
                case "devbox":
                    pdmsEndpoint = "https://management.privacy.microsoft-int.com";
                    break;

                case "CI1":
                    pdmsEndpoint = "https://ci1.management.privacy.microsoft-int.com";
                    break;

                case "CI2":
                    pdmsEndpoint = "https://ci2.management.privacy.microsoft-int.com";
                    break;

                default:
                    throw new InvalidOperationException("Can run tests only in valid environments. PDMS_TestEnvironmentName environment variable has invalid value");
            }
            if (debug)
            {
                Console.WriteLine($"TestEnvironment : {testEnvironment}. Endpoint : {pdmsEndpoint}");
            }
            return new Uri(pdmsEndpoint);
        }
    }
}
