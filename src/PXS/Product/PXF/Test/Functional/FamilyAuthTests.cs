// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.FunctionalTests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Models;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class FamilyAuthTests : TestBase
    {
        [TestMethod]
        [TestCategory("FCT")]
        public async Task GetTimelineOnBehalfSuccess()
        {
            // Make call to family roster to get JWT for a child account
            TestUser testUser = TestUsers.Parent1;
            string userProxyTicket = await MockTestHooks.GetProxyTicketAsync(testUser).ConfigureAwait(false);
            FamilyModel familyModel = await FamilyModel.GetFamilyAsync(testUser.Puid, userProxyTicket, Test.Common.Config.TestConfiguration.S2SCert.Value).ConfigureAwait(false);
            FamilyModel.FamilyMemberModel child = familyModel.Members.First(m => m.Id == TestUsers.Child1.Puid.ToString());

            var requestArgs =
                new GetTimelineArgs(userProxyTicket, new[] { TimelineCard.CardTypes.AppUsageCard }, 1, null, null, null, TimeSpan.Zero, DateTimeOffset.UtcNow)
                {
                    CorrelationVector = new CorrelationVector().ToString(),
                    FamilyTicket = child.JsonWebToken
                };
            PagedResponse<TimelineCard> parentResponse = await S2SClient.GetTimelineAsync(requestArgs).ConfigureAwait(false);

            // NOTE: This test will fail once the child account is > 12 years old. Whenever that happens log into the child account in amc-int and change the birthday
            // Int_Accounts.txt line 21 and 22.
            //       And, the test is set to private so won't execute during checkin.
            Assert.IsNotNull(parentResponse);
            Assert.IsNotNull(parentResponse.Items);
            Assert.IsTrue(parentResponse.Items.Any(), "Response should contain items.");

            // Redo request with child creds and see that we get the same response
            userProxyTicket = await MockTestHooks.GetProxyTicketAsync(TestUsers.Child1).ConfigureAwait(false);
            requestArgs = new GetTimelineArgs(userProxyTicket, new[] { TimelineCard.CardTypes.AppUsageCard }, 1, null, null, null, TimeSpan.Zero, DateTimeOffset.UtcNow)
            {
                CorrelationVector = new CorrelationVector().ToString()
            };

            PagedResponse<TimelineCard> childResponse = await S2SClient.GetTimelineAsync(requestArgs).ConfigureAwait(false);

            Assert.IsNotNull(childResponse);
            Assert.IsNotNull(childResponse.Items);
            Assert.IsTrue(childResponse.Items.Any(), "Response should contain items.");
        }

        [TestMethod]
        [TestCategory("FCT")]
        public async Task GetTimelineSelfAuthChildSuccess()
        {
            // this user is a child, but not in a family
            var testUser = TestUsers.Child2;
            var userProxyTicket = await MockTestHooks.GetProxyTicketAsync(testUser).ConfigureAwait(false);

            var requestArgs = new GetTimelineArgs(userProxyTicket, new[] { TimelineCard.CardTypes.AppUsageCard }, 1, null, null, null, TimeSpan.Zero, DateTimeOffset.UtcNow);
            requestArgs.CorrelationVector = new CorrelationVector().ToString();
            var childResponse = await S2SClient.GetTimelineAsync(requestArgs).ConfigureAwait(false);

            Assert.IsNotNull(childResponse);
            Assert.IsNotNull(childResponse.Items);
            Assert.IsTrue(childResponse.Items.Any(), "Response should contain items.");
        }

        [TestMethod]
        [TestCategory("FCT")]
        public async Task GetTimelineWithGarbageJWT()
        {
            // Make call to family roster, but use the JWT for the parents own account
            TestUser testUser = TestUsers.Parent1;
            string userProxyTicket = await MockTestHooks.GetProxyTicketAsync(testUser).ConfigureAwait(false);

            var requestArgs =
                new GetTimelineArgs(userProxyTicket, new[] { TimelineCard.CardTypes.AppUsageCard }, 1, null, null, null, TimeSpan.Zero, DateTimeOffset.UtcNow)
                {
                    CorrelationVector = new CorrelationVector().ToString(),
                    FamilyTicket = "ThisIsNotAValidJwtToken"
                };

            try
            {
                PagedResponse<TimelineCard> parentResponse = await S2SClient.GetTimelineAsync(requestArgs).ConfigureAwait(false);
                Assert.Fail("Expected a transport exception");
            }
            catch (PrivacyExperienceTransportException pete)
            {
                Assert.AreEqual("InvalidClientCredentials", pete.Error.Code);
            }
        }

        [TestMethod]
        [TestCategory("FCT")]
        public async Task GetTimelineWithParentsJWT()
        {
            // Make call to family roster, but use the JWT for the parents own account
            TestUser testUser = TestUsers.Parent1;
            string userProxyTicket = await MockTestHooks.GetProxyTicketAsync(testUser).ConfigureAwait(false);
            FamilyModel familyModel = await FamilyModel.GetFamilyAsync(testUser.Puid, userProxyTicket, Test.Common.Config.TestConfiguration.S2SCert.Value).ConfigureAwait(false);
            FamilyModel.FamilyMemberModel userModel = familyModel.Members.First(m => m.Id == testUser.Puid.ToString());

            var requestArgs =
                new GetTimelineArgs(userProxyTicket, new[] { TimelineCard.CardTypes.AppUsageCard }, 1, null, null, null, TimeSpan.Zero, DateTimeOffset.UtcNow)
                {
                    CorrelationVector = new CorrelationVector().ToString(),
                    FamilyTicket = userModel.JsonWebToken
                };

            try
            {
                PagedResponse<TimelineCard> parentResponse = await S2SClient.GetTimelineAsync(requestArgs).ConfigureAwait(false);
                Assert.Fail("Expected a transport exception");
            }
            catch (PrivacyExperienceTransportException pete)
            {
                Assert.AreEqual("InvalidClientCredentials", pete.Error.Code);
            }
        }
    }
}
