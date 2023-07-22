namespace PCF.UnitTests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common.DistributedLocking;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.Common.Azure;
    using Moq;
    using Newtonsoft.Json;
    using SemanticComparison.Fluent;
    using Xunit;

    [Trait("Category", "UnitTest")]
    public class DistributedLockTests
    {
        /// <summary>
        /// Tests some basic distributed lock operations. These are tricky since we're really just testing against mock data...
        /// </summary>
        [Fact]
        public async Task BasicLockOperations()
        {
            var primitives = new FakeLockPrimitivies<LockState>();
            DistributedLock<LockState> lock1 = new DistributedLock<LockState>("lock", primitives);
            DistributedLock<LockState> lock2 = new DistributedLock<LockState>("lock", primitives);

            var result = await lock1.TryAcquireAsync(TimeSpan.FromSeconds(1), new ConsoleLogger());
            Assert.NotNull(result);
            Assert.True(result.Succeeded);
            Assert.Null(result.Status);
            Assert.True(lock1.IsLocked);
            Assert.True(lock1.RemainingTime >= TimeSpan.FromMilliseconds(100));
            Assert.True(lock1.RemainingTime <= TimeSpan.FromMilliseconds(1500));

            var result2 = await lock2.TryAcquireAsync(TimeSpan.FromSeconds(1), new ConsoleLogger());
            Assert.NotNull(result2);
            Assert.False(result2.Succeeded);
            Assert.False(lock2.IsLocked);

            // Update the state.
            var status = new LockState { Property = "foobar" };
            await lock1.TryExtendAsync(TimeSpan.FromSeconds(1), status, new ConsoleLogger());

            await Task.Delay(TimeSpan.FromSeconds(1.5));

            Assert.False(lock1.IsLocked);

            result2 = await lock2.TryAcquireAsync(TimeSpan.FromSeconds(1), new ConsoleLogger());
            Assert.True(result2.Succeeded);
            Assert.True(lock2.IsLocked);
            Assert.Equal("foobar", result2.Status.Property);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
        private class LockState
        {
            public string Property { get; set; }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
        private class FakeLockPrimitivies<T> : IDistributedLockPrimitives<T> where T : class
        {
            private readonly object syncRoot = new object();
            private DistributedLockStatus<T> state;
            private bool exists;
            private CancellationTokenSource expirationCancellation = new CancellationTokenSource();
            
            public Task CreateIfNotExistsAsync()
            {
                lock (this.syncRoot)
                {
                    if (!this.exists)
                    {
                        this.state = new DistributedLockStatus<T>();
                        this.exists = true;
                    }

                    return Task.FromResult(true);
                }
            }

            public Task<DistributedLockStatus<T>> GetStatusAsync()
            {
                var newStatus = JsonConvert.DeserializeObject<DistributedLockStatus<T>>(JsonConvert.SerializeObject(this.state));
                newStatus.ETag = this.state.ETag;

                return Task.FromResult(newStatus);
            }

            public Task<bool> TryAcquireOrExtendLeaseAsync(T value, DateTimeOffset expirationTime, string ownerId, string etag)
            {
                lock (this.syncRoot)
                {
                    if (etag != this.state.ETag)
                    {
                        return Task.FromResult(false);
                    }
                    
                    this.state = new DistributedLockStatus<T>
                    {
                        ExpirationTime = expirationTime,
                        OwnerId = ownerId,
                        State = value,
                        ETag = Guid.NewGuid().ToString(),
                    };

                    return Task.FromResult(true);
                }
            }
        }
    }
}
