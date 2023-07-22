// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.UnitTests.Collections
{
    using Microsoft.Membership.MemberServices.Common.Collections;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class LastRecentlyModifiedDictionaryTests
    {
        [DataTestMethod]
        [DataRow(5, 0, 0)]
        [DataRow(5, 4, 4)]
        [DataRow(5, 5, 5)]
        [DataRow(5, 6, 5)]
        [DataRow(5, 8, 5)]
        public void AddTests(int capacity, int itemsToAdd, int expectedCount)
        {
            var cache = new LastRecentlyModifiedDictionary<int, string>(capacity);
            for (int x = 0; x < itemsToAdd; ++x)
            {
                cache.Add(x, "i'm a value");
            }

            Assert.AreEqual(expectedCount, cache.Count);

            for (int x = 0; x < itemsToAdd - capacity; ++x)
            {
                Assert.IsFalse(cache.ContainsKey(x));
            }
        }

        [DataTestMethod]
        [DataRow(0, 0, true)]
        [DataRow(1, 0, false)]
        [DataRow(1, 1, false)]
        [DataRow(1, 2, true)]
        public void ConstructorCapacityTests(int max, int init, bool shouldThrow)
        {
            bool hadException = false;
            try
            {
                new LastRecentlyModifiedDictionary<string, string>(max, init);
            }
            catch
            {
                hadException = true;
            }

            Assert.AreEqual(shouldThrow, hadException);
        }

        [DataTestMethod]
        [DataRow(5, 0, 0)]
        [DataRow(5, 4, 4)]
        [DataRow(5, 5, 5)]
        [DataRow(5, 6, 5)]
        [DataRow(5, 8, 5)]
        public void SetTests(int capacity, int itemsToAdd, int expectedCount)
        {
            var cache = new LastRecentlyModifiedDictionary<int, string>(capacity);
            for (int x = 0; x < itemsToAdd; ++x)
            {
                if ((1 + x) % capacity == 0)
                {
                    cache[0] = "update";
                }

                cache[x] = "i'm a value";
            }

            if (itemsToAdd > 0)
            {
                Assert.IsTrue(cache.ContainsKey(0));
            }

            Assert.AreEqual(expectedCount, cache.Count);
            for (int x = 1; x < itemsToAdd - capacity; ++x)
            {
                Assert.IsFalse(cache.ContainsKey(x));
            }
        }

        [TestMethod]
        public void ClearTest()
        {
            var cache = new LastRecentlyModifiedDictionary<int, int>(5)
            {
                [1] = 1
            };

            Assert.AreEqual(1, cache.Count);

            cache.Clear();

            Assert.AreEqual(0, cache.Count);
        }

        [TestMethod]
        public void ShouldMaintainOrder()
        {
            var cache = new LastRecentlyModifiedDictionary<int, int>(3);
            cache[0] = 0;
            cache[1] = 1;
            cache[2] = 2;

            for (int x = 0; x < 3; ++x)
            {
                Assert.IsTrue(cache.Keys.Contains(x), $"Expected Key: {x}");
            }

            cache[3] = 3;

            // Oldest should be removed
            Assert.IsFalse(cache.Keys.Contains(0));
            cache[1] = 7;

            // Make sure get doesn't modify ordering
            var getCheck = cache[2];
            Assert.AreEqual(2, getCheck);

            cache[4] = 4;

            // 2 has not been modified, should be removed
            Assert.IsFalse(cache.Keys.Contains(2));
            Assert.IsTrue(cache.Keys.Contains(1));
        }

        [TestMethod]
        public void RemoveTest()
        {
            var cache = new LastRecentlyModifiedDictionary<int, int>(5)
            {
                [1] = 1,
                [2] = 2
            };

            Assert.AreEqual(2, cache.Count);

            cache.Remove(1);

            Assert.AreEqual(1, cache.Count);

            Assert.IsTrue(cache.TryGetValue(2, out int value) && value == 2);
            Assert.IsFalse(cache.TryGetValue(1, out int _));
        }
    }
}
