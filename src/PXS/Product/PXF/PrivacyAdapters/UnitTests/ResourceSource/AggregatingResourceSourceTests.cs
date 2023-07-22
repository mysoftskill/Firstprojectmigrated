// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.PrivacyAdapters.Dispatcher;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class AggregatingResourceSourceTests
    {
        [TestMethod]
        public async Task CompleteAggregationTest()
        {
            IResourceSource<Item> source = ResourceSourceFactory<Item>.Generate(new Item("1"), new Item("1"), new Item("1"));
            var aggregatingSource = new AggregatingResourceSource<Item>(source, this.Aggregate);

            IList<Item> results = await aggregatingSource.FetchAsync(int.MaxValue).ConfigureAwait(false);
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("1", results[0].Data);
            Assert.AreEqual(3, results[0].AggregatedCount);
        }

        [TestMethod]
        public async Task ContinuationTest()
        {
            IResourceSource<Item> source = ResourceSourceFactory<Item>.Generate(new Item("3"), new Item("3"), new Item("2"), new Item("3"), new Item("2"), new Item("1"));
            var aggregatingSource = new AggregatingResourceSource<Item>(source, this.Aggregate);

            IList<Item> results = await aggregatingSource.FetchAsync(2).ConfigureAwait(false);
            Assert.IsNotNull(results);
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual("3", results[0].Data);
            Assert.AreEqual(2, results[0].AggregatedCount);
            Assert.AreEqual("2", results[1].Data);
            Assert.AreEqual(1, results[1].AggregatedCount);

            string token = aggregatingSource.GetNextToken()?.Serialize();
            source = ResourceSourceFactory<Item>.Generate(new Item("3"), new Item("3"), new Item("2"), new Item("3"), new Item("2"), new Item("1"));
            aggregatingSource = new AggregatingResourceSource<Item>(source, this.Aggregate);
            aggregatingSource.SetNextToken(token);

            results = await aggregatingSource.FetchAsync(int.MaxValue).ConfigureAwait(false);
            Assert.IsNotNull(results);
            Assert.AreEqual(3, results.Count);
            Assert.AreEqual("3", results[0].Data);
            Assert.AreEqual(1, results[0].AggregatedCount);
            Assert.AreEqual("2", results[1].Data);
            Assert.AreEqual(1, results[1].AggregatedCount);
            Assert.AreEqual("1", results[2].Data);
            Assert.AreEqual(1, results[2].AggregatedCount);
        }

        [TestMethod]
        public async Task FirstAggregationTest()
        {
            IResourceSource<Item> source = ResourceSourceFactory<Item>.Generate(new Item("2"), new Item("2"), new Item("1"));
            var aggregatingSource = new AggregatingResourceSource<Item>(source, this.Aggregate);

            IList<Item> results = await aggregatingSource.FetchAsync(int.MaxValue).ConfigureAwait(false);
            Assert.IsNotNull(results);
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual("2", results[0].Data);
            Assert.AreEqual(2, results[0].AggregatedCount);
            Assert.AreEqual("1", results[1].Data);
            Assert.AreEqual(1, results[1].AggregatedCount);
        }

        [TestMethod]
        public async Task LastAggregationTest()
        {
            IResourceSource<Item> source = ResourceSourceFactory<Item>.Generate(new Item("2"), new Item("1"), new Item("1"));
            var aggregatingSource = new AggregatingResourceSource<Item>(source, this.Aggregate);

            IList<Item> results = await aggregatingSource.FetchAsync(int.MaxValue).ConfigureAwait(false);
            Assert.IsNotNull(results);
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual("2", results[0].Data);
            Assert.AreEqual(1, results[0].AggregatedCount);
            Assert.AreEqual("1", results[1].Data);
            Assert.AreEqual(2, results[1].AggregatedCount);
        }

        [TestMethod]
        public async Task NoAggregationTest()
        {
            IResourceSource<Item> source = ResourceSourceFactory<Item>.Generate(new Item("3"), new Item("2"), new Item("1"));
            var aggregatingSource = new AggregatingResourceSource<Item>(source, this.Aggregate);

            IList<Item> results = await aggregatingSource.FetchAsync(int.MaxValue).ConfigureAwait(false);
            Assert.IsNotNull(results);
            Assert.AreEqual(3, results.Count);
            Assert.AreEqual("3", results[0].Data);
            Assert.AreEqual(1, results[0].AggregatedCount);
            Assert.AreEqual("2", results[1].Data);
            Assert.AreEqual(1, results[1].AggregatedCount);
            Assert.AreEqual("1", results[2].Data);
            Assert.AreEqual(1, results[2].AggregatedCount);
        }

        [TestMethod]
        public async Task NonGreedyAggregationTest()
        {
            IResourceSource<Item> source = ResourceSourceFactory<Item>.Generate(new Item("2"), new Item("1"), new Item("2"));
            var aggregatingSource = new AggregatingResourceSource<Item>(source, this.Aggregate);

            IList<Item> results = await aggregatingSource.PeekAsync(2).ConfigureAwait(false);
            Assert.IsNotNull(results);
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual("2", results[0].Data);
            Assert.AreEqual(1, results[0].AggregatedCount);
            Assert.AreEqual("1", results[1].Data);
            Assert.AreEqual(1, results[1].AggregatedCount);

            results = await aggregatingSource.PeekAsync(3).ConfigureAwait(false);
            Assert.IsNotNull(results);
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual("2", results[0].Data);
            Assert.AreEqual(2, results[0].AggregatedCount);
            Assert.AreEqual("1", results[1].Data);
            Assert.AreEqual(1, results[1].AggregatedCount);
        }

        [TestMethod]
        public async Task SafetyTest()
        {
            IResourceSource<Item> source = ResourceSourceFactory<Item>.Generate(new Item("3"), new Item("3"), new Item("2"), new Item("3"), new Item("2"), new Item("1"));
            var aggregatingSource = new AggregatingResourceSource<Item>(source, this.Aggregate);

            IList<Item> results = await aggregatingSource.PeekAsync(3).ConfigureAwait(false);
            Assert.IsNotNull(results);
            Assert.AreEqual(3, results.Count);
            Assert.AreEqual("3", results[0].Data);
            Assert.AreEqual(3, results[0].AggregatedCount);
            Assert.AreEqual("2", results[1].Data);
            Assert.AreEqual(2, results[1].AggregatedCount);
            Assert.AreEqual("1", results[2].Data);
            Assert.AreEqual(1, results[2].AggregatedCount);

            // You might expect this would work, and we'd be left with '1' but that's not true
            // We would actually consume 3, 3, 2 (and we'd stop at the 2 cuz we have two results then)
            // So, we expect this to throw.
            try
            {
                await aggregatingSource.ConsumeAsync(2).ConfigureAwait(false);
                Assert.Fail("Did not throw safety exception");
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Assert.AreEqual("count", ex.ParamName);
            }
        }

        private bool Aggregate(Item first, Item second)
        {
            if (first.Data != second.Data)
                return false;

            first.AggregatedCount += second.AggregatedCount;
            return true;
        }

        private class Item
        {
            public int AggregatedCount { get; set; }

            public string Data { get; }

            public Item(string data, int aggregatedCount = 1)
            {
                this.AggregatedCount = aggregatedCount;
                this.Data = data;
            }
        }
    }
}
