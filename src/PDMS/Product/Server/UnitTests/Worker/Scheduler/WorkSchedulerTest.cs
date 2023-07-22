namespace Microsoft.PrivacyServices.DataManagement.Worker.Scheduler.UnitTest
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.Testing;

    using Moq;

    using Ploeh.AutoFixture.Xunit2;

    using Xunit;

    public class WorkSchedulerTest
    {
        /// <summary>
        /// Default cancellationToken timeout make sure that test does not stop responding if control never
        /// reaches to step setting cancellationTokenSource.Cancel never gets called.
        /// </summary>
        private TimeSpan defaultCancellationTokenTimeout = TimeSpan.FromMilliseconds(5000);

        [Theory(Skip = "azurification todo: figure out how to mock lifetime scope", DisplayName = "When immediate callback is requested, then scheduler does not sleep."), AutoMoqData]
        public async Task VerifyImmediateCallback(
            [Frozen] Mock<IWorker> workerMock,
            WorkScheduler testScheduler)
        {
            int doWorkCallCount = 0;

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(this.defaultCancellationTokenTimeout);
            workerMock
                .Setup(m => m.DoWorkAsync(cancellationTokenSource.Token))
                .Returns(() =>
                {
                    doWorkCallCount++;
                    if (doWorkCallCount == 1)
                    {
                        return Task.FromResult(string.Empty);
                    }

                    cancellationTokenSource.Cancel();
                    return Task.FromResult(string.Empty);
                });

            TimeSpan schedulerInterval = TimeSpan.FromMilliseconds(5000);
            workerMock
                .Setup(m => m.IdleTimeBetweenCallsInMilliseconds)
                .Returns((int)schedulerInterval.TotalMilliseconds);
            
            Stopwatch runDuration = Stopwatch.StartNew();
            await testScheduler.RunAsync(cancellationTokenSource.Token).ConfigureAwait(false);
            runDuration.Stop();

            workerMock.Verify(m => m.DoWorkAsync(cancellationTokenSource.Token), Times.Exactly(2));
            Assert.True(cancellationTokenSource.Token.IsCancellationRequested);
            Assert.True(runDuration.ElapsedMilliseconds < schedulerInterval.TotalMilliseconds);
        }

        [Theory(Skip = "azurification todo: figure out how to mock lifetime scope", DisplayName = "When immediate callback is not requested, then scheduler sleeps for schedulerInterval."), AutoMoqData]
        public async Task VerifySchedulerInterval(
            [Frozen] Mock<IWorker> workerMock,
            WorkScheduler testScheduler)
        {
            int doWorkCallCount = 0;

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(this.defaultCancellationTokenTimeout);
            workerMock
                .Setup(m => m.DoWorkAsync(cancellationTokenSource.Token))
                .Returns(() =>
                {
                    doWorkCallCount++;
                    if (doWorkCallCount == 1)
                    {
                        return Task.FromResult("Backoff");
                    }

                    cancellationTokenSource.Cancel();
                    return Task.FromResult(string.Empty);
                });

            TimeSpan schedulerInterval = TimeSpan.FromMilliseconds(50);
            workerMock
                .Setup(m => m.IdleTimeBetweenCallsInMilliseconds)
                .Returns((int)schedulerInterval.TotalMilliseconds);

            Stopwatch runDuration = Stopwatch.StartNew();
            await testScheduler.RunAsync(cancellationTokenSource.Token).ConfigureAwait(false);
            runDuration.Stop();

            workerMock.Verify(m => m.DoWorkAsync(cancellationTokenSource.Token), Times.Exactly(2));
            Assert.True(cancellationTokenSource.Token.IsCancellationRequested);
            Assert.True(runDuration.ElapsedMilliseconds >= schedulerInterval.TotalMilliseconds);
        }

        [Theory(Skip = "azurification todo: figure out how to mock lifetime scope", DisplayName = "When exception is thrown, stop the loop."), AutoMoqData]
        public async Task VerifyExceptionIsHandled(
            [Frozen] Mock<IWorker> workerMock,
            WorkScheduler testScheduler)
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(20000);

            workerMock
                .Setup(m => m.DoWorkAsync(cancellationTokenSource.Token))
                .Returns(() =>
                {
                    return Task.Run((Func<string>)(() =>
                    {
                        throw new Exception("Test Exception");
                    }));
                });

            workerMock
                .Setup(m => m.IdleTimeBetweenCallsInMilliseconds)
                .Returns(0);

            await Assert.ThrowsAsync<Exception>(() => testScheduler.RunAsync(cancellationTokenSource.Token)).ConfigureAwait(false);
            
            workerMock.Verify(m => m.DoWorkAsync(cancellationTokenSource.Token), Times.Once);
            Assert.False(cancellationTokenSource.Token.IsCancellationRequested);
        }
    }
}
