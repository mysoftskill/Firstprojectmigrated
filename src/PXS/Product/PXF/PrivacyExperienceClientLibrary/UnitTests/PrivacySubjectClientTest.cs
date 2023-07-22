// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.PrivacySubject;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Models.PrivacySubject;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    /// <summary>
    ///     PrivacySubjectClient Test
    /// </summary>
    [TestClass]
    public class PrivacySubjectClientTest : ClientTestBase
    {
        [TestMethod]
        public async Task PrivacySubjectClient_DeleteByTypesAsync_SendsDeleteOperationRequest_MsaSelfAuthSubject()
        {
            var testClient = this.CreateBasicClient();

            var privacySubject = new MsaSelfAuthSubject(this.TestUserProxyTicket);
            var args = new DeleteByTypesArgs(privacySubject, new List<string> { "qwe" }, DateTimeOffset.MinValue, DateTimeOffset.MaxValue)
            {
                Context = "operation context",
                CorrelationVector = new Guid().ToString(),
                RequestId = new Guid().ToString()
            };

            await testClient.DeleteByTypesAsync(args).ConfigureAwait(false);

            this.ValidateHttpClientMockHeader(HeaderNames.AccessToken, this.TestAccessToken);
            this.ValidateHttpClientMockHeader(HeaderNames.ClientRequestId, args.RequestId);
            this.ValidateHttpClientMockHeader(HeaderNames.CorrelationVector, args.CorrelationVector);

            //  MSA self auth subject will result in user proxy ticket passed through headers.
            this.ValidateHttpClientMockHeader(HeaderNames.ProxyTicket, this.TestUserProxyTicket);

            this.ValidateHttpClientMockSendAsync(
                m =>
                {
                    var request = (m.Content as ObjectContent)?.Value as DeleteOperationRequest;
                    if (request == null)
                        return false;

                    return this.IsOperationRequestValid(request, privacySubject);
                });
        }

        [TestMethod]
        public async Task PrivacySubjectClient_DeleteByTypesAsync_SendsDeleteOperationRequest_NonMsaSelfAuthSubject()
        {
            var testClient = this.CreateBasicClient();

            var privacySubjectMock = this.CreateMockPrivacySubject(SubjectUseContext.Delete);
            var args = new DeleteByTypesArgs(privacySubjectMock.Object, new List<string> { "qwe" }, DateTimeOffset.MinValue, DateTimeOffset.MaxValue)
            {
                Context = "operation context",
                CorrelationVector = new Guid().ToString(),
                RequestId = new Guid().ToString()
            };

            await testClient.DeleteByTypesAsync(args).ConfigureAwait(false);

            this.ValidateHttpClientMockHeader(HeaderNames.AccessToken, this.TestAccessToken);
            this.ValidateHttpClientMockHeader(HeaderNames.ClientRequestId, args.RequestId);
            this.ValidateHttpClientMockHeader(HeaderNames.CorrelationVector, args.CorrelationVector);

            //  Non MSA self auth subjects do not have user proxy tickets.
            this.ValidateHttpClientMockHasNoHeader(HeaderNames.ProxyTicket);

            this.ValidateHttpClientMockSendAsync(
                m =>
                {
                    var request = (m.Content as ObjectContent)?.Value as DeleteOperationRequest;
                    if (request == null)
                        return false;

                    return this.IsOperationRequestValid(request, privacySubjectMock.Object);
                });
            privacySubjectMock.Verify();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task PrivacySubjectClient_DeleteByTypesAsync_Throws_ArgsNull()
        {
            var testClient = this.CreateBasicClient();
            await testClient.DeleteByTypesAsync(null);
        }

        [TestMethod]
        public async Task PrivacySubjectClient_ExportByTypesAsync_SendsExportOperationRequest_MsaSelfAuthSubject()
        {
            var testClient = this.CreateBasicClient();

            var privacySubject = new MsaSelfAuthSubject(this.TestUserProxyTicket);
            var testUri = new Uri("https://example.com");
            var args = new ExportByTypesArgs(privacySubject, new List<string> { "qwe" }, DateTimeOffset.MinValue, DateTimeOffset.MaxValue, testUri, false)
            {
                Context = "operation context",
                CorrelationVector = new Guid().ToString(),
                RequestId = new Guid().ToString()
            };

            await testClient.ExportByTypesAsync(args).ConfigureAwait(false);

            this.ValidateHttpClientMockHeader(HeaderNames.AccessToken, this.TestAccessToken);
            this.ValidateHttpClientMockHeader(HeaderNames.ClientRequestId, args.RequestId);
            this.ValidateHttpClientMockHeader(HeaderNames.CorrelationVector, args.CorrelationVector);

            //  MSA self auth subject will result in user proxy ticket passed through headers.
            this.ValidateHttpClientMockHeader(HeaderNames.ProxyTicket, this.TestUserProxyTicket);

            this.ValidateHttpClientMockSendAsync(
                m =>
                {
                    var request = (m.Content as ObjectContent)?.Value as ExportOperationRequest;
                    if (request == null)
                        return false;

                    return this.IsOperationRequestValid(request, privacySubject)
                           && request.StorageLocationUri == testUri;
                });
        }

        [TestMethod]
        public async Task PrivacySubjectClient_ExportByTypesAsync_SendsExportOperationRequest_NonMsaSelfAuthSubject()
        {
            var testClient = this.CreateBasicClient();

            var privacySubjectMock = this.CreateMockPrivacySubject(SubjectUseContext.Export);
            var testUri = new Uri("https://example.com");
            var args = new ExportByTypesArgs(privacySubjectMock.Object, new List<string> { "qwe" }, DateTimeOffset.MinValue, DateTimeOffset.MaxValue, testUri, false)
            {
                Context = "operation context",
                CorrelationVector = new Guid().ToString(),
                RequestId = new Guid().ToString()
            };

            await testClient.ExportByTypesAsync(args).ConfigureAwait(false);

            this.ValidateHttpClientMockHeader(HeaderNames.AccessToken, this.TestAccessToken);
            this.ValidateHttpClientMockHeader(HeaderNames.ClientRequestId, args.RequestId);
            this.ValidateHttpClientMockHeader(HeaderNames.CorrelationVector, args.CorrelationVector);

            //  Non MSA self auth subjects do not have user proxy tickets.
            this.ValidateHttpClientMockHasNoHeader(HeaderNames.ProxyTicket);

            this.ValidateHttpClientMockSendAsync(
                m =>
                {
                    var request = (m.Content as ObjectContent)?.Value as ExportOperationRequest;
                    if (request == null)
                        return false;

                    return this.IsOperationRequestValid(request, privacySubjectMock.Object)
                           && request.StorageLocationUri == testUri;
                });
            privacySubjectMock.Verify();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task PrivacySubjectClient_ExportByTypesAsync_Throws_ArgsNull()
        {
            var testClient = this.CreateBasicClient();
            await testClient.ExportByTypesAsync(null);
        }

        private Mock<IPrivacySubject> CreateMockPrivacySubject(SubjectUseContext useContext)
        {
            var privacySubjectMock = new Mock<IPrivacySubject>(MockBehavior.Strict);
            privacySubjectMock.Setup(ps => ps.Validate(It.Is<SubjectUseContext>(su => su == useContext))).Verifiable();

            return privacySubjectMock;
        }

        private bool IsOperationRequestValid(OperationRequestBase request, IPrivacySubject privacySubject)
        {
            return request.Subject == privacySubject
                   && request.Context == "operation context";
        }

        private void ValidateHttpClientMockSendAsync(Func<HttpRequestMessage, bool> isValid)
        {
            this.MockHttpClient.Verify(c => c.SendAsync(It.Is<HttpRequestMessage>(m => isValid(m)), HttpCompletionOption.ResponseContentRead));
        }
    }
}
