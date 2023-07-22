// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>using System;

namespace Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net;
    using System.Net.Http;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Membership.MemberServices.Test.Common;
    using Microsoft.Windows.Services.AuthN.Client.S2S;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents the family of a user
    /// </summary>
    public class FamilyModel
    {
        private const string BaseUriTemplate = "/api/v1.0/groups(idNamespace='Puid',id='{0}')?$expand=members($select=idNamespace,id,jsonWebToken)";
        private const string IntFamilyBaseUri = "https://management.family.microsoft-int.com";
        private const string ProdFamilyBaseUri = "https://management.family.microsoft.com";

        public static readonly Lazy<FamilyClientConfiguration> IntFamilyClientConfiguration =
            new Lazy<FamilyClientConfiguration>(
                () =>
                {
                    var config = new FamilyClientConfiguration();
                    config.MsaAuthenticationUri = new Uri(TestData.IntMsaOathEndpoint);
                    config.ClientCertificate = TestData.CloudTestCertificate.LoadFromStore();
                    config.FamilyServiceTargetSite = "family.api.account.microsoft-int.com";
                    config.FamilyBaseUri = IntFamilyBaseUri;
                    return config;
                });

        public static readonly Lazy<FamilyClientConfiguration> PpeFamilyClientConfiguration =
            new Lazy<FamilyClientConfiguration>(
                () =>
                {
                    var config = new FamilyClientConfiguration();
                    config.MsaAuthenticationUri = new Uri(TestData.ProdMsaOathEndpoint);
                    config.ClientCertificate = TestData.PpeTestS2SCertificate.LoadFromStore();
                    config.FamilyServiceTargetSite = "family.api.account.microsoft.com";
                    config.FamilyBaseUri = ProdFamilyBaseUri;
                    return config;
                });

        public static readonly Lazy<FamilyClientConfiguration> ProdFamilyClientConfiguration =
            new Lazy<FamilyClientConfiguration>(
                () => 
                {
                    var config = new FamilyClientConfiguration();
                    config.MsaAuthenticationUri = new Uri(TestData.ProdMsaOathEndpoint);
                    config.ClientCertificate = TestData.ProdTestS2SCertificate.LoadFromStore();
                    config.FamilyServiceTargetSite = "family.api.account.microsoft.com";
                    config.FamilyBaseUri = ProdFamilyBaseUri;
                    return config;
                });

        /// <summary>
        /// Gets the user's family model
        /// </summary>
        /// <param name="puid">Puid of the user</param>
        /// <param name="proxyTicket">Proxy ticket for the user</param>
        /// <returns>Family model</returns>
        public static Task<FamilyModel> GetFamilyAsync(long puid, string proxyTicket)
        {
            return GetFamilyAsync(puid, proxyTicket, IntFamilyClientConfiguration.Value);
        }

        /// <summary>
        /// Gets the user's family model with the given clientCertificate with INT configuration
        /// </summary>
        /// <param name="puid">Puid of the user</param>
        /// <param name="proxyTicket">Proxy ticket for the user</param>
        /// <param name="clientCertificate">The client cert.</param>
        /// <returns>Family model</returns>
        public static Task<FamilyModel> GetFamilyAsync(long puid, string proxyTicket, X509Certificate2 clientCertificate)
        {
            FamilyClientConfiguration familyClientConfiguration = new FamilyClientConfiguration();
            familyClientConfiguration.ClientCertificate = clientCertificate;
            familyClientConfiguration.MsaAuthenticationUri = new Uri(TestData.IntMsaOathEndpoint);
            familyClientConfiguration.FamilyBaseUri = IntFamilyBaseUri;
            familyClientConfiguration.FamilyServiceTargetSite = "family.api.account.microsoft-int.com";

            return GetFamilyAsync(puid, proxyTicket, familyClientConfiguration);
        }

        /// <summary>
        /// Gets the user's family model
        /// </summary>
        /// <param name="puid">Puid of the user</param>
        /// <param name="proxyTicket">Proxy ticket for the user</param>
        /// <param name="clientConfiguration">The client cert.</param>
        /// <returns>Family model</returns>
        public static async Task<FamilyModel> GetFamilyAsync(long puid, string proxyTicket, FamilyClientConfiguration clientConfiguration)
        {
            S2SAuthClient authClient = S2SAuthClient.Create(TestData.TestSiteIdIntProd, clientConfiguration.ClientCertificate, clientConfiguration.MsaAuthenticationUri);
            string appTicket = await authClient.GetAccessTokenAsync(clientConfiguration.FamilyServiceTargetSite, CancellationToken.None).ConfigureAwait(false);
            string requestUri = string.Format(CultureInfo.InvariantCulture, clientConfiguration.FamilyBaseUri + BaseUriTemplate, puid);

            var webHandler = new WebRequestHandler();
            webHandler.CheckCertificateRevocationList = true;
            webHandler.ClientCertificates.Add(clientConfiguration.ClientCertificate);
            var httpClient = new HttpClient(webHandler);
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                requestUri);
            request.Headers.Add("X-S2SAccessToken", appTicket);
            request.Headers.Add("X-S2SProxyTicket", proxyTicket);

            HttpResponseMessage response = await httpClient.SendAsync(request).ConfigureAwait(false);
            string responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new WebException(string.Format(CultureInfo.InvariantCulture, "Got error {0} from Family service, with error message = {1}", response.StatusCode, responseContent));
            }

            // Get the content and return it
            var familyModel = JsonConvert.DeserializeObject<FamilyModel>(responseContent);
            return familyModel;
        }

        [JsonProperty("idNamespace")]
        public string IdNamespace { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("members")]
        public IList<FamilyMemberModel> Members { get; set; }

        public class FamilyMemberModel
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("jsonWebToken")]
            public string JsonWebToken { get; set; }
        }

        public class FamilyClientConfiguration
        {
            public X509Certificate2 ClientCertificate { get; set; }

            public string FamilyServiceTargetSite { get; set; }

            public string FamilyBaseUri { get; set; }

            public Uri MsaAuthenticationUri { get; set; }
        }
    }
}
