using System;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Osgs.Infra.Cache;
using Microsoft.Osgs.Infra.Cache.Tracking;
using Microsoft.PrivacyServices.UX.Core.Cache;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

/*
 * This is a local clone of AMC's Product\WebRole\UnitTests\Core\Cms\SimpleMemoryCacheClientTests.cs.
 * Keep changes minimal, or backport them to AMC.
 */

namespace Microsoft.PrivacyServices.UX.Tests.Core.Cache
{
    [TestClass]
    public class SimpleMemoryCacheClientTests
    {
        private ICacheClient cacheClient;

        private Mock<ObjectCache> mockMemoryCache;

        private Mock<ICacheTracking> mockCacheTracking;

        [TestInitialize]
        public void TestInitialize()
        {
            mockMemoryCache = new Mock<ObjectCache>(MockBehavior.Strict);
            mockCacheTracking = new Mock<ICacheTracking>(MockBehavior.Strict);

            cacheClient = new SimpleMemoryCacheClient(mockCacheTracking.Object, mockMemoryCache.Object);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            cacheClient.Dispose();
            cacheClient = null;
        }

        [TestMethod]
        public void SimpleMemoryCacheClient_TryInitialize_AlwaysReturnsTrue()
        {
            Assert.IsTrue(cacheClient.TryInitialize());
        }

        [TestMethod]
        public async Task SimpleMemoryCacheClient_GetAsync_CacheMiss_ReturnsNull()
        {
            mockMemoryCache
                .Setup(mmc => mmc.Get(It.Is<string>(arg => arg == "test"), It.IsAny<string>()))
                .Returns(null)
                .Verifiable();
            mockCacheTracking
                .Setup(mct => mct.CacheMiss(It.Is<string>(arg => arg == "test")))
                .Verifiable();
            mockCacheTracking
                .Setup(mct => mct.GetAsync(It.Is<string>(arg => arg == "test"), It.IsAny<Func<GetOperationContext, Task<object>>>(), It.IsAny<CacheSmartGetOperationContext>(), It.IsAny<LoggingContext>()))
                .Callback(new Action<string, Func<GetOperationContext, Task<object>>, CacheSmartGetOperationContext, LoggingContext>((arg, callback, context1, context2) =>
                {
                    callback(new GetOperationContext());
                }))
                .Returns(Task.FromResult<object>(null))
                .Verifiable();

            var item = await cacheClient.GetAsync<object>("test", new CacheSmartGetOperationContext());
            Assert.IsNull(item);

            mockMemoryCache.VerifyAll();
            mockCacheTracking.VerifyAll();
        }

        [TestMethod]
        public async Task SimpleMemoryCacheClient_GetAsync_CacheHit_ReturnsObject()
        {
            var cachedObject = new object();

            mockMemoryCache
                .Setup(mmc => mmc.Get(It.Is<string>(arg => arg == "test"), It.IsAny<string>()))
                .Returns(cachedObject)
                .Verifiable();
            mockCacheTracking
                .Setup(mct => mct.CacheHit(It.Is<string>(arg => arg == "test")))
                .Verifiable();
            mockCacheTracking
                .Setup(mct => mct.GetAsync(It.Is<string>(arg => arg == "test"), It.IsAny<Func<GetOperationContext, Task<object>>>(), It.IsAny<CacheSmartGetOperationContext>(), It.IsAny<LoggingContext>()))
                .Callback(new Action<string, Func<GetOperationContext, Task<object>>, CacheSmartGetOperationContext, LoggingContext>((arg, callback, context1, context2) =>
                {
                    callback(new GetOperationContext());
                }))
                .Returns(Task.FromResult(cachedObject))
                .Verifiable();

            var item = await cacheClient.GetAsync<object>("test", new CacheSmartGetOperationContext());
            Assert.AreSame(cachedObject, item);

            mockMemoryCache.VerifyAll();
            mockCacheTracking.VerifyAll();
        }

        [TestMethod]
        public async Task SimpleMemoryCacheClient_PutAsync_Null_ReturnsFalse()
        {
            mockCacheTracking
                .Setup(mct => mct.PutAsync(It.Is<string>(arg => arg == "test"), It.IsAny<Func<Task<bool>>>(), It.IsAny<LoggingContext>()))
                .Returns(Task.FromResult(false))
                .Verifiable();

            var putResult = await cacheClient.PutAsync<object>("test", null, TimeSpan.MaxValue);
            Assert.IsFalse(putResult);

            mockCacheTracking.VerifyAll();
        }

        [TestMethod]
        public async Task SimpleMemoryCacheClient_PutAsync_ValidObject_SetsCacheItem()
        {
            var cachedObject = new object();

            mockMemoryCache
                .Setup(mmc => mmc.Set(It.Is<string>(arg => arg == "test"), It.Is<object>(arg => arg == cachedObject), It.Is<CacheItemPolicy>(arg => arg.AbsoluteExpiration != ObjectCache.InfiniteAbsoluteExpiration && arg.AbsoluteExpiration > DateTimeOffset.UtcNow), It.IsAny<string>()))
                .Verifiable();
            mockCacheTracking
                .Setup(mct => mct.PutAsync(It.Is<string>(arg => arg == "test"), It.IsAny<Func<Task<bool>>>(), It.IsAny<LoggingContext>()))
                .Callback(new Action<string, Func<Task<bool>>, LoggingContext>((arg, callback, context) =>
                {
                    callback();
                }))
                .Returns(Task.FromResult(true))
                .Verifiable();

            var putResult = await cacheClient.PutAsync("test", cachedObject, TimeSpan.FromDays(2));
            Assert.IsTrue(putResult);

            mockMemoryCache.VerifyAll();
            mockCacheTracking.VerifyAll();
        }

        [TestMethod]
        public async Task SimpleMemoryCacheClient_SmartGetAsync_CacheHit_ReturnsObject()
        {
            var operationContext = new CacheSmartGetOperationContext();
            var cachedObject = new object();

            mockCacheTracking
                .Setup(mct => mct.GetAsync(It.Is<string>(arg => arg == "test"), It.IsAny<Func<GetOperationContext, Task<TimeStampedItem<object>>>>(), It.IsAny<CacheSmartGetOperationContext>(), It.IsAny<LoggingContext>()))
                .Returns(Task.FromResult(new TimeStampedItem<object>(cachedObject)))
                .Verifiable();

            var item = await cacheClient.SmartGetAsync<object>("test", null, operationContext, CancellationToken.None, TimeSpan.Zero);
            Assert.AreSame(cachedObject, item);
            Assert.IsFalse(operationContext.IsCacheMiss);

            mockCacheTracking.VerifyAll();
        }

        [TestMethod]
        public async Task SimpleMemoryCacheClient_SmartGetAsync_CacheMiss_GetsFromSource()
        {
            var operationContext = new CacheSmartGetOperationContext();
            var cachedObject = new object();

            mockCacheTracking
                .Setup(mct => mct.GetAsync(It.Is<string>(arg => arg == "test"), It.IsAny<Func<GetOperationContext, Task<TimeStampedItem<object>>>>(), It.IsAny<CacheSmartGetOperationContext>(), It.IsAny<LoggingContext>()))
                .Returns(Task.FromResult<TimeStampedItem<object>>(null))
                .Verifiable();
            mockCacheTracking
                .Setup(mct => mct.SourceGetter(It.IsAny<string>(), It.IsAny<Func<Task<TimeStampedItem<object>>>>(), It.IsAny<LoggingContext>()))
                .Returns(Task.FromResult(new TimeStampedItem<object>(cachedObject)))
                .Verifiable();
            mockCacheTracking
                .Setup(mct => mct.PutAsync(It.Is<string>(arg => arg == "test"), It.IsAny<Func<Task<bool>>>(), It.IsAny<LoggingContext>()))
                .Returns(Task.FromResult(true))
                .Verifiable();

            var item = await cacheClient.SmartGetAsync<object>("test", null, operationContext, CancellationToken.None, TimeSpan.Zero);
            Assert.AreSame(cachedObject, item);
            Assert.IsTrue(operationContext.IsCacheMiss);

            mockCacheTracking.VerifyAll();
        }

        [TestMethod]
        public async Task SimpleMemoryCacheClient_SmartGetAsync_NullTreatedAsCacheMiss_GetsFromSource()
        {
            var operationContext = new CacheSmartGetOperationContext();
            var cachedObject = new object();

            mockCacheTracking
                .Setup(mct => mct.GetAsync(It.Is<string>(arg => arg == "test"), It.IsAny<Func<GetOperationContext, Task<TimeStampedItem<object>>>>(), It.IsAny<CacheSmartGetOperationContext>(), It.IsAny<LoggingContext>()))
                .Returns(Task.FromResult(new TimeStampedItem<object>(null)))
                .Verifiable();
            mockCacheTracking
                .Setup(mct => mct.SourceGetter(It.IsAny<string>(), It.IsAny<Func<Task<TimeStampedItem<object>>>>(), It.IsAny<LoggingContext>()))
                .Returns(Task.FromResult(new TimeStampedItem<object>(cachedObject)))
                .Verifiable();
            mockCacheTracking
                .Setup(mct => mct.PutAsync(It.Is<string>(arg => arg == "test"), It.IsAny<Func<Task<bool>>>(), It.IsAny<LoggingContext>()))
                .Returns(Task.FromResult(true))
                .Verifiable();

            var item = await cacheClient.SmartGetAsync<object>("test", null, operationContext, CancellationToken.None, TimeSpan.Zero);
            Assert.AreSame(cachedObject, item);
            Assert.IsTrue(operationContext.IsCacheMiss);

            mockCacheTracking.VerifyAll();
        }

        [TestMethod]
        [ExpectedException(typeof(SimpleMemoryCacheClientException))]
        public async Task SimpleMemoryCacheClient_SmartGetAsync_FailureToGetFromSource_Throws()
        {
            var operationContext = new CacheSmartGetOperationContext();
            var cachedObject = new object();

            mockCacheTracking
                .Setup(mct => mct.GetAsync(It.Is<string>(arg => arg == "test"), It.IsAny<Func<GetOperationContext, Task<TimeStampedItem<object>>>>(), It.IsAny<CacheSmartGetOperationContext>(), It.IsAny<LoggingContext>()))
                .Returns(Task.FromResult<TimeStampedItem<object>>(null))
                .Verifiable();
            mockCacheTracking
                .Setup(mct => mct.SourceGetter(It.IsAny<string>(), It.IsAny<Func<Task<TimeStampedItem<object>>>>(), It.IsAny<LoggingContext>()))
                .Returns(Task.FromResult<TimeStampedItem<object>>(null))
                .Verifiable();

            await cacheClient.SmartGetAsync<object>("test", null, operationContext, CancellationToken.None, TimeSpan.Zero);
        }

        [TestMethod]
        public async Task SimpleMemoryCacheClient_SmartGetAsync_GetFromSourceReturnsNull_PutAsyncNotCalled_ReturnsNull()
        {
            var operationContext = new CacheSmartGetOperationContext();

            mockCacheTracking
                .Setup(mct => mct.GetAsync(It.Is<string>(arg => arg == "test"), It.IsAny<Func<GetOperationContext, Task<TimeStampedItem<object>>>>(), It.IsAny<CacheSmartGetOperationContext>(), It.IsAny<LoggingContext>()))
                .Returns(Task.FromResult<TimeStampedItem<object>>(null))
                .Verifiable();
            mockCacheTracking
                .Setup(mct => mct.SourceGetter(It.IsAny<string>(), It.IsAny<Func<Task<TimeStampedItem<object>>>>(), It.IsAny<LoggingContext>()))
                .Returns(Task.FromResult(new TimeStampedItem<object>(null)))
                .Verifiable();

            var item = await cacheClient.SmartGetAsync<object>("test", null, operationContext, CancellationToken.None, TimeSpan.Zero);
            Assert.IsNull(item);
            Assert.IsTrue(operationContext.IsCacheMiss);

            mockCacheTracking.VerifyAll();
        }
    }
}
