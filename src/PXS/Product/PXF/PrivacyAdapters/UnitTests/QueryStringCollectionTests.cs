// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyAdapters.UnitTests
{
    using System;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class QueryStringCollectionTests : TestBase
    {
        [TestMethod]
        public void AddDateFilterSingleDay()
        {
            var qsc = new QueryStringCollection();

            // No date filter specified
            // This is no longer an error
            //ExpectedException<ArgumentNullException>(() => qsc.AddDateFilter(null, null, null));

            // Missing required date parameter
            ExpectedException<ArgumentNullException>(() => qsc.AddDateFilter(DateOption.SingleDay, null, null));

            // Single day filter
            // AddDateFilter assumes that the date is Utc
            qsc.AddDateFilter(DateOption.SingleDay, new DateTime(2016, 2, 8, 0, 0, 0, DateTimeKind.Utc), null);
            Assert.AreEqual("date eq datetime'2016-02-08'", qsc[0]);
        }

        [TestMethod]
        public void AddDateFilterBetween()
        {
            var qsc = new QueryStringCollection();

            // Missing required date parameter
            ExpectedException<ArgumentNullException>(() => qsc.AddDateFilter(DateOption.Between, null, null));
            ExpectedException<ArgumentNullException>(() => qsc.AddDateFilter(DateOption.Between, new DateTime(2016, 2, 8), null));

            // Before filter
            qsc.AddDateFilter(DateOption.Between, new DateTime(2016, 2, 8, 0, 0, 0, DateTimeKind.Utc), new DateTime(2016, 2, 9, 0, 0, 0, DateTimeKind.Utc));
            Assert.AreEqual("date ge datetime'2016-02-08T00:00:00.0000000' and date le datetime'2016-02-09T00:00:00.0000000'", qsc[0]);
        }
    }
}
