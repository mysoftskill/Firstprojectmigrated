namespace Microsoft.PrivacyServices.DataManagement.DataAccess.ActiveDirectory.UnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Common.Authentication;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.ActiveDirectory;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Authentication;
    using Microsoft.PrivacyServices.Testing;

    using Moq;
    using Ploeh.AutoFixture.Xunit2;

    using Xunit;

    public class CachedActiveDirectoryTest
    {
        [Theory(DisplayName = "When the cache contains non-expired data, then return that data and do not call the directory."), ValidData]
        public async Task When_CacheContainsNonExpiredData_Then_ReturnCacheValue(
            [Frozen] DateTimeOffset currentTime,
            [Frozen] Mock<IActiveDirectory> directory,
            [Frozen] Mock<IActiveDirectoryCache> cache,
            CacheData cacheData, 
            CachedActiveDirectory system, 
            AuthenticatedPrincipal principal)
        {
            cacheData.Expiration = currentTime.Add(TimeSpan.FromMilliseconds(1));
            cache.Setup(m => m.ReadDataAsync(principal)).ReturnsAsync(cacheData);

            var groups = await system.GetSecurityGroupIdsAsync(principal).ConfigureAwait(false);

            Assert.Equal(cacheData.SecurityGroupIds, groups);

            directory.Verify(m => m.GetSecurityGroupIdsAsync(principal), Times.Never);
        }

        [Theory(DisplayName = "When the cache contains expired data, then return directory data."), ValidData]
        public async Task When_CacheContainsExpiredData_Then_ReturnDirectoryValue(
            [Frozen] DateTimeOffset currentTime,
            [Frozen] Mock<IActiveDirectory> directory,
            [Frozen] Mock<IActiveDirectoryCache> cache,
            CacheData cacheData,
            IEnumerable<Guid> directoryData,
            CachedActiveDirectory system,
            AuthenticatedPrincipal principal)
        {
            directory.Setup(m => m.GetSecurityGroupIdsAsync(principal)).ReturnsAsync(directoryData);

            cacheData.Expiration = currentTime.Add(TimeSpan.FromMilliseconds(-1));

            cache.Setup(m => m.ReadDataAsync(principal)).ReturnsAsync(cacheData);

            var groups = await system.GetSecurityGroupIdsAsync(principal).ConfigureAwait(false);

            Assert.Equal(directoryData, groups);
        }

        [Theory(DisplayName = "When the cache contains expired data, then refresh cache data."), ValidData]
        public async Task When_CacheContainsExpiredData_Then_RefreshCacheData(
            [Frozen] IDataAccessConfiguration config,
            [Frozen] DateTimeOffset currentTime,
            [Frozen] Mock<IActiveDirectoryCache> cache,
            [Frozen] CacheData cacheData,
            [Frozen] IEnumerable<Guid> directoryData,
            CachedActiveDirectory system,
            AuthenticatedPrincipal principal)
        {
            cacheData.Expiration = currentTime.Add(TimeSpan.FromMilliseconds(-1));

            await system.GetSecurityGroupIdsAsync(principal).ConfigureAwait(false);

            Action<CacheData> verify = value => 
            {
                Assert.Equal(cacheData.ETag, value.ETag);
                Assert.Null(value.Id);
                Assert.Equal(directoryData, value.SecurityGroupIds);
                Assert.Equal(currentTime.AddMilliseconds(config.ActiveDirectoryCacheExpirationInMilliseconds), value.Expiration);
            };

            cache.Verify(m => m.UpdateDataAsync(principal, Is.Value(verify)), Times.Once);
        }

        [Theory(DisplayName = "When the cache is empty, then return directory data."), ValidData]
        public async Task When_CacheEmpty_Then_ReturnDirectoryData(
            [Frozen] Mock<IActiveDirectory> directory,
            [Frozen] Mock<IActiveDirectoryCache> cache,
            IEnumerable<Guid> directoryData,
            CachedActiveDirectory system,
            AuthenticatedPrincipal principal)
        {
            cache.Setup(m => m.ReadDataAsync(principal)).ReturnsAsync(null, TimeSpan.FromSeconds(5));

            directory.Setup(m => m.GetSecurityGroupIdsAsync(principal)).ReturnsAsync(directoryData);

            var groups = await system.GetSecurityGroupIdsAsync(principal).ConfigureAwait(false);

            Assert.Equal(directoryData, groups);
        }

        [Theory(DisplayName = "When the cache is empty, then set cache data."), ValidData]
        public async Task When_CacheEmpty_Then_SetCacheData(
            [Frozen] IDataAccessConfiguration config,
            [Frozen] DateTimeOffset currentTime,
            [Frozen] Mock<IActiveDirectoryCache> cache,
            [Frozen] IEnumerable<Guid> directoryData,
            CachedActiveDirectory system,
            AuthenticatedPrincipal principal)
        {
            cache.Setup(m => m.ReadDataAsync(principal)).ReturnsAsync(null, TimeSpan.FromSeconds(5));
            
            await system.GetSecurityGroupIdsAsync(principal).ConfigureAwait(false);

            Action<CacheData> verify = value =>
            {
                Assert.Null(value.ETag);
                Assert.Null(value.Id);
                Assert.Equal(directoryData, value.SecurityGroupIds);
                Assert.Equal(currentTime.AddMilliseconds(config.ActiveDirectoryCacheExpirationInMilliseconds), value.Expiration);
            };

            cache.Verify(m => m.CreateDataAsync(principal, Is.Value(verify)), Times.Once);
        }

        [Theory(DisplayName = "When an exception occurs reading from cache, then suppress and continue."), ValidData]
        public async Task When_ReadCacheException_Then_Continue(
            [Frozen] Mock<IActiveDirectoryCache> cache,
            CachedActiveDirectory system,
            AuthenticatedPrincipal principal)
        {
            cache.Setup(m => m.ReadDataAsync(principal)).Returns(Task.FromException<CacheData>(new Exception()));
            
            await system.GetSecurityGroupIdsAsync(principal).ConfigureAwait(false);

            cache.Verify(m => m.ReadDataAsync(principal), Times.Once);
        }

        [Theory(DisplayName = "When an exception occurs writing to cache, then suppress and continue."), ValidData]
        public async Task When_WriteCacheException_Then_Continue(
            [Frozen] Mock<IActiveDirectoryCache> cache,            
            CachedActiveDirectory system,
            AuthenticatedPrincipal principal)
        {
            cache.Setup(m => m.ReadDataAsync(principal)).ReturnsAsync(null, TimeSpan.FromSeconds(5));
            cache.Setup(m => m.CreateDataAsync(principal, It.IsAny<CacheData>())).Returns(Task.FromException(new Exception()));

            await system.GetSecurityGroupIdsAsync(principal).ConfigureAwait(false);

            cache.Verify(m => m.CreateDataAsync(principal, It.IsAny<CacheData>()), Times.Once);
        }

        [Theory(DisplayName = "When the force refresh flag is set to true, then return directory data."), ValidData]
        public async Task When_ForceRefreshFlagSetToTrue_Then_ReturnDirectoryValue(
            [Frozen] Mock<IActiveDirectory> directory,
            [Frozen] Mock<IActiveDirectoryCache> cache,
            CacheData cacheData,
            IEnumerable<Guid> directoryData,
            CachedActiveDirectory system,
            AuthenticatedPrincipal principal)
        {
            system.ForceRefreshCache = true;

            directory.Setup(m => m.GetSecurityGroupIdsAsync(principal)).ReturnsAsync(directoryData);

            cache.Setup(m => m.ReadDataAsync(principal)).ReturnsAsync(cacheData);

            var groups = await system.GetSecurityGroupIdsAsync(principal).ConfigureAwait(false);

            Assert.Equal(directoryData, groups);
        }

        [Theory(DisplayName = "When initializing a data owner writer, then set the force refresh flag to true."), ValidData]
        public void When_InitializingDataOwnerWriter_Then_SetForceRefreshFlagToTrue(
            [Frozen] Mock<ICachedActiveDirectory> cache)
        {
            cache.VerifySet(m => m.ForceRefreshCache = true);
        }

        public class ValidDataAttribute : AutoMoqDataAttribute
        {
            public ValidDataAttribute() : base(true)
            {
                this.Fixture.Customize<CachedActiveDirectory>(obj =>
                    obj
                    .Without(x => x.ForceRefreshCache));
            }
        }

        public class InlineValidDataAttribute : InlineAutoMoqDataAttribute
        {
            public InlineValidDataAttribute(params object[] values) : base(new ValidDataAttribute(), values)
            {
            }
        }
    }
}