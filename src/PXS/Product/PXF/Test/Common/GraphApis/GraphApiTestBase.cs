// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.GraphApis
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.IdentityModel.S2S.Tokens;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.Test.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Newtonsoft.Json;

    using TestConfiguration = Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.Config.TestConfiguration;

    /// <summary>
    ///     GraphApiTestBase
    /// </summary>
    [TestClass]
    public abstract class GraphApiTestBase
    {
        public class ErrorResponse
        {
            [JsonProperty("error")]
            public Error Error { get; set; }
        }

        /// <summary>
        ///     Empty Body used for some Graph Post calls.
        /// </summary>
        public class EmptyBody
        {
        }

        public class GraphTestTenant
        {
            public TestUser Admin;
            public string GraphAppId;
            public string S2SAppId;
            public string TenantId;
        };

        public GraphTestTenant TestIdentityHomeTenant = new GraphTestTenant
        {
            Admin = TestConfiguration.AadHomeTenantAdmin.Value,

            // 3rd party app is configured for creating Graph tokens.
            // Must login from this account to see the app: functional-test-admin@meepxs.onmicrosoft.com
            // https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/Overview/appId/90b23419-a7ce-4459-95e1-8f251ea7f606/isMSAApp/
            // app display name: meepxsfunctionaltest
            GraphAppId = "90b23419-a7ce-4459-95e1-8f251ea7f606",

            // 3rd party app registered @ https://portal.azure.com/#blade/Microsoft_AAD_IAM/ApplicationBlade/appId/feb76379-5080-4b88-86d0-7bef3558d507/objectId/dc2b9811-144b-4a68-bc5f-2dc548dd093e
            S2SAppId = "feb76379-5080-4b88-86d0-7bef3558d507",

            // meepxs
            TenantId = TestData.HomeTenantId
        };

        public GraphTestTenant TestIdentityResourceTenant = new GraphTestTenant
        {
            Admin = TestConfiguration.AadResourceTenantAdmin.Value,

            // 3rd party app is configured for creating Graph tokens.
            // Must login from this account to see the app: functional-test-admin@meepxsresource.onmicrosoft.com
            // https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/Overview/appId/f107c8c7-500f-406f-84b1-5b90576a8297/isMSAApp/
            // app display name: meepxsfunctionaltest2
            GraphAppId = "f107c8c7-500f-406f-84b1-5b90576a8297",

            // 3rd party app registered @ https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/Overview/appId/31e2ae73-1a3f-4104-9868-4007cc2ee6ce
            S2SAppId = "31e2ae73-1a3f-4104-9868-4007cc2ee6ce",

            // meepxsresource
            TenantId = TestData.ResourceTenantId
        };

        // Maps the test object ids to the expected http status codes for error responses
        public static Dictionary<int, (string TargetObject, HttpStatusCode HttpStatusCode)> TestAadRvsErrorObjectIds = new Dictionary<int, (string TargetObject, HttpStatusCode HttpStatusCode)>
        {
            { 400, (TestData.ObjectId400, HttpStatusCode.BadRequest) },
            { 401, (TestData.ObjectId401, HttpStatusCode.Unauthorized) },
            { 403, (TestData.ObjectId403, HttpStatusCode.Forbidden) },
            { 404, (TestData.ObjectId404, HttpStatusCode.NotFound) },
            { 405, (TestData.ObjectId405, HttpStatusCode.MethodNotAllowed) },
            { 409, (TestData.ObjectId409, HttpStatusCode.Conflict) },
            { 429, (TestData.ObjectId429, (HttpStatusCode)429) }
        };

        protected const string AppTokenAuthority = "https://login.microsoftonline.com/";

        protected const string TargetAadAudience = "https://pxs.api.account.microsoft-int.com";

        [TestInitialize]
        public void TestInitialize()
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
        }

        protected static async Task<HttpResponseMessage> CallGetAsync(string targetEndpoint, GraphTestTenant tenant)
        {
            HttpClient client = new HttpClient();

            // Retrieve a test admin credentials and access token to construct auth header.
            string authHeaderValue = await CreateAuthHeaderValue(tenant).ConfigureAwait(false);
            client.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestHeader.Authorization.ToString(), authHeaderValue);

            return await client.GetAsync(new Uri(TestConfiguration.ServiceEndpoint.Value, targetEndpoint)).ConfigureAwait(false);
        }

        protected static Task<HttpResponseMessage> CallPostAsync<T>(string targetEndpoint, GraphTestTenant tenant, T postContent)
        {
            return CallPostAsync(
                targetEndpoint,
                tenant,
                postContent,
                new Dictionary<string, string> { { "x-ms-gateway-serviceRoot", TestConfiguration.ServiceEndpoint.Value.ToString() } });
        }

        protected static async Task<HttpResponseMessage> CallPostAsync<T>(string targetEndpoint, GraphTestTenant tenant, T postContent, IDictionary<string, string> additionalHeaders)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Accept", "application/json;odata.metadata=none");

            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(TestConfiguration.ServiceEndpoint.Value, targetEndpoint)))
            {
                // Retrieve a test admin credentials and access token to construct auth header.
                string authHeaderValue = await CreateAuthHeaderValue(tenant).ConfigureAwait(false);
                httpRequestMessage.Headers.TryAddWithoutValidation(HttpRequestHeader.Authorization.ToString(), authHeaderValue);

                if (additionalHeaders != null)
                {
                    foreach (var header in additionalHeaders)
                    {
                        httpRequestMessage.Headers.Add(header.Key, header.Value);
                    }
                }

                httpRequestMessage.Content = new StringContent(JsonConvert.SerializeObject(postContent), Encoding.UTF8, "application/json");
                return await client.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);
            }
        }

        protected static async Task<string> CreateAuthHeaderValue(GraphTestTenant tenant)
        {
            string accessToken = await GetProtectedForwardedAccessTokenAsync(tenant).ConfigureAwait(false);

            string[] scopes = new string[] { $"{TargetAadAudience}/.default" };
            var result = await ConfidentialCredential.GetTokenAsync(tenant.S2SAppId, TestConfiguration.S2SCert.Value, new Uri(AppTokenAuthority + tenant.TenantId), scopes);
            string appToken = result?.AccessToken;

            Assert.IsNotNull(appToken);
            string authHeaderValue = TokenCreator.CreateMSAuth1_0PFATHeader(accessToken, appToken);
            return authHeaderValue;
        }

        protected static async Task<string> GetProtectedForwardedAccessTokenAsync(GraphTestTenant tenant)
        {
            string accessToken = await GraphAuthenticationHelper.GetGraphAccessTokenAsync(tenant.Admin.UserName, tenant.Admin.Password, tenant.GraphAppId).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new InvalidOperationException($"{nameof(accessToken)} Is Null Or WhiteSpace");
            }

            return TokenCreator.TransformProtectedForwardedToken(accessToken);
        }
    }
}
