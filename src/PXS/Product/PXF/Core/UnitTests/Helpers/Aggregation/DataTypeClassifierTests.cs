// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.Helpers.Aggregation
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Aggregation;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DataTypeClassifierTests
    {
        /// <summary>
        ///     Check if data types are segregated according to Policy
        /// </summary>
        [TestMethod]
        public void TestDataTypeClassifier()
        {
            IList<string> dataTypesList = new List<string> { "BrowsingHistory", "InterestsAndFavorites" };
            DataTypesClassifier segregatedDataTypes1 = new DataTypesClassifier(Policies.Current);

            segregatedDataTypes1.Classify(dataTypesList, out IList<string> dataTypesForTimeline, out IList<string> dataTypesForPcf);
            Assert.AreEqual(1, dataTypesForTimeline.Count);
            CollectionAssert.Contains(dataTypesForTimeline.ToList(), "BrowsingHistory");
            CollectionAssert.Contains(dataTypesForPcf.ToList(),"InterestsAndFavorites");

            dataTypesList = new List<string>
            {
                "BrowsingHistory",
                "FitnessAndActivity",
                "InterestsAndFavorites",
                "ProductAndServiceUsage",
                "ProductAndServicePerformance",
                "InkingTypingAndSpeechUtterance",
                "SearchRequestsAndQuery",
                "PreciseUserLocation",
                "ContentConsumption"
            };

            DataTypesClassifier segregatedDataTypes2 = new DataTypesClassifier(Policies.Current);
            segregatedDataTypes2.Classify(dataTypesList, out IList<string> dataTypesForTimeline2, out IList<string> dataTypesForPcf2);
            List<string> list1 = dataTypesForTimeline2.ToList();
            List<string> list2 = dataTypesForPcf2.ToList();
            Assert.AreEqual(6, dataTypesForTimeline2.Count);
            CollectionAssert.Contains(list1, "BrowsingHistory");
            CollectionAssert.Contains(list1, "ProductAndServiceUsage");
            Assert.AreEqual(3, dataTypesForPcf2.Count);
            CollectionAssert.Contains(list2,"FitnessAndActivity");
            CollectionAssert.Contains(list2, "InterestsAndFavorites");
            CollectionAssert.Contains(list2, "ProductAndServicePerformance");

            dataTypesList = new List<string> { "InterestsAndFavorites" };
            DataTypesClassifier segregatedDataTypes3 = new DataTypesClassifier(Policies.Current);
            segregatedDataTypes3.Classify(dataTypesList, out IList<string> dataTypesForTimeline3, out IList<string> dataTypesForPcf3);
            Assert.AreEqual(0, dataTypesForTimeline3.Count);
            Assert.AreEqual(1, dataTypesForPcf3.Count);
        }
    }
}
