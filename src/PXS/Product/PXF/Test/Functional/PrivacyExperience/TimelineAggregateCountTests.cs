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
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    
    using TestConfiguration = Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.Config.TestConfiguration;

    /// <summary>
    ///     Timeline aggregate count tests
    /// </summary>
    [TestClass]
    public class TimelineAggregateCountTests : TestBase
    {
        [TestMethod, TestCategory("FCT")]
        [DataRow(TimelineCard.CardTypes.AppUsageCard)]
        [DataRow(TimelineCard.CardTypes.BrowseCard)]
        [DataRow(TimelineCard.CardTypes.SearchCard)]
        public async Task GetTimelineAggregateCountSuccess(string cardTypes)
        {
            string[] timelineCardTypes = { cardTypes };

            // This user account is not important, so we don't check it in the Mock.
            string userProxyTicket = await MockTestHooks.GetProxyTicketAsync(TestUsers.ViewVoice0).ConfigureAwait(false);
            var requestArgs = new GetTimelineAggregateCountArgs(
                userProxyTicket,
                timelineCardTypes)
            {
                CorrelationVector = new CorrelationVector().ToString(),
                RequestId = Guid.NewGuid().ToString()
            };

            var response = await S2SClient.GetAggregateCountAsync(requestArgs).ConfigureAwait(false);

            Assert.IsNotNull(response);
            Assert.IsNotNull(response.AggregateCounts);
            Assert.AreEqual(response.AggregateCounts.Keys.First(), cardTypes);
        }

        [TestMethod, TestCategory("FCT")]
        [ExpectedException(typeof(PrivacyExperienceTransportException))]
        [DataRow(TimelineCard.CardTypes.BookConsumptionCard)]
        public async Task GetTimelineAggregateCountFailedForUnsupportedCardTypes(string cardTypes)
        {
            string[] timelineCardTypes = { cardTypes };

            string userProxyTicket = await MockTestHooks.GetProxyTicketAsync(TestUsers.ViewVoice0).ConfigureAwait(false);
            var requestArgs = new GetTimelineAggregateCountArgs(
                userProxyTicket,
                timelineCardTypes)
            {
                CorrelationVector = new CorrelationVector().ToString(),
                RequestId = Guid.NewGuid().ToString()
            };

            await S2SClient.GetAggregateCountAsync(requestArgs).ConfigureAwait(false);            
        }
    }
}
