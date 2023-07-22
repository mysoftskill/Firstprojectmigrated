// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.UserSettings
{
    using System.Net;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Privacy.Core.UserSettings;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1;
    using Microsoft.Membership.MemberServices.PrivacyAdapters;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.CustomerMasterAdapter;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common;
    using Microsoft.Membership.MemberServices.Test.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Newtonsoft.Json.Linq;

    [TestClass]
    public class UserSettingsServiceTest : CoreServiceTestBase
    {
        private Mock<ICustomerMasterAdapter> mockCustomerMasterAdapter;

        [TestInitialize]
        public void Initialize()
        {
            this.mockCustomerMasterAdapter = new Mock<ICustomerMasterAdapter>(MockBehavior.Strict);
            this.mockCustomerMasterAdapter
                .Setup(m => m.GetPrivacyProfileAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<string>()))
                .ReturnsAsync(
                    new AdapterResponse<PrivacyProfile>
                    {
                        Result = new PrivacyProfile { Advertising = false, TailoredExperiencesOffers = false, Type = CustomerMasterAdapter.PrivacyProfileGetType, SharingState = true }
                    });
            this.mockCustomerMasterAdapter
                .Setup(m => m.GetPrivacyProfileJObjectAsync(It.IsAny<IPxfRequestContext>()))
                .ReturnsAsync(new AdapterResponse<JObject> { Result = CreateDefaultProfile() });
            this.mockCustomerMasterAdapter
                .Setup(m => m.CreatePrivacyProfileAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<PrivacyProfile>()))
                .ReturnsAsync(new AdapterResponse<PrivacyProfile>());
            this.mockCustomerMasterAdapter
                .Setup(m => m.UpdatePrivacyProfileAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<PrivacyProfile>(), It.IsAny<JObject>()))
                .ReturnsAsync(new AdapterResponse<PrivacyProfile>());
        }

        private static JObject CreateDefaultProfile(
            bool? advertising = false,
            bool? tailoredExperienceOffers = false,
            bool? locationPrivacy = null,
            bool? oboPrivacy = null,
            bool? sharing_state = null)
        {
            string jsonValue =
                @"{
  ""total_count"": 1,
  ""items"": [
    {
      ""object_type"": ""Profile"",
      ""id"": ""89f56b74-3ada-5676-7095-f55d2ec64abf"",
      ""type"": ""msa_privacy"",
      ""customer_id"": ""05f08f8bc88848628943d2b8aa2b436901""," +
                (advertising != null ? $"\"advertising\": {advertising.ToString().ToLower()}," : "") +
                (tailoredExperienceOffers != null ? $"\"tailored_experiences_offers\": {tailoredExperienceOffers.ToString().ToLower()}," : "") +
                (oboPrivacy != null ? $"\"OBOPrivacy\": {oboPrivacy.ToString().ToLower()}," : "") +
                (locationPrivacy != null ? $"\"OBOPrivacyLocation\": {locationPrivacy.ToString().ToLower()}," : "") +
                (sharing_state != null ? $"\"sharing_state\": {sharing_state.ToString().ToLower()}," : "") +
                @"""etag"": ""-3454209939327893012"",
      ""snapshot_id"": ""89f56b74-3ada-5676-7095-f55d2ec64abf/1"",
      ""resource_status"": ""Active"",
      ""links"": {
        ""self"": {
          ""href"": ""05f08f8bc88848628943d2b8aa2b436901/profiles/89f56b74-3ada-5676-7095-f55d2ec64abf"",
          ""method"": ""GET""
        },
        ""snapshot"": {
          ""href"": ""05f08f8bc88848628943d2b8aa2b436901/profiles/89f56b74-3ada-5676-7095-f55d2ec64abf/1"",
          ""method"": ""GET""
        },
        ""update"": {
          ""href"": ""05f08f8bc88848628943d2b8aa2b436901/profiles/89f56b74-3ada-5676-7095-f55d2ec64abf"",
          ""method"": ""PUT""
        },
        ""delete"": {
          ""href"": ""05f08f8bc88848628943d2b8aa2b436901/profiles/89f56b74-3ada-5676-7095-f55d2ec64abf"",
          ""method"": ""DELETE""
        }
      }
    }
  ],
  ""links"": {
    ""add"": {
      ""href"": ""05f08f8bc88848628943d2b8aa2b436901/profiles"",
      ""method"": ""POST""
    }
  },
  ""object_type"": ""Profiles"",
  ""resource_status"": ""Active""
}";
            return JObject.Parse(jsonValue);
        }

        #region Get User Settings

        /// <summary>
        ///     Test get returns success (privacy profile exists), with custom privacy profile properties set
        /// </summary>
        [DataTestMethod]
        [DataRow(true, false, null, null, true)] // if tailored experience offers is null, it triggers the required update code which isn't covered in this test
        [DataRow(true, false, null, null, true)]
        [DataRow(false, true, null, null, false)]
        [DataRow(false, false, null, null, false)]
        [DataRow(false, false, null, null, false)]
        [DataRow(false, false, null, null, false)]
        [DataRow(false, false, null, null, false)]
        public async Task GetAsync_Success(bool? advertising, bool? tailoredExperiencesOffers, bool? oboPrivacy, bool? oboPrivacyLocation, bool? sharingState)
        {
            var expectedProfile = new PrivacyProfile
            {
                Advertising = advertising,
                TailoredExperiencesOffers = tailoredExperiencesOffers,
                OnBehalfOfPrivacy = oboPrivacy,
                LocationPrivacy = oboPrivacyLocation,
                Type = CustomerMasterAdapter.PrivacyProfileGetType,
                SharingState = sharingState
            };

            this.mockCustomerMasterAdapter
                .Setup(m => m.GetPrivacyProfileAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<string>()))
                .ReturnsAsync(
                    new AdapterResponse<PrivacyProfile>
                    {
                        Result = expectedProfile
                    });

            var userSettingsService = new UserSettingsService(this.mockCustomerMasterAdapter.Object, TestMockFactory.CreateLogger().Object);

            ServiceResponse<ResourceSettingV1> response = await userSettingsService.GetAsync(this.TestRequestContext).ConfigureAwait(false);

            Assert.IsNotNull(response);
            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual(advertising, response.Result.Advertising);
            Assert.AreEqual(tailoredExperiencesOffers, response.Result.TailoredExperiencesOffers);
            Assert.AreEqual(oboPrivacy, response.Result.OnBehalfOfPrivacy);
            Assert.AreEqual(oboPrivacyLocation, response.Result.LocationPrivacy);
            Assert.AreEqual(sharingState, response.Result.SharingState);

            this.mockCustomerMasterAdapter.Verify(m => m.GetPrivacyProfileAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<string>()), Times.Once);
            this.mockCustomerMasterAdapter.Verify(m => m.CreatePrivacyProfileAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<PrivacyProfile>()), Times.Never);
        }

        /// <summary>
        ///     Test get returns error when privacy profile does not exist
        /// </summary>
        [TestMethod]
        public async Task GetAsync_ResourceNotFound()
        {
            const string ErrorMessageFailedToCreateProfile = "The request was an attempt to create a resource that already exists.";

            var adapterError = new AdapterError(AdapterErrorCode.ResourceAlreadyExists, ErrorMessageFailedToCreateProfile, (int)HttpStatusCode.Conflict);
            this.mockCustomerMasterAdapter
                .Setup(m => m.GetPrivacyProfileAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<string>()))
                .ReturnsAsync(new AdapterResponse<PrivacyProfile> { Result = null });
            this.mockCustomerMasterAdapter
                .Setup(m => m.CreatePrivacyProfileAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<PrivacyProfile>()))
                .ReturnsAsync(new AdapterResponse<PrivacyProfile> { Error = adapterError });

            var userSettingsService = new UserSettingsService(this.mockCustomerMasterAdapter.Object, TestMockFactory.CreateLogger().Object);

            ServiceResponse<ResourceSettingV1> response = await userSettingsService.GetAsync(this.TestRequestContext).ConfigureAwait(false);

            Assert.IsNotNull(response);
            Assert.IsFalse(response.IsSuccess);
            Assert.IsNotNull(response.Error);
            EqualityHelper.AreEqual(
                new Error(ErrorCode.ResourceNotFound, "Privacy profile not found for user, it must be created to get the profile."),
                response.Error);

            this.mockCustomerMasterAdapter.Verify(m => m.GetPrivacyProfileAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<string>()), Times.Exactly(1));
            this.mockCustomerMasterAdapter.Verify(
                m => m.CreatePrivacyProfileAsync(It.IsAny<IPxfRequestContext>(), It.Is<PrivacyProfile>(p => p.Advertising == true)),
                Times.Never);
        }

        #endregion

        #region Get or Create User Settings

        /// <summary>
        ///     Test get returns success (privacy profile exists), with custom privacy profile properties set
        /// </summary>
        [DataTestMethod]
        [DataRow(true, false, null, null, true)] // if tailored experience offers is null, it triggers the required update code which isn't covered in this test
        [DataRow(true, false, null, null, true)]
        [DataRow(false, true, null, null, false)]
        [DataRow(false, false, null, null, false)]
        [DataRow(false, false, null, null, false)]
        [DataRow(false, false, null, null, false)]
        [DataRow(false, false, null, null, false)]
        public async Task GetOrCreateAsync_Success(bool? advertising, bool? tailoredExperiencesOffers, bool? oboPrivacy, bool? oboPrivacyLocation, bool? sharing_state)
        {
            var expectedProfile = new PrivacyProfile
            {
                Advertising = advertising,
                TailoredExperiencesOffers = tailoredExperiencesOffers,
                OnBehalfOfPrivacy = oboPrivacy,
                LocationPrivacy = oboPrivacyLocation,
                Type = CustomerMasterAdapter.PrivacyProfileGetType,
                SharingState = sharing_state
            };

            this.mockCustomerMasterAdapter
                .Setup(m => m.GetPrivacyProfileAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<string>()))
                .ReturnsAsync(
                    new AdapterResponse<PrivacyProfile>
                    {
                        Result = expectedProfile
                    });

            var userSettingsService = new UserSettingsService(this.mockCustomerMasterAdapter.Object, TestMockFactory.CreateLogger().Object);

            ServiceResponse<ResourceSettingV1> response = await userSettingsService.GetOrCreateAsync(this.TestRequestContext).ConfigureAwait(false);

            Assert.IsNotNull(response);
            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual(advertising, response.Result.Advertising);
            Assert.AreEqual(tailoredExperiencesOffers, response.Result.TailoredExperiencesOffers);
            Assert.AreEqual(oboPrivacy, response.Result.OnBehalfOfPrivacy);
            Assert.AreEqual(oboPrivacyLocation, response.Result.LocationPrivacy);
            Assert.AreEqual(sharing_state, response.Result.SharingState);

            this.mockCustomerMasterAdapter.Verify(m => m.GetPrivacyProfileAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<string>()), Times.Once);
            this.mockCustomerMasterAdapter.Verify(m => m.CreatePrivacyProfileAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<PrivacyProfile>()), Times.Never);
        }

        /// <summary>
        ///     Test get returns success (privacy profile does not exist, so a new one with default privacy settings is created and returned)
        /// </summary>
        [DataTestMethod]
        [DataRow(LegalAgeGroup.Undefined, null, null /* obo */, null /* relevant offers */)]
        [DataRow(LegalAgeGroup.MinorWithoutParentalConsent, null, null, null)]
        [DataRow(LegalAgeGroup.MinorWithParentalConsent, null, null, null)]
        [DataRow(LegalAgeGroup.Adult, null, null, null)]
        [DataRow(LegalAgeGroup.NotAdult, null, null, null)]
        [DataRow(LegalAgeGroup.MinorNoParentalConsentRequired, null, null, null)]
        [DataRow(LegalAgeGroup.Undefined, "jwt", null, null)]
        [DataRow(LegalAgeGroup.MinorWithoutParentalConsent, "jwt", null, null)]
        [DataRow(LegalAgeGroup.MinorWithParentalConsent, "jwt", null, null)]
        [DataRow(LegalAgeGroup.Adult, "jwt", null, null)]
        [DataRow(LegalAgeGroup.NotAdult, "jwt", null, null)]
        [DataRow(LegalAgeGroup.MinorNoParentalConsentRequired, "jwt", null, null)]
        public async Task GetOrCreateAsync_ProfileDoesNotExistSuccess(LegalAgeGroup legalAgeGroup, string familyJwt, bool? expectedOnBehalfOfFlag, bool? expectedTailoredExperience)
        {
            PrivacyProfile defaultProfile = DefaultProfileSettingsFactory.CreateDefaultPrivacyProfile();

            this.mockCustomerMasterAdapter
                .Setup(m => m.GetPrivacyProfileAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<string>()))
                .ReturnsAsync(
                    new AdapterResponse<PrivacyProfile>
                    {
                        Result = null
                    });

            PrivacyProfile actualPrivacyProfile = null; // Verify that the privacy profile passed has the correct OBO flag based on the IRequestContext
            this.mockCustomerMasterAdapter
                .Setup(m => m.CreatePrivacyProfileAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<PrivacyProfile>()))
                .ReturnsAsync(
                    new AdapterResponse<PrivacyProfile>
                    {
                        Result = defaultProfile
                    })
                .Callback<IPxfRequestContext, PrivacyProfile>((ctx, profile) => actualPrivacyProfile = profile);

            var userSettingsService = new UserSettingsService(this.mockCustomerMasterAdapter.Object, TestMockFactory.CreateLogger().Object);

            ServiceResponse<ResourceSettingV1> response = await userSettingsService.GetOrCreateAsync(this.CreateRequestContext(null, legalAgeGroup, familyJwt)).ConfigureAwait(false);

            Assert.IsNotNull(response);
            Assert.IsTrue(response.IsSuccess);
            Assert.AreEqual(true, response.Result.Advertising);
            Assert.IsNotNull(actualPrivacyProfile);
            Assert.AreEqual(null, response.Result.SharingState);

            // This is what we'd passed to Customer Master
            Assert.AreEqual(expectedOnBehalfOfFlag, actualPrivacyProfile.OnBehalfOfPrivacy);
            Assert.AreEqual(expectedOnBehalfOfFlag, actualPrivacyProfile.LocationPrivacy);
            Assert.AreEqual(expectedTailoredExperience, response.Result.TailoredExperiencesOffers);

            this.mockCustomerMasterAdapter.Verify(m => m.GetPrivacyProfileAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<string>()), Times.Once);
            this.mockCustomerMasterAdapter.Verify(m => m.CreatePrivacyProfileAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<PrivacyProfile>()), Times.Once);
        }

        [DataTestMethod]
        [DataRow(LegalAgeGroup.Undefined, null, null, true, null, true, true)]
        [DataRow(LegalAgeGroup.Adult, null, null, true, null, true, true)]
        [DataRow(LegalAgeGroup.MinorNoParentalConsentRequired, null, null, true, null, true, true)]
        [DataRow(LegalAgeGroup.MinorWithParentalConsent, null, null, true, null, true, true)]
        [DataRow(LegalAgeGroup.MinorWithoutParentalConsent, null, null, true, null, true, true)]
        [DataRow(LegalAgeGroup.NotAdult, null, null, true, null, true, true)]
        [DataRow(LegalAgeGroup.Undefined, false, null, true, false, true, true)]
        [DataRow(LegalAgeGroup.Adult, true, null, true, true, true, true)]
        [DataRow(LegalAgeGroup.MinorNoParentalConsentRequired, false, null, true, false, true, true)]
        [DataRow(LegalAgeGroup.MinorWithParentalConsent, true, null, true, true, true, true)]
        [DataRow(LegalAgeGroup.MinorWithoutParentalConsent, true, null, true, true, true, true)]
        [DataRow(LegalAgeGroup.NotAdult, false, null, true, false, true, true)]
        [DataRow(LegalAgeGroup.Undefined, null, false, false, null, false, false)]
        [DataRow(LegalAgeGroup.Undefined, true, true, false, true, true, false)]
        public async Task GetOrCreateAsync_ProfileUpdated(
            LegalAgeGroup ticketAge,
            bool? tailoredExperience,
            bool? advertising,
            bool updated,
            bool? expectedTailoredExperience,
            bool? expectedAdvertising,
            bool? sharing_state)
        {
            this.mockCustomerMasterAdapter
                .Setup(m => m.GetPrivacyProfileAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<string>()))
                .ReturnsAsync(
                    new AdapterResponse<PrivacyProfile>
                    {
                        Result = GetExistingTestProfile()
                    });

            PrivacyProfile actualPrivacyProfile = null;
            this.mockCustomerMasterAdapter
                .Setup(m => m.UpdatePrivacyProfileAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<PrivacyProfile>(), It.IsAny<JObject>()))
                .ReturnsAsync(
                    new AdapterResponse<PrivacyProfile>
                    {
                        Result = GetUpdatedTestProfile()
                    })
                .Callback<IPxfRequestContext, PrivacyProfile, JObject>((ctx, profile, jobject) => actualPrivacyProfile = profile);

            var userSettingsService = new UserSettingsService(this.mockCustomerMasterAdapter.Object, TestMockFactory.CreateLogger().Object);

            ServiceResponse<ResourceSettingV1> response = await userSettingsService.GetOrCreateAsync(this.CreateRequestContext(null, ticketAge)).ConfigureAwait(false);

            Assert.IsNotNull(response);
            Assert.IsTrue(response.IsSuccess);
            Assert.IsNull(response.Result.OnBehalfOfPrivacy);

            // If tailor experienced was not set, we expect it to be updated to true
            Assert.AreEqual(expectedTailoredExperience, response.Result.TailoredExperiencesOffers);
            Assert.AreEqual(expectedAdvertising, response.Result.Advertising);
            Assert.AreEqual(sharing_state, response.Result.SharingState);
            Assert.IsTrue(IsExpectedProfile(actualPrivacyProfile));

            this.mockCustomerMasterAdapter.Verify(m => m.GetPrivacyProfileAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<string>()), Times.Once);
            this.mockCustomerMasterAdapter
                .Verify(m => m.UpdatePrivacyProfileAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<PrivacyProfile>(), It.IsAny<JObject>()), Times.Exactly(updated ? 1 : 0));
            this.mockCustomerMasterAdapter.Verify(m => m.CreatePrivacyProfileAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<PrivacyProfile>()), Times.Never);

            PrivacyProfile GetExistingTestProfile()
            {
                PrivacyProfile defaultProfile = DefaultProfileSettingsFactory.CreateDefaultPrivacyProfile();
                defaultProfile.TailoredExperiencesOffers = tailoredExperience;
                defaultProfile.Advertising = advertising;
                defaultProfile.Type = CustomerMasterAdapter.PrivacyProfileGetType;
                defaultProfile.SharingState = sharing_state;
                return defaultProfile;
            }

            PrivacyProfile GetUpdatedTestProfile()
            {
                PrivacyProfile defaultProfile = DefaultProfileSettingsFactory.CreateDefaultPrivacyProfile();
                defaultProfile.TailoredExperiencesOffers = tailoredExperience ?? defaultProfile.TailoredExperiencesOffers;
                defaultProfile.Advertising = advertising ?? defaultProfile.Advertising;
                defaultProfile.Type = CustomerMasterAdapter.PrivacyProfileGetType;
                defaultProfile.SharingState = sharing_state ?? defaultProfile.SharingState;
                return defaultProfile;
            }

            bool IsExpectedProfile(PrivacyProfile profile)
            {
                if (!updated)
                {
                    return profile == null;
                }

                PrivacyProfile expected = GetUpdatedTestProfile();
                return
                    expected.Advertising == profile.Advertising &&
                    expected.OnBehalfOfPrivacy == profile.OnBehalfOfPrivacy &&
                    expected.TailoredExperiencesOffers == profile.TailoredExperiencesOffers &&
                    expected.SharingState == profile.SharingState &&
                    string.Equals(expected.Type, profile.Type);
            }
        }

        /// <summary>
        ///     Test get returns error when privacy profile does not exist and cannot be created
        /// </summary>
        [TestMethod]
        public async Task GetOrCreateAsync_FailedToCreateProfileError()
        {
            const string ErrorMessageFailedToCreateProfile = "The request was an attempt to create a resource that already exists.";

            var adapterError = new AdapterError(AdapterErrorCode.ResourceAlreadyExists, ErrorMessageFailedToCreateProfile, (int)HttpStatusCode.Conflict);
            this.mockCustomerMasterAdapter
                .Setup(m => m.GetPrivacyProfileAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<string>()))
                .ReturnsAsync(new AdapterResponse<PrivacyProfile> { Result = null });
            this.mockCustomerMasterAdapter
                .Setup(m => m.CreatePrivacyProfileAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<PrivacyProfile>()))
                .ReturnsAsync(new AdapterResponse<PrivacyProfile> { Error = adapterError });

            var userSettingsService = new UserSettingsService(this.mockCustomerMasterAdapter.Object, TestMockFactory.CreateLogger().Object);

            ServiceResponse<ResourceSettingV1> response = await userSettingsService.GetOrCreateAsync(this.TestRequestContext).ConfigureAwait(false);

            Assert.IsNotNull(response);
            Assert.IsFalse(response.IsSuccess);
            Assert.IsNotNull(response.Error);
            EqualityHelper.AreEqual(
                new Error(ErrorCode.CreateConflict, adapterError.ToString()),
                response.Error);

            this.mockCustomerMasterAdapter.Verify(m => m.GetPrivacyProfileAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<string>()), Times.Exactly(1));
            this.mockCustomerMasterAdapter.Verify(
                m => m.CreatePrivacyProfileAsync(It.IsAny<IPxfRequestContext>(), It.Is<PrivacyProfile>(p => p.Advertising == true)),
                Times.Exactly(1));
        }

        /// <summary>
        ///     Test get returns error when privacy profile does not exist and cannot be created
        /// </summary>
        [TestMethod]
        public async Task GetOrCreateAsync_FailedToCreateProfileUnknownError()
        {
            const string ErrorMessageFailedToCreateProfile = "Failed to create profile";

            var adapterError = new AdapterError(AdapterErrorCode.Unknown, "Adapter error message", (int)HttpStatusCode.InternalServerError);
            this.mockCustomerMasterAdapter
                .Setup(m => m.GetPrivacyProfileAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<string>()))
                .ReturnsAsync(new AdapterResponse<PrivacyProfile> { Result = null });
            this.mockCustomerMasterAdapter
                .Setup(m => m.CreatePrivacyProfileAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<PrivacyProfile>()))
                .ReturnsAsync(new AdapterResponse<PrivacyProfile> { Error = adapterError });

            var userSettingsService = new UserSettingsService(this.mockCustomerMasterAdapter.Object, TestMockFactory.CreateLogger().Object);

            ServiceResponse<ResourceSettingV1> response = await userSettingsService.GetOrCreateAsync(this.TestRequestContext).ConfigureAwait(false);

            Assert.IsNotNull(response);
            Assert.IsFalse(response.IsSuccess);
            Assert.IsNotNull(response.Error);
            EqualityHelper.AreEqual(
                new Error(ErrorCode.PartnerError, ErrorMessageFailedToCreateProfile) { ErrorDetails = adapterError.ToString() },
                response.Error);

            this.mockCustomerMasterAdapter.Verify(m => m.GetPrivacyProfileAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<string>()), Times.Once);
            this.mockCustomerMasterAdapter.Verify(m => m.CreatePrivacyProfileAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<PrivacyProfile>()), Times.Once);
        }

        #endregion

        #region Update User Settings

        [TestMethod]
        public async Task UpdateAsync_Success()
        {
            this.mockCustomerMasterAdapter
                .Setup(m => m.GetPrivacyProfileJObjectAsync(It.IsAny<IPxfRequestContext>()))
                .ReturnsAsync(
                    new AdapterResponse<JObject>
                    {
                        Result = CreateDefaultProfile()
                    });

            var userSettingsService = new UserSettingsService(this.mockCustomerMasterAdapter.Object, TestMockFactory.CreateLogger().Object);

            var newSettingsRequest = new ResourceSettingV1
            {
                Advertising = true,
                TailoredExperiencesOffers = true,
                SharingState = true
            };

            ServiceResponse<ResourceSettingV1> response = await userSettingsService.UpdateAsync(this.TestRequestContext, newSettingsRequest).ConfigureAwait(false);

            Assert.IsNotNull(response);
            Assert.IsTrue(response.IsSuccess);

            this.mockCustomerMasterAdapter.Verify(m => m.GetPrivacyProfileJObjectAsync(It.IsAny<IPxfRequestContext>()), Times.Once);
            this.mockCustomerMasterAdapter.Verify(m => m.UpdatePrivacyProfileAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<PrivacyProfile>(), It.IsAny<JObject>()), Times.Once);
        }

        [TestMethod]
        public async Task UpdateAsync_NoChangeRequested()
        {
            var newSettingsRequest = new ResourceSettingV1();

            this.mockCustomerMasterAdapter
                .Setup(m => m.GetPrivacyProfileAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<string>()))
                .ReturnsAsync(new AdapterResponse<PrivacyProfile> { Result = null });

            var userSettingsService = new UserSettingsService(this.mockCustomerMasterAdapter.Object, TestMockFactory.CreateLogger().Object);

            ServiceResponse<ResourceSettingV1> response = await userSettingsService.UpdateAsync(this.TestRequestContext, newSettingsRequest).ConfigureAwait(false);

            Assert.IsNotNull(response);
            Assert.IsFalse(response.IsSuccess);
            Assert.AreEqual(response.Error.Code, ErrorCode.ResourceNotModified.ToString());

            // since no changes requested, adapter requests are never invoked
            this.mockCustomerMasterAdapter.Verify(m => m.GetPrivacyProfileAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<string>()), Times.Never);
            this.mockCustomerMasterAdapter.Verify(m => m.UpdatePrivacyProfileAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<PrivacyProfile>(), It.IsAny<JObject>()), Times.Never);
        }

        [TestMethod]
        public async Task UpdateAsync_Error()
        {
            var adapterError = new AdapterError(AdapterErrorCode.Unknown, "Unknown adapter error message from update request.", (int)HttpStatusCode.InternalServerError);
            var expectedError = new Error(ErrorCode.Unknown, adapterError.ToString());

            this.mockCustomerMasterAdapter
                .Setup(m => m.UpdatePrivacyProfileAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<PrivacyProfile>(), It.IsAny<JObject>()))
                .ReturnsAsync(new AdapterResponse<PrivacyProfile> { Error = adapterError });

            var userSettingsService = new UserSettingsService(this.mockCustomerMasterAdapter.Object, TestMockFactory.CreateLogger().Object);

            var newSettingsRequest = new ResourceSettingV1
            {
                Advertising = true,
                TailoredExperiencesOffers = true,
                SharingState = true
            };

            ServiceResponse<ResourceSettingV1> response = await userSettingsService.UpdateAsync(this.TestRequestContext, newSettingsRequest).ConfigureAwait(false);

            Assert.IsNotNull(response);
            Assert.IsFalse(response.IsSuccess);
            EqualityHelper.AreEqual(expectedError, response.Error);

            this.mockCustomerMasterAdapter.Verify(m => m.GetPrivacyProfileJObjectAsync(It.IsAny<IPxfRequestContext>()), Times.Once);
            this.mockCustomerMasterAdapter.Verify(m => m.UpdatePrivacyProfileAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<PrivacyProfile>(), It.IsAny<JObject>()), Times.Once);
        }

        [TestMethod]
        public async Task UpdateAsync_ConflictError()
        {
            var adapterError = new AdapterError(AdapterErrorCode.ResourceAlreadyExists, "The request was an attempt to create a resource that already exists.", 409);
            var expectedError = new Error(ErrorCode.UpdateConflict, adapterError.ToString());

            this.mockCustomerMasterAdapter
                .Setup(m => m.UpdatePrivacyProfileAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<PrivacyProfile>(), It.IsAny<JObject>()))
                .ReturnsAsync(new AdapterResponse<PrivacyProfile> { Error = adapterError });

            var userSettingsService = new UserSettingsService(this.mockCustomerMasterAdapter.Object, TestMockFactory.CreateLogger().Object);

            var newSettingsRequest = new ResourceSettingV1
            {
                Advertising = true,
                TailoredExperiencesOffers = true,
                SharingState = true
            };

            ServiceResponse<ResourceSettingV1> response = await userSettingsService.UpdateAsync(this.TestRequestContext, newSettingsRequest).ConfigureAwait(false);

            Assert.IsNotNull(response);
            Assert.IsFalse(response.IsSuccess);
            EqualityHelper.AreEqual(expectedError, response.Error);

            this.mockCustomerMasterAdapter.Verify(m => m.GetPrivacyProfileJObjectAsync(It.IsAny<IPxfRequestContext>()), Times.Once);
            this.mockCustomerMasterAdapter.Verify(m => m.UpdatePrivacyProfileAsync(It.IsAny<IPxfRequestContext>(), It.IsAny<PrivacyProfile>(), It.IsAny<JObject>()), Times.Once);
        }

        #endregion
    }
}
