// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.Config
{
    using System;
    using System.Configuration;
    using System.Security;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.KeyVault;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Azure;
    using Microsoft.Membership.MemberServices.Test.Common;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     Test Configuration
    /// </summary>
    public static class TestConfiguration
    {
        /// <summary>
        ///     The Privacy Mock service endpoint
        /// </summary>
        public static readonly Lazy<Uri> MockBaseUrl = new Lazy<Uri>(() => new Uri(GetPrivacyMockEndpoint()));

        public static readonly IKeyVaultClient KeyVaultClient = new KeyVaultClientInstrumented(new KeyVaultClient(GetAccessTokenAsync));

        /// <summary>
        ///     The PXS service endpoint
        /// </summary>
        public static readonly Lazy<Uri> ServiceEndpoint = new Lazy<Uri>(() => new Uri(GetPxsEndpoint()));

        public static readonly string TestKeyVaultEndpoint = "https://pxs-test-ame.vault.azure.net";

        public static readonly Lazy<TestUser> AadHomeTenantAdmin = new Lazy<TestUser>(
            () =>
            {
                string userName = KeyVaultClient.GetSecretAsync(TestKeyVaultEndpoint, "user1-upn").GetAwaiter().GetResult().Value;
                string password = KeyVaultClient.GetSecretAsync(TestKeyVaultEndpoint, "user1-password").GetAwaiter().GetResult().Value;
                return new TestUser(userName, password);
            });

        public static readonly Lazy<TestUser> AadResourceTenantAdmin = new Lazy<TestUser>(
            () =>
            {
                string userName = KeyVaultClient.GetSecretAsync(TestKeyVaultEndpoint, "user2-upn").GetAwaiter().GetResult().Value;
                string password = KeyVaultClient.GetSecretAsync(TestKeyVaultEndpoint, "user2-password").GetAwaiter().GetResult().Value;
                return new TestUser(userName, password);
            });

        public static readonly Lazy<string> BlobStorageTestConnectionString = new Lazy<string>(
            () => KeyVaultClient.GetSecretAsync(TestKeyVaultEndpoint, "pxs-test-blob-storage-connection-string").GetAwaiter().GetResult().Value);

        public static readonly Lazy<string> PremiumBlobStorageTestConnectionString = new Lazy<string>(
            () => KeyVaultClient.GetSecretAsync(TestKeyVaultEndpoint, "pxs-test-premium-blob-storage-connection-string").GetAwaiter().GetResult().Value);

        public static readonly Lazy<SecureString> AadAccountCloseEventHubConnectionString = new Lazy<SecureString>(
            () =>
            {
                var secureString = new SecureString();
                var secret = KeyVaultClient.GetSecretAsync(TestKeyVaultEndpoint, GetAadAccountCloseEventHubSecretName()).GetAwaiter().GetResult().Value;
                foreach (char c in secret)
                {
                    secureString.AppendChar(c);
                }

                secureString.MakeReadOnly();
                return secureString;
            });

        public static readonly Lazy<string> TargetEnvironmentName = new Lazy<string>(
            () =>
            {
                try
                {
                    return Environment.GetEnvironmentVariable(EnvironmentKey) ?? string.Empty;
                }
                catch (Exception)
                {
                    return string.Empty;
                }
            });

        public static readonly Lazy<X509Certificate2> S2SCert = new Lazy<X509Certificate2>(
            () =>
            {
                try
                {
                    string cert = Environment.GetEnvironmentVariable(S2SCertKeyname);
                    string pwd = Environment.GetEnvironmentVariable(S2SCertPasswordKeyname);

                    if (!string.IsNullOrWhiteSpace(cert))
                    {
                        if (!string.IsNullOrWhiteSpace(pwd))
                        {
                            return new X509Certificate2(Convert.FromBase64String(cert), pwd);
                        }

                        Console.WriteLine("Password values not specified.");
                    }
                    else
                    {
                        Console.WriteLine("Cert value not specified.");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                // Load from local cert store instead. This would only happen when running from a dev box or cloud test.
                Console.WriteLine("Loading from local cert store.");

                CertificateProvider certProvider = new CertificateProvider(new ConsoleLogger());

                string certSubject = TestData.CloudTestCertificate.Subject.Replace("CN=", string.Empty);

                try
                {
                    X509Certificate2 certificate = certProvider.GetClientCertificate(certSubject, StoreLocation.LocalMachine);

                    if (certificate != null)
                    {
                        return certificate;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                // If not in LocalMachine, try CurrentUser.
                return certProvider.GetClientCertificate(certSubject, StoreLocation.CurrentUser);
            },
            LazyThreadSafetyMode.PublicationOnly);

        // The following keys must be specified in the task properties setup in VSO.
        private const string EnvironmentKey = "TargetEnvironmentName";

        private const string S2SCertKeyname = "S2SCert";

        private const string S2SCertPasswordKeyname = "S2SCertPwd";

        private static string GetAadAccountCloseEventHubSecretName()
        {
            // The secret name corresponds to the secret value in KeyVault
            switch (TargetEnvironmentName.Value)
            {
                case EnvironmentNames.ContinuousIntegration1: return "aad-accountclose-ci1";
                case EnvironmentNames.ContinuousIntegration2: return "aad-accountclose-ci2";
				case EnvironmentNames.ContinuousIntegrationDev1: return "aad-accountclose-dev1";
                case EnvironmentNames.ContinuousIntegration1Proxy: return "aad-accountclose-proxytest";
                default: return "aad-accountclose-dev";
            }
        }

        private static async Task<string> GetAccessTokenAsync(string authority, string resource, string scope = null)
        {

            string[] scopes = new string[] { $"{resource}/.default" };
            var result = await ConfidentialCredential.GetTokenAsync(TestData.TestAadAppId, S2SCert.Value, new Uri(authority), scopes);
            return result?.AccessToken ?? throw new InvalidOperationException("Failed to get AAD JWT token");
        }

        private static string GetPrivacyMockEndpoint()
        {
            switch (TargetEnvironmentName.Value)
            {
                case EnvironmentNames.ContinuousIntegration1: return "https://sf-pxsmockci1.api.account.microsoft-int.com";
                case EnvironmentNames.ContinuousIntegration2: return "https://sf-pxsmockci2.api.account.microsoft-int.com";
				case EnvironmentNames.ContinuousIntegrationDev1: return "https://sf-pxsmockdev1.api.account.microsoft-int.com";
                case EnvironmentNames.ContinuousIntegration1Proxy: return "https://sf-pxsmockci1.api.account.microsoft-int.com";
                case EnvironmentNames.SandboxMw1P: return "https://sf-pxsmock.api.account.microsoft-int.com";
                case EnvironmentNames.SandboxSn3P: return "https://sf-pxsmock.api.account.microsoft-int.com";
                default: return ConfigurationManager.AppSettings["mockEndpoint"];
            }
        }

        private static string GetPxsEndpoint()
        {
            switch (TargetEnvironmentName.Value)
            {
                case EnvironmentNames.ContinuousIntegration1: return "https://sf-pxsci1.api.account.microsoft-int.com";
                case EnvironmentNames.ContinuousIntegration2: return "https://sf-pxsci2.api.account.microsoft-int.com";
                case EnvironmentNames.ContinuousIntegrationDev1: return "https://sf-pxsdev1.api.account.microsoft-int.com";
                case EnvironmentNames.ContinuousIntegration1Proxy: return "https://sf-proxytest.api.account.microsoft-int.com";
                case EnvironmentNames.SandboxMw1P: return "https://sf-pxs.api.account.microsoft-int.com";
                case EnvironmentNames.SandboxSn3P: return "https://sf-pxs.api.account.microsoft-int.com";
                default: return ConfigurationManager.AppSettings["endpoint"];
            }
        }
    }
}
