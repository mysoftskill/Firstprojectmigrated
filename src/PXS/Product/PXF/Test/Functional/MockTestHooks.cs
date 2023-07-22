// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.FunctionalTests
{
    using System;
    using System.Globalization;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.TableStorage;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.Config;
    using Microsoft.Membership.MemberServices.PrivacyMockService.Contracts;
    using Microsoft.Membership.MemberServices.Test.Common;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    using Osgs = Microsoft.OSGS.HttpClientCommon;

    public static class MockTestHooks
    {
        private static async Task<T> GetPrivacyRequestInternalAsync<T>(string relativeUri)
        {
            Uri baseUri = TestConfiguration.MockBaseUrl.Value;
            using (var httpMessageHandler = new WebRequestHandler())
            {
                httpMessageHandler.ServerCertificateValidationCallback = (s, c, ch, e) => true;

                using (var httpClient = new Osgs.HttpClient(httpMessageHandler))
                using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri(baseUri, relativeUri)))
                {
                    var response = await httpClient.SendAsync(requestMessage).ConfigureAwait(false);

                    T responseObject = default;

                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        return default;
                    }

                    if (response.Content != null)
                    {
                        responseObject = await response.Content.ReadAsAsync<T>().ConfigureAwait(false);
                    }

                    return responseObject;
                }
            }
        }

        public static async Task<PrivacyRequest> GetPrivacyRequestAsync(Guid objectId, Guid tenantId)
        {
            var relativePath = string.Format(CultureInfo.InvariantCulture, $"pcf/pxs/test/commands?objectId={objectId.ToString()}&tenantId={tenantId.ToString()}");
            return await GetPrivacyRequestInternalAsync<PrivacyRequest>(relativePath);
        }

        public static async Task<PrivacyRequest> GetPrivacyRequestAsync(long msaPuid)
        {
            var relativePath = string.Format(CultureInfo.InvariantCulture, $"pcf/pxs/test/msa/commands?msaPuid={msaPuid}");
            return await GetPrivacyRequestInternalAsync<PrivacyRequest>(relativePath);
        }

        /// <summary>
        ///     Gets a proxy ticket for the given test user account
        /// </summary>
        /// <param name="testUser">The test user.</param>
        /// <param name="authPolicy">The MSA auth policy for the user (ie. MBI_SSL, MBI_SSL_SA)</param>
        /// <returns>Proxy ticket for this user</returns>
        /// <remarks>Uses the RPS installation on the mock to get a proxy ticket from the user ticket</remarks>
        public static Task<string> GetProxyTicketAsync(TestUser testUser, string authPolicy = "MBI_SSL_SA")
        {
            return GetProxyTicketAsync(TestConfiguration.MockBaseUrl.Value, testUser.UserName, testUser.Password, authPolicy);
        }

        /// <summary>
        ///     Gets a proxy ticket for the given test user account
        /// </summary>
        /// <param name="mockBaseUrl">Base url of the mock instance</param>
        /// <param name="userName">User name for test account</param>
        /// <param name="password">Password for test account</param>
        /// <param name="authPolicy">The MSA auth policy for the user (ie. MBI_SSL, MBI_SSL_SA)</param>
        /// <returns>Proxy ticket for this user</returns>
        /// <remarks>Uses the RPS installation on the mock to get a proxy ticket from the user ticket</remarks>
        public static async Task<string> GetProxyTicketAsync(
            Uri mockBaseUrl,
            string userName,
            string password,
            string authPolicy = "MBI_SSL_SA")
        {
            UserProxyTicketProvider userProxyTicketProvider = new UserProxyTicketProvider(TestData.IntUserTicketConfiguration(authPolicy));
            var userTicket = await userProxyTicketProvider.GetUserTicket(userName, password).ConfigureAwait(false);
            var request = new ProxyTicketRequest
            {
                UserTicket = userTicket
            };

            var httpMessageHandler = new WebRequestHandler();
            httpMessageHandler.ServerCertificateValidationCallback = (s, c, ch, e) => true;

            var httpClient = new Osgs.HttpClient(httpMessageHandler);
            var baseUri = mockBaseUrl;
            var relativePath = string.Format(CultureInfo.InvariantCulture, "v1/my/testhook/getproxyticket");

            // Post request to the mock's test hook controller
            Uri targetUri = new Uri(baseUri, relativePath);
            Console.WriteLine($"Target environment: {TestConfiguration.TargetEnvironmentName.Value}");
            Console.WriteLine($"Target Uri: {targetUri}");
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, targetUri);
            var response = await httpClient.SendObjectAsync(requestMessage, request).ConfigureAwait(false);

            var responseObject = await response.Content.ReadAsAsync<ProxyTicketResponse>().ConfigureAwait(false);

            if (responseObject == null)
            {
                throw new InvalidOperationException($"Response for retrieving proxy ticket was null. Response Status code: {response.StatusCode}");
            }

            return responseObject.ProxyTicket;
        }

        /// <summary>
        ///     Posts a test hook to the PrivacyPartnerMock
        /// </summary>
        /// <param name="mockBaseUrl">Base url of the mock instance</param>
        /// <param name="puid">Puid of the test user</param>
        /// <param name="resource">Which resource should be impacted</param>
        /// <param name="action">What action to take on the resource</param>
        /// <returns>Response message</returns>
        public static async Task<HttpResponseMessage> PostTestHookAsync(
            Uri mockBaseUrl,
            long puid,
            string resource,
            string action)
        {
            var httpMessageHandler = new WebRequestHandler();
            httpMessageHandler.ServerCertificateValidationCallback = (s, c, ch, e) => true;

            var httpClient = new Osgs.HttpClient(httpMessageHandler);
            var baseUri = mockBaseUrl;
            var relativePath = string.Format(CultureInfo.InvariantCulture, "v1/{0}/testhook/{1}/{2}", puid, resource, action);
            var response = await httpClient.PostAsync(new Uri(baseUri, relativePath), null).ConfigureAwait(false);

            return response;
        }

        public static async Task<AccountCloseDeadLetterStorage> GetDeadLetterItemAsync(Guid tenantId, Guid objectId)
        {
            var relativePath = string.Format(CultureInfo.InvariantCulture, $"pcf/pxs/test/deadletter?tenantId={tenantId}&objectId={objectId}");
            return await GetPrivacyRequestInternalAsync<AccountCloseDeadLetterStorage>(relativePath);
        }
    }
}
