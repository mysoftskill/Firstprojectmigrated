namespace PCF.UnitTests
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.Helpers;
    using Xunit;

    [Trait("Category", "UnitTest")]
    public class ExponentialBackoffTests
    {
        [Fact]
        public void HandleNullArgumentException()
        {
            Assert.Throws<ArgumentNullException>(() => new ExponentialBackoff(delay: null, maxDelay: TimeSpan.FromSeconds(5)));
        }

        [Fact]
        public Task DelayWithExponentialBackoffTime()
        {
            ExponentialBackoff exponentialBackoff = new ExponentialBackoff(delay: TimeSpan.FromSeconds(2), maxDelay: TimeSpan.FromSeconds(5));

            var firstWatch = Stopwatch.StartNew();
            var firstDelay = exponentialBackoff.Delay().ContinueWith(_ =>
            {
                firstWatch.Stop();
                return firstWatch.ElapsedMilliseconds;
            });

            // Expect to see about 1 second delay
            var firstDelaySeconds = firstDelay.Result / 1000;
            Assert.True(firstDelaySeconds >= 0 && firstDelaySeconds <= 1);

            var secondWatch = Stopwatch.StartNew();
            var secondDelay = exponentialBackoff.Delay().ContinueWith(_ =>
            {
                secondWatch.Stop();
                return secondWatch.ElapsedMilliseconds;
            });

            
            var secondDelaySeconds = secondDelay.Result / 1000;
            // Expect to see about 2 seconds delay
            Assert.True(secondDelaySeconds >= 2 && secondDelaySeconds <= 3);
            return Task.CompletedTask;
        }

        [Fact]
        public Task DelayWithReset()
        {
            ExponentialBackoff exponentialBackoff = new ExponentialBackoff(delay: TimeSpan.FromSeconds(2), maxDelay: TimeSpan.FromSeconds(5));

            var firstWatch = Stopwatch.StartNew();
            var firstDelay = exponentialBackoff.Delay().ContinueWith(_ =>
            {
                firstWatch.Stop();
                return firstWatch.ElapsedMilliseconds;
            });

            // Expect to see about 1 second delay
            var firstDelaySeconds = firstDelay.Result / 1000.0;
            Assert.True(firstDelaySeconds > 0 && firstDelaySeconds <= 1.9);

            // reset
            exponentialBackoff.Reset();

            var secondWatch = Stopwatch.StartNew();
            var secondDelay = exponentialBackoff.Delay().ContinueWith(_ =>
            {
                secondWatch.Stop();
                return secondWatch.ElapsedMilliseconds;
            });

            // Expect to see about 1 second delay
            var secondDelaySeconds = secondDelay.Result / 1000.0;
            Assert.True(secondDelaySeconds > 0 && secondDelaySeconds <= 1.9);
            return Task.CompletedTask;
        }
    }
}
