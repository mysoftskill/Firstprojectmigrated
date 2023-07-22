namespace Microsoft.Azure.ComplianceServices.Common.UnitTests
{
    using Microsoft.Azure.ComplianceServices.Common.AppConfig.Cache;
    using System.Threading;
    using Xunit;

    [Trait("Category", "UnitTest")]
    public class LruCacheTest
    {

        private LruCache<bool> PopulateCache(int arg)
        {
            LruCache<bool> cache = new LruCache<bool>(arg);
            for (int i = 0; i < (arg * 2); i++)
            {
                cache.AddItem(i.ToString(), false);
                Thread.Sleep(10);
            }

            return cache;
        }

        [Theory]
        [InlineData(10)]
        [InlineData(20)]
        public void VerifyMaxCapacity(int arg)
        {
            LruCache<bool> cache = PopulateCache(arg);
            Assert.True(cache.Count() <= arg );
        }


        [Theory]
        [InlineData(10)]
        [InlineData(20)]
        public void VerifyItemExists(int arg)
        {
            LruCache<bool> cache = PopulateCache(arg);

            Assert.True(cache.GetItem(((arg*2)-3).ToString(), out bool _));
        }

        [Theory]
        [InlineData(10)]
        [InlineData(20)]
        public void VerifyLeastRecentlyUsedItemDeleted(int arg)
        {
            LruCache<bool> cache = PopulateCache(arg);
            Thread.Sleep(10);
            Assert.False(cache.GetItem(1.ToString(), out bool _));
        }

        [Theory]
        [InlineData(10)]
        [InlineData(20)]
        public void VerifyCacheReset(int arg)
        {
            LruCache<bool> cache = PopulateCache(arg);

            cache.Reset();

            Thread.Sleep(10);

            Assert.True(cache.Count() == 0);
        }

        [Theory]
        [InlineData(10)]
        [InlineData(20)]
        public void VerifyCompaction(int arg)
        {
            LruCache<bool> cache = new LruCache<bool>(arg);
            for (int i = 0; i <= arg; i++)
            {
                cache.AddItem(i.ToString(), false);
                Thread.Sleep(10);
            }

            Thread.Sleep(200);

            // 50% must be removed when capacity is hit.
            Assert.True(cache.Count() <= arg / 2);

            // Oldest of arg/2 items should have been removed.
            for (int i = 0; i < arg/2; i++)
            {
                Assert.False(cache.GetItem(i.ToString(), out _));
            }
        }

    }
}
