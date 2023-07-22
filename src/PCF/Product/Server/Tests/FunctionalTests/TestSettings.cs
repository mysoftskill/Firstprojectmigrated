namespace PCF.FunctionalTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Reflection;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Validator.Configuration;
    using Microsoft.PrivacyServices.PXS.Command.CommandStatus;
    using Newtonsoft.Json;
    using Xunit;
    using Xunit.Abstractions;

    using PXSV1 = Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    public static class TestSettings
    {
        // AAD application for authenticating as a service using a username and password. Used when running tests in the lab.
        private const string KeyVaultAppId = "061be1ab-f7cb-4d44-bc8e-c0dfb357b7fc";

        private const string KeyVaultUriAME = "https://pcf-int-ame.vault.azure.net/";
        private const string KeyVaultUriMS = "https://ngp-nonprod-akv.vault.azure.net/";

        private static MicrosoftAccountAuthClient msaAuthClient;

        // Host name for the frontdoor.
        public static string ApiHostName { get; private set; }

        /// <summary>
        /// Test MSA Site ID
        /// </summary>
        public static long TestSiteId => 296170;

        /// <summary>
        /// Invalid MSA Site ID
        /// </summary>
        public static long TestInvalidSiteId => 296045;

        /// <summary>
        /// Test AAD MS Single Tenant App ID (PCF Test Data Agent PROD)
        /// </summary>
        public static string TestClientId => "7819dd7c-2f73-4787-9557-0e342743f34b";

        /// <summary>
        /// Test AAD AME Multi-Tenant App ID (NGP PCF NonProd)
        /// </summary>
        public static string TestClientIdAME => "061be1ab-f7cb-4d44-bc8e-c0dfb357b7fc";

        /// <summary>
        /// Test AAD AME Single Tenant App ID (NGP PCF Test)
        /// </summary>
        public static string TestInvalidClientIdAME => "88643b8b-52c9-45dd-9060-b08d8fdfd298";

        /// <summary>
        /// An HTTP client that ignores all SSL errors.
        /// </summary>
        public static HttpClient HttpClient { get; }

        public static TestSecretClient SecretClient { get; }

        [SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        static TestSettings()
        {
            // The environment variables referenced here are set as part of the release definition.
            // When not set, we can therefore infer that we are running on an engineer's onebox machine.
            string environmentVariable = Environment.GetEnvironmentVariable("TargetEnvironmentName")?.ToUpperInvariant();
            environmentVariable = environmentVariable ?? "LOCALHOST";

            WebRequestHandler handler = new WebRequestHandler();
            HttpClient = new HttpClient(handler);  // lgtm [cs/httpclient-checkcertrevlist-disabled]

            var KeyVaultUri = environmentVariable == "LOCALHOST" ? KeyVaultUriMS : KeyVaultUriAME;
            X509Certificate2 cert = CertificateFinder.FindCertificateByName("cloudtest.privacy.microsoft-int.ms", false);
            Assert.NotNull(cert);
            SecretClient = new CertificateSecretClient(
                KeyVaultAppId,
                cert,
                KeyVaultUri);

            handler.ServerCertificateValidationCallback = delegate { return true; };

            if (environmentVariable == "PXSCI1-TEST-MW1P")
            {
                InitializeCi1();
            }
            else if (environmentVariable == "PXSCI2-TEST-MW1P")
            {
                InitializeCi2();
            }
            else if (environmentVariable == "PXSDEV1-TEST-MW1P")
            {
                InitializeDev1();
            }
            else if (environmentVariable == "LOCALHOST")
            {
                InitializeOnebox();
            }
            else if (environmentVariable == "ADGCS-INT")
            {
                InitializeInt();
            }
            else
            {
                throw new ArgumentOutOfRangeException(environmentVariable);
            }
        }

        private static void InitializeDev1()
        {
            ApiHostName = "sf-dev1.pcf.privacy.microsoft-int.com";
        }

        private static void InitializeCi2()
        {
            ApiHostName = "sf-ci2.pcf.privacy.microsoft-int.com";
        }

        private static void InitializeCi1()
        {
            ApiHostName = "sf-ci1.pcf.privacy.microsoft-int.com";
        }

        private static void InitializeInt()
        {
            ApiHostName = "pcf.privacy.microsoft-int.com";
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private static void InitializeOnebox()
        {
            ApiHostName = "LOCALHOST";

            // In OneBox environment, this config file can be overwritten by PXS functional test. To make sure we have the correct config for running PCF FCT, always copy the file
            const string rpsConfigFilename = "rpsserver.xml";

            Assembly currentAssembly = typeof(TestSettings).Assembly;
            string rpsConfigFile = currentAssembly.GetManifestResourceNames().Single(x => x.IndexOf(rpsConfigFilename, StringComparison.OrdinalIgnoreCase) >= 0);

            string targetRpsConfigFilepath = Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles"), "Microsoft Passport RPS", "config", rpsConfigFilename);

            using (var sourceStream = currentAssembly.GetManifestResourceStream(rpsConfigFile))
            using (var targetStream = File.Open(targetRpsConfigFilepath, FileMode.Create))
            {
                sourceStream.CopyTo(targetStream);
            }
        }

        /// <summary>
        /// Gets the command feed client configuration.
        /// </summary>
        public static CommandFeedEndpointConfiguration TestEndpointConfig
        {
            get =>
                new CommandFeedEndpointConfiguration(
                    new Uri("https://login.live-int.com/pksecure/oauth20_clientcredentials.srf"),
                    "pcf.privacy.microsoft-int.com",
                    "https://login.microsoftonline.com/microsoft.onmicrosoft.com",
                    "https://pcf.privacy.microsoft-int.com",
                    TestSettings.ApiHostName,
                    PcvEnvironment.Preproduction,
                    false);
        }

        /// <summary>
        /// Gets the command feed client configuration.
        /// </summary>
        public static CommandFeedEndpointConfiguration TestEndpointConfigAME
        {
            get =>
                new CommandFeedEndpointConfiguration(
                    new Uri("https://login.live-int.com/pksecure/oauth20_clientcredentials.srf"),
                    "pcf.privacy.microsoft-int.com",
                    "https://login.microsoftonline.com/MSAzureCloud.onmicrosoft.com",
                    "https://MSAzureCloud.onmicrosoft.com/613e14a9-7c60-4f8b-863c-f719e68cd8db",
                    TestSettings.ApiHostName,
                    PcvEnvironment.Preproduction,
                    false);
        }

        /// <summary>
        /// Gets the command feed client configuration for AME apps against the MSFT tenant.
        /// </summary>
        public static CommandFeedEndpointConfiguration TestEndpointConfigAMEMultitenant
        {
            get =>
                new CommandFeedEndpointConfiguration(
                    new Uri("https://login.live-int.com/pksecure/oauth20_clientcredentials.srf"),
                    "pcf.privacy.microsoft-int.com",
                    "https://login.microsoftonline.com/microsoft.onmicrosoft.com",
                    "https://MSAzureCloud.onmicrosoft.com/613e14a9-7c60-4f8b-863c-f719e68cd8db",
                    TestSettings.ApiHostName,
                    PcvEnvironment.Preproduction,
                    false);
        }

        /// <summary>
        /// Gets the STS certificate asynchronously.
        /// </summary>
        public static Task<X509Certificate2> GetStsCertificateAsync()
        {
            return SecretClient.GetPrivateCertificateAsync("cloudtest-privacy-int");
        }

        /// <summary>
        /// Inserts a set of PXS commands into PCF.
        /// </summary>
        public static async Task InsertPxsCommandAsync(IEnumerable<PXSV1.PrivacyRequest> privacyRequests, ITestOutputHelper outputHelper)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, new Uri($"https://{TestSettings.ApiHostName}/pxs/commands"))
            {
                Content = new StringContent(JsonConvert.SerializeObject(privacyRequests.ToArray()))
            };

            HttpResponseMessage response = await TestSettings.SendWithS2SAync(request, outputHelper);
            string body = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        /// Queries command status.
        /// </summary>
        public static async Task<CommandStatusResponse> GetCommandStatusAsync(Guid commandId, ITestOutputHelper outputHelper)
        {
            Uri getStatusUri = new Uri($"https://{TestSettings.ApiHostName}/coldstorage/v3/status/commandid/{commandId:n}");
            var request = new HttpRequestMessage(HttpMethod.Get, getStatusUri);
            var response = await TestSettings.SendWithS2SAync(request, outputHelper);

            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                var responseItem = JsonConvert.DeserializeObject<CommandStatusResponse>(responseBody);
                Assert.Equal(commandId, responseItem.CommandId);

                return responseItem;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Attaches an S2S header to the given request and sends it.
        /// </summary>
        public static async Task<HttpResponseMessage> SendWithS2SAync(HttpRequestMessage requestMessage, ITestOutputHelper outputHelper, TimeSpan? timeout = null)
        {
            requestMessage.Headers.Add("X-PCF-Test-Case", "true");

            using (var handler = new WebRequestHandler())
            {
                handler.ServerCertificateValidationCallback = delegate { return true; };
                var cert = await GetStsCertificateAsync();
                handler.ClientCertificates.Add(cert);

                using (var client = new HttpClient(handler))  // lgtm [cs/httpclient-checkcertrevlist-disabled]
                {
                    if (timeout == null)
                    {
                        timeout = TimeSpan.FromMinutes(2);
                    }

                    client.Timeout = timeout.Value;

                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue(
                        "MSAS2S",
                        await GetMsaS2STicketAsync());

                    outputHelper?.WriteLine($"Sending {requestMessage.Method} to {requestMessage.RequestUri}");

                    var response = await client.SendAsync(requestMessage);

                    if (!response.Headers.TryGetValues("MS-CV", out var cvValues))
                    {
                        cvValues = new[] { "(null)" };
                    }

                    outputHelper?.WriteLine($"Got response: {response.StatusCode}, CV = {string.Join(",", cvValues)}");

                    if (response.Content != null)
                    {
                        string body = await response.Content.ReadAsStringAsync();
                        outputHelper?.WriteLine("Response Body: ");
                        outputHelper?.WriteLine(body);
                    }

                    return response;
                }
            }
        }

        /// <summary>
        /// Gets a ticket to talk to PCF.
        /// </summary>
        public static async Task<string> GetMsaS2STicketAsync()
        {
            if (msaAuthClient == null)
            {
                var cert = await GetStsCertificateAsync();
                var endpointConfig = TestEndpointConfig;

                msaAuthClient = new MicrosoftAccountAuthClient(
                    TestSiteId,
                    new NullCommandFeedLogger(),
                    new DefaultHttpClient(cert),
                    endpointConfig);
            }

            string token = await msaAuthClient.GetAccessTokenAsync();
            return token;
        }

        private class NullCommandFeedLogger : CommandFeedLogger
        {
            public override void UnhandledException(Exception ex)
            {
            }
        }
    }
}
