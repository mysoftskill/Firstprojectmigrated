// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.PrivacyRequest
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using Microsoft.ComplianceServices.AnaheimIdLib.Schema;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Privacy.Core.PrivacyCommand;
    using Microsoft.Membership.MemberServices.Privacy.Core.Vortex.Event;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.PrivacySubject;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2;
    using Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.Vortex;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Predicates;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;
    using Newtonsoft.Json.Linq;
    using static Microsoft.Membership.MemberServices.PrivacyAdapters.AadRequestVerificationService.AadRequestVerificationServiceAdapter;
    using PcfPrivacySubjects = Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;

    [TestClass]
    public class PrivacyRequestConverterTests
    {
        private static readonly Random random = new Random();

        public struct PrivacyScope
        {
            public bool ControllerApplicable { get; set; }

            public bool ProcessorApplicable { get; set; }
        }

        private readonly DemographicSubject.Address fakeAddress =
            new DemographicSubject.Address
            {
                StreetNumbers = new List<string> { "123" },
                StreetNames = new List<string> { "street1", "street2" },
                UnitNumbers = new List<string> { "456", "789" },
                Cities = new List<string> { "city" },
                Regions = new List<string> { "qwe", "asd" },
                PostalCodes = new List<string> { "34234234", "34234234-3453" }
            };

        private readonly PrivacyServices.PrivacyOperation.Contracts.PrivacySubject.DemographicSubject.Address fakeAddressV2 =
            new PrivacyServices.PrivacyOperation.Contracts.PrivacySubject.DemographicSubject.Address
            {
                StreetNumbers = new List<string> { "123" },
                StreetNames = new List<string> { "street1", "street2" },
                UnitNumbers = new List<string> { "456", "789" },
                Cities = new List<string> { "city" },
                Regions = new List<string> { "qwe", "asd" },
                PostalCodes = new List<string> { "34234234", "34234234-3453" }
            };

        private readonly IList<string> fakeEmails = new List<string> { "qwe@example.com", "asd@contoso.com" };

        private readonly string fakeEmployeeId = "2394802384234";

        private readonly DateTimeOffset fakeEmploymentEndDate = DateTimeOffset.UtcNow.AddYears(-1);

        private readonly DateTimeOffset fakeEmploymentStartDate = DateTimeOffset.UtcNow.AddYears(-5);

        private readonly IList<string> fakeNames = new List<string> { "qwe" };

        private readonly IList<string> fakePhones = new List<string> { "1234567890" };

        [TestMethod]
        public void AsDeviceRequestPreventDuplicateIds()
        {
            var originalRequest = new DeleteRequest { RequestId = Guid.NewGuid() };
            PrivacyRequest newRequest = originalRequest.AsDeviceRequest("1337");

            Assert.AreNotEqual(originalRequest.RequestId, newRequest.RequestId);
            Assert.AreNotEqual(Guid.Empty, originalRequest.RequestId);
            Assert.AreNotEqual(Guid.Empty, newRequest.RequestId);
        }

        [TestMethod]
        public void ConvertToPcfDeleteRequest_AppUsage()
        {
            // Arrange
            uint authorizingPuid = 2853426262;
            int targetPuid = 1243562653;
            uint targetCid = 2365263467;
            IRequestContext requestContext = RequestContext.CreateOldStyle(new Uri("https://www.test.com"),
                "testProxyTicket",
                "testFamilyJWT",
                authorizingPuid,
                targetPuid,
                3463463232,
                targetCid,
                "US",
                "PartnerCaller",
                16111166,
                new string[0],
                false);
            DateTimeOffset currentTime = DateTimeOffset.UtcNow;
            DateTimeOffset cardTime = DateTimeOffset.UtcNow.AddHours(-5);
            DateTimeOffset endCardTime = DateTimeOffset.UtcNow.AddHours(-2);
            var expectedPredicate = new ProductAndServiceUsagePredicate();
            expectedPredicate.AppId = Guid.NewGuid().ToString();
            expectedPredicate.WindowsDiagnosticsDeleteOnly = true;
            var appUsageCard = new AppUsageCard(expectedPredicate.AppId, null, null, null, null, null, cardTime, endCardTime, null, null, null);
            Guid requestGuid = Guid.NewGuid();

            // Act
            DeleteRequest deleteRequest = appUsageCard.ToUserPcfDeleteRequest(
                requestContext,
                requestGuid,
                "testCv.0",
                currentTime,
                Policies.Current,
                "the_cloud!!1",
                "portal1",
                false);

            // Assert
            ValidateBaseDeleteRequest(deleteRequest, authorizingPuid, currentTime, requestGuid);
            ValidateSingleUser(deleteRequest, targetPuid, targetCid);
            Assert.IsFalse(deleteRequest.IsWatchdogRequest);

            // Assert: PDT specific
            Assert.AreEqual(Policies.Current.DataTypes.Ids.ProductAndServiceUsage.Value, deleteRequest.PrivacyDataType);
            Assert.IsNotNull(deleteRequest.Predicate);
            var actualPredicate = deleteRequest.Predicate as ProductAndServiceUsagePredicate;
            Assert.IsNotNull(actualPredicate);
            Assert.AreEqual(expectedPredicate.AppId, actualPredicate.AppId);
            Assert.IsNotNull(deleteRequest.TimeRangePredicate);
            Assert.AreEqual(deleteRequest.TimeRangePredicate.EndTime, endCardTime);
            Assert.AreEqual(deleteRequest.TimeRangePredicate.StartTime, cardTime);
        }

        [TestMethod]
        public void ConvertToPcfDeleteRequest_AppUsageWithPropertyBag()
        {
            const string PropBagKey = "key1";
            const string PropBagVal = "val1";
            string expectedAppId = Guid.NewGuid().ToString();

            // Arrange
            uint authorizingPuid = 2853426262;
            int targetPuid = 1243562653;
            uint targetCid = 2365263467;
            IRequestContext requestContext = RequestContext.CreateOldStyle(new Uri("https://www.test.com"),
                "testProxyTicket",
                "testFamilyJWT",
                authorizingPuid,
                targetPuid,
                3463463232,
                targetCid,
                "US",
                "PartnerCaller",
                16111166,
                new string[0],
                false);
            DateTimeOffset currentTime = DateTimeOffset.UtcNow;
            DateTimeOffset cardTime = DateTimeOffset.UtcNow.AddHours(-5);
            DateTimeOffset endCardTime = DateTimeOffset.UtcNow.AddHours(-3);
            Guid requestGuid = Guid.NewGuid();

            IDictionary<string, IList<string>> expectedPropBag =
                new Dictionary<string, IList<string>> { { PropBagKey, new[] { PropBagVal } } };

            var appUsageCard = new AppUsageCard(
                expectedAppId,
                null,
                null,
                null,
                null,
                null,
                cardTime,
                endCardTime,
                null,
                null,
                expectedPropBag);

            // Act
            DeleteRequest deleteRequest = appUsageCard.ToUserPcfDeleteRequest(
                requestContext,
                requestGuid,
                "testCv.0",
                currentTime,
                Policies.Current,
                "cLoudInStanCe",
                "portal",
                false);

            // Assert
            ValidateBaseDeleteRequest(deleteRequest, authorizingPuid, currentTime, requestGuid);
            ValidateSingleUser(deleteRequest, targetPuid, targetCid);

            // Assert: PDT specific
            Assert.AreEqual(Policies.Current.DataTypes.Ids.ProductAndServiceUsage.Value, deleteRequest.PrivacyDataType);
            Assert.IsNotNull(deleteRequest.Predicate);
            var actualPredicate = deleteRequest.Predicate as ProductAndServiceUsagePredicate;
            Assert.IsNotNull(actualPredicate);
            Assert.AreEqual(expectedAppId, actualPredicate.AppId);
            Assert.IsNotNull(deleteRequest.TimeRangePredicate);
            Assert.AreEqual(deleteRequest.TimeRangePredicate.EndTime, endCardTime);
            Assert.AreEqual(deleteRequest.TimeRangePredicate.StartTime, cardTime);
            Assert.AreEqual(expectedPropBag.Count, actualPredicate.PropertyBag.Count);
            Assert.IsTrue(actualPredicate.PropertyBag.ContainsKey(PropBagKey));
            Assert.AreEqual(expectedPropBag[PropBagKey].Count, actualPredicate.PropertyBag[PropBagKey].Count);
            Assert.AreEqual(expectedPropBag[PropBagKey].First(), actualPredicate.PropertyBag[PropBagKey].First());
        }

        [TestMethod]
        public void ConvertToPcfDeleteRequest_Voice()
        {
            // Arrange
            uint authorizingPuid = 2853426262;
            int targetPuid = 1243562653;
            uint targetCid = 2365263467;
            IRequestContext requestContext = this.CreateRequestContext(authorizingPuid, targetPuid, targetCid);

            DateTimeOffset currentTime = DateTimeOffset.UtcNow;
            DateTimeOffset cardTime = DateTimeOffset.UtcNow.AddHours(-5);
            var expectedPredicate = new InkingTypingAndSpeechUtterancePredicate();
            expectedPredicate.ImpressionGuid = Guid.NewGuid().ToString();
            var card = new VoiceCard(expectedPredicate.ImpressionGuid, null, null, null, cardTime, null, null);
            Guid requestGuid = Guid.NewGuid();

            // Act
            DeleteRequest deleteRequest = card.ToUserPcfDeleteRequest(
                requestContext,
                requestGuid,
                "testCv.0",
                currentTime,
                Policies.Current,
                "thecloudisgreat",
                "portalisgreater",
                false);

            // Assert
            ValidateBaseDeleteRequest(deleteRequest, authorizingPuid, currentTime, requestGuid);
            ValidateSingleUser(deleteRequest, targetPuid, targetCid);

            // Assert: PDT specific
            Assert.AreEqual(Policies.Current.DataTypes.Ids.InkingTypingAndSpeechUtterance.Value, deleteRequest.PrivacyDataType);
            Assert.IsNotNull(deleteRequest.Predicate);
            var actualPredicate = deleteRequest.Predicate as InkingTypingAndSpeechUtterancePredicate;
            Assert.IsNotNull(actualPredicate);
            Assert.AreEqual(expectedPredicate.ImpressionGuid, actualPredicate.ImpressionGuid);
            Assert.AreEqual(expectedPredicate.WindowsDiagnosticsDeleteOnly, actualPredicate.WindowsDiagnosticsDeleteOnly);
            Assert.IsNotNull(deleteRequest.TimeRangePredicate);
            Assert.AreEqual(deleteRequest.TimeRangePredicate.EndTime, cardTime);
            Assert.AreEqual(deleteRequest.TimeRangePredicate.StartTime, cardTime);
        }

        [TestMethod]
        public void CreatePcfDeleteRequestsTest()
        {
            const string GlobalDeviceId1 = "global[A1B2C3D4F5]";
            const string GlobalDeviceId2 = "ABCDEF0123456789";

            const uint AuthorizingPuid = 2853426262;
            const long TargetPuid = 1243562653;
            const long TargetCid = long.MaxValue;
            const string TargetXuid = "999";
            IRequestContext requestContext = this.CreateRequestContext(AuthorizingPuid, TargetPuid, TargetCid);
            DateTimeOffset start = DateTimeOffset.Parse("2017-02-05 10:15", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
            DateTimeOffset end = DateTimeOffset.Parse("2017-05-03 3:12", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
            
            const string CorrelationVector = "ImACorrelationVector.0";
            const string Context = "TestContext";
            Guid requestGuid = Guid.NewGuid();
            const string PrivacyDataType = "TestDataType";

            PcfPrivacySubjects.MsaSubject msaSubjectFromContext = PrivacyRequestConverter.CreateMsaSubjectFromContext(requestContext);
            msaSubjectFromContext.Xuid = TargetXuid;
            List<DeleteRequest> pcfDeleteRequests = PrivacyRequestConverter.CreatePcfDeleteRequests(
                msaSubjectFromContext,
                requestContext,
                requestGuid,
                CorrelationVector,
                Context,
                end,
                new[] { PrivacyDataType },
                start,
                end,
                "yetanothercloud",
                "andstillmoreportals",
                false).ToList();
            pcfDeleteRequests.AddRange(
                pcfDeleteRequests.SelectMany(r => new[] { r.AsDeviceRequest(GlobalDeviceId1), r.AsDeviceRequest(GlobalDeviceId2) }).Cast<DeleteRequest>().ToList());

            Assert.IsNotNull(pcfDeleteRequests);
            Assert.AreEqual(3, pcfDeleteRequests.Count);

            ValidateUserDeleteRequest(
                pcfDeleteRequests[0],
                requestGuid,
                CorrelationVector,
                Context,
                AuthorizingPuid,
                TargetPuid,
                TargetCid,
                PrivacyDataType,
                start,
                end,
                TargetXuid);
            ValidateDeviceDeleteRequest(
                pcfDeleteRequests[1],
                requestGuid,
                CorrelationVector,
                Context,
                AuthorizingPuid,
                PrivacyDataType,
                start,
                end,
                GlobalDeviceId1);
            ValidateDeviceDeleteRequest(
                pcfDeleteRequests[2],
                requestGuid,
                CorrelationVector,
                Context,
                AuthorizingPuid,
                PrivacyDataType,
                start,
                end,
                GlobalDeviceId2);
        }

        [DataTestMethod]
        [DynamicData(nameof(GetRequestApplicabilityTestsData), DynamicDataSourceType.Method)]
        public void GetRequestApplicabilityTests(PrivacyRequest request, PrivacyScope scope)
        {
            PrivacyRequestConverter.UpdateRequestApplicability(request);

            Assert.AreEqual(scope.ControllerApplicable, request.ControllerApplicable);
            Assert.AreEqual(scope.ProcessorApplicable, request.ProcessorApplicable);
        }

        [TestMethod]
        public void LocationCardsToUserPcfDeleteRequestsShouldNotDuplicateIds()
        {
            IRequestContext mockRequestContext = CreateMockRequestContext();
            Guid expectedRequestGuid = Guid.NewGuid();
            string expectedCv = "abc123.0";
            DateTimeOffset expectedTimestamp = DateTimeOffset.UtcNow.AddMinutes(42);
            Policy policy = Policies.Current;

            IList<LocationCard> cards = new List<LocationCard>
            {
                CreateRandomLocationCard(),
                CreateRandomLocationCard(),
                CreateRandomLocationCard()
            };

            List<DeleteRequest> result = cards.ToUserPcfDeleteRequests(
                mockRequestContext,
                expectedRequestGuid,
                expectedCv,
                expectedTimestamp,
                policy,
                "morecloudsyousay?",
                "no, more portals.",
                false).ToList();

            Assert.IsNotNull(result);
            Assert.AreEqual(result.Count, result.Select(c => c.RequestId).Distinct().Count());
        }

        [TestMethod]
        public void PrivacyRequestConvert_CreatePcfAccountCloseRequest()
        {
            // Arrange
            Guid expectedTenantId = Guid.NewGuid();
            Guid expectedObjectId = Guid.NewGuid();
            Guid expectedRequestGuid = Guid.NewGuid();
            string expectedCv = "abc123.0";
            DateTimeOffset expectedTimestamp = DateTimeOffset.UtcNow;
            Guid expectedAuthorizingObjectId = Guid.NewGuid();
            int expectedOrgIdPuid = 123;
            const string ExpectedPreverifier = "verifiertokensarecool!!1!1";

            AadIdentity identity = new AadIdentity(
                "appId",
                expectedAuthorizingObjectId,
                expectedObjectId,
                expectedTenantId,
                "accessToken",
                "appDisplayName");
            identity.OrgIdPuid = expectedOrgIdPuid;
            var requestContext = new RequestContext(identity);

            // Act
            PcfPrivacySubjects.AadSubject subject = PrivacyRequestConverter.CreateAadSubjectFromIdentity(identity);
            AccountCloseRequest result = PrivacyRequestConverter.CreatePcfAccountCloseRequest(
                subject,
                requestContext,
                expectedRequestGuid,
                expectedCv,
                expectedTimestamp,
                "cloud!!!!",
                "portal!!!!",
                ExpectedPreverifier,
                false);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedRequestGuid, result.RequestGuid);
            Assert.AreEqual(RequestType.AccountClose, result.RequestType);
            Assert.AreEqual(expectedCv, result.CorrelationVector);
            Assert.AreEqual(expectedTimestamp, result.Timestamp);

            var aadSubject = result.Subject as PcfPrivacySubjects.AadSubject;
            Assert.IsNotNull(aadSubject);
            Assert.AreEqual(expectedTenantId, aadSubject.TenantId);
            Assert.AreEqual(expectedObjectId, aadSubject.ObjectId);
            Assert.AreEqual(expectedOrgIdPuid, aadSubject.OrgIdPUID);
            Assert.AreEqual($"a:{expectedAuthorizingObjectId}", result.AuthorizationId);
            Assert.IsFalse(result.IsWatchdogRequest);
            Assert.AreEqual(ExpectedPreverifier, result.VerificationToken);

            // This was a bug before. These should never be the same. If they were, it would be a strange coincidence. Bug # 15767139 
            Assert.AreNotEqual(expectedRequestGuid, result.RequestId);
        }

        [TestMethod]
        public void PrivacyRequestConverter_CreateFromAltSubject_DemographicSubject_Empty()
        {
            var altSubject = new DemographicSubject();
            var actualSubject = (PcfPrivacySubjects.DemographicSubject)PrivacyRequestConverter.ToSubject(altSubject, null);

            Assert.IsNull(actualSubject.Names);
            Assert.IsNull(actualSubject.EmailAddresses);
            Assert.IsNull(actualSubject.PhoneNumbers);
            Assert.IsNull(actualSubject.Address);
        }

        [TestMethod]
        public void PrivacyRequestConverter_CreateFromAltSubject_DemographicSubject_Empty_V2()
        {
            var altSubject = new PrivacyServices.PrivacyOperation.Contracts.PrivacySubject.DemographicSubject();
            var actualSubject = (PcfPrivacySubjects.DemographicSubject)PrivacyRequestConverter.ToSubject(altSubject, null);

            Assert.IsNull(actualSubject.Names);
            Assert.IsNull(actualSubject.EmailAddresses);
            Assert.IsNull(actualSubject.PhoneNumbers);
            Assert.IsNull(actualSubject.Address);
        }

        [TestMethod]
        public void PrivacyRequestConverter_CreateFromAltSubject_DemographicSubject_HasPostalAddress()
        {
            var altSubject = new DemographicSubject
            {
                Names = this.fakeNames,
                Emails = this.fakeEmails,
                Phones = this.fakePhones,
                PostalAddress = this.fakeAddress
            };
            var actualSubject = (PcfPrivacySubjects.DemographicSubject)PrivacyRequestConverter.ToSubject(altSubject, null);

            CollectionAssert.AreEqual(altSubject.Names.ToArray(), actualSubject.Names.ToArray());
            CollectionAssert.AreEqual(altSubject.Emails.ToArray(), actualSubject.EmailAddresses.ToArray());
            CollectionAssert.AreEqual(altSubject.Phones.ToArray(), actualSubject.PhoneNumbers.ToArray());
            Assert.IsNotNull(actualSubject.Address);
            CollectionAssert.AreEqual(altSubject.PostalAddress.StreetNumbers.ToArray(), actualSubject.Address.StreetNumbers.ToArray());
            CollectionAssert.AreEqual(altSubject.PostalAddress.StreetNames.ToArray(), actualSubject.Address.Streets.ToArray());
            CollectionAssert.AreEqual(altSubject.PostalAddress.UnitNumbers.ToArray(), actualSubject.Address.UnitNumbers.ToArray());
            CollectionAssert.AreEqual(altSubject.PostalAddress.Cities.ToArray(), actualSubject.Address.Cities.ToArray());
            CollectionAssert.AreEqual(altSubject.PostalAddress.Regions.ToArray(), actualSubject.Address.States.ToArray());
            CollectionAssert.AreEqual(altSubject.PostalAddress.PostalCodes.ToArray(), actualSubject.Address.PostalCodes.ToArray());
        }

        [TestMethod]
        public void PrivacyRequestConverter_CreateFromAltSubject_DemographicSubject_HasPostalAddressV2()
        {
            var altSubject = new PrivacyServices.PrivacyOperation.Contracts.PrivacySubject.DemographicSubject
            {
                Names = this.fakeNames,
                Emails = this.fakeEmails,
                Phones = this.fakePhones,
                PostalAddress = this.fakeAddressV2
            };
            var actualSubject = (PcfPrivacySubjects.DemographicSubject)PrivacyRequestConverter.ToSubject(altSubject, null);

            CollectionAssert.AreEqual(altSubject.Names.ToArray(), actualSubject.Names.ToArray());
            CollectionAssert.AreEqual(altSubject.Emails.ToArray(), actualSubject.EmailAddresses.ToArray());
            CollectionAssert.AreEqual(altSubject.Phones.ToArray(), actualSubject.PhoneNumbers.ToArray());
            Assert.IsNotNull(actualSubject.Address);
            CollectionAssert.AreEqual(altSubject.PostalAddress.StreetNumbers.ToArray(), actualSubject.Address.StreetNumbers.ToArray());
            CollectionAssert.AreEqual(altSubject.PostalAddress.StreetNames.ToArray(), actualSubject.Address.Streets.ToArray());
            CollectionAssert.AreEqual(altSubject.PostalAddress.UnitNumbers.ToArray(), actualSubject.Address.UnitNumbers.ToArray());
            CollectionAssert.AreEqual(altSubject.PostalAddress.Cities.ToArray(), actualSubject.Address.Cities.ToArray());
            CollectionAssert.AreEqual(altSubject.PostalAddress.Regions.ToArray(), actualSubject.Address.States.ToArray());
            CollectionAssert.AreEqual(altSubject.PostalAddress.PostalCodes.ToArray(), actualSubject.Address.PostalCodes.ToArray());
        }

        [TestMethod]
        public void PrivacyRequestConverter_CreateFromAltSubject_DemographicSubject_NoPostalAddress()
        {
            var altSubject = new DemographicSubject
            {
                Names = this.fakeNames,
                Emails = this.fakeEmails,
                Phones = this.fakePhones
            };
            var actualSubject = (PcfPrivacySubjects.DemographicSubject)PrivacyRequestConverter.ToSubject(altSubject, null);

            CollectionAssert.AreEqual(altSubject.Names.ToArray(), actualSubject.Names.ToArray());
            CollectionAssert.AreEqual(altSubject.Emails.ToArray(), actualSubject.EmailAddresses.ToArray());
            CollectionAssert.AreEqual(altSubject.Phones.ToArray(), actualSubject.PhoneNumbers.ToArray());
            Assert.IsNull(actualSubject.Address);
        }

        [TestMethod]
        public void PrivacyRequestConverter_CreateFromAltSubject_DemographicSubject_NoPostalAddressV2()
        {
            var altSubject = new PrivacyServices.PrivacyOperation.Contracts.PrivacySubject.DemographicSubject
            {
                Names = this.fakeNames,
                Emails = this.fakeEmails,
                Phones = this.fakePhones
            };
            var actualSubject = (PcfPrivacySubjects.DemographicSubject)PrivacyRequestConverter.ToSubject(altSubject, null);

            CollectionAssert.AreEqual(altSubject.Names.ToArray(), actualSubject.Names.ToArray());
            CollectionAssert.AreEqual(altSubject.Emails.ToArray(), actualSubject.EmailAddresses.ToArray());
            CollectionAssert.AreEqual(altSubject.Phones.ToArray(), actualSubject.PhoneNumbers.ToArray());
            Assert.IsNull(actualSubject.Address);
        }

        [TestMethod]
        public void PrivacyRequestConverter_CreateFromAltSubject_MicrosoftEmployeeSubject_FeatureFlagTest()
        {
            var altSubject = new PrivacyServices.PrivacyOperation.Contracts.PrivacySubject.MicrosoftEmployeeSubject();
            var actualSubject = PrivacyRequestConverter.ToSubject(altSubject, null);

            Assert.IsTrue(actualSubject.GetType() == typeof(PcfPrivacySubjects.MicrosoftEmployee));
        }

        [TestMethod]
        public void PrivacyRequestConverter_CreateFromAltSubject_MicrosoftEmployeeSubject_Empty()
        {
            var altSubject = new MicrosoftEmployeeSubject();
            var actualSubject = PrivacyRequestConverter.ToSubject(altSubject, null);

            Assert.IsInstanceOfType(actualSubject, typeof(PcfPrivacySubjects.MicrosoftEmployee));
        }

        [TestMethod]
        public void PrivacyRequestConverter_CreateFromAltSubject_MicrosoftEmployeeSubject_EmptyV2()
        {
            var altSubject = new PrivacyServices.PrivacyOperation.Contracts.PrivacySubject.MicrosoftEmployeeSubject();
            var actualSubject = PrivacyRequestConverter.ToSubject(altSubject, null);

            Assert.IsInstanceOfType(actualSubject, typeof(PcfPrivacySubjects.MicrosoftEmployee));
        }

        [TestMethod]
        public void PrivacyRequestConverter_CreateFromAltSubject_MicrosoftEmployeeSubject_HasEmploymentEnd()
        {
            var altSubject = new MicrosoftEmployeeSubject
            {
                Emails = this.fakeEmails,
                EmployeeId = this.fakeEmployeeId,
                EmploymentStart = this.fakeEmploymentStartDate,
                EmploymentEnd = this.fakeEmploymentEndDate
            };
            var actualSubject = PrivacyRequestConverter.ToSubject(altSubject, null);

            Assert.IsInstanceOfType(actualSubject, typeof(PcfPrivacySubjects.MicrosoftEmployee));
        }

        [TestMethod]
        public void PrivacyRequestConverter_CreateFromAltSubject_MicrosoftEmployeeSubject_HasEmploymentEndV2()
        {
            var altSubject = new PrivacyServices.PrivacyOperation.Contracts.PrivacySubject.MicrosoftEmployeeSubject
            {
                Emails = this.fakeEmails,
                EmployeeId = this.fakeEmployeeId,
                EmploymentStart = this.fakeEmploymentStartDate,
                EmploymentEnd = this.fakeEmploymentEndDate
            };
            var actualSubject = PrivacyRequestConverter.ToSubject(altSubject, null);

            Assert.IsInstanceOfType(actualSubject, typeof(PcfPrivacySubjects.MicrosoftEmployee));
        }

        [TestMethod]
        public void PrivacyRequestConverter_CreateFromAltSubject_MicrosoftEmployeeSubject_NoEmploymentEnd()
        {
            var altSubject = new MicrosoftEmployeeSubject
            {
                Emails = this.fakeEmails,
                EmployeeId = this.fakeEmployeeId,
                EmploymentStart = this.fakeEmploymentStartDate
            };
            var actualSubject = PrivacyRequestConverter.ToSubject(altSubject, null);

            Assert.IsInstanceOfType(actualSubject, typeof(PcfPrivacySubjects.MicrosoftEmployee));
        }

        [TestMethod]
        public void PrivacyRequestConverter_CreateFromAltSubject_MicrosoftEmployeeSubject_NoEmploymentEndV2()
        {
            var altSubject = new PrivacyServices.PrivacyOperation.Contracts.PrivacySubject.MicrosoftEmployeeSubject
            {
                Emails = this.fakeEmails,
                EmployeeId = this.fakeEmployeeId,
                EmploymentStart = this.fakeEmploymentStartDate
            };
            var actualSubject = PrivacyRequestConverter.ToSubject(altSubject, null);

            Assert.IsInstanceOfType(actualSubject, typeof(PcfPrivacySubjects.MicrosoftEmployee));
        }

        [TestMethod]
        public void PrivacyRequestConverter_ToSubject_MsaSelfAuthSubject()
        {
            IRequestContext requestContext = this.CreateRequestContext(123, 456, 789);
            PcfPrivacySubjects.IPrivacySubject actualSubject = PrivacyRequestConverter.ToSubject(new MsaSelfAuthSubject("proxy ticket"), requestContext);

            Assert.IsInstanceOfType(actualSubject, typeof(PcfPrivacySubjects.MsaSubject));
        }

        [TestMethod]
        public void PrivacyRequestConverter_ToSubject_MsaSelfAuthSubjectV2()
        {
            IRequestContext requestContext = this.CreateRequestContext(123, 456, 789);
            PcfPrivacySubjects.IPrivacySubject actualSubject = PrivacyRequestConverter.ToSubject(
                new PrivacyServices.PrivacyOperation.Contracts.PrivacySubject.MsaSelfAuthSubject("proxy ticket"),
                requestContext);

            Assert.IsInstanceOfType(actualSubject, typeof(PcfPrivacySubjects.MsaSubject));
        }

        [TestMethod]
        public void SearchCardsToUserPcfDeleteRequestsShouldNotDuplicateIds()
        {
            IRequestContext mockRequestContext = CreateMockRequestContext();
            Guid expectedRequestGuid = Guid.NewGuid();
            string expectedCv = "abc123.0";
            DateTimeOffset expectedTimestamp = DateTimeOffset.UtcNow.AddMinutes(42);
            Policy policy = Policies.Current;

            IList<SearchCard> cards = new List<SearchCard>
            {
                CreateRandomSearchCard(),
                CreateRandomSearchCard(),
                CreateRandomSearchCard(),
                CreateRandomSearchCard()
            };

            List<DeleteRequest> result = cards.ToUserPcfDeleteRequests(
                mockRequestContext,
                expectedRequestGuid,
                expectedCv,
                expectedTimestamp,
                policy,
                "somanyclouds",
                "toomanyportals",
                false).ToList();

            Assert.IsNotNull(result);
            Assert.AreEqual(result.Count, result.Select(c => c.RequestId).Distinct().Count());
        }

        [TestMethod]
        public void PrivacyRequestConverter_MsaExport_ShallHave_CCDDataType()
        {
            IRequestContext mockRequestContext = CreateMockRequestContext();
            Guid expectedRequestGuid = Guid.NewGuid();
            DateTimeOffset expectedTimestamp = DateTimeOffset.UtcNow.AddMinutes(42);

            var exportRequest = PrivacyRequestConverter.CreateExportRequest(null, mockRequestContext, expectedRequestGuid, null, expectedTimestamp, null, null, null, null, true, null, null, true);
            Assert.IsTrue(exportRequest.PrivacyDataTypes.Contains(Policies.Current.DataTypes.Ids.CapturedCustomerContent.Value));
        }

        [TestMethod]
        public void PrivacyRequestConverter_MsaExportWithDataTypes_ShallHave_CCDDataType()
        {
            IRequestContext mockRequestContext = CreateMockRequestContext();
            Guid expectedRequestGuid = Guid.NewGuid();
            DateTimeOffset expectedTimestamp = DateTimeOffset.UtcNow.AddMinutes(42);
            string[] exportDataTypes =   { Policies.Current.DataTypes.Ids.PreciseUserLocation.Value };
            
            var exportRequest = PrivacyRequestConverter.CreateExportRequest(null, mockRequestContext, expectedRequestGuid, null, expectedTimestamp, null, null, null, exportDataTypes, true, null, null, true);
            Assert.AreEqual(1,exportRequest.PrivacyDataTypes.Count());
            Assert.IsTrue(exportRequest.PrivacyDataTypes.Contains(Policies.Current.DataTypes.Ids.PreciseUserLocation.Value));
            Assert.IsFalse(exportRequest.PrivacyDataTypes.Contains(Policies.Current.DataTypes.Ids.CapturedCustomerContent.Value));
        }

        [TestMethod]
        public void PrivacyRequestConverter_AadExport_ShallNotHave_CCDDataType()
        {
            AadIdentity identity = new AadIdentity(null, new Guid(), new Guid(), null, null);
            IRequestContext mockRequestContext = new RequestContext(identity, new Uri("https://unittest"), new Dictionary<string, string[]>());
           
            Guid expectedRequestGuid = Guid.NewGuid();
            DateTimeOffset expectedTimestamp = DateTimeOffset.UtcNow.AddMinutes(42);

            var exportRequest = PrivacyRequestConverter.CreateAadExportRequest(mockRequestContext, expectedRequestGuid, null, expectedTimestamp, null, true, null, null, true);
            Assert.IsFalse(exportRequest.PrivacyDataTypes.Contains(Policies.Current.DataTypes.Ids.CapturedCustomerContent.Value));
        }
        
        [DataTestMethod]
        [DataRow(AadRvsOperationType.AccountClose)]
        [DataRow(AadRvsOperationType.AccountCleanup)]
        public void PrivacyRequestConverter_CreateAadRvsGdprRequestV2(AadRvsOperationType operationType)
        {
            var request = new AccountCloseRequest
            {
                Subject = new PcfPrivacySubjects.AadSubject
                {
                    TenantId = Guid.NewGuid(),
                    ObjectId = Guid.NewGuid(),
                    OrgIdPUID = 123,
                },
                RequestGuid = Guid.NewGuid(),
                RequestId = Guid.NewGuid(),
                VerificationToken = "verifier",
            };

            var aadRvsRequest = PrivacyRequestConverter.CreateAadRvsGdprRequestV2(request, operationType);

            Assert.AreEqual((request.Subject as PcfPrivacySubjects.AadSubject).TenantId.ToString(), aadRvsRequest.TenantId);
            Assert.AreEqual((request.Subject as PcfPrivacySubjects.AadSubject).ObjectId.ToString(), aadRvsRequest.ObjectId);
            Assert.AreEqual((request.Subject as PcfPrivacySubjects.AadSubject).OrgIdPUID.ToString("X16"), aadRvsRequest.OrgIdPuid);
            Assert.AreEqual(request.RequestGuid.ToString(), aadRvsRequest.CorrelationId);
            Assert.AreEqual(request.RequestId.ToString(), aadRvsRequest.CommandIds);
            Assert.AreEqual(request.VerificationToken, aadRvsRequest.PreVerifier);
            Assert.AreEqual(operationType.ToString(), aadRvsRequest.Operation);
            Assert.IsTrue(aadRvsRequest.ProcessorApplicable);
            if (operationType == AadRvsOperationType.AccountClose)
            {
                Assert.IsTrue(aadRvsRequest.ControllerApplicable);
            }
            else
            {
                Assert.IsFalse(aadRvsRequest.ControllerApplicable);
            }
        }

        [TestMethod]
        public void PrivacyRequestConverter_CreateAnaheimDeleteDeviceIdRequest()
        {
            var requestId = Guid.NewGuid();
            bool testSignal = true;
            var evt = JObject.Parse(VortexTestSettings.JsonEvent).ToObject<VortexEvent>();

            var requestTime = DateTimeOffset.UtcNow;
            var deleteDeviceIdRequest = PrivacyRequestConverter.CreateAnaheimDeleteDeviceIdRequest(evt, requestId, requestTime, testSignal);

            ValidateDeviceDeleteIdRequest(deleteDeviceIdRequest, 123456, requestTime, requestId);
        }

        private IRequestContext CreateRequestContext(uint authorizingPuid, long targetPuid, long targetCid)
        {
            return RequestContext.CreateOldStyle(new Uri("https://www.test.com"),
                "testProxyTicket",
                "testFamilyJWT",
                authorizingPuid,
                targetPuid,
                3463463232,
                targetCid,
                "US",
                "PartnerCaller",
                6226677,
                new string[0],
                false);
        }

        private static IRequestContext CreateMockRequestContext()
        {
            var msaIdentity = new MsaSelfIdentity(
                "proxyticket",
                "familytoken",
                773717751157,
                10161016161,
                null,
                "callerName",
                1234,
                15252626,
                null,
                null,
                false);

            return new RequestContext(msaIdentity, new Uri("https://unittest"), new Dictionary<string, string[]>());
        }

        private static IList<LocationCard.LocationImpression> CreateAdditionalLocations(int numberAdditionalLocations)
        {
            var additionalLocations = new List<LocationCard.LocationImpression>();
            for (int i = 0; i < numberAdditionalLocations; i++)
            {
                additionalLocations.Add(new LocationCard.LocationImpression(random.Next(0, 90), random.Next(0, 180), DateTimeOffset.UtcNow));
            }

            return additionalLocations;
        }

        internal static LocationCard CreateRandomLocationCard()
        {
            return CreateRandomLocationCard(new Random().Next(1, 10));
        }

        internal static LocationCard CreateRandomLocationCard(int numberLocations)
        {
            return new LocationCard(
                name: "foo",
                location: new LocationCard.GeographyPoint(12, 24, 42),
                accuracyRadius: null,
                activityType: "blah",
                endDateTime: null,
                url: new Uri($"https://{Guid.NewGuid():N}.com", UriKind.Absolute),
                distance: null,
                deviceType: "the type of device",
                additionalLocations: CreateAdditionalLocations(Math.Abs(numberLocations - 1)),
                timestamp: DateTimeOffset.UtcNow.AddSeconds(-1),
                deviceIds: new[] { "1", "2", "3" },
                sources: new[] { "a", "b", "c" });
        }

        private static SearchCard CreateRandomSearchCard()
        {
            var impressionIds = new List<string>();
            for (int i = 0; i < new Random().Next(1, 10); i++)
            {
                impressionIds.Add(Guid.NewGuid().ToString());
            }

            return new SearchCard(
                search: Guid.NewGuid().ToString(),
                navigations: new[] { new SearchCard.Navigation(Guid.NewGuid().ToString(), new Uri($"https://{Guid.NewGuid():N}.com", UriKind.Absolute), DateTimeOffset.UtcNow) },
                impressionIds: impressionIds,
                timestamp: DateTimeOffset.UtcNow.AddSeconds(-1),
                deviceIds: new[] { "1", "2", "3" },
                sources: new[] { "a", "b", "c" });
        }

        /// <summary>
        ///     Gets the get request applicability tests data base on the table from:
        ///     <a href="https://microsoft.sharepoint.com/teams/osg_unistore/mem/mee/_layouts/OneNote.aspx?id=%2Fteams%2Fosg_unistore%2Fmem%2Fmee%2FShared%20Documents%2FPXS%2FPrivacy%20Command%20Feed%20Team%26wd=target%28Design%20Specs.one%7C94748613-D2D1-4D1A-BEBF-F35EBB506B6B%2FValidator%20Changes%20for%20March%202018%7CC44F1F2A-69CC-4223-9DBB-C94A8CB5DE3A%2F%29" />
        /// </summary>
        private static IEnumerable<object[]> GetRequestApplicabilityTestsData() => new[]
        {
            new object[] // Msa Subject Account Close
            {
                new AccountCloseRequest
                {
                    Subject = new PcfPrivacySubjects.MsaSubject()
                },
                new PrivacyScope
                {
                    ControllerApplicable = true,
                    ProcessorApplicable = true
                }
            },
            new object[] // Aad Account Close
            {
                new AccountCloseRequest
                {
                    Subject = new PcfPrivacySubjects.AadSubject()
                },
                new PrivacyScope
                {
                    ControllerApplicable = true,
                    ProcessorApplicable = true
                }
            },
            new object[] // Msa Subject Delete
            {
                new DeleteRequest
                {
                    Subject = new PcfPrivacySubjects.MsaSubject()
                },
                new PrivacyScope
                {
                    ControllerApplicable = true,
                    ProcessorApplicable = false
                }
            },
            new object[] // Alt Subject Delete
            {
                new DeleteRequest(),
                new PrivacyScope
                {
                    ControllerApplicable = true,
                    ProcessorApplicable = false
                }
            },
            new object[] // Msa Subject Export
            {
                new ExportRequest
                {
                    Subject = new PcfPrivacySubjects.MsaSubject()
                },
                new PrivacyScope
                {
                    ControllerApplicable = true,
                    ProcessorApplicable = false
                }
            },
            new object[] // Aad Subject Export
            {
                new ExportRequest
                {
                    Subject = new PcfPrivacySubjects.AadSubject()
                },
                new PrivacyScope
                {
                    ControllerApplicable = false,
                    ProcessorApplicable = true
                }
            },
            new object[] // Alt Subject Export
            {
                new ExportRequest(),
                new PrivacyScope
                {
                    ControllerApplicable = true,
                    ProcessorApplicable = false
                }
            },
            new object[] // Device Subject Delete
            {
                new DeleteRequest
                {
                    Subject = new PcfPrivacySubjects.DeviceSubject()
                },
                new PrivacyScope
                {
                    ControllerApplicable = true,
                    ProcessorApplicable = false
                }
            }
        };

        private static void ValidateBaseDeleteRequest(
            DeleteRequest baseDeleteRequest,
            Guid requestGuid,
            string correlationVector,
            string context,
            uint authorizingPuid,
            string privacyDataType,
            DateTimeOffset start,
            DateTimeOffset end)
        {
            Assert.AreEqual($"p:{authorizingPuid}", baseDeleteRequest.AuthorizationId);
            Assert.AreEqual(correlationVector, baseDeleteRequest.CorrelationVector);
            Assert.AreEqual(context, baseDeleteRequest.Context);
            Assert.AreEqual(privacyDataType, baseDeleteRequest.PrivacyDataType);
            Assert.AreEqual(end, baseDeleteRequest.Timestamp);
            Assert.AreEqual(RequestType.Delete, baseDeleteRequest.RequestType);
            Assert.IsNull(baseDeleteRequest.Predicate);
            Assert.AreEqual(requestGuid, baseDeleteRequest.RequestGuid);
            Assert.AreNotEqual(default(Guid), baseDeleteRequest.RequestId);
        }

        private static void ValidateBaseDeleteRequest(
            DeleteRequest deleteRequest,
            uint authorizingPuid,
            DateTimeOffset currentTime,
            Guid requestGuid)
        {
            Assert.IsNotNull(deleteRequest);

            Assert.AreEqual($"p:{authorizingPuid}", deleteRequest.AuthorizationId);
            Assert.AreEqual("testCv.0", deleteRequest.CorrelationVector);
            Assert.AreEqual(currentTime, deleteRequest.Timestamp);
            Assert.AreEqual(requestGuid, deleteRequest.RequestGuid);
            Assert.AreNotEqual(default(Guid), deleteRequest.RequestId);
        }

        private static void ValidateDeviceDeleteRequest(
            DeleteRequest deviceDeleteRequest,
            Guid requestGuid,
            string correlationVector,
            string context,
            uint authorizingPuid,
            string privacyDataType,
            DateTimeOffset start,
            DateTimeOffset end,
            string globalDeviceId)
        {
            ValidateBaseDeleteRequest(
                deviceDeleteRequest,
                requestGuid,
                correlationVector,
                context,
                authorizingPuid,
                privacyDataType,
                start,
                end);
            var deviceSubject = deviceDeleteRequest.Subject as PcfPrivacySubjects.DeviceSubject;
            var msaSubject = deviceDeleteRequest.Subject as PcfPrivacySubjects.MsaSubject;
            Assert.IsNull(msaSubject);
            Assert.IsNotNull(deviceSubject);

            Assert.AreEqual(
                long.Parse(PrivacyRequestConverter.RemoveGlobalFormat(globalDeviceId), NumberStyles.HexNumber),
                deviceSubject.GlobalDeviceId);
        }

        private static void ValidateDeviceDeleteIdRequest(
            DeleteDeviceIdRequest deleteDeviceIdRequest,
            uint authorizingPuid,
            DateTimeOffset createTime,
            Guid requestGuid)
        {
            Assert.IsNotNull(deleteDeviceIdRequest);
            Assert.AreEqual($"p:{authorizingPuid}", deleteDeviceIdRequest.AuthorizationId);
            Assert.AreEqual("correlationvector", deleteDeviceIdRequest.CorrelationVector);
            Assert.AreEqual(requestGuid, deleteDeviceIdRequest.RequestId);
            Assert.AreEqual((long)123456, deleteDeviceIdRequest.GlobalDeviceId);
            Assert.AreEqual(createTime, deleteDeviceIdRequest.CreateTime);
            Assert.AreEqual(true, deleteDeviceIdRequest.TestSignal);
        }

        private static void ValidateSingleUser(DeleteRequest deleteRequest, int targetPuid, uint targetCid)
        {
            var msaSubject = deleteRequest.Subject as PcfPrivacySubjects.MsaSubject;
            Assert.IsNotNull(msaSubject);
            Assert.AreEqual(targetPuid, msaSubject.Puid);
            Assert.AreEqual(targetCid, msaSubject.Cid);
        }

        private static void ValidateUserDeleteRequest(
            DeleteRequest userDeleteRequest,
            Guid requestGuid,
            string correlationVector,
            string context,
            uint authorizingPuid,
            long targetPuid,
            long targetCid,
            string privacyDataType,
            DateTimeOffset start,
            DateTimeOffset end,
            string targetXuid)
        {
            ValidateBaseDeleteRequest(
                userDeleteRequest,
                requestGuid,
                correlationVector,
                context,
                authorizingPuid,
                privacyDataType,
                start,
                end);
            var msaSubject = userDeleteRequest.Subject as PcfPrivacySubjects.MsaSubject;
            Assert.IsNotNull(msaSubject);
            var deviceSubject = userDeleteRequest.Subject as PcfPrivacySubjects.DeviceSubject;
            Assert.IsNull(deviceSubject);
            Assert.AreEqual(targetPuid, msaSubject.Puid);
            Assert.AreEqual(targetCid, msaSubject.Cid);
            Assert.AreEqual(targetXuid, msaSubject.Xuid);
        }
    }
}
