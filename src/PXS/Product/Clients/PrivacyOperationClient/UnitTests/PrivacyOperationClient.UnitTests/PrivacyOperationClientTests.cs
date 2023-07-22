// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.PrivacyServices.PrivacyOperation.Client.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Identity.Client;
    using Microsoft.OSGS.HttpClientCommon;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.PrivacyOperation.Client.Clients.Interfaces;
    using Microsoft.PrivacyServices.PrivacyOperation.Client.Models;
    using Microsoft.PrivacyServices.PrivacyOperation.Contracts;
    using Microsoft.PrivacyServices.PrivacyOperation.Contracts.PrivacySubject;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Newtonsoft.Json;

    [TestClass]
    public class PrivacyOperationClientTests
    {
        private Mock<IPrivacyOperationAuthClient> authClient;

        private Mock<IHttpClient> httpClient;

        private IPrivacyOperationClient poClient;

        [TestMethod]
        public async Task DeleteSuccess()
        {
            var request = new DeleteOperationArgs
            {
                EndTime = DateTimeOffset.UtcNow,
                StartTime = DateTimeOffset.UtcNow.AddDays(-1),
                CorrelationVector = "I'm a cV",
                Subject = new MsaSelfAuthSubject("I'm a user proxy ticket"),
                DataTypes = new List<string> { "dataType1", "dataType2" },
                UserAssertion = new UserAssertion("I'maJWT")
            };

            Guid id1 = Guid.NewGuid();
            Guid id2 = Guid.NewGuid();

            var expectedResponse = new DeleteOperationResponse
            {
                Ids = new List<Guid> { id1, id2 }
            };

            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonConvert.SerializeObject(expectedResponse)) };
            httpResponseMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            this.httpClient.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<HttpCompletionOption>()))
                .Returns(Task.FromResult(httpResponseMessage));

            DeleteOperationResponse response = await this.poClient.PostDeleteRequestAsync(request).ConfigureAwait(false);

            Assert.IsNotNull(response.Ids);
            Assert.AreEqual(2, response.Ids.Count);
            Assert.IsTrue(response.Ids.Contains(id1));
            Assert.IsTrue(response.Ids.Contains(id2));
        }

        [TestMethod]
        public async Task ExportSuccess()
        {
            var request = new ExportOperationArgs
            {
                EndTime = DateTimeOffset.UtcNow,
                StartTime = DateTimeOffset.UtcNow.AddDays(-1),
                CorrelationVector = "I'm a cV",
                Subject = new MsaSelfAuthSubject("I'm a user proxy ticket"),
                DataTypes = new List<string> { "dataType1", "dataType2" },
                StorageLocationUri = new Uri("https://storagelocation"),
                UserAssertion = new UserAssertion("I'maJWT")
            };

            Guid id1 = Guid.NewGuid();

            var expectedResponse = new ExportOperationResponse
            {
                Ids = new List<Guid> { id1 }
            };

            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonConvert.SerializeObject(expectedResponse)) };
            httpResponseMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            this.httpClient.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<HttpCompletionOption>()))
                .Returns(Task.FromResult(httpResponseMessage));

            ExportOperationResponse response = await this.poClient.PostExportRequestAsync(request).ConfigureAwait(false);

            Assert.IsNotNull(response.Ids);
            Assert.AreEqual(1, response.Ids.Count);
            Assert.IsTrue(response.Ids.Contains(id1));
        }

        [TestMethod]
        public async Task ListSucccess()
        {
            var request = new ListOperationArgs
            {
                CorrelationVector = "I'm a cV",
                UserAssertion = new UserAssertion("assertion")
            };

            Guid Id1 = Guid.NewGuid();
            Guid Id2 = Guid.NewGuid();

            var msaSubject = new MsaSubject
            {
                Puid = 123,
                Anid = "123",
                Opid = "123"
            };

            var aadSubject = new AadSubject
            {
                ObjectId = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                OrgIdPUID = 123
            };

            var expectedResponse = new List<PrivacyRequestStatus>
            {
                new PrivacyRequestStatus(
                    Id1,
                    PrivacyRequestType.Delete,
                    DateTimeOffset.UtcNow.AddDays(-1),
                    DateTimeOffset.UtcNow,
                    msaSubject,
                    new List<string> { "foo" },
                    string.Empty,
                    PrivacyRequestState.Completed,
                    new Uri("https://storagelocation"),
                    42.0),
                new PrivacyRequestStatus(
                    Id2,
                    PrivacyRequestType.Export,
                    DateTimeOffset.UtcNow.AddDays(-1),
                    DateTimeOffset.UtcNow,
                    aadSubject,
                    new List<string> { "foo" },
                    string.Empty,
                    PrivacyRequestState.Completed,
                    new Uri("https://storagelocation2"),
                    42.0)
            };

            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonConvert.SerializeObject(expectedResponse)) };
            httpResponseMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            this.httpClient.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<HttpCompletionOption>()))
                .Returns(Task.FromResult(httpResponseMessage));

            IList<PrivacyRequestStatus> response = await this.poClient.ListRequestsAsync(request).ConfigureAwait(false);

            Assert.IsNotNull(response);
            Assert.AreEqual(2, response.Count);
            Assert.IsNotNull(response.FirstOrDefault(o => Equals(Id1, o.Id)));
            Assert.IsNotNull(response.FirstOrDefault(o => Equals(Id2, o.Id)));
        }

        [TestInitialize]
        public void TestInitialize()
        {
            this.authClient = new Mock<IPrivacyOperationAuthClient>();
            var authenticationHeaderValue = new AuthenticationHeaderValue("Bearer", "I'm an AAD token");
            this.authClient.Setup(a => a.GetAadAuthToken(CancellationToken.None, It.IsAny<UserAssertion>()))
                .Returns(Task.FromResult(authenticationHeaderValue));

            this.httpClient = new Mock<IHttpClient>();

            this.poClient = new PrivacyOperationClient(this.httpClient.Object, this.authClient.Object);
        }
    }
}
