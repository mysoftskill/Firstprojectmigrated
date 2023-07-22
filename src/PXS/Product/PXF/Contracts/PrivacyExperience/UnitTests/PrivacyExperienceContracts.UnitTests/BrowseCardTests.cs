// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperienceContracts.UnitTests
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class BrowseCardTests
    {
        [TestMethod]
        public void MaxAggregation()
        {
            BrowseCard card = this.CreateTestCard();
            for (int i = 0; i < 59; i++)
                Assert.IsTrue(TimelineCard.Aggregate(TimeSpan.Zero, card, this.CreateTestCard(i + 1)));
            Assert.IsFalse(TimelineCard.Aggregate(TimeSpan.Zero, card, this.CreateTestCard(100)));
        }

        private BrowseCard CreateTestCard(int offset = 0)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow.Date.AddTicks(offset);
            return new BrowseCard(
                "domain",
                new List<BrowseCard.Navigation> { new BrowseCard.Navigation("urlHash", new List<DateTimeOffset> { now }, "title", new Uri("https://uri")) },
                now,
                new List<string> { "deviceId1", "deviceId2" },
                new List<string> { "source1", "source2" });
        }
    }
}
