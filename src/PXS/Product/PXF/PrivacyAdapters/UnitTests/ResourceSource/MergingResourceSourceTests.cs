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
    public class MergingResourceSourceTests
    {
        [TestMethod]
        public async Task ContinuationTest()
        {
            IResourceSource<string> source1 = ResourceSourceFactory<string>.Generate("8", "7", "4");
            IResourceSource<string> source2 = ResourceSourceFactory<string>.Generate("6", "5", "2");
            IResourceSource<string> source3 = ResourceSourceFactory<string>.Generate("9", "3", "1");
            var mergingSource = new MergingResourceSource<string>(
                (a, b) => int.Parse(b) - int.Parse(a),
                new Dictionary<string, IResourceSource<string>> { { "s1", source1 }, { "s2", source2 }, { "s3", source3 } });

            IList<string> results = await mergingSource.FetchAsync(3).ConfigureAwait(false);
            Assert.IsNotNull(results);
            Assert.AreEqual(3, results.Count);
            Assert.AreEqual("9", results[0]);
            Assert.AreEqual("8", results[1]);
            Assert.AreEqual("7", results[2]);

            string token = mergingSource.GetNextToken()?.Serialize();

            source1 = ResourceSourceFactory<string>.Generate("8", "7", "4");
            source2 = ResourceSourceFactory<string>.Generate("6", "5", "2");
            source3 = ResourceSourceFactory<string>.Generate("9", "3", "1");
            mergingSource = new MergingResourceSource<string>(
                (a, b) => int.Parse(b) - int.Parse(a),
                new Dictionary<string, IResourceSource<string>> { { "s1", source1 }, { "s2", source2 }, { "s3", source3 } });
            mergingSource.SetNextToken(token);

            results = await mergingSource.FetchAsync(3).ConfigureAwait(false);
            Assert.IsNotNull(results);
            Assert.AreEqual(3, results.Count);
            Assert.AreEqual("6", results[0]);
            Assert.AreEqual("5", results[1]);
            Assert.AreEqual("4", results[2]);

            token = mergingSource.GetNextToken().Serialize();

            source1 = ResourceSourceFactory<string>.Generate("8", "7", "4");
            source2 = ResourceSourceFactory<string>.Generate("6", "5", "2");
            source3 = ResourceSourceFactory<string>.Generate("9", "3", "1");
            mergingSource = new MergingResourceSource<string>(
                (a, b) => int.Parse(b) - int.Parse(a),
                new Dictionary<string, IResourceSource<string>> { { "s1", source1 }, { "s2", source2 }, { "s3", source3 } });
            mergingSource.SetNextToken(token);

            results = await mergingSource.FetchAsync(int.MaxValue).ConfigureAwait(false);
            Assert.IsNotNull(results);
            Assert.AreEqual(3, results.Count);
            Assert.AreEqual("3", results[0]);
            Assert.AreEqual("2", results[1]);
            Assert.AreEqual("1", results[2]);
        }

        [TestMethod]
        public async Task MergeEqualFetchTest()
        {
            IResourceSource<string> source1 = ResourceSourceFactory<string>.Generate("3", "2", "1");
            IResourceSource<string> source2 = ResourceSourceFactory<string>.Generate("3", "2", "1");
            var mergingSource = new MergingResourceSource<string>(
                (a, b) => int.Parse(b) - int.Parse(a),
                new Dictionary<string, IResourceSource<string>> { { "s1", source1 }, { "s2", source2 } });

            IList<string> results = await mergingSource.FetchAsync(int.MaxValue).ConfigureAwait(false);
            Assert.IsNotNull(results);
            Assert.AreEqual(6, results.Count);
            Assert.AreEqual("3", results[0]);
            Assert.AreEqual("3", results[1]);
            Assert.AreEqual("2", results[2]);
            Assert.AreEqual("2", results[3]);
            Assert.AreEqual("1", results[4]);
            Assert.AreEqual("1", results[5]);
        }

        [TestMethod]
        public async Task MergeEqualPeekTest()
        {
            IResourceSource<string> source1 = ResourceSourceFactory<string>.Generate("3", "2", "1");
            IResourceSource<string> source2 = ResourceSourceFactory<string>.Generate("3", "2", "1");
            var mergingSource = new MergingResourceSource<string>(
                (a, b) => int.Parse(b) - int.Parse(a),
                new Dictionary<string, IResourceSource<string>> { { "s1", source1 }, { "s2", source2 } });

            IList<string> results = await mergingSource.PeekAsync(int.MaxValue).ConfigureAwait(false);
            Assert.IsNotNull(results);
            Assert.AreEqual(6, results.Count);
            Assert.AreEqual("3", results[0]);
            Assert.AreEqual("3", results[1]);
            Assert.AreEqual("2", results[2]);
            Assert.AreEqual("2", results[3]);
            Assert.AreEqual("1", results[4]);
            Assert.AreEqual("1", results[5]);
        }

        [TestMethod]
        public async Task MergeNotEqualFetchTest()
        {
            IResourceSource<string> source1 = ResourceSourceFactory<string>.Generate("6", "5", "2");
            IResourceSource<string> source2 = ResourceSourceFactory<string>.Generate("4", "3", "1");
            var mergingSource = new MergingResourceSource<string>(
                (a, b) => int.Parse(b) - int.Parse(a),
                new Dictionary<string, IResourceSource<string>> { { "s1", source1 }, { "s2", source2 } });

            IList<string> results = await mergingSource.FetchAsync(int.MaxValue).ConfigureAwait(false);
            Assert.IsNotNull(results);
            Assert.AreEqual(6, results.Count);
            Assert.AreEqual("6", results[0]);
            Assert.AreEqual("5", results[1]);
            Assert.AreEqual("4", results[2]);
            Assert.AreEqual("3", results[3]);
            Assert.AreEqual("2", results[4]);
            Assert.AreEqual("1", results[5]);
        }

        [TestMethod]
        public async Task MergeNotEqualPeekTest()
        {
            IResourceSource<string> source1 = ResourceSourceFactory<string>.Generate("6", "5", "2");
            IResourceSource<string> source2 = ResourceSourceFactory<string>.Generate("4", "3", "1");
            var mergingSource = new MergingResourceSource<string>(
                (a, b) => int.Parse(b) - int.Parse(a),
                new Dictionary<string, IResourceSource<string>> { { "s1", source1 }, { "s2", source2 } });

            IList<string> results = await mergingSource.PeekAsync(int.MaxValue).ConfigureAwait(false);
            Assert.IsNotNull(results);
            Assert.AreEqual(6, results.Count);
            Assert.AreEqual("6", results[0]);
            Assert.AreEqual("5", results[1]);
            Assert.AreEqual("4", results[2]);
            Assert.AreEqual("3", results[3]);
            Assert.AreEqual("2", results[4]);
            Assert.AreEqual("1", results[5]);
        }

        [TestMethod]
        public async Task PeekConsumePeekTest()
        {
            IResourceSource<string> source1 = ResourceSourceFactory<string>.Generate("6", "5", "2");
            IResourceSource<string> source2 = ResourceSourceFactory<string>.Generate("4", "3", "1");
            var mergingSource = new MergingResourceSource<string>(
                (a, b) => int.Parse(b) - int.Parse(a),
                new Dictionary<string, IResourceSource<string>> { { "s1", source1 }, { "s2", source2 } });

            IList<string> results = await mergingSource.PeekAsync(2).ConfigureAwait(false);
            Assert.IsNotNull(results);
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual("6", results[0]);
            Assert.AreEqual("5", results[1]);

            await mergingSource.ConsumeAsync(2).ConfigureAwait(false);

            results = await mergingSource.PeekAsync(3).ConfigureAwait(false);
            Assert.IsNotNull(results);
            Assert.AreEqual(3, results.Count);
            Assert.AreEqual("4", results[0]);
            Assert.AreEqual("3", results[1]);
            Assert.AreEqual("2", results[2]);

            await mergingSource.ConsumeAsync(2).ConfigureAwait(false);

            results = await mergingSource.PeekAsync(int.MaxValue).ConfigureAwait(false);
            Assert.IsNotNull(results);
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual("2", results[0]);
            Assert.AreEqual("1", results[1]);
        }

        [TestMethod]
        public async Task TriplePeekTest()
        {
            IResourceSource<string> source1 = ResourceSourceFactory<string>.Generate("8", "7", "4");
            IResourceSource<string> source2 = ResourceSourceFactory<string>.Generate("6", "5", "2");
            IResourceSource<string> source3 = ResourceSourceFactory<string>.Generate("9", "3", "1");
            var mergingSource = new MergingResourceSource<string>(
                (a, b) => int.Parse(b) - int.Parse(a),
                new Dictionary<string, IResourceSource<string>> { { "s1", source1 }, { "s2", source2 }, { "s3", source3 } });

            IList<string> results = await mergingSource.PeekAsync(int.MaxValue).ConfigureAwait(false);
            Assert.IsNotNull(results);
            Assert.AreEqual(9, results.Count);

            Assert.AreEqual("9", results[0]);
            Assert.AreEqual("8", results[1]);
            Assert.AreEqual("7", results[2]);
            Assert.AreEqual("6", results[3]);
            Assert.AreEqual("5", results[4]);
            Assert.AreEqual("4", results[5]);
            Assert.AreEqual("3", results[6]);
            Assert.AreEqual("2", results[7]);
            Assert.AreEqual("1", results[8]);
        }
    }
}
