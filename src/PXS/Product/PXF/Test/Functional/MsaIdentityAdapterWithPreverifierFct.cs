// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.FunctionalTests
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Helpers;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Osgs = Microsoft.OSGS.HttpClientCommon;
    using TestConfiguration = Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.Config.TestConfiguration;

    [TestClass]
    public class MsaIdentityAdapterWithPreverifierFct : TestBase
    {
        [TestInitialize]
        public void Initialize()
        {
            Sll.ResetContext();
            Sll.Context.Vector = new CorrelationVector();
        }

        [TestMethod, TestCategory("FCT")]
        public async Task GetMsaVerifierWithPreveifierSuccess()
        {
            // In INT, the test account must be unprotected for this to work (bypasses TFA requirement)
            // MBI_SSL_SA is strong auth.
            TestUser user = TestUsers.TimelineDeleteById;
            string userProxyTicket = await MockTestHooks.GetProxyTicketAsync(user).ConfigureAwait(false);

            // First call MSA RVS with refresh claim to get a verifier
            var httpClient = new Osgs.HttpClient();
            var baseUri = TestConfiguration.MockBaseUrl.Value;
            var relativePath = string.Format(CultureInfo.InvariantCulture, "msaRvs/GetGdprUserDeleteVerifierWithRefreshClaim");

            var request = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUri, relativePath));
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                              {
                                { "userProxyTicket", userProxyTicket },
                                { "userPuid",  user.Puid.ToString()}
                              });

            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            Assert.IsNotNull(response);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            
            // Get verifier from response
            var result = await HttpHelper.HandleHttpResponseAsync<AdapterResponse<string>>(response);

            // Assert the expiry date is more than 3 months in the future.
            var verifier1 = result.Result;
            var date = MsaIdentityServiceAdapter.GetExpiryTimeFromVerifier(verifier1);
            Assert.IsTrue(DateTimeOffset.Compare(date, DateTimeOffset.UtcNow.AddMonths(3)) > 0);

            // Next call MSA RVS to get verifier for actual delete command using verifer received from previous step.
            relativePath = string.Format(CultureInfo.InvariantCulture, "msaRvs/GetGdprUserDeleteVerifierForCommand");
            request = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUri, relativePath));
            
            var commandId = Guid.NewGuid();
            var xuid = "123";
            var datatype = Policies.Current.DataTypes.Ids.ProductAndServiceUsage.Value;
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                              {
                                { "commandId",  commandId.ToString() },
                                { "preVerifier",  verifier1},
                                { "xuid", xuid },
                                { "predicate", null },
                                { "datatype", datatype }
                              });

            response = await httpClient.SendAsync(request).ConfigureAwait(false);

            // Assert verifier is valid.
            Assert.IsNotNull(response);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            result = await HttpHelper.HandleHttpResponseAsync<AdapterResponse<string>>(response);
            var verifier2 = result.Result;

            // Check if the verifer is valid using PCF validation service
            relativePath = string.Format(CultureInfo.InvariantCulture, "msaRvs/VerifyUserDeleteVerifier");
            request = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUri, relativePath));
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                              {
                                { "commandId",  commandId.ToString() },
                                { "userPuid", user.Puid.ToString() },
                                { "preVerifier",  verifier2},
                                { "xuid", xuid },
                                { "predicate", null },
                                { "datatype", datatype }
                              });

            response = await httpClient.SendAsync(request).ConfigureAwait(false);

            Assert.IsNotNull(response);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            // Last check if we can renew the verifier before it expires
            relativePath = string.Format(CultureInfo.InvariantCulture, "msaRvs/GetGdprUserDeleteVerifierUsingPreVerifier");
            request = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUri, relativePath));
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                              {
                                { "preVerifier",  verifier1}
                              });

            response = await httpClient.SendAsync(request).ConfigureAwait(false);
            
            Assert.IsNotNull(response);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            result = await HttpHelper.HandleHttpResponseAsync<AdapterResponse<string>>(response);
            var verifier3 = result.Result;
            Assert.IsNotNull(verifier3);
            
            date = MsaIdentityServiceAdapter.GetExpiryTimeFromVerifier(verifier3);
            Assert.IsTrue(DateTimeOffset.Compare(date, DateTimeOffset.UtcNow.AddMonths(3)) > 0);

            long cidValue = MsaIdentityServiceAdapter.GetCidFromVerifier(verifier3);
            Assert.AreEqual(3641766018837987378, cidValue);
        }
    }
}
