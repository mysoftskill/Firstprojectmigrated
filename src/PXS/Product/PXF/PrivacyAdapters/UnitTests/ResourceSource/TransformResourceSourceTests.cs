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
    public class TransformResourceSourceTests
    {
        [TestMethod]
        public async Task TransformContinuationTest()
        {
            IResourceSource<string> source = ResourceSourceFactory<string>.Generate("1", "2", "3");
            var transformSource = new TransformResourceSource<string, string>(
                source,
                s => s == null ? null : (int.Parse(s) * 2).ToString());

            IList<string> results = await transformSource.FetchAsync(2).ConfigureAwait(false);
            Assert.IsNotNull(results);
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual("2", results[0]);
            Assert.AreEqual("4", results[1]);

            string token = transformSource.GetNextToken()?.Serialize();

            source = ResourceSourceFactory<string>.Generate("1", "2", "3");
            transformSource = new TransformResourceSource<string, string>(
                source,
                s => s == null ? null : (int.Parse(s) * 2).ToString());
            transformSource.SetNextToken(token);

            results = await transformSource.FetchAsync(int.MaxValue).ConfigureAwait(false);
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("6", results[0]);
        }

        [TestMethod]
        public async Task TransformFetchTest()
        {
            IResourceSource<string> source = ResourceSourceFactory<string>.Generate("1", "2", "3");
            var transformSource = new TransformResourceSource<string, string>(
                source,
                s => s == null ? null : (int.Parse(s) * 2).ToString());

            IList<string> results = await transformSource.FetchAsync(int.MaxValue).ConfigureAwait(false);
            Assert.IsNotNull(results);
            Assert.AreEqual(3, results.Count);
            Assert.AreEqual("2", results[0]);
            Assert.AreEqual("4", results[1]);
            Assert.AreEqual("6", results[2]);
        }

        [TestMethod]
        public async Task TransformPeekTest()
        {
            IResourceSource<string> source = ResourceSourceFactory<string>.Generate("1", "2", "3");
            var transformSource = new TransformResourceSource<string, string>(
                source,
                s => s == null ? null : (int.Parse(s) * 2).ToString());

            IList<string> results = await transformSource.PeekAsync(int.MaxValue).ConfigureAwait(false);
            Assert.IsNotNull(results);
            Assert.AreEqual(3, results.Count);
            Assert.AreEqual("2", results[0]);
            Assert.AreEqual("4", results[1]);
            Assert.AreEqual("6", results[2]);
        }
    }
}
