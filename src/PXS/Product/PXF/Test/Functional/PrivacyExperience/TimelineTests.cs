// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.FunctionalTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Models;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using TestConfiguration = Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.Config.TestConfiguration;

    /// <summary>
    ///     Timeline Tests
    /// </summary>
    [TestClass]
    public class TimelineTests : TestBase
    {
        // Deleting one datatype should not be any different than any other data type, so will just do voice for now.
        private static readonly string[] deleteTypes =
        {
            Policies.Current.DataTypes.Ids.InkingTypingAndSpeechUtterance.Value
        };

        // TODO: Should try various combinations of these, without taking too much time or too many calls trying every single combination
        // Combinations will grow over time. For now, just *all* of them should be good enough.
        private static readonly string[] getCardTypes =
        {
            TimelineCard.CardTypes.VoiceCard,
            TimelineCard.CardTypes.AppUsageCard,
            TimelineCard.CardTypes.SearchCard,

            //TimelineCard.CardTypes.BrowseCard, TODO: Browse still needs work
            TimelineCard.CardTypes.BookConsumptionCard,
            TimelineCard.CardTypes.EpisodeConsumptionCard,
            TimelineCard.CardTypes.SongConsumptionCard,
            TimelineCard.CardTypes.SurroundVideoConsumptionCard,
            TimelineCard.CardTypes.VideoConsumptionCard
        };

        [TestMethod, TestCategory("FCT")]
        public async Task DeleteTimelineByType_UnauthorizedAuthPolicy()
        {
            // This auth policy is unauthorized
            var authPolicy = "MBI_SSL";
            TestUser testUser = TestUsers.TimelineDeleteByType;
            try
            {
                await TestDeleteTimelineByType(testUser, authPolicy).ConfigureAwait(false);
                Assert.Fail("Should have thrown auth exception");
            }
            catch (PrivacyExperienceTransportException transportException)
            {
                Assert.AreEqual(HttpStatusCode.Unauthorized, transportException.HttpStatusCode);
            }
        }

        [TestMethod, TestCategory("FCT")]
        public async Task DeleteTimelineByTypeSuccess()
        {
            // This auth policy is required
            var authPolicy = "MBI_SSL_SA";
            TestUser testUser = TestUsers.TimelineDeleteByType;
            await TestDeleteTimelineByType(testUser, authPolicy).ConfigureAwait(false);
        }

        [DataTestMethod, TestCategory("FCT")]
        [DataRow(1)]
        [DataRow(10)]
        [DataRow(25)]
        public async Task DeleteTimelineByIdSuccess(int count)
        {
            //// the ids are an opaque blob that are intended to be fetched and passed back, so it's easiest to just fetch timeline items and 
            ////  pass back the generated ids

            TestUser testUser = TestUsers.TimelineDeleteById;
            string userProxyTicket = await MockTestHooks.GetProxyTicketAsync(testUser).ConfigureAwait(false);
            var requestArgs = new GetTimelineArgs(
                userProxyTicket,
                new[] { TimelineCard.CardTypes.AppUsageCard },
                count,
                null,
                null,
                null,
                TimeSpan.Zero,
                DateTimeOffset.UtcNow)
            {
                CorrelationVector = new CorrelationVector().ToString(),
                RequestId = Guid.NewGuid().ToString()
            };

            PagedResponse<TimelineCard> response = await S2SClient.GetTimelineAsync(requestArgs).ConfigureAwait(false);

            Assert.IsNotNull(response);
            Assert.IsNotNull(response.Items);
            Assert.IsTrue(response.Items.Any(), "Response should contain items. ClientRequestId: {0}", requestArgs.RequestId);

            var deleteRequestArgs = new DeleteTimelineByIdsArgs(userProxyTicket, response.Items.Select(o => o.Id).ToList())
            {
                CorrelationVector = new CorrelationVector().ToString(),
                RequestId = Guid.NewGuid().ToString()
            };

            await S2SClient.DeleteTimelineAsync(deleteRequestArgs).ConfigureAwait(false);

            // Post-condition: Delete the user from the mock to allow data to be recreated.
            HttpResponseMessage mockResponse =
                await MockTestHooks.PostTestHookAsync(TestConfiguration.MockBaseUrl.Value, testUser.Puid, "appusage", "delete").ConfigureAwait(false);
            Assert.AreEqual(HttpStatusCode.OK, mockResponse.StatusCode);
        }

        [DataTestMethod, TestCategory("FCT")]
        [ExpectedException(typeof(PrivacyExperienceTransportException))]
        [DataRow(1)]
        public async Task DeleteTimelineByIdUsingHttpMethodHead(int count)
        {
            TestUser testUser = TestUsers.TimelineDeleteById;
            string userProxyTicket = await MockTestHooks.GetProxyTicketAsync(testUser).ConfigureAwait(false);
            var requestArgs = new GetTimelineArgs(
                userProxyTicket,
                new[] { TimelineCard.CardTypes.AppUsageCard },
                count,
                null,
                null,
                null,
                TimeSpan.Zero,
                DateTimeOffset.UtcNow)
            {
                CorrelationVector = new CorrelationVector().ToString(),
                RequestId = Guid.NewGuid().ToString()
            };

            await S2SClient.GetTimelineAsync(requestArgs, HttpMethod.Head).ConfigureAwait(false);
        }

        [ExpectedException(typeof(PrivacyExperienceTransportException))]
        [TestMethod, TestCategory("FCT")]
        public async Task GetTimelineMissingCorrelationVectorReturnsBadRequest()
        {
            var expectedError = new Error(ErrorCode.InvalidInput, "Request header did not contain a CV in the header: " + CorrelationVector.HeaderName);

            try
            {
                string userProxyTicket = await MockTestHooks.GetProxyTicketAsync(TestUsers.ViewVoice0).ConfigureAwait(false);
                var requestArgs = new GetTimelineArgs(
                        userProxyTicket,
                        getCardTypes,
                        100,
                        null,
                        null,
                        null,
                        TimeSpan.Zero,
                        DateTimeOffset.UtcNow)
                { RequestId = Guid.NewGuid().ToString() };

                PagedResponse<TimelineCard> response = await S2SClient.GetTimelineAsync(requestArgs).ConfigureAwait(false);
                Assert.IsNotNull(response);
                Assert.Fail("An exception should have been thrown. ClientRequestId: {0}", requestArgs.RequestId);
            }
            catch (PrivacyExperienceTransportException e)
            {
                Assert.AreEqual(HttpStatusCode.BadRequest, e.HttpStatusCode);
                EqualityHelper.AreEqual(expectedError, e.Error);
                throw;
            }
        }

        [ExpectedException(typeof(PrivacyExperienceTransportException))]
        [TestMethod, TestCategory("FCT")]
        public async Task GetTimelineInvalidCardTypeThrowsException()
        {
            var invalidType = "invalidType";
            var invalidCardType = new string[] { invalidType};

            try
            {
                string userProxyTicket = await MockTestHooks.GetProxyTicketAsync(TestUsers.ViewVoice0).ConfigureAwait(false);
                var requestArgs = new GetTimelineArgs(
                        userProxyTicket,
                        invalidCardType,
                        100,
                        null,
                        null,
                        null,
                        TimeSpan.Zero,
                        DateTimeOffset.UtcNow)
                {
                    CorrelationVector = new CorrelationVector().ToString(),
                    RequestId = Guid.NewGuid().ToString()
                };

                PagedResponse<TimelineCard> response = await S2SClient.GetTimelineAsync(requestArgs).ConfigureAwait(false);
                Assert.IsNotNull(response);
                Assert.Fail("An exception should have been thrown. CardTypes: {0}", requestArgs.CardTypes);
            }
            catch (PrivacyExperienceTransportException e)
            {
                Console.WriteLine(e.ToString());
                Assert.AreEqual(HttpStatusCode.InternalServerError, e.HttpStatusCode);
                Assert.AreEqual("Unknown", e.Error.Code);
                Assert.IsTrue(e.Error.Message.Contains($"Cannot map CardType {invalidType} to ResourceType"));
                throw;
            }
            catch (Exception e) 
            {
                Console.WriteLine(e.Message);
                throw;
            }
        }

        [TestMethod, TestCategory("FCT")]
        public async Task GetTimelineNextPageSuccess()
        {
            string userProxyTicket = await MockTestHooks.GetProxyTicketAsync(TestUsers.ViewVoice0).ConfigureAwait(false);
            var requestArgs = new GetTimelineArgs(userProxyTicket, getCardTypes, 1, null, null, null, TimeSpan.Zero, DateTimeOffset.UtcNow)
            {
                CorrelationVector = new CorrelationVector().ToString(),
                RequestId = Guid.NewGuid().ToString()
            };

            PagedResponse<TimelineCard> response = await S2SClient.GetTimelineAsync(requestArgs).ConfigureAwait(false);

            Assert.IsNotNull(response);
            Assert.IsNotNull(response.Items);
            Assert.IsTrue(response.Items.Any(), "Response should contain items. ClientRequestId: {0}", requestArgs.RequestId);
            Assert.IsNotNull(response.NextLink);

            var nextPageArgs = new PrivacyExperienceClientBaseArgs(userProxyTicket)
            {
                CorrelationVector = new CorrelationVector().ToString(),
                RequestId = Guid.NewGuid().ToString()
            };
            response = await S2SClient.GetTimelineNextPageAsync(nextPageArgs, response.NextLink).ConfigureAwait(false);

            Assert.IsNotNull(response);
            Assert.IsNotNull(response.Items);
            Assert.IsTrue(response.Items.Any(), "Response should contain items. ClientRequestId: {0}", nextPageArgs.RequestId);
        }

        [TestMethod, TestCategory("FCT")]
        [ExpectedException(typeof(PrivacyExperienceTransportException))]
        public async Task GetTimelineNextPageUsingHttpMethodHead()
        {
            string userProxyTicket = await MockTestHooks.GetProxyTicketAsync(TestUsers.ViewVoice0).ConfigureAwait(false);
            var requestArgs = new GetTimelineArgs(userProxyTicket, getCardTypes, 1, null, null, null, TimeSpan.Zero, DateTimeOffset.UtcNow)
            {
                CorrelationVector = new CorrelationVector().ToString(),
                RequestId = Guid.NewGuid().ToString()
            };

            await S2SClient.GetTimelineAsync(requestArgs, HttpMethod.Head).ConfigureAwait(false);
        }

        [TestMethod, TestCategory("FCT")]
        public async Task GetTimelineResultsSortedDateTime()
        {
            string userProxyTicket = await MockTestHooks.GetProxyTicketAsync(TestUsers.ViewVoice0).ConfigureAwait(false);
            var requestArgs = new GetTimelineArgs(userProxyTicket, getCardTypes, null, null, null, null, TimeSpan.Zero, DateTimeOffset.UtcNow)
            {
                CorrelationVector = new CorrelationVector().ToString(),
                RequestId = Guid.NewGuid().ToString()
            };

            PagedResponse<TimelineCard> response = await S2SClient.GetTimelineAsync(requestArgs).ConfigureAwait(false);

            Assert.IsNotNull(response);
            Assert.IsNotNull(response.Items);
            Assert.IsTrue(response.Items.Any(), "Response should contain items. ClientRequestId: {0}", requestArgs.RequestId);

            List<TimelineCard> itemsList = response.Items.ToList();

            // Validate the results are sorted correctly.
            for (int i = 0; i < itemsList.Count - 1; i++)
            {
                Assert.IsTrue(itemsList[i].Timestamp.UtcDateTime >= itemsList[i + 1].Timestamp.UtcDateTime);
            }
        }

        [TestMethod, TestCategory("FCT")]
        [ExpectedException(typeof(PrivacyExperienceTransportException))]
        public async Task GetTimelineResultsSortedDateTimeUsingHttpMethodHead()
        {
            string userProxyTicket = await MockTestHooks.GetProxyTicketAsync(TestUsers.ViewVoice0).ConfigureAwait(false);
            var requestArgs = new GetTimelineArgs(userProxyTicket, getCardTypes, null, null, null, null, TimeSpan.Zero, DateTimeOffset.UtcNow)
            {
                CorrelationVector = new CorrelationVector().ToString(),
                RequestId = Guid.NewGuid().ToString()
            };

            await S2SClient.GetTimelineAsync(requestArgs, HttpMethod.Head).ConfigureAwait(false);
        }

        [Ignore]
        [TestMethod, TestCategory("FCT")]
        public async Task GetTimelineResultsSortedDateTimeViaAadAuth()
        {
            string userProxyTicket = await MockTestHooks.GetProxyTicketAsync(TestUsers.AadAuthTestUser).ConfigureAwait(false);

            GetTimelineArgs requestArgs =
                new GetTimelineArgs(
                    userProxyTicket,
                    new[] { TimelineCard.CardTypes.AppUsageCard },
                    null,
                    null,
                    null,
                    null,
                    TimeSpan.Zero,
                    DateTimeOffset.UtcNow)
                {
                    CorrelationVector = new CorrelationVector().ToString(),
                    RequestId = Guid.NewGuid().ToString()
                };

            PagedResponse<TimelineCard> response = await S2SClient.GetTimelineAsync(requestArgs).ConfigureAwait(false);

            Assert.IsNotNull(response);
            Assert.IsNotNull(response.Items);
            Assert.IsTrue(response.Items.Any(), "Response should contain items. ClientRequestId: {0}", requestArgs.RequestId);

            List<TimelineCard> itemsList = response.Items.ToList();

            // Validate the results are sorted correctly.
            for (int i = 0; i < itemsList.Count - 1; i++)
            {
                Assert.IsTrue(itemsList[i].Timestamp.UtcDateTime >= itemsList[i + 1].Timestamp.UtcDateTime);
            }
        }

        [TestMethod, TestCategory("FCT")]
        public async Task GetTimelineSuccess()
        {
            string userProxyTicket = await MockTestHooks.GetProxyTicketAsync(TestUsers.ViewVoice0).ConfigureAwait(false);
            var requestArgs = new GetTimelineArgs(
                userProxyTicket,
                getCardTypes,
                100,
                null,
                null,
                null,
                TimeSpan.Zero,
                DateTimeOffset.UtcNow)
            {
                CorrelationVector = new CorrelationVector().ToString(),
                RequestId = Guid.NewGuid().ToString()
            };

            PagedResponse<TimelineCard> response = await S2SClient.GetTimelineAsync(requestArgs).ConfigureAwait(false);

            Assert.IsNotNull(response);
            Assert.IsNotNull(response.Items);
            Assert.IsTrue(response.Items.Any(), "Response should contain items. ClientRequestId: {0}", requestArgs.RequestId);
        }

        private static async Task TestDeleteTimelineByType(TestUser testUser, string authPolicy)
        {
            string userProxyTicket = await MockTestHooks.GetProxyTicketAsync(testUser, authPolicy: authPolicy).ConfigureAwait(false);
            var requestArgs = new DeleteTimelineByTypesArgs(userProxyTicket, TimeSpan.FromDays(365 * 20), deleteTypes)
            {
                CorrelationVector = new CorrelationVector().ToString(),
                RequestId = Guid.NewGuid().ToString()
            };

            await S2SClient.DeleteTimelineAsync(requestArgs).ConfigureAwait(false);

            // Post-condition: Delete the user from the mock to allow data to be recreated.
            HttpResponseMessage mockResponse =
                await MockTestHooks.PostTestHookAsync(TestConfiguration.MockBaseUrl.Value, testUser.Puid, "appusage", "delete").ConfigureAwait(false);
            Assert.AreEqual(HttpStatusCode.OK, mockResponse.StatusCode);
        }
    }
}
