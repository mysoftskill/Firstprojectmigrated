namespace Microsoft.PrivacyServices.CommandFeed.Client.Test.PrivacyCommandValidator
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.CommandFeed.Validator.KeyDiscovery;
    using Microsoft.PrivacyServices.CommandFeed.Validator.KeyDiscovery.Keys;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class InMemoryCacheTests
    {
        private JsonWebKey sampleItem = new JsonWebKey
        {
            KeyId = It.IsAny<string>(),
            KeyType = JwkKeyType.RSA,
            X509Chain = new string[] { It.IsAny<string>() },
            X509Thumbprint = It.IsAny<string>()
        };

        [TestMethod]
        public void ReadAsyncReturnsItem()
        {
            const string key = "key";
            JsonWebKey item = this.sampleItem;

            var cacheItems = new Dictionary<string, CacheItem>
            {
                { key, new CacheItem(item) }
            };

            var cache = new InMemoryCache();
            cache.WriteAsync(cacheItems, CancellationToken.None).Wait();

            CacheItem result = cache.ReadAsync(key).Result;
            Assert.AreEqual(item, result.Item);
            Assert.IsTrue(result.Found);
        }

        [TestMethod]
        public void ReadAsyncReturnsNullIfExpired()
        {
            const string key = "expiredKey";
            JsonWebKey item = this.sampleItem;

            var cacheItems = new Dictionary<string, CacheItem>
            {
                { key, new CacheItem(item, timeToLive: 1) }
            };

            var cache = new InMemoryCache();
            cache.WriteAsync(cacheItems, CancellationToken.None).Wait();
            Thread.Sleep(1000);
            Assert.IsNull(cache.ReadAsync(key).Result);
        }

        [TestMethod]
        public void ReadAsyncReturnsNullIfKeyNotFound()
        {
            const string key = "notFoundKey";
            var cache = new InMemoryCache();
            Assert.IsNull(cache.ReadAsync(key).Result);
        }

        [TestMethod]
        public async Task WriteAsyncCachesAllItems()
        {
            const string key = "key{0}";

            var cacheItems = new Dictionary<string, CacheItem>();
            for (int i = 0; i < 10; i++)
            {
                var item = new JsonWebKey
                {
                    KeyId = i.ToString()
                };
                cacheItems.Add(string.Format(key, i), new CacheItem(item));
            }

            var cache = new InMemoryCache();
            await cache.WriteAsync(cacheItems, default(CancellationToken));

            for (int i = 0; i < 10; i++)
            {
                var result = await cache.ReadAsync(string.Format(key, i));
                
                Assert.AreEqual(i.ToString(), result.Item.KeyId);
                Assert.IsTrue(result.Expiration > DateTimeOffset.UtcNow);
            }
        }

        [TestMethod]
        public void WriteAsyncUpdatesExistingItem()
        {
            const string key = "key";
            JsonWebKey item = this.sampleItem;

            var cacheItems = new Dictionary<string, CacheItem>
            {
                { key, new CacheItem(this.sampleItem) }
            };

            var cache = new InMemoryCache();
            cache.WriteAsync(cacheItems, CancellationToken.None).Wait();

            Assert.AreEqual(item, cache.ReadAsync(key).Result.Item);

            JsonWebKey itemNew = this.sampleItem;
            itemNew.KeyId = "value2";

            cacheItems[key] = new CacheItem(itemNew);
            cache.WriteAsync(cacheItems, CancellationToken.None).Wait();

            Assert.AreEqual(itemNew, cache.ReadAsync(key).Result.Item);
        }
    }
}
