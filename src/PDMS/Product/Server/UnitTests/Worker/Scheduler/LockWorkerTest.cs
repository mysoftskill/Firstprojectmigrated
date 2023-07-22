namespace Microsoft.PrivacyServices.DataManagement.Worker.Scheduler.UnitTest
{
    using System;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Common;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.DocumentDb.UnitTest;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Scheduler;
    using Microsoft.PrivacyServices.Testing;

    using Moq;
    using Moq.Protected;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Xunit2;

    using Xunit;

    public class LockWorkerTest
    {
        [Theory(DisplayName = "When Lock object does not exist in storage, then Lock object is created and stored."), ValidData]
        public async Task VerifyLockCreation(
            [Frozen] Mock<ILockDataAccess<TestReaderState>> dataAccessMock,
            Mock<LockWorker<TestReaderState>> lockWorker,
            Lock<TestReaderState> persistedLock)
        {
            lockWorker.Setup(m => m.LockName).Returns("lock");
            lockWorker.Setup(m => m.EnableAcquireLock).Returns(true);

            Action<Lock<TestReaderState>> verify = d =>
            {
                Assert.Equal(lockWorker.Object.LockName, d.Id);
                Assert.Equal(lockWorker.Object.Id, d.WorkerId);
                Assert.Null(d.State);
            };

            dataAccessMock
                .Setup(m => m.GetAsync(lockWorker.Object.LockName))
                .ReturnsAsync((Lock<TestReaderState>)null);

            dataAccessMock
                .Setup(m => m.CreateAsync(Is.Value(verify)))
                .ReturnsAsync(persistedLock);

            await lockWorker.Object.DoWorkAsync(CancellationToken.None).ConfigureAwait(false);

            dataAccessMock.Verify(m => m.GetAsync(lockWorker.Object.LockName), Times.Once);
            dataAccessMock.Verify(m => m.CreateAsync(Is.Value(verify)), Times.Once);
            dataAccessMock.Verify(m => m.UpdateAsync(It.IsAny<Lock<TestReaderState>>()), Times.Never);

            lockWorker.Verify(m => m.DoLockWorkAsync(persistedLock, CancellationToken.None), Times.Once);
        }

        [Theory(DisplayName = "When same lock worker holds lock and expiry time is in future, then lock worker updates lock and call DoLockWorkAsync."), ValidData]
        public async Task VerifyLockRenewal(
            [Frozen] Mock<ILockDataAccess<TestReaderState>> dataAccessMock,
            Mock<LockWorker<TestReaderState>> lockWorker,
            Lock<TestReaderState> persistedLock)
        {
            Action<Lock<TestReaderState>> verify = d =>
            {
                Assert.Equal(lockWorker.Object.LockName, d.Id);
                Assert.Equal(lockWorker.Object.Id, d.WorkerId);
            };

            lockWorker.Setup(m => m.EnableAcquireLock).Returns(true);
            lockWorker
                .SetupGet(m => m.LockName)
                .Returns(persistedLock.Id);

            persistedLock.ExpiryTime = DateTimeOffset.UtcNow.AddSeconds(5);
            persistedLock.WorkerId = lockWorker.Object.Id;

            dataAccessMock
                .Setup(m => m.GetAsync(lockWorker.Object.LockName))
                .ReturnsAsync(persistedLock);

            dataAccessMock
                .Setup(m => m.UpdateAsync(Is.Value(verify)))
                .ReturnsAsync(persistedLock);

            await lockWorker.Object.DoWorkAsync(CancellationToken.None).ConfigureAwait(false);

            dataAccessMock.Verify(m => m.GetAsync(lockWorker.Object.LockName), Times.Once);
            dataAccessMock.Verify(m => m.CreateAsync(Is.Value(verify)), Times.Never);
            dataAccessMock.Verify(m => m.UpdateAsync(It.IsAny<Lock<TestReaderState>>()), Times.Once);
            lockWorker.Verify(m => m.DoLockWorkAsync(persistedLock, CancellationToken.None), Times.Once);
        }

        [Theory(DisplayName = "When existing lock expires, then lock worker updates lock and call DoLockWorkAsync."), ValidData]
        public async Task VerifyExpiredLock(
            [Frozen] Mock<ILockDataAccess<TestReaderState>> dataAccessMock,
            [Frozen] Mock<IDateFactory> dateFactoryMock,
            Mock<LockWorker<TestReaderState>> lockWorker,
            DateTimeOffset date,
            Lock<TestReaderState> persistedLock,
            Lock<TestReaderState> updatedLock)
        {
            Action<Lock<TestReaderState>> verify = d =>
            {
                Assert.Equal(lockWorker.Object.Id, d.WorkerId);
            };

            dateFactoryMock.Setup(m => m.GetCurrentTime()).Returns(date);

            lockWorker.Setup(m => m.EnableAcquireLock).Returns(true);
            lockWorker
                .SetupGet(m => m.LockName)
                .Returns(persistedLock.Id);

            persistedLock.ExpiryTime = date.AddSeconds(-5);

            dataAccessMock
                .Setup(m => m.GetAsync(lockWorker.Object.LockName))
                .ReturnsAsync(persistedLock);

            dataAccessMock
                .Setup(m => m.UpdateAsync(Is.Value(verify)))
                .ReturnsAsync(updatedLock);

            await lockWorker.Object.DoWorkAsync(CancellationToken.None).ConfigureAwait(false);

            dataAccessMock.Verify(m => m.GetAsync(lockWorker.Object.LockName), Times.Once);
            dataAccessMock.Verify(m => m.CreateAsync(Is.Value(verify)), Times.Never);
            dataAccessMock.Verify(m => m.UpdateAsync(Is.Value(verify)), Times.Once);
            lockWorker.Verify(m => m.DoLockWorkAsync(updatedLock, CancellationToken.None), Times.Once);
        }

        [Theory(DisplayName = "When other instance holds lock and expiry time is in future, then lock worker returns immediately without asking for immediate callback."), ValidData]
        public async Task VerifyLockByOtherProcess(
            [Frozen] Mock<ILockDataAccess<TestReaderState>> dataAccessMock,
            [Frozen] Mock<IDateFactory> dateFactoryMock,
            Mock<LockWorker<TestReaderState>> lockWorker,
            DateTimeOffset date,
            Lock<TestReaderState> persistedLock)
        {
            lockWorker.Setup(m => m.LockName).Returns("lock");
            lockWorker.Setup(m => m.EnableAcquireLock).Returns(true);

            dateFactoryMock.Setup(m => m.GetCurrentTime()).Returns(date);
            persistedLock.ExpiryTime = date.AddSeconds(5);

            dataAccessMock
                .Setup(m => m.GetAsync(lockWorker.Object.LockName))
                .ReturnsAsync(persistedLock);

            string immediateCallbackRequested = await lockWorker.Object.DoWorkAsync(CancellationToken.None).ConfigureAwait(false);

            dataAccessMock.Verify(m => m.GetAsync(lockWorker.Object.LockName), Times.Once);
            lockWorker.Verify(m => m.DoLockWorkAsync(persistedLock, CancellationToken.None), Times.Never);
            Assert.False(string.IsNullOrEmpty(immediateCallbackRequested));
        }

        [Theory(DisplayName = "When lock update fails due to etag mismatch, then dont throw exception."), ValidData]
        public async Task VerifyLockUpdateFailureDueToEtagMismatch(
            [Frozen] Mock<ILockDataAccess<TestReaderState>> dataAccessMock,
            [Frozen] Mock<IDateFactory> dateFactoryMock,
            Mock<LockWorker<TestReaderState>> lockWorker,
            DateTimeOffset date,
            Lock<TestReaderState> persistedLock)
        {
            dateFactoryMock.Setup(m => m.GetCurrentTime()).Returns(date);

            lockWorker.Setup(m => m.EnableAcquireLock).Returns(true);
            lockWorker
                .SetupGet(m => m.LockName)
                .Returns(persistedLock.Id);
            persistedLock.ExpiryTime = date.AddSeconds(-5);

            dataAccessMock
                .Setup(m => m.GetAsync(lockWorker.Object.LockName))
                .ReturnsAsync(persistedLock);

            dataAccessMock
                .Setup(m => m.UpdateAsync(It.IsAny<Lock<TestReaderState>>()))
                .ThrowsAsync(DocumentClientExceptionModule.Create(HttpStatusCode.PreconditionFailed));

            string immediateCallbackRequested = await lockWorker.Object.DoWorkAsync(CancellationToken.None).ConfigureAwait(false);

            dataAccessMock.Verify(m => m.GetAsync(lockWorker.Object.LockName), Times.Once);
            dataAccessMock.Verify(m => m.CreateAsync(It.IsAny<Lock<TestReaderState>>()), Times.Never);
            dataAccessMock.Verify(m => m.UpdateAsync(It.IsAny<Lock<TestReaderState>>()), Times.Once);
            lockWorker.Verify(m => m.DoLockWorkAsync(persistedLock, CancellationToken.None), Times.Never);
            Assert.False(string.IsNullOrEmpty(immediateCallbackRequested));
        }

        [Theory(DisplayName = "When lock create fails due to conflict, then dont throw exception."), ValidData]
        public async Task VerifyLockCreateFailureDueToConflict(
            [Frozen] Mock<ILockDataAccess<TestReaderState>> dataAccessMock,
            Mock<LockWorker<TestReaderState>> lockWorker,
            Lock<TestReaderState> persistedLock)
        {
            lockWorker.Setup(m => m.LockName).Returns("lock");
            lockWorker.Setup(m => m.EnableAcquireLock).Returns(true);

            dataAccessMock
                .Setup(m => m.GetAsync(lockWorker.Object.LockName))
                .ReturnsAsync((Lock<TestReaderState>)null);

            dataAccessMock
                .Setup(m => m.CreateAsync(It.IsAny<Lock<TestReaderState>>()))
                .ThrowsAsync(DocumentClientExceptionModule.Create(HttpStatusCode.Conflict));

            string immediateCallbackRequested = await lockWorker.Object.DoWorkAsync(CancellationToken.None).ConfigureAwait(false);

            dataAccessMock.Verify(m => m.GetAsync(lockWorker.Object.LockName), Times.Once);
            dataAccessMock.Verify(m => m.CreateAsync(It.IsAny<Lock<TestReaderState>>()), Times.Once);
            dataAccessMock.Verify(m => m.UpdateAsync(It.IsAny<Lock<TestReaderState>>()), Times.Never);
            lockWorker.Verify(m => m.DoLockWorkAsync(persistedLock, CancellationToken.None), Times.Never);
            Assert.False(string.IsNullOrEmpty(immediateCallbackRequested));
        }

        [Theory(DisplayName = "When lock create fails due to internal service error, then catch the exception and log it."), ValidData]
        public async Task VerifyLockCreateFailureDueToInternalError(
            [Frozen] Mock<ILockDataAccess<TestReaderState>> dataAccessMock,
            [Frozen] Mock<ISessionFactory> sessionFactory,
            Mock<ISession> session,
            Mock<LockWorker<TestReaderState>> lockWorker)
        {
            lockWorker.Setup(m => m.LockName).Returns("lock");
            lockWorker.Setup(m => m.EnableAcquireLock).Returns(true);

            var lockName = lockWorker.Object.LockName;
            sessionFactory
                .Setup(m => m.StartSession(lockName + ".DoWorkAsync", SessionType.Incoming))
                .Returns(session.Object);

            dataAccessMock
                .Setup(m => m.GetAsync(lockName))
                .ReturnsAsync((Lock<TestReaderState>)null);

            dataAccessMock
                .Setup(m => m.CreateAsync(It.IsAny<Lock<TestReaderState>>()))
                .ThrowsAsync(DocumentClientExceptionModule.Create(HttpStatusCode.InternalServerError));

            var result = await lockWorker.Object.DoWorkAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.False(string.IsNullOrEmpty(result));

            dataAccessMock.Verify(m => m.GetAsync(lockName), Times.Once);
            dataAccessMock.Verify(m => m.CreateAsync(It.IsAny<Lock<TestReaderState>>()), Times.Once);
            session.Verify(m => m.Done(SessionStatus.Fault, It.IsAny<Exception>()), Times.Once);
        }

        [Theory(DisplayName = "When EnableAcquireLock is false, then lock should not be acquired"), ValidData]
        public async Task VerifyEnableAcquireLock(
            [Frozen] Mock<ILockDataAccess<TestReaderState>> dataAccessMock,
            Mock<LockWorker<TestReaderState>> lockWorker)
        {
            lockWorker.Setup(m => m.LockName).Returns("lock");
            lockWorker.Setup(m => m.EnableAcquireLock).Returns(false);

            string immediateCallbackRequested = await lockWorker.Object.DoWorkAsync(CancellationToken.None).ConfigureAwait(false);

            dataAccessMock.Verify(m => m.GetAsync(lockWorker.Object.LockName), Times.Never);
            lockWorker.Verify(m => m.DoLockWorkAsync(It.IsAny<Lock<TestReaderState>>(), CancellationToken.None), Times.Never);
            Assert.False(string.IsNullOrEmpty(immediateCallbackRequested));
        }

        [Theory(DisplayName = "When DoLockWorkAsync fails, then increment failure count."), ValidData]
        public async Task VerifyFailureCountIncrement(
            [Frozen] Mock<ILockDataAccess<TestReaderState>> dataAccessMock,
            IFixture fixture,
            Mock<LockWorker<TestReaderState>> lockWorker)
        {
            lockWorker.Setup(m => m.LockName).Returns("lock");
            lockWorker.Setup(m => m.EnableAcquireLock).Returns(true);

            var callCount = 0;

            fixture.Customize<Lock<TestReaderState>>(x => x.With(y => y.WorkerId, lockWorker.Object.Id));

            lockWorker.Setup(m => m.DoLockWorkAsync(It.IsAny<Lock<TestReaderState>>(), CancellationToken.None)).ThrowsAsync(new Exception());
            dataAccessMock
                .Setup(m => m.UpdateAsync(It.Is<Lock<TestReaderState>>(x => x.FailureCount == 1))).ReturnsAsync((Lock<TestReaderState>)null)
                .Callback(() => callCount++);

            await lockWorker.Object.DoWorkAsync(CancellationToken.None).ConfigureAwait(false);

            // Assert failure count is read/saved.
            dataAccessMock.Verify(m => m.GetAsync(lockWorker.Object.LockName), Times.Exactly(2));

            Assert.Equal(1, callCount); // Ensure update is called with the correct failure count only once.
        }

        [Theory(DisplayName = "When failure count exceeded, then release the lock."), ValidData]
        public async Task VerifyFailureCountExceeded(
            [Frozen] Lock<TestReaderState> existingLock,
            Mock<LockWorker<TestReaderState>> lockWorker)
        {
            lockWorker.Setup(m => m.LockName).Returns("lock");
            lockWorker.Setup(m => m.EnableAcquireLock).Returns(true);

            existingLock.WorkerId = lockWorker.Object.Id;
            existingLock.FailureCount = lockWorker.Object.LockMaxFailureCountPerInstance;
            
            var result = await lockWorker.Object.DoWorkAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.Equal("WaitingForLock", result);
            Assert.Equal(0, existingLock.FailureCount); // Ensure count is reset.
        }

        #region AutoFixture Custom Attributes
        public class ValidDataAttribute : AutoMoqDataAttribute
        {
            public ValidDataAttribute() : base(false)
            {
                this.Fixture.Customize<Lock<TestReaderState>>(x => x.With(y => y.FailureCount, 0));
            }
        }
        #endregion

        public class TestReaderState
        {
            public string ReaderState { get; set; }
        }
    }
}
