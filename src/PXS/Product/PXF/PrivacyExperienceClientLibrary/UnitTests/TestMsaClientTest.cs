// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.UnitTests
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Models;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    /// <summary>
    ///     TestMsaClient Test
    /// </summary>
    [TestClass]
    public class TestMsaClientTest : ClientTestBase
    {
        [TestMethod]
        public async Task PrivacySubjectClient_TestMsaCloseAsync_Sends_MsaCloseOperationRequest_When_MsaSelfAuthSubject()
        {
            // Arrange
            PrivacyExperienceClient testClient = this.CreateBasicClient();

            var args = new TestMsaCloseArgs(this.TestUserProxyTicket)
            {
                CorrelationVector = new Guid().ToString(),
                RequestId = new Guid().ToString()
            };

            // Act
            await testClient.TestMsaCloseAsync(args).ConfigureAwait(false);

            // Assert
            this.ValidateHttpClientMockHeader(HeaderNames.AccessToken, this.TestAccessToken);
            this.ValidateHttpClientMockHeader(HeaderNames.ClientRequestId, args.RequestId);
            this.ValidateHttpClientMockHeader(HeaderNames.CorrelationVector, args.CorrelationVector);

            //  MSA self auth subject will result in user proxy ticket passed through headers.
            this.ValidateHttpClientMockHeader(HeaderNames.ProxyTicket, this.TestUserProxyTicket);
            this.MockHttpClient.Verify(c => c.SendAsync(It.Is<HttpRequestMessage>(request => request != null), HttpCompletionOption.ResponseContentRead));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task PrivacySubjectClient_TestMsaCloseAsync_Throws_When_Args_Null()
        {
            PrivacyExperienceClient testClient = this.CreateBasicClient();
            await testClient.TestMsaCloseAsync(null).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Test_TestForceCommandCompletion()
        {
            // Arrange
            PrivacyExperienceClient testClient = this.CreateBasicClient();

            Guid id = Guid.NewGuid();
            var args = new TestForceCompletionArgs(this.TestUserProxyTicket, id)
            {
                CorrelationVector = new Guid().ToString(),
                RequestId = new Guid().ToString()
            };

            // Act
            await testClient.TestForceCommandCompletionAsync(args).ConfigureAwait(false);

            // Assert
            this.ValidateHttpClientMockHeader(HeaderNames.AccessToken, this.TestAccessToken);
            this.ValidateHttpClientMockHeader(HeaderNames.ClientRequestId, args.RequestId);
            this.ValidateHttpClientMockHeader(HeaderNames.CorrelationVector, args.CorrelationVector);
            this.ValidateHttpClientMockHeader(HeaderNames.ProxyTicket, this.TestUserProxyTicket);

            string expectedApiPath = $"v1/privacyrequest/forcecomplete?commandId={id}";
            this.MockHttpClient.Verify(
                c => c.SendAsync(It.Is<HttpRequestMessage>(m => m.RequestUri.OriginalString.Equals(expectedApiPath)), HttpCompletionOption.ResponseContentRead));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task Test_TestForceCommandCompletion_ArgsNull()
        {
            PrivacyExperienceClient testClient = this.CreateBasicClient();
            await testClient.TestForceCommandCompletionAsync(null).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Test_TestGetAgentStatistics()
        {
            // Arrange
            PrivacyExperienceClient testClient = this.CreateBasicClient();

            Guid id = Guid.NewGuid();
            var args = new TestGetAgentStatisticsArgs(this.TestUserProxyTicket, id)
            {
                CorrelationVector = new Guid().ToString(),
                RequestId = new Guid().ToString()
            };

            // Act
            await testClient.TestGetAgentStatisticsAsync(args).ConfigureAwait(false);

            // Assert
            this.ValidateHttpClientMockHeader(HeaderNames.AccessToken, this.TestAccessToken);
            this.ValidateHttpClientMockHeader(HeaderNames.ClientRequestId, args.RequestId);
            this.ValidateHttpClientMockHeader(HeaderNames.CorrelationVector, args.CorrelationVector);
            this.ValidateHttpClientMockHeader(HeaderNames.ProxyTicket, this.TestUserProxyTicket);

            string expectedApiPath = $"v1/privacyrequest/agentqueuestats?agentId={id}";
            this.MockHttpClient.Verify(
                c => c.SendAsync(It.Is<HttpRequestMessage>(m => m.RequestUri.OriginalString.Equals(expectedApiPath)), HttpCompletionOption.ResponseContentRead));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task Test_TestGetAgentStatistics_ArgsNull()
        {
            PrivacyExperienceClient testClient = this.CreateBasicClient();
            await testClient.TestGetAgentStatisticsAsync(null).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Test_TestGetCommandStatusById()
        {
            // Arrange
            PrivacyExperienceClient testClient = this.CreateBasicClient();

            Guid commandId = Guid.NewGuid();
            var args = new TestGetCommandStatusByIdArgs(this.TestUserProxyTicket, commandId)
            {
                CorrelationVector = new Guid().ToString(),
                RequestId = new Guid().ToString()
            };

            // Act
            await testClient.TestGetCommandStatusByIdAsync(args).ConfigureAwait(false);

            // Assert
            this.ValidateHttpClientMockHeader(HeaderNames.AccessToken, this.TestAccessToken);
            this.ValidateHttpClientMockHeader(HeaderNames.ClientRequestId, args.RequestId);
            this.ValidateHttpClientMockHeader(HeaderNames.CorrelationVector, args.CorrelationVector);
            this.ValidateHttpClientMockHeader(HeaderNames.ProxyTicket, this.TestUserProxyTicket);

            string expectedApiPath = $"v1/privacyrequest/testrequestbyid?commandId={commandId}";
            this.MockHttpClient.Verify(
                c => c.SendAsync(It.Is<HttpRequestMessage>(m => m.RequestUri.OriginalString.Equals(expectedApiPath)), HttpCompletionOption.ResponseContentRead));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task Test_TestGetCommandStatusById_ArgsNull()
        {
            PrivacyExperienceClient testClient = this.CreateBasicClient();
            await testClient.TestGetCommandStatusByIdAsync(null).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Test_TestGetCommandStatusesByUser()
        {
            // Arrange
            PrivacyExperienceClient testClient = this.CreateBasicClient();

            var args = new PrivacyExperienceClientBaseArgs(this.TestUserProxyTicket)
            {
                CorrelationVector = new Guid().ToString(),
                RequestId = new Guid().ToString()
            };

            // Act
            await testClient.TestGetCommandStatusesAsync(args).ConfigureAwait(false);

            // Assert
            this.ValidateHttpClientMockHeader(HeaderNames.AccessToken, this.TestAccessToken);
            this.ValidateHttpClientMockHeader(HeaderNames.ClientRequestId, args.RequestId);
            this.ValidateHttpClientMockHeader(HeaderNames.CorrelationVector, args.CorrelationVector);
            this.ValidateHttpClientMockHeader(HeaderNames.ProxyTicket, this.TestUserProxyTicket);

            string expectedApiPath = "v1/privacyrequest/testrequestsbyuser";
            this.MockHttpClient.Verify(
                c => c.SendAsync(It.Is<HttpRequestMessage>(m => m.RequestUri.OriginalString.Equals(expectedApiPath)), HttpCompletionOption.ResponseContentRead));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task Test_TestGetCommandStatusesByUser_ArgsNull()
        {
            PrivacyExperienceClient testClient = this.CreateBasicClient();
            await testClient.TestGetCommandStatusesAsync(null).ConfigureAwait(false);
        }
    }
}
