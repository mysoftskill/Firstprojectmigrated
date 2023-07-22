// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Common.UnitTests.Collections
{
    using Microsoft.Membership.MemberServices.Common.Collections;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class LastRecentlyUsedDictionaryTests
    {
        [TestMethod]
        public void ShouldMaintainOrder()
        {
            var cache = new LastRecentlyUsedDictionary<int, int>(3);
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

            // Get should cause [2] to update
            var getCheck = cache[2];
            Assert.AreEqual(2, getCheck);

            cache[4] = 4;

            // 3 has not been modified, should be removed
            Assert.IsFalse(cache.Keys.Contains(3));
            Assert.IsTrue(cache.Keys.Contains(1));
            Assert.IsTrue(cache.Keys.Contains(2));
            Assert.IsTrue(cache.Keys.Contains(4));
        }
    }
}
