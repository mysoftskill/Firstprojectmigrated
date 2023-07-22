// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.UnitTests
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.PrivacyAdapters.Dispatcher;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class FilterResourceSourceTests
    {
        [TestMethod]
        public async Task FilterContinuationTest()
        {
            IResourceSource<string> source = ResourceSourceFactory<string>.Generate("1", "2", "3", "4");
            var filterSource = new FilterResourceSource<string>(
                source,
                s => s == "2");

            IList<string> results = await filterSource.FetchAsync(2).ConfigureAwait(false);
            Assert.IsNotNull(results);
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual("1", results[0]);
            Assert.AreEqual("3", results[1]);

            string token = filterSource.GetNextToken()?.Serialize();

            source = ResourceSourceFactory<string>.Generate("1", "2", "3", "4");
            filterSource = new FilterResourceSource<string>(
                source,
                s => s == "2");
            filterSource.SetNextToken(token);

            results = await filterSource.FetchAsync(int.MaxValue).ConfigureAwait(false);
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("4", results[0]);
        }

        [TestMethod]
        public async Task FilterFetchTest()
        {
            IResourceSource<string> source = ResourceSourceFactory<string>.Generate("1", "2", "3", "4");
            var FilterSource = new FilterResourceSource<string>(
                source,
                s => s == "2");

            IList<string> results = await FilterSource.FetchAsync(int.MaxValue).ConfigureAwait(false);
            Assert.IsNotNull(results);
            Assert.AreEqual(3, results.Count);
            Assert.AreEqual("1", results[0]);
            Assert.AreEqual("3", results[1]);
            Assert.AreEqual("4", results[2]);
        }

        [TestMethod]
        public async Task FilterPeekTest()
        {
            IResourceSource<string> source = ResourceSourceFactory<string>.Generate("1", "2", "3", "4");
            var FilterSource = new FilterResourceSource<string>(
                source,
                s => s == "2");

            IList<string> results = await FilterSource.PeekAsync(int.MaxValue).ConfigureAwait(false);
            Assert.IsNotNull(results);
            Assert.AreEqual(3, results.Count);
            Assert.AreEqual("1", results[0]);
            Assert.AreEqual("3", results[1]);
            Assert.AreEqual("4", results[2]);
        }
    }
}
