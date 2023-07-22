// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.FunctionalTests.GraphApis
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.GraphApis;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.Utilities;
    using Microsoft.Membership.MemberServices.Test.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Azure.Storage.Blob;

    using Newtonsoft.Json;

    using TestConfiguration = Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.Config.TestConfiguration;

    /// <summary>
    ///     ExportPersonalDataTest
    /// </summary>
    [TestClass]
    public class ExportPersonalDataTest : GraphApiTestBase
    {
        [DataTestMethod, TestCategory("FCT"), TestCategory("PROXY")]
        [DataRow("users", true, SharedAccessBlobPermissions.List, ErrorCode.StorageLocationShouldNotAllowListAccess, GraphApiErrorMessage.StorageLocationShouldNotAllowListAccess)]
        [DataRow("users", true,
            SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write,
            ErrorCode.StorageLocationShouldNotAllowReadAccess,
            GraphApiErrorMessage.StorageLocationShouldNotAllowReadAccess)]
        [DataRow("users", true, SharedAccessBlobPermissions.Create, ErrorCode.StorageLocationNeedsWriteAddPermissions, GraphApiErrorMessage.StorageLocationNeedsWriteAddPermissions)]
        [DataRow("directory/inboundSharedUserProfiles", false, SharedAccessBlobPermissions.List, ErrorCode.StorageLocationShouldNotAllowListAccess, GraphApiErrorMessage.StorageLocationShouldNotAllowListAccess)]
        [DataRow("directory/inboundSharedUserProfiles", false,
            SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write,
            ErrorCode.StorageLocationShouldNotAllowReadAccess,
            GraphApiErrorMessage.StorageLocationShouldNotAllowReadAccess)]
        [DataRow("directory/inboundSharedUserProfiles", false, SharedAccessBlobPermissions.Create, ErrorCode.StorageLocationNeedsWriteAddPermissions, GraphApiErrorMessage.StorageLocationNeedsWriteAddPermissions)]
        public async Task ExportPersonalDataInvalidSasUriBadRequest(string apiPathPrefix, bool useHomeTenant, SharedAccessBlobPermissions sasPermissions, ErrorCode expectedErrorCode, string expectedErrorMessage)
        {
            CloudBlobContainer container = null;

            try
            {
                Guid targetObjectId = Guid.NewGuid();
                container = await BlobStorageHelper.GetCloudBlobContainerAsync(
                    TestConfiguration.BlobStorageTestConnectionString.Value,
                    "functionaltest" + Guid.NewGuid().ToString("N")).ConfigureAwait(false);
                Uri targetStorageLocation = await BlobStorageHelper.GetSharedAccessSignatureAsync(container, sasPermissions).ConfigureAwait(false);
                using (HttpResponseMessage httpResponseMessage =
                    await CallPostAsync(
                        $"{apiPathPrefix}('{targetObjectId}')/exportPersonalData",
                        (useHomeTenant ? TestIdentityHomeTenant : TestIdentityResourceTenant),
                        new ExportPersonalDataBody { StorageLocation = targetStorageLocation }).ConfigureAwait(false))
                {
                    Assert.IsNotNull(httpResponseMessage);
                    string content = httpResponseMessage.Content != null ? await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false) : string.Empty;
                    var responseDetails = $"Status Code: {httpResponseMessage.StatusCode}, Content: {content}";
                    Assert.IsFalse(httpResponseMessage.IsSuccessStatusCode, responseDetails, "Response should have been a failure, but it was successful.");
                    Assert.AreEqual(HttpStatusCode.BadRequest, httpResponseMessage.StatusCode, responseDetails);

                    var odataError = JsonConvert.DeserializeObject<ErrorResponse>(content);
                    Assert.IsNotNull(odataError, responseDetails);
                    Assert.IsNotNull(odataError.Error, responseDetails);
                    Assert.AreEqual(expectedErrorCode.ToString(), odataError.Error.Code, responseDetails);
                    Assert.AreEqual(expectedErrorMessage, odataError.Error.Message, responseDetails);

                    Console.WriteLine(responseDetails);
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

        [TestMethod, TestCategory("FCT"), TestCategory("PROXY")]
        [DataRow("users", true)]
        [DataRow("directory/inboundSharedUserProfiles", false)]
        public async Task ExportPersonalDataPremiumStorageRequest(string apiPathPrefix, bool useHomeTenant)
        {
            CloudBlobContainer container = null;

            try
            {
                Guid targetObjectId = Guid.NewGuid();
                container = await BlobStorageHelper.GetCloudBlobContainerAsync(
                    TestConfiguration.PremiumBlobStorageTestConnectionString.Value,
                    "functionaltest" + Guid.NewGuid().ToString("N")).ConfigureAwait(false);
                Uri targetStorageLocation = await BlobStorageHelper.GetSharedAccessSignatureAsync(container).ConfigureAwait(false);
                using (HttpResponseMessage httpResponseMessage =
                    await CallPostAsync(
                        $"{apiPathPrefix}('{targetObjectId}')/exportPersonalData",
                        (useHomeTenant ? TestIdentityHomeTenant : TestIdentityResourceTenant),
                        new ExportPersonalDataBody { StorageLocation = targetStorageLocation }).ConfigureAwait(false))
                {
                    Assert.IsNotNull(httpResponseMessage);
                    string content = httpResponseMessage.Content != null ? await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false) : string.Empty;
                    var responseDetails = $"Status Code: {httpResponseMessage.StatusCode}, Content: {content}";
                    Assert.IsFalse(httpResponseMessage.IsSuccessStatusCode, responseDetails, "Response should have been a failure, but it was successful.");
                    Assert.AreEqual(HttpStatusCode.BadRequest, httpResponseMessage.StatusCode, responseDetails);

                    var odataError = JsonConvert.DeserializeObject<ErrorResponse>(content);
                    Assert.IsNotNull(odataError, responseDetails);
                    Assert.IsNotNull(odataError.Error, responseDetails);
                    Assert.AreEqual(ErrorCode.StorageLocationShouldSupportAppendBlobs.ToString(), odataError.Error.Code, responseDetails);
                    Assert.AreEqual(GraphApiErrorMessage.StorageLocationShouldSupportAppendBlobs, odataError.Error.Message, responseDetails);

                    Console.WriteLine(responseDetails);
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

        [TestMethod, TestCategory("FCT"), TestCategory("PROXY")]
        [DataRow("users", true)]
        [DataRow("directory/inboundSharedUserProfiles", false)]
        public async Task ExportPersonalDataMissingMsGraphServiceRootShouldBadRequest(string apiPathPrefix, bool useHomeTenant)
        {
            CloudBlobContainer container = null;

            try
            {
                Guid targetObjectId = Guid.NewGuid();
                container = await BlobStorageHelper.GetCloudBlobContainerAsync(
                    TestConfiguration.BlobStorageTestConnectionString.Value,
                    "functionaltest" + Guid.NewGuid().ToString("N")).ConfigureAwait(false);
                Uri targetStorageLocation = await BlobStorageHelper.GetSharedAccessSignatureAsync(container).ConfigureAwait(false);
                using (HttpResponseMessage httpResponseMessage =
                    await CallPostAsync(
                    $"{apiPathPrefix}('{targetObjectId}')/exportPersonalData",
                        (useHomeTenant ? TestIdentityHomeTenant : TestIdentityResourceTenant),
                        new ExportPersonalDataBody { StorageLocation = targetStorageLocation },
                        new Dictionary<string, string>()).ConfigureAwait(false))
                {
                    Assert.IsNotNull(httpResponseMessage);
                    string content = httpResponseMessage.Content != null ? await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false) : string.Empty;
                    var responseDetails = $"Status Code: {httpResponseMessage.StatusCode}, Content: {content}";
                    Assert.IsFalse(httpResponseMessage.IsSuccessStatusCode, responseDetails);
                    Assert.AreEqual(HttpStatusCode.BadRequest, httpResponseMessage.StatusCode, responseDetails);

                    var odataError = JsonConvert.DeserializeObject<ErrorResponse>(content);
                    Assert.IsNotNull(odataError, responseDetails);
                    Assert.IsNotNull(odataError.Error, responseDetails);
                    Assert.AreEqual(ErrorCode.InvalidInput.ToString(), odataError.Error.Code, responseDetails);
                    Assert.AreEqual(string.Format(GraphApiErrorMessage.MissingHeaderFormat, HeaderNames.MsGraphServiceRoot), odataError.Error.Message, responseDetails);

                    Console.WriteLine(responseDetails);
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

        [DataTestMethod, TestCategory("FCT"), TestCategory("PROXY")]
        [DataRow("users", true)]
        [DataRow("directory/inboundSharedUserProfiles", false)]
        public async Task ExportPersonalDataShouldBadRequestForInvalidStorageLocation(string apiPathPrefix, bool useHomeTenant)
        {
            // Create request that does NOT contain a storage location
            ExportPersonalDataBody request = new ExportPersonalDataBody();
            Guid targetObjectId = Guid.NewGuid();
            using (HttpResponseMessage httpResponseMessage =
                await CallPostAsync(
                    $"{apiPathPrefix}('{targetObjectId}')/exportPersonalData",
                    (useHomeTenant ? TestIdentityHomeTenant : TestIdentityResourceTenant),
                    request).ConfigureAwait(false))
            {
                Assert.IsNotNull(httpResponseMessage);
                string content = httpResponseMessage.Content != null ? await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false) : string.Empty;
                var responseDetails = $"Status Code: {httpResponseMessage.StatusCode}, Content: {content}";
                Assert.IsFalse(httpResponseMessage.IsSuccessStatusCode, responseDetails);
                Assert.AreEqual(HttpStatusCode.BadRequest, httpResponseMessage.StatusCode, responseDetails);

                var odataError = JsonConvert.DeserializeObject<ErrorResponse>(content);
                Assert.IsNotNull(odataError, responseDetails);
                Assert.IsNotNull(odataError.Error, responseDetails);
                Assert.AreEqual(ErrorCode.StorageLocationInvalid.ToString(), odataError.Error.Code, responseDetails);
                Assert.AreEqual(GraphApiErrorMessage.StorageLocationInvalid, odataError.Error.Message, responseDetails);

                Console.WriteLine(responseDetails);
            }
        }

        [TestMethod, TestCategory("FCT"), TestCategory("PROXY")]
        [DataRow("users", true, "exportPersonalData", HttpStatusCode.Accepted, true)]
        [DataRow("users", true, "Microsoft.PrivacyServices.DataSubjectRight.Contracts.V1.exportPersonalData", HttpStatusCode.Accepted, true)]
        [DataRow("directory/inboundSharedUserProfiles", false, "exportPersonalData", HttpStatusCode.Accepted, true)]
        [DataRow("directory/inboundSharedUserProfiles", false, "Microsoft.PrivacyServices.DataSubjectRight.Contracts.V1.exportPersonalData", HttpStatusCode.Accepted, true)]
        public async Task ExportPersonalDataShouldSucceed(string apiPathPrefix, bool useHomeTenant, string testEndpoint, HttpStatusCode expectedStatusCode, bool shouldReturnHeaders)
        {
            CloudBlobContainer container = null;
            try
            {
                Guid targetObjectId = Guid.NewGuid();
                container = await BlobStorageHelper.GetCloudBlobContainerAsync(
                    TestConfiguration.BlobStorageTestConnectionString.Value,
                    "functionaltest" + Guid.NewGuid().ToString("N")).ConfigureAwait(false);
                Uri targetStorageLocation = await BlobStorageHelper.GetSharedAccessSignatureAsync(container).ConfigureAwait(false);
                using (HttpResponseMessage httpResponseMessage =
                    await CallPostAsync(
                        $"{apiPathPrefix}('{targetObjectId}')/{testEndpoint}",
                        (useHomeTenant ? TestIdentityHomeTenant : TestIdentityResourceTenant),
                        new ExportPersonalDataBody { StorageLocation = targetStorageLocation }).ConfigureAwait(false))
                {
                    Assert.IsNotNull(httpResponseMessage);
                    string content = httpResponseMessage.Content != null ? await (httpResponseMessage.Content.ReadAsStringAsync()).ConfigureAwait(false) : string.Empty;
                    var responseDetails = $"Status Code: {httpResponseMessage.StatusCode}, Content: {content}";
                    Assert.IsTrue(httpResponseMessage.IsSuccessStatusCode, responseDetails);
                    Assert.AreEqual(expectedStatusCode, httpResponseMessage.StatusCode, responseDetails);

                    if (shouldReturnHeaders)
                    {
                        Assert.IsTrue(
                            httpResponseMessage.Headers.TryGetValues("Operation-Location", out IEnumerable<string> operationLocationValues),
                            "Should contain header Operation-Location");
                        IEnumerable<string> locationValues = operationLocationValues?.ToList();
                        Assert.IsTrue(Uri.IsWellFormedUriString(locationValues?.FirstOrDefault(), UriKind.Absolute), $"Should be absolute uri: {locationValues?.FirstOrDefault()}.");
                    }
                    else
                    {
                        Assert.IsFalse(
                            httpResponseMessage.Headers.TryGetValues("Operation-Location", out IEnumerable<string> operationLocationValues),
                            "Should not contain header Operation-Location");
                    }
                    Console.WriteLine(responseDetails);
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

        [TestMethod, TestCategory("FCT"), TestCategory("PROXY")]
        [DataRow("users", true)]
        [DataRow("directory/inboundSharedUserProfiles", false)]
        public async Task ExportPersonalDataTargetObjectIdNotGuidShouldBadRequest(string apiPathPrefix, bool useHomeTenant)
        {
            CloudBlobContainer container = null;

            try
            {
                string targetObjectId = "i.am.not.a.guid";
                container = await BlobStorageHelper.GetCloudBlobContainerAsync(
                    TestConfiguration.BlobStorageTestConnectionString.Value,
                    "functionaltest" + Guid.NewGuid().ToString("N")).ConfigureAwait(false);
                Uri targetStorageLocation = await BlobStorageHelper.GetSharedAccessSignatureAsync(container).ConfigureAwait(false);
                using (HttpResponseMessage httpResponseMessage =
                    await CallPostAsync(
                    $"{apiPathPrefix}('{targetObjectId}')/exportPersonalData",
                    (useHomeTenant ? TestIdentityHomeTenant : TestIdentityResourceTenant),
                        new ExportPersonalDataBody { StorageLocation = targetStorageLocation }).ConfigureAwait(false))
                {
                    Assert.IsNotNull(httpResponseMessage);
                    string content = httpResponseMessage.Content != null ? await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false) : string.Empty;
                    var responseDetails = $"Status Code: {httpResponseMessage.StatusCode}, Content: {content}";
                    Assert.IsFalse(httpResponseMessage.IsSuccessStatusCode, responseDetails);
                    Assert.AreEqual(HttpStatusCode.BadRequest, httpResponseMessage.StatusCode, responseDetails);

                    var odataError = JsonConvert.DeserializeObject<ErrorResponse>(content);
                    Assert.IsNotNull(odataError, responseDetails);
                    Assert.IsNotNull(odataError.Error, responseDetails);
                    Assert.AreEqual(ErrorCode.InvalidInput.ToString(), odataError.Error.Code, responseDetails);
                    Assert.AreEqual(string.Format(GraphApiErrorMessage.InvalidObjectIdFormat, targetObjectId), odataError.Error.Message, responseDetails);

                    Console.WriteLine(responseDetails);
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

        [TestMethod, TestCategory("FCT"), TestCategory("PROXY")]
        [DataRow("users", true, "Microsoft.PrivacyServices.DataSubjectRight.Contracts.exportPersonalData")]
        [DataRow("users", true, "Microsoft.PrivacyServices.DataSubjectRight.Contracts.V2.exportPersonalData")]
        [DataRow("directory/inboundSharedUserProfiles", false, "Microsoft.PrivacyServices.DataSubjectRight.Contracts.exportPersonalData")]
        [DataRow("directory/inboundSharedUserProfiles", false, "Microsoft.PrivacyServices.DataSubjectRight.Contracts.V2.exportPersonalData")]
        public async Task ExportPersonalDataFailsWithBadNameQualifier(string apiPathPrefix, bool useHomeTenant, string testEndpoint)
        {
            CloudBlobContainer container = null;
            try
            {
                Guid targetObjectId = Guid.NewGuid();
                container = await BlobStorageHelper.GetCloudBlobContainerAsync(
                TestConfiguration.BlobStorageTestConnectionString.Value,
                "functionaltest" + Guid.NewGuid().ToString("N")).ConfigureAwait(false);
                Uri targetStorageLocation = await BlobStorageHelper.GetSharedAccessSignatureAsync(container).ConfigureAwait(false);
                using (HttpResponseMessage httpResponseMessage =
                await CallPostAsync(
                    $"{apiPathPrefix}('{targetObjectId}')/{testEndpoint}",
                    (useHomeTenant ? TestIdentityHomeTenant : TestIdentityResourceTenant),
                    new ExportPersonalDataBody { StorageLocation = targetStorageLocation }).ConfigureAwait(false))
                {
                    Assert.IsNotNull(httpResponseMessage);
                    string content = httpResponseMessage.Content != null ? await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false) : string.Empty;
                    var responseDetails = $"Status Code: {httpResponseMessage.StatusCode}, Content: {content}";
                    Assert.IsFalse(httpResponseMessage.IsSuccessStatusCode, responseDetails);
                    Assert.AreEqual(HttpStatusCode.NotFound, httpResponseMessage.StatusCode, responseDetails);
                    Console.WriteLine(responseDetails);
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

        [TestMethod, TestCategory("FCT"), TestCategory("PROXY")]
        [DataRow("users", false)]
        [DataRow("directory/inboundSharedUserProfiles", true)]
        public async Task ExportPersonalDataWrongApiForTenantTypeShouldFail(string apiPathPrefix, bool useHomeTenant)
        {
            CloudBlobContainer container = null;

            try
            {
                // Set TenantId and ObjectId to opposite tenant type to fake cross tenant call
                string targetObjectId = useHomeTenant ? TestData.ResourceUserObjIdNonComplexOrg : TestData.HomeUserObjIdNonComplexOrg;
                container = await BlobStorageHelper.GetCloudBlobContainerAsync(
                    TestConfiguration.BlobStorageTestConnectionString.Value,
                    "functionaltest" + Guid.NewGuid().ToString("N")).ConfigureAwait(false);
                Uri targetStorageLocation = await BlobStorageHelper.GetSharedAccessSignatureAsync(container).ConfigureAwait(false);
                using (HttpResponseMessage httpResponseMessage =
                    await CallPostAsync(
                    $"{apiPathPrefix}('{targetObjectId}')/exportPersonalData",
                    (useHomeTenant ? TestIdentityHomeTenant : TestIdentityResourceTenant),
                        new ExportPersonalDataBody { StorageLocation = targetStorageLocation }).ConfigureAwait(false))
                {
                    Assert.IsNotNull(httpResponseMessage);
                    string content = httpResponseMessage.Content != null ? await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false) : string.Empty;
                    var responseDetails = $"Status Code: {httpResponseMessage.StatusCode}, Content: {content}";
                    Assert.IsFalse(httpResponseMessage.IsSuccessStatusCode, responseDetails);

                    // TODO: Figure out what the real error will be from AadRvs
                    Assert.AreEqual(HttpStatusCode.Forbidden, httpResponseMessage.StatusCode, responseDetails);

                    var odataError = JsonConvert.DeserializeObject<ErrorResponse>(content);
                    Assert.IsNotNull(odataError, responseDetails);
                    Assert.IsNotNull(odataError.Error, responseDetails);
                    Assert.AreEqual(ErrorCode.Forbidden.ToString(), odataError.Error.Code, responseDetails);
                    Assert.AreEqual(string.Format(GraphApiErrorMessage.ForbiddenDefault, targetObjectId), odataError.Error.Message, responseDetails);

                    Console.WriteLine(responseDetails);
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

        [TestMethod, TestCategory("FCT"), TestCategory("PROXY")]
        [DataRow(400)] // this test triggers a 400 error in AadRvs mock
        [DataRow(401)] // this test triggers a 401 error in AadRvs mock
        [DataRow(403)] // this test triggers a 403 error in AadRvs mock
        [DataRow(404)] // this test triggers a 404 error in AadRvs mock
        [DataRow(405)] // this test triggers a 405 error in AadRvs mock
        [DataRow(409)] // this test triggers a 409 error in AadRvs mock
        [DataRow(429)] // this test triggers a 429 error in AadRvs mock

        public async Task ExportPersonalDataShouldRelayCorrectErrorCodeForRVSFailure(int statusCode)
        {
            CloudBlobContainer container = null;

            try
            {
                container = await BlobStorageHelper.GetCloudBlobContainerAsync(
                    TestConfiguration.BlobStorageTestConnectionString.Value,
                    "functionaltest" + Guid.NewGuid().ToString("N")).ConfigureAwait(false);
                Uri targetStorageLocation = await BlobStorageHelper.GetSharedAccessSignatureAsync(container).ConfigureAwait(false);
                using (HttpResponseMessage httpResponseMessage =
                    await CallPostAsync(
                    $"users('{TestAadRvsErrorObjectIds[statusCode].TargetObject}')/exportPersonalData",
                    TestIdentityHomeTenant,
                    new ExportPersonalDataBody { StorageLocation = targetStorageLocation }).ConfigureAwait(false))
                {
                    Assert.IsNotNull(httpResponseMessage);
                    string content = httpResponseMessage.Content != null ? await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false) : string.Empty;
                    var responseDetails = $"Status Code: {httpResponseMessage.StatusCode}, Content: {content}";
                    Assert.IsFalse(httpResponseMessage.IsSuccessStatusCode, responseDetails);

                    // Assert the real error will be relayed correctly to the client from AadRvs
                    Assert.AreEqual(TestAadRvsErrorObjectIds[statusCode].HttpStatusCode, httpResponseMessage.StatusCode, responseDetails);
                    Console.WriteLine(responseDetails);
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
    }
}
