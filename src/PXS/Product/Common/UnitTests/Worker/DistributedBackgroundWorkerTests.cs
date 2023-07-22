namespace Microsoft.Membership.MemberServices.Common.UnitTests.Worker
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common.DistributedLocking;
    using Microsoft.Membership.MemberServices.Common.Worker;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;

    using LockState = Microsoft.Membership.MemberServices.Common.Worker.DistributedBackgroundWorker.LockState;

    /// <summary>
    ///     DistributedBackgroundWorkerTests
    /// </summary>
    [TestClass]
    public class DistributedBackgroundWorkerTests
    {
        [TestMethod]
        public async Task SucceedToExtendLockAndHoldLockUtilNextTimeToRun()
        {
            var primitives = new FakeAlwaysTrueLockPrimitivies<LockState>();
            LockState lockState = new DistributedBackgroundWorker.LockState
            {
                LockAcquiredTime = DateTimeOffset.MinValue,
                MinLeaseTime = TimeSpan.FromSeconds(2),
                TaskRunFrequency = TimeSpan.FromSeconds(3),
                MaxExtensionTtl = TimeSpan.FromSeconds(7),
                ExtensionThreshold = TimeSpan.FromSeconds(1),
                NextStartTime = DateTimeOffset.MinValue
            };
            DistributedLock<LockState> testLock = new DistributedLock<LockState>("testLock", primitives);
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            TestWorker worker = new TestWorker(testLock, primitives, lockState, tokenSource.Token);
            
            // Act
            var result = await worker.DoWorkAsync();

            Assert.AreEqual(lockState.MinLeaseTime.TotalSeconds * 2, 
                Math.Round((DateTimeOffset.UtcNow - lockState.LockAcquiredTime).TotalSeconds),
                "The duration for the worker to finish the work should be about 4 seconds since the lease was extended once");
            var start = DateTimeOffset.UtcNow;
            while (testLock.IsLocked)
            {
            }
            Assert.AreEqual(lockState.TaskRunFrequency.TotalSeconds, 
                Math.Round((DateTimeOffset.UtcNow - start).TotalSeconds),
                "After work is finished, the duration for the worker to hold the lock should be about 3 seconds(WorkerRunInterval)");
            Assert.IsNotNull(result);
            Assert.IsTrue(result, "Task Succeeded");
        }

        [TestMethod]
        public async Task FailToAcquireLock()
        {
            var primitives = new FakeAlwaysFalseLockPrimitivies<LockState>();
            LockState lockState = new DistributedBackgroundWorker.LockState
            {
                LockAcquiredTime = DateTimeOffset.MinValue,
                MinLeaseTime = TimeSpan.FromSeconds(4),
            };
            DistributedLock<LockState> testLock = new DistributedLock<LockState>("testLock", primitives);
            TestWorker worker = new TestWorker(testLock, primitives, lockState, new CancellationTokenSource().Token);

            // Act
            var result = await worker.DoWorkAsync();

            Assert.IsNotNull(result);
            Assert.IsFalse(result, "Task Failed");
        }

        [TestMethod]
        public async Task TaskCanceledByCaller()
        {
            var primitives = new FakeAlwaysTrueLockPrimitivies<LockState>();
            LockState lockState = new DistributedBackgroundWorker.LockState
            {
                LockAcquiredTime = DateTimeOffset.MinValue,
                MinLeaseTime = TimeSpan.FromSeconds(4),
            };
            DistributedLock<LockState> testLock = new DistributedLock<LockState>("testLock", primitives);
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            TestWorker worker = new TestWorker(testLock, primitives, lockState, tokenSource.Token);

            // Act
            tokenSource.Cancel();
            var result = await worker.DoWorkAsync().ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.IsFalse(result, "Task Failed");
            Assert.IsTrue(tokenSource.IsCancellationRequested, "Task canceled by the caller");
            Assert.IsFalse(worker.isWorkFinished);
        }

        [TestMethod]
        public async Task TaskCanceledDueToReachedMaxExtensionTtl()
        {
            var primitives = new FakeAlwaysTrueLockPrimitivies<LockState>();
            LockState lockState = new DistributedBackgroundWorker.LockState
            {
                LockAcquiredTime = DateTimeOffset.MinValue,
                MinLeaseTime = TimeSpan.FromSeconds(1),
                MaxExtensionTtl = TimeSpan.FromSeconds(3),
                ExtensionThreshold = TimeSpan.FromSeconds(1),
                NextStartTime = DateTimeOffset.MinValue
            };

            DistributedLock<LockState> testLock = new DistributedLock<LockState>("testLock", primitives);
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            TestWorker worker = new TestWorker(testLock, primitives, lockState, tokenSource.Token);

            // Act
            var result = await worker.DoWorkAsync();
            
            Assert.IsTrue((DateTimeOffset.UtcNow - lockState.LockAcquiredTime).TotalSeconds > lockState.MaxExtensionTtl.TotalSeconds,
                "Time spent on doing the task should be greater than MaxExtensionTtl");
            Assert.IsNotNull(result);
            Assert.IsFalse(result, "Task Failed");
            Assert.IsFalse(worker.isWorkFinished);
        }

        /// <summary>
        ///     A dummy worker used for test
        /// </summary>
        public class TestWorker : DistributedBackgroundWorker
        {
            public bool isWorkFinished = false;
            IDistributedLockPrimitives<DistributedBackgroundWorker.LockState> lockPrimitives;

            public TestWorker(DistributedLock<LockState> distributedLock, IDistributedLockPrimitives<DistributedBackgroundWorker.LockState> lockPrimitives, LockState lockState, CancellationToken cancellationToken)
                : base(distributedLock, lockPrimitives, lockState, cancellationToken)
            {
                this.lockPrimitives = lockPrimitives;
            }

            public override Task<bool> DoDistributedWorkAsync(CancellationToken cancellationToken)
            {
                Thread.Sleep(4000);
                isWorkFinished = true;
                return Task.FromResult(true);
            }
        }

        /// <summary>
        ///     A dummy lock primitives that always return true to task acquiring/extending requests
        /// </summary>
        public class FakeAlwaysTrueLockPrimitivies<T> : IDistributedLockPrimitives<T> where T : class
        {
            private readonly object syncRoot = new object();
            private DistributedLockStatus<T> state;
            private bool exists;

            public Task CreateIfNotExistsAsync()
            {
                lock (this.syncRoot)
                {
                    if (!this.exists)
                    {
                        this.state = new DistributedLockStatus<T>
                        {
                            ExpirationTime = DateTimeOffset.MinValue,
                            OwnerId = null,
                            State = null,
                        };
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

        /// <summary>
        ///     A dummy lock primitives that always return false to task acquiring/extending requests
        /// </summary>
        public class FakeAlwaysFalseLockPrimitivies<T> : IDistributedLockPrimitives<T> where T : class
        {
            private readonly object syncRoot = new object();
            private DistributedLockStatus<T> state;
            private bool exists;

            public Task CreateIfNotExistsAsync()
            {
                lock (this.syncRoot)
                {
                    if (!this.exists)
                    {
                        this.state = new DistributedLockStatus<T>
                        {
                            ExpirationTime = DateTimeOffset.MinValue,
                            OwnerId = null,
                            State = null,
                        };
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

                    return Task.FromResult(false);
                }
            }
        }
    }
}
