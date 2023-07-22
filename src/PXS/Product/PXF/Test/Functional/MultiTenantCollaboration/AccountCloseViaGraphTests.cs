// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.FunctionalTests.MultiTenantCollaboration
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.GraphApis;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.Utilities;
    using Microsoft.Membership.MemberServices.Test.Common;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    ///     AadAccountClose Functional Tests for multi tenant collaboration scenarios calls from Graph
    /// </summary>
    [TestClass]
    public class AccountCloseViaGraphTests : GraphApiTestBase
    {
        [TestMethod, TestCategory("FCT"), TestCategory("NI")]
        public async Task ResourceTenantAccountCleanupViaMSGraph()
        {
            Guid targetObjectId = Guid.NewGuid();
            Guid tenantId = Guid.Parse(TestIdentityResourceTenant.TenantId);

            using (HttpResponseMessage httpResponseMessage =
                await CallPostAsync(
                    $"directory/inboundSharedUserProfiles('{targetObjectId}')/removePersonalData",
                    TestIdentityResourceTenant,
                    new EmptyBody { }).ConfigureAwait(false))
            {
                Assert.IsNotNull(httpResponseMessage);
                string content = httpResponseMessage.Content != null ? await (httpResponseMessage.Content.ReadAsStringAsync()).ConfigureAwait(false) : string.Empty;
                var responseDetails = $"Status Code: {httpResponseMessage.StatusCode}, Content: {content}";
                Assert.IsTrue(httpResponseMessage.IsSuccessStatusCode, responseDetails);
                Assert.AreEqual(HttpStatusCode.NoContent, httpResponseMessage.StatusCode, responseDetails);

                PrivacyRequest result = null;
                await PollingUtility.PollForCondition(
                    iterationWait: TimeSpan.FromSeconds(2),
                    maximumPollRetry: 30,
                    conditionCheck: async () =>
                    {
                        result = await MockTestHooks.GetPrivacyRequestAsync(targetObjectId, tenantId).ConfigureAwait(false);
                        if (result == null)
                        {
                            Console.WriteLine("Data not found in storage yet.");
                        }

                        return result != null;
                    }
                ).ConfigureAwait(false);

                VerifyAccountCloseRequest(result, targetObjectId, tenantId, Guid.Parse(TestData.HomeTenantId));
            }
        }

        [TestMethod, TestCategory("FCT"), TestCategory("NI")]
        public async Task HomeTenantAccountCleanupViaMSGraphShouldFail()
        {
            Guid targetObjectId = Guid.NewGuid();
            Guid tenantId = Guid.Parse(TestIdentityHomeTenant.TenantId);

            using (HttpResponseMessage httpResponseMessage =
                await CallPostAsync(
                    $"directory/inboundSharedUserProfiles('{targetObjectId}')/removePersonalData", TestIdentityHomeTenant,
                    new EmptyBody { }).ConfigureAwait(false))
            {
                Assert.IsNotNull(httpResponseMessage);
                string content = httpResponseMessage.Content != null ? await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false) : string.Empty;
                var responseDetails = $"Status Code: {httpResponseMessage.StatusCode}, Content: {content}";
                Assert.IsFalse(httpResponseMessage.IsSuccessStatusCode, responseDetails);

                // NOTE: Not checking the error details because we do not properly mock the error response in AadRvsController,
                // so the error comes back as an internal PartnerError
            }
        }

        private static void VerifyAccountCloseRequest(PrivacyRequest result, Guid objectId, Guid tenantId, Guid homeTenantId)
        {
            Assert.IsNotNull(result, $"Command was not found in test storage. Object ID: {objectId}, Tenant ID: {tenantId}");
            Assert.AreEqual(RequestType.AccountClose, result.RequestType);
            Assert.IsInstanceOfType(result.Subject, typeof(AadSubject2), $"Subject should be {nameof(AadSubject2)}");

            AadSubject2 subject = (AadSubject2)result.Subject;
            Assert.AreEqual(objectId, subject.ObjectId);
            Assert.AreEqual(tenantId, subject.TenantId);
            Assert.AreEqual(homeTenantId, subject.HomeTenantId);
            Assert.AreEqual(TenantIdType.Resource, subject.TenantIdType);

            // Should always have v3 verifier
            Assert.IsFalse(string.IsNullOrEmpty(result.VerificationTokenV3), "V3 verifier is not null");

            // Should not have V2 verifier if the request was from resource tenant
            Assert.IsTrue(string.IsNullOrEmpty(result.VerificationToken));

            Console.WriteLine($"Validation succeeded for command id: {result.RequestId}.");
        }
    }
}
