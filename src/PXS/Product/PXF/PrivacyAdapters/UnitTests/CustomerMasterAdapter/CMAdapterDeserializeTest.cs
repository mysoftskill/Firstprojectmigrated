// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyAdapters.UnitTests.CustomerMasterAdapter
{
    using System.Net;

    using Microsoft.Membership.MemberServices.PrivacyAdapters.CustomerMasterAdapter;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.Test.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// CustomerMasterAdapter-Test
    /// </summary>
    [TestClass]
    public class CMAdapterDeserializeTest : CMAdapterTestBase
    {
        [TestMethod]
        public void HandlePartnerErrorCode_ConcurrencyFailure()
        {
            string errorMessage =
                @"{""error_code"":""ConcurrencyFailure"",""message"":""The ???If-Match??? header of the request contained an ETag that didn???t match the ETag of the resource specified in the URI."",""object_type"":""Error""}";
            var error = CustomerMasterAdapter.HandlePartnerErrorCode(errorMessage, (int)HttpStatusCode.PreconditionFailed);
            Assert.IsNotNull(error);
            Assert.AreEqual(AdapterErrorCode.ConcurrencyConflict, error.Code);
            Assert.AreEqual(errorMessage, error.Message);
        }

        [TestMethod]
        public void HandlePartnerErrorCode_ResourceAlreadyExists()
        {
            string errorMessage =
                @"{""error_code"":""ResourceAlreadyExists"",""message"":""The request was an attempt to create a resource that already exists."",""object_type"":""Error""}";
            var error = CustomerMasterAdapter.HandlePartnerErrorCode(errorMessage, (int)HttpStatusCode.Conflict);
            Assert.IsNotNull(error);
            Assert.AreEqual(AdapterErrorCode.ResourceAlreadyExists, error.Code);
            Assert.AreEqual(errorMessage, error.Message);
        }

        [TestMethod]
        public void HandlePartnerErrorCode_UnknownError()
        {
            string errorMessage =
                @"{""error_code"":""NewCustomErrorCode"",""message"":""We don't know about this error code."",""object_type"":""Error""}";
            var error = CustomerMasterAdapter.HandlePartnerErrorCode(errorMessage, (int)HttpStatusCode.BadGateway);
            Assert.IsNotNull(error);
            Assert.AreEqual(AdapterErrorCode.Unknown, error.Code);
            Assert.AreEqual($"Unknown error: {errorMessage}", error.Message);
        }

        [TestMethod]
        public void HandlePartnerErrorCode_UnknownInvalidJsonError()
        {
            string errorMessage = "this is not json";
            var error = CustomerMasterAdapter.HandlePartnerErrorCode(errorMessage, (int)HttpStatusCode.BadRequest);
            Assert.IsNotNull(error);
            Assert.AreEqual(AdapterErrorCode.Unknown, error.Code);
            Assert.AreEqual($"Unknown error: {errorMessage}", error.Message);
        }

        [TestMethod]
        public void DeserializePrivacyProfileResponse_NullEmptySuccess()
        {
            var expectedNullError = new AdapterError(AdapterErrorCode.EmptyResponse, "Null response from partner adapter.", (int)HttpStatusCode.BadRequest);

            var result = CustomerMasterAdapter.DeserializeFromGetProfilesResponseToPrivacyProfile(string.Empty, TestMockFactory.CreateLogger().Object, (int)HttpStatusCode.BadRequest);
            Assert.IsNotNull(result);
            AreEqual(expectedNullError, result.Error);
            Assert.IsNull(result.Result);

            result = CustomerMasterAdapter.DeserializeFromGetProfilesResponseToPrivacyProfile(null, TestMockFactory.CreateLogger().Object, (int)HttpStatusCode.BadRequest);
            Assert.IsNotNull(result);
            AreEqual(expectedNullError, result.Error);
            Assert.IsNull(result.Result);
        }

        [TestMethod]
        public void DeserializePrivacyProfileResponse_MissingPrivacyProfileSuccess()
        {
            // The following content is the result of a consumer profile
            // This is expected not to return any results and no error because "type" = "consumer"
            var content = CreateConsumerProfileContent();
            var result = CustomerMasterAdapter.DeserializeFromGetProfilesResponseToPrivacyProfile(content, TestMockFactory.CreateLogger().Object, (int)HttpStatusCode.BadRequest);
            Assert.IsNotNull(result);
            Assert.IsNull(result.Error);
            Assert.IsNull(result.Result);
        }

        [TestMethod]
        public void DeserializePrivacyProfileResponse_ValidPrivacyProfileSuccess()
        {
            string actualContent = CreateResponseContent();

            var expectedResult = new PrivacyProfile();
            expectedResult.Advertising = true;
            expectedResult.TailoredExperiencesOffers = true;
            expectedResult.Type = "msa_privacy";
            expectedResult.SharingState = true;

            var result = CustomerMasterAdapter.DeserializeFromGetProfilesResponseToPrivacyProfile(actualContent, TestMockFactory.CreateLogger().Object, (int)HttpStatusCode.BadRequest);
            Assert.IsNotNull(result);
            Assert.IsNull(result.Error);
            Assert.IsNotNull(result.Result);
            Assert.IsTrue(result.IsSuccess);
            AreEqual(expectedResult, result.Result);
        }

        [TestMethod]
        public void DeserializePrivacyProfileResponse_ValidPrivacyProfileFalseSuccess()
        {
            string actualContent = CreateResponseContent(@"""advertising"": false, ""tailored_experiences_offers"": false, ""sharing_state"": true,");

            var expectedResult = new PrivacyProfile();
            expectedResult.Advertising = false;
            expectedResult.TailoredExperiencesOffers = false;
            expectedResult.Type = "msa_privacy";
            expectedResult.SharingState = true;

            var result = CustomerMasterAdapter.DeserializeFromGetProfilesResponseToPrivacyProfile(actualContent, TestMockFactory.CreateLogger().Object, (int)HttpStatusCode.BadRequest);
            Assert.IsNotNull(result);
            Assert.IsNull(result.Error);
            Assert.IsNotNull(result.Result);
            Assert.IsTrue(result.IsSuccess);
            AreEqual(expectedResult, result.Result);
        }

        [TestMethod]
        public void DeserializePrivacyProfileResponse_ValidPrivacyProfileMissingSuccess()
        {
            // test a profile that doesn't have these settings set, sets them to null when deserializing
            string actualContent = CreateResponseContent(string.Empty);

            var expectedResult = new PrivacyProfile();
            expectedResult.Advertising = null;
            expectedResult.TailoredExperiencesOffers = null;
            expectedResult.Type = "msa_privacy";
            expectedResult.SharingState = null;

            var result = CustomerMasterAdapter.DeserializeFromGetProfilesResponseToPrivacyProfile(actualContent, TestMockFactory.CreateLogger().Object, (int)HttpStatusCode.BadRequest);
            Assert.IsNotNull(result);
            Assert.IsNull(result.Error);
            Assert.IsNotNull(result.Result);
            Assert.IsTrue(result.IsSuccess);
            AreEqual(expectedResult, result.Result);
        }
    }
}