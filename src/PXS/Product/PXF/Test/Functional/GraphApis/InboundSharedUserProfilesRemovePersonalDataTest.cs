// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.FunctionalTests.GraphApis
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.GraphApis;
    using Microsoft.Membership.MemberServices.Test.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Newtonsoft.Json;

    /// <summary>
    ///     InboundSharedUserProfileRemovePersonalDataTest
    /// </summary>
    [TestClass]
    public class InboundSharedUserProfilesRemovePersonalDataTest : GraphApiTestBase
    {
        [TestMethod, TestCategory("FCT"), TestCategory("PROXY")]
        public async Task RemovePersonalDataMissingMsGraphServiceRootShouldBadRequest()
        {
            Guid targetObjectId = Guid.NewGuid();
            using (HttpResponseMessage httpResponseMessage =
                await CallPostAsync(
                    $"directory/inboundSharedUserProfiles('{targetObjectId}')/removePersonalData",
                    TestIdentityResourceTenant,
                    new EmptyBody { },
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

        [TestMethod, TestCategory("FCT"), TestCategory("PROXY")]
        public async Task RemovePersonalDataShouldSucceed()
        {
            Guid targetObjectId = Guid.NewGuid();
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
            }
        }

        [TestMethod, TestCategory("FCT"), TestCategory("PROXY")]
        public async Task RemovePersonalDataTargetObjectIdNotGuidShouldBadRequest()
        {
            string targetObjectId = "i.am.not.a.guid";
            using (HttpResponseMessage httpResponseMessage =
                await CallPostAsync(
                    $"directory/inboundSharedUserProfiles('{targetObjectId}')/removePersonalData", TestIdentityResourceTenant,
                    new EmptyBody { }).ConfigureAwait(false))
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

        [TestMethod, TestCategory("FCT"), TestCategory("PROXY")]
        public async Task InboundRemovePersonalDataInHomeTenantShouldFail()
        {
            string targetObjectId = TestData.HomeUserObjIdNonComplexOrg;
            using (HttpResponseMessage httpResponseMessage =
                await CallPostAsync(
                    $"directory/inboundSharedUserProfiles('{targetObjectId}')/removePersonalData",
                    TestIdentityHomeTenant,
                    new EmptyBody { }).ConfigureAwait(false))
            {
                Assert.IsNotNull(httpResponseMessage);
                string content = httpResponseMessage.Content != null ? await (httpResponseMessage.Content.ReadAsStringAsync()).ConfigureAwait(false) : string.Empty;
                var responseDetails = $"Status Code: {httpResponseMessage.StatusCode}, Content: {content}";
                Assert.IsFalse(httpResponseMessage.IsSuccessStatusCode, responseDetails);
                Assert.AreEqual(HttpStatusCode.Forbidden, httpResponseMessage.StatusCode, responseDetails);
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

        public async Task InboundRemovePersonalDataShouldRelayCorrectErrorCodeForRVSFailure(int statusCode)
        {
            using (HttpResponseMessage httpResponseMessage =
                await CallPostAsync(
                    $"directory/inboundSharedUserProfiles('{TestAadRvsErrorObjectIds[statusCode].TargetObject}')/removePersonalData",
                    TestIdentityHomeTenant,
                    new EmptyBody { }).ConfigureAwait(false))
            {
                Assert.IsNotNull(httpResponseMessage);
                string content = httpResponseMessage.Content != null ? await (httpResponseMessage.Content.ReadAsStringAsync()).ConfigureAwait(false) : string.Empty;
                var responseDetails = $"Status Code: {httpResponseMessage.StatusCode}, Content: {content}";
                Assert.IsFalse(httpResponseMessage.IsSuccessStatusCode, responseDetails);

                // Assert the real error will be relayed correctly to the client from AadRvs
                Assert.AreEqual(TestAadRvsErrorObjectIds[statusCode].HttpStatusCode, httpResponseMessage.StatusCode, responseDetails);
            }
        }
    }
}
