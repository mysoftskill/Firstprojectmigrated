// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.FunctionalTests.PrivacyExperience
{
    using global::Microsoft.VisualStudio.TestTools.UnitTesting;

    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Extensions;
    using Microsoft.OSGS.HttpClientCommon;

    using HttpClient = Microsoft.OSGS.HttpClientCommon.HttpClient;
    using TestConfiguration = Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.Config.TestConfiguration;

    /// <summary>
    ///  Functional tests that verifying AnaheimIdQueueWorker is functionable
    /// </summary>
    [TestClass]
    public class AnaheimIdQueueWorkerTests : TestBase
    {
        private static IHttpClient TestHttpClient => httpClient.Value;
        private static Uri TestBaseUrl => TestConfiguration.MockBaseUrl.Value;
        private static readonly Lazy<IHttpClient> httpClient = new Lazy<IHttpClient>(
            () =>
            {
                var certHandler = new WebRequestHandler();
                var client = new HttpClient(certHandler) { BaseAddress = TestConfiguration.ServiceEndpoint.Value };
                client.MessageHandler.AttachClientCertificate(TestConfiguration.S2SCert.Value);
                ServicePointManager.ServerCertificateValidationCallback += (o, c, ch, er) => true;
                return client;
            });


        [TestMethod, TestCategory("FCT")]
        public async Task FailedMessageShouldStayInQueueForRetry()
        {
            HttpResponseMessage response = await TestHttpClient.GetAsync(new Uri(TestBaseUrl, "anaheimid/processpcferror"), HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);
            Assert.IsNotNull(response);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod, TestCategory("FCT")]
        public async Task ThrottledMessageLeaseShouldBeRenewed()
        {
            HttpResponseMessage response = await TestHttpClient.GetAsync(new Uri(TestBaseUrl, "anaheimid/processthrottledmsg"), HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);
            Assert.IsNotNull(response);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
