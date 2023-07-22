namespace Microsoft.PrivacyServices.CommandFeed.Client.Test
{
    using System;

    using Microsoft.PrivacyServices.CommandFeed.Client.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ExponentialBackoffTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ThrowsExcpetionWithNullDelay()
        {
            // Act
            new ExponentialBackoff(delay: null, TimeSpan.FromSeconds(10)); ;
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ThrowsExcpetionWithNullMaxDelay()
        {
            // Act
            new ExponentialBackoff(delay: TimeSpan.FromSeconds(2), maxDelay: null); ;
        }

        [TestMethod]
        public void ReturnsExpectedDelays()
        {
            // Arrange
            TimeSpan delay = TimeSpan.FromSeconds(1);
            TimeSpan maxDelay = TimeSpan.FromSeconds(20);
            var backoff = new ExponentialBackoff(delay, maxDelay);

            var expectedDelays = new[] { TimeSpan.FromSeconds(0.5).Ticks, TimeSpan.FromSeconds(1.5).Ticks, TimeSpan.FromSeconds(3.5).Ticks, TimeSpan.FromSeconds(7.5).Ticks, TimeSpan.FromSeconds(15.5).Ticks };

            // Act
            // Should return exponential delays if the result delay < max delay
            for (int i = 0; i < 5; i++)
            {
               var resultDelay = backoff.Delay();

                // Assert
                Assert.AreEqual(expectedDelays[i], resultDelay.Ticks);
            }

            // Should only return max delay if the result delay > max delay
            for (int i = 0; i < 2; i++)
            {
                var resultDelay = backoff.Delay();

                // Assert
                Assert.AreEqual(maxDelay.Ticks, resultDelay.Ticks);
            }
        }

        [TestMethod]
        public void ResetsRetryAndStartover()
        {
            // Arrange
            TimeSpan delay = TimeSpan.FromSeconds(1);
            TimeSpan maxDelay = TimeSpan.FromSeconds(20);
            var backoff = new ExponentialBackoff(delay, maxDelay);

            var expectedDelays = new[] { TimeSpan.FromSeconds(0.5).Ticks, TimeSpan.FromSeconds(1.5).Ticks, TimeSpan.FromSeconds(3.5).Ticks, TimeSpan.FromSeconds(7.5).Ticks, TimeSpan.FromSeconds(15.5).Ticks };

            // Should return exponential delays
            for (int i = 0; i < 5; i++)
            {
                var resultDelay = backoff.Delay();

                // Assert
                Assert.AreEqual(expectedDelays[i], resultDelay.Ticks);
            }

            // Act
            backoff.Reset();

            // Should return exponential delays
            for (int i = 0; i < 5; i++)
            {
                var resultDelay = backoff.Delay();

                // Assert
                Assert.AreEqual(expectedDelays[i], resultDelay.Ticks);
            }
        }
    }
}
