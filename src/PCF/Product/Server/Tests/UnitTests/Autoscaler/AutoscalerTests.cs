namespace PCF.UnitTests
{
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Autoscaler;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.ConfigGen;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Xunit;

    [Trait("Category", "UnitTest")]
    public class AutoscalerTests : INeedDataBuilders
    {
        /// <summary>
        /// Tests sliding scale incremental increase logic.
        /// </summary>
        [Fact]
        public async Task IncrementalIncreases()
        {
            var mock = this.AMockOf<ISimpleCosmosDbCollectionClient>();
            var configMock = this.AMockOf<ICosmosDbAutoscalerConfig>();

            // Configure to remove clamping logic so we can test the scaling behavior.
            configMock.SetupGet(m => m.MaxIncrementalDecreasePercent).Returns(1);
            configMock.SetupGet(m => m.MaxIncrementalIncreasePercent).Returns(1000);
            configMock.SetupGet(m => m.MinRuIncrease).Returns(0);
            configMock.SetupGet(m => m.TargetSuccessRate).Returns(.99);
            configMock.SetupGet(m => m.Enabled).Returns(true);

            ThrottleSummary summary = new ThrottleSummary
            {
                TotalRecentRequests = 1000,
                TotalRecentThrottledRequests = 500,
                TotalOlderRequests = 1,
                TotalOlderThrottledRequests = 0,
            };

            int updatedThroughput = -1;

            mock.Setup(m => m.FriendlyName).Returns("collection");
            mock.Setup(m => m.GetCurrentThroughputAsync()).ReturnsAsync(1000);
            mock.Setup(m => m.GetPartitionCountAsync()).ReturnsAsync(10);
            mock.Setup(m => m.ReplaceThroughputAsync(It.IsAny<int>()))
                .Callback<int>(i => updatedThroughput = i)
                .Returns(Task.FromResult(true));

            mock.Setup(m => m.GetThrottleStatsAsync(It.IsAny<DateTimeOffset>()))
                .ReturnsAsync(summary);

            // A no-op redis client to bypass the throttling logic
            var redisMock = this.AMockOf<IRedisClient>();
            redisMock.Setup(m => m.GetDataTime(It.IsAny<string>())).Returns(default(DateTime));
            redisMock.Setup(m => m.SetDataTime(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<TimeSpan>())).Returns(true);

            using (new FlightDisabled(FlightingNames.AutoScalerDisabled))
            {
                ScaledCollection collection = new ScaledCollection(mock.Object, redisMock.Object, configMock.Object);

                // Observed success rate is at 50%. It wants 99%, and current throughput is 1000.
                // Therefore, it should adjust RUs upward according to
                // 1000 * .99 / .9 = 1980, which rounds up to 2000.
                await collection.ScaleCollectionAsync(new List<string>());

                Assert.Equal(2000, updatedThroughput);
                updatedThroughput = -1;

                // Observed success rate is now 90%. By the same formula, it should adjust upwards to 1100. Due to rounding behavior, we always round up to the next
                // 100, so we should expect 1200 here.
                summary.TotalRecentThrottledRequests = 100;
                await collection.ScaleCollectionAsync(new List<string>());
                Assert.Equal(1200, updatedThroughput);
                updatedThroughput = -1;

                summary.TotalRecentThrottledRequests = 900;
                await collection.ScaleCollectionAsync(new List<string>());
                Assert.Equal(10000, updatedThroughput);
            }
        }

        /// <summary>
        /// Tests that clamps work when the sliding scale logic wants to increase too much.
        /// </summary>
        [Fact]
        public async Task ClampedIncreases()
        {
            var mock = this.AMockOf<ISimpleCosmosDbCollectionClient>();
            var configMock = this.AMockOf<ICosmosDbAutoscalerConfig>();

            // Configure to remove clamping logic so we can test the scaling behavior.
            configMock.SetupGet(m => m.MaxIncrementalDecreasePercent).Returns(.05);
            configMock.SetupGet(m => m.MaxIncrementalIncreasePercent).Returns(.5);
            configMock.SetupGet(m => m.MinRuIncrease).Returns(0);
            configMock.SetupGet(m => m.TargetSuccessRate).Returns(.99);
            configMock.SetupGet(m => m.Enabled).Returns(true);

            ThrottleSummary summary = new ThrottleSummary
            {
                TotalRecentRequests = 1000,
                TotalRecentThrottledRequests = 500,
                TotalOlderRequests = 1,
                TotalOlderThrottledRequests = 0,
            };

            int updatedThroughput = -1;

            mock.Setup(m => m.FriendlyName).Returns("collection");
            mock.Setup(m => m.GetCurrentThroughputAsync()).ReturnsAsync(1000);
            mock.Setup(m => m.GetPartitionCountAsync()).ReturnsAsync(10);
            mock.Setup(m => m.ReplaceThroughputAsync(It.IsAny<int>()))
                .Callback<int>(i => updatedThroughput = i)
                .Returns(Task.FromResult(true));

            mock.Setup(m => m.GetThrottleStatsAsync(It.IsAny<DateTimeOffset>()))
                .ReturnsAsync(summary);

            using (new FlightDisabled(FlightingNames.AutoScalerDisabled))
            {
                ScaledCollection collection = new ScaledCollection(mock.Object, new InMemoryRedisClient(), configMock.Object);

                // At 50% throttles, the sliding scale increase will want to rougly double RUs.
                // However, our clamp allows a 50% increase at most in one step. Therefore, we should
                // expect to see this come in at 1600 (1500 + 100 for rounding).
                await collection.ScaleCollectionAsync(new List<string>());

                Assert.Equal(1600, updatedThroughput);
                updatedThroughput = -1;
            }
        }

        [Fact]
        public async Task SinglePartitionCannotIncreasePastTenThousand()
        {
            var mock = this.AMockOf<ISimpleCosmosDbCollectionClient>();
            var configMock = this.AMockOf<ICosmosDbAutoscalerConfig>();

            // Configure to remove clamping logic so we can test the scaling behavior.
            configMock.SetupGet(m => m.MaxIncrementalDecreasePercent).Returns(1);
            configMock.SetupGet(m => m.MaxIncrementalIncreasePercent).Returns(1000);
            configMock.SetupGet(m => m.MinRuIncrease).Returns(0);
            configMock.SetupGet(m => m.TargetSuccessRate).Returns(.99);
            configMock.SetupGet(m => m.Enabled).Returns(true);

            ThrottleSummary summary = new ThrottleSummary
            {
                TotalRecentRequests = 1000,
                TotalRecentThrottledRequests = 999,
                TotalOlderRequests = 1,
                TotalOlderThrottledRequests = 0,
            };

            int updatedThroughput = -1;

            mock.Setup(m => m.FriendlyName).Returns("collection");
            mock.Setup(m => m.GetCurrentThroughputAsync()).ReturnsAsync(1000);
            mock.Setup(m => m.GetPartitionCountAsync()).ReturnsAsync(1);
            mock.Setup(m => m.ReplaceThroughputAsync(It.IsAny<int>()))
                .Callback<int>(i => updatedThroughput = i)
                .Returns(Task.FromResult(true));

            mock.Setup(m => m.GetThrottleStatsAsync(It.IsAny<DateTimeOffset>()))
                .ReturnsAsync(summary);

            using (new FlightDisabled(FlightingNames.AutoScalerDisabled))
            {
                ScaledCollection collection = new ScaledCollection(mock.Object, new InMemoryRedisClient(), configMock.Object);

                // This will want to increase to 99,000,000 RUs. Let's make sure we stay at 10,000.
                await collection.ScaleCollectionAsync(new List<string>());
                Assert.Equal(10000, updatedThroughput);
            }
        }

        /// <summary>
        /// Tests sliding scale incremental decrease logic.
        /// </summary>
        [Fact]
        public async Task IncrementalDecreases()
        {
            var mock = this.AMockOf<ISimpleCosmosDbCollectionClient>();
            var configMock = this.AMockOf<ICosmosDbAutoscalerConfig>();

            // Configure to remove clamping logic so we can test the scaling behavior.
            configMock.SetupGet(m => m.MaxIncrementalDecreasePercent).Returns(1);
            configMock.SetupGet(m => m.MaxIncrementalIncreasePercent).Returns(1000);
            configMock.SetupGet(m => m.MinRuIncrease).Returns(0);
            configMock.SetupGet(m => m.TargetSuccessRate).Returns(.99);
            configMock.SetupGet(m => m.Enabled).Returns(true);

            ThrottleSummary summary = new ThrottleSummary
            {
                TotalRecentRequests = 1,
                TotalRecentThrottledRequests = 0,
                TotalOlderRequests = 1000,
                TotalOlderThrottledRequests = 1,
            };

            int updatedThroughput = -1;

            mock.Setup(m => m.FriendlyName).Returns("collection");
            mock.Setup(m => m.GetCurrentThroughputAsync()).ReturnsAsync(100000);
            mock.Setup(m => m.GetPartitionCountAsync()).ReturnsAsync(10);
            mock.Setup(m => m.ReplaceThroughputAsync(It.IsAny<int>()))
                .Callback<int>(i => updatedThroughput = i)
                .Returns(Task.FromResult(true));

            mock.Setup(m => m.GetThrottleStatsAsync(It.IsAny<DateTimeOffset>()))
                .ReturnsAsync(summary);

            using (new FlightDisabled(FlightingNames.AutoScalerDisabled))
            {
                ScaledCollection collection = new ScaledCollection(mock.Object, new InMemoryRedisClient(), configMock.Object);

                // Observed success rate is at 99.9%. It wants 99%, and current throughput is 100k.
                // Therefore, it should adjust RUs upward according to
                // 100000 * .99 / .999 = 99099, which rounds down to 99000.
                await collection.ScaleCollectionAsync(new List<string>());
                Assert.Equal(99000, updatedThroughput);
            }
        }

        /// <summary>
        /// Tests that there are guardrails on the sliding scale decrease.
        /// </summary>
        [Fact]
        public async Task ClampedDecreases()
        {
            var mock = this.AMockOf<ISimpleCosmosDbCollectionClient>();
            var configMock = this.AMockOf<ICosmosDbAutoscalerConfig>();

            configMock.SetupGet(m => m.MaxIncrementalDecreasePercent).Returns(.05);
            configMock.SetupGet(m => m.MaxIncrementalIncreasePercent).Returns(.50);
            configMock.SetupGet(m => m.MinRuIncrease).Returns(0);
            configMock.SetupGet(m => m.TargetSuccessRate).Returns(.50);
            configMock.SetupGet(m => m.Enabled).Returns(true);

            ThrottleSummary summary = new ThrottleSummary
            {
                TotalRecentRequests = 1,
                TotalRecentThrottledRequests = 0,
                TotalOlderRequests = 1000,
                TotalOlderThrottledRequests = 1,
            };

            int updatedThroughput = -1;

            mock.Setup(m => m.FriendlyName).Returns("collection");
            mock.Setup(m => m.GetCurrentThroughputAsync()).ReturnsAsync(100000);
            mock.Setup(m => m.GetPartitionCountAsync()).ReturnsAsync(10);
            mock.Setup(m => m.ReplaceThroughputAsync(It.IsAny<int>()))
                .Callback<int>(i => updatedThroughput = i)
                .Returns(Task.FromResult(true));

            mock.Setup(m => m.GetThrottleStatsAsync(It.IsAny<DateTimeOffset>()))
                .ReturnsAsync(summary);

            using (new FlightDisabled(FlightingNames.AutoScalerDisabled))
            {
                ScaledCollection collection = new ScaledCollection(mock.Object, new InMemoryRedisClient(), configMock.Object);

                // Target is 50%, observed is 99.9%, so it will want to cut RUs roughly in half.
                // However, clamp logic will only allow 5% reduction, so we'll end up with 100k * .95 = 95000
                await collection.ScaleCollectionAsync(new List<string>());
                Assert.Equal(95000, updatedThroughput);
            }
        }
        
        /// <summary>
        /// Tests that the behavior when there are no throttles observed correctly adjusts the 
        /// throughput to be on the order of what is being consumed.
        /// </summary>
        [Fact]
        public async Task NoThrottleBackoff()
        {
            var mock = this.AMockOf<ISimpleCosmosDbCollectionClient>();
            var configMock = this.AMockOf<ICosmosDbAutoscalerConfig>();

            configMock.SetupGet(m => m.MaxIncrementalDecreasePercent).Returns(.05);
            configMock.SetupGet(m => m.MaxIncrementalIncreasePercent).Returns(.50);
            configMock.SetupGet(m => m.MinRuIncrease).Returns(0);
            configMock.SetupGet(m => m.TargetSuccessRate).Returns(.99);
            configMock.SetupGet(m => m.Enabled).Returns(true);

            ThrottleSummary summary = new ThrottleSummary
            {
                TotalRecentRequests = 0,
                TotalRecentThrottledRequests = 0,
                TotalOlderRequests = 1000,
                TotalOlderThrottledRequests = 0,
            };

            int updatedThroughput = -1;

            mock.Setup(m => m.FriendlyName).Returns("collection");
            mock.Setup(m => m.GetCurrentThroughputAsync()).ReturnsAsync(100000);
            mock.Setup(m => m.GetPartitionCountAsync()).ReturnsAsync(10);
            mock.Setup(m => m.ReplaceThroughputAsync(It.IsAny<int>()))
                .Callback<int>(i => updatedThroughput = i)
                .Returns(Task.FromResult(true));

            mock.Setup(m => m.GetThrottleStatsAsync(It.IsAny<DateTimeOffset>()))
                .ReturnsAsync(summary);

            using (new FlightDisabled(FlightingNames.AutoScalerDisabled))
            {
                ScaledCollection collection = new ScaledCollection(mock.Object, new InMemoryRedisClient(), configMock.Object);

                // No throttled observed. Our config suggests that we aim for a success rate of 99%,
                // which would normally equate to dropping RUs by 1%. However, there are no throttles
                // in this case, so we should drop RUs by the 'MaxIncrementalDecreasePercent' property
                // from config, which, in this case, will leave us with 95000 RUs.
                await collection.ScaleCollectionAsync(new List<string>());
                Assert.Equal(95000, updatedThroughput);
            }
        }

        // Doing auto scale on the same collection more than once within the min interval should be throttled
        [Fact]
        public async Task AutoScalerThrottling()
        {
            var configMock = this.AMockOf<ICosmosDbAutoscalerConfig>();
            configMock.SetupGet(m => m.Enabled).Returns(true);

            var mock = this.AMockOf<ISimpleCosmosDbCollectionClient>();
            mock.Setup(m => m.FriendlyName).Returns("collection");
            mock.Setup(m => m.GetCurrentThroughputAsync()).ReturnsAsync(100000);
            mock.Setup(m => m.GetPartitionCountAsync()).ReturnsAsync(10);
            mock.Setup(m => m.ReplaceThroughputAsync(It.IsAny<int>())).Returns(Task.FromResult(true));
            mock.Setup(m => m.GetThrottleStatsAsync(It.IsAny<DateTimeOffset>())).ReturnsAsync(new ThrottleSummary());

            using (new FlightDisabled(FlightingNames.AutoScalerDisabled))
            {
                ScaledCollection collection = new ScaledCollection(mock.Object, new InMemoryRedisClient(), configMock.Object);

                await collection.ScaleCollectionAsync(new List<string>());

                // Second call should be throttled
                await collection.ScaleCollectionAsync(new List<string>());

                mock.Verify(m => m.GetThrottleStatsAsync(It.IsAny<DateTimeOffset>()), Times.Once);
            }
        }

        [Fact]
        public void ThrottleStatsTests()
        {
            ThrottleSummary summary = new ThrottleSummary
            {
                TotalOlderRequests = 1000,
                TotalOlderThrottledRequests = 100,

                TotalRecentRequests = 100,
                TotalRecentThrottledRequests = 90,
            };

            Assert.Equal(.9, summary.OlderSuccessRate);
            Assert.Equal(.1, summary.RecentSuccessRate);
        }
    }
}
