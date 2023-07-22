// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.Converters
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Membership.MemberServices.Privacy.Core.Converters;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// NextLinkConverter V1 Test
    /// </summary>
    [TestClass]
    public class NextLinkConverterV1Test
    {
        public static readonly Uri Endpoint = new Uri("https://fakeendpoint.com/");
        public const string RouteNameSearchHistoryV1 = "v1/searchhistory";

        [TestMethod]
        public void CreateNextLinkSuccess()
        {
            var expected = new Uri(Endpoint + RouteNameSearchHistoryV1 + "?count=100&skip=300");
            var actual = NextLinkConverterV1.CreateNextLink(Endpoint, RouteNameSearchHistoryV1, 100, 200, string.Empty, string.Empty);
            Assert.AreEqual(expected, actual);

            expected = new Uri(Endpoint + RouteNameSearchHistoryV1 + "?count=100&skip=500&orderBy=DateTime&filter=date+eq+datetime%272016-04-01%27");
            actual = NextLinkConverterV1.CreateNextLink(Endpoint, RouteNameSearchHistoryV1, 100, 400, "DateTime", "date eq datetime'2016-04-01'");
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DoesNextPageExist()
        {
            Assert.IsTrue(NextLinkConverterV1.DoesNextPageExist(6, CreateBrowseHistory(5)));
            Assert.IsFalse(NextLinkConverterV1.DoesNextPageExist(5, CreateBrowseHistory(5)));
            Assert.IsFalse(NextLinkConverterV1.DoesNextPageExist(4, CreateBrowseHistory(5)));
        }

        private static IList<BrowseHistoryV1> CreateBrowseHistory(int numberItems)
        {
            List<BrowseHistoryV1> browseHistory = new List<BrowseHistoryV1>();
            for (int i = 0; i < numberItems; i++)
            {
                browseHistory.Add(new BrowseHistoryV1());
            }
            return browseHistory;
        }
    }
}