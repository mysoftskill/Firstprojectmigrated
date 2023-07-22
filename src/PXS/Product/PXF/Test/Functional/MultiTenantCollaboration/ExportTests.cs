// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.FunctionalTests.MultiTenantCollaboration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.GraphApis;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.Utilities;
    using Microsoft.Membership.MemberServices.Test.Common;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Azure.Storage.Blob;

    using TestConfiguration = Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.Config.TestConfiguration;

    /// <summary>
    /// Borrowed from ExportPersonalDataTest, focusing on multi tenant collaboration scenarios
    /// </summary>
    [TestClass]
    public class ExportTests : GraphApiTestBase
    {
        [TestMethod, TestCategory("FCT"), TestCategory("NI")]
        public async Task ExportFromHomeTenant()
        {
            CloudBlobContainer container = null;

            try
            {
                Guid targetObjectId = Guid.NewGuid();
                Guid tenantId = Guid.Parse(TestIdentityHomeTenant.TenantId);
                container = await BlobStorageHelper.GetCloudBlobContainerAsync(
                    TestConfiguration.BlobStorageTestConnectionString.Value,
                    "functionaltest" + Guid.NewGuid().ToString("N")).ConfigureAwait(false);
                Uri targetStorageLocation = await BlobStorageHelper.GetSharedAccessSignatureAsync(container).ConfigureAwait(false);
                using (HttpResponseMessage httpResponseMessage =
                    await CallPostAsync(
                        $"users('{targetObjectId}')/exportPersonalData",
                        TestIdentityHomeTenant,
                        new ExportPersonalDataBody { StorageLocation = targetStorageLocation }).ConfigureAwait(false))
                {
                    Assert.IsNotNull(httpResponseMessage);
                    string content = httpResponseMessage.Content != null ? await (httpResponseMessage.Content.ReadAsStringAsync()).ConfigureAwait(false) : string.Empty;
                    var responseDetails = $"Status Code: {httpResponseMessage.StatusCode}, Content: {content}";
                    Assert.IsTrue(httpResponseMessage.IsSuccessStatusCode, responseDetails);
                    Assert.AreEqual(HttpStatusCode.Accepted, httpResponseMessage.StatusCode, responseDetails);

                    Assert.IsTrue(
                        httpResponseMessage.Headers.TryGetValues("Operation-Location", out IEnumerable<string> operationLocationValues),
                        "Should contain header Operation-Location");
                    IEnumerable<string> locationValues = operationLocationValues?.ToList();
                    Assert.IsTrue(Uri.IsWellFormedUriString(locationValues?.FirstOrDefault(), UriKind.Absolute), $"Should be absolute uri: {locationValues?.FirstOrDefault()}.");
                    Console.WriteLine(responseDetails);

                    PrivacyRequest result = null;
                    await PollingUtility.PollForCondition(
                        iterationWait: TimeSpan.FromSeconds(1),
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

                    VerifyExportRequest(result, targetObjectId, tenantId, tenantId);
                }
            }
            finally
            {
                try
                {
                    container?.DeleteIfExistsAsync().GetAwaiter().GetResult();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to cleanup container: {e}");
                }
            }
        }

        [TestMethod, TestCategory("FCT"), TestCategory("NI")]
        public async Task ExportFromResourceTenant()
        {
            CloudBlobContainer container = null;

            try
            {
                Guid targetObjectId = Guid.NewGuid();
                Guid tenantId = Guid.Parse(TestIdentityResourceTenant.TenantId);
                container = await BlobStorageHelper.GetCloudBlobContainerAsync(
                    TestConfiguration.BlobStorageTestConnectionString.Value,
                    "functionaltest" + Guid.NewGuid().ToString("N")).ConfigureAwait(false);
                Uri targetStorageLocation = await BlobStorageHelper.GetSharedAccessSignatureAsync(container).ConfigureAwait(false);
                using (HttpResponseMessage httpResponseMessage =
                    await CallPostAsync(
                        $"directory/inboundSharedUserProfiles('{targetObjectId}')/exportPersonalData",
                        TestIdentityResourceTenant,
                        new ExportPersonalDataBody { StorageLocation = targetStorageLocation }).ConfigureAwait(false))
                {
                    Assert.IsNotNull(httpResponseMessage);
                    string content = httpResponseMessage.Content != null ? await (httpResponseMessage.Content.ReadAsStringAsync()).ConfigureAwait(false) : string.Empty;
                    var responseDetails = $"Status Code: {httpResponseMessage.StatusCode}, Content: {content}";
                    Assert.IsTrue(httpResponseMessage.IsSuccessStatusCode, responseDetails);
                    Assert.AreEqual(HttpStatusCode.Accepted, httpResponseMessage.StatusCode, responseDetails);

                    Assert.IsTrue(
                        httpResponseMessage.Headers.TryGetValues("Operation-Location", out IEnumerable<string> operationLocationValues),
                        "Should contain header Operation-Location");
                    IEnumerable<string> locationValues = operationLocationValues?.ToList();
                    Assert.IsTrue(Uri.IsWellFormedUriString(locationValues?.FirstOrDefault(), UriKind.Absolute), $"Should be absolute uri: {locationValues?.FirstOrDefault()}.");
                    Console.WriteLine(responseDetails);

                    PrivacyRequest result = null;
                    await PollingUtility.PollForCondition(
                        iterationWait: TimeSpan.FromSeconds(1),
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

                    VerifyExportRequest(result, targetObjectId, tenantId, Guid.Parse(TestData.HomeTenantId));
                }
            }
            finally
            {
                try
                {
                    container?.DeleteIfExistsAsync().GetAwaiter().GetResult();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to cleanup container: {e}");
                }
            }
        }

        private static void VerifyExportRequest(PrivacyRequest result, Guid objectId, Guid tenantId, Guid homeTenantId)
        {
            Assert.IsNotNull(result, $"Command was not found in test storage. Object ID: {objectId}, Tenant ID: {tenantId}");
            Assert.AreEqual(RequestType.Export, result.RequestType);
            if (tenantId == homeTenantId)
            {
                Assert.IsInstanceOfType(result.Subject, typeof(AadSubject), $"Subject should be {nameof(AadSubject)}");
                Assert.IsNotInstanceOfType(result.Subject, typeof(AadSubject2), $"Subject should not be {nameof(AadSubject2)}");
                Assert.AreEqual(objectId, ((AadSubject)result.Subject).ObjectId);
                Assert.AreEqual(tenantId, ((AadSubject)result.Subject).TenantId);
            }
            else
            {
                Assert.IsInstanceOfType(result.Subject, typeof(AadSubject2), $"Subject should be {nameof(AadSubject2)}");
                Assert.AreEqual(objectId, ((AadSubject2)result.Subject).ObjectId);
                Assert.AreEqual(tenantId, ((AadSubject2)result.Subject).TenantId);
                Assert.AreEqual(homeTenantId, ((AadSubject2)result.Subject).HomeTenantId);
            }

            if (tenantId == homeTenantId)
            {
                // Should have V2 verifier if the request was from home tenant
                Assert.IsFalse(string.IsNullOrEmpty(result.VerificationToken));
                Assert.IsTrue(string.IsNullOrEmpty(result.VerificationTokenV3));
            }
            else
            {
                // Should not have V2 verifier if the request was from resource tenant
                Assert.IsTrue(string.IsNullOrEmpty(result.VerificationToken));
                Assert.IsFalse(string.IsNullOrEmpty(result.VerificationTokenV3));
            }

            Console.WriteLine($"Validation succeeded for command id: {result.RequestId}.");
        }
    }
}
