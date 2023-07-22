namespace Microsoft.PrivacyServices.CommandFeed.Service.Autoscaler
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.ConfigGen;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// Contains logic to autoscale a single collection in a single account.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
    public class ScaledCollection
    {
        private static readonly object LogLock = new object();

        private const int MaxCollectionThroughput = 1000000;
        private const int MinScalingDelaySeconds = 5*60; // 5 minutes

        private readonly ISimpleCosmosDbCollectionClient collectionClient;
        private readonly ICosmosDbAutoscalerConfig config;
        private readonly IRedisClient redisClient;

        public ScaledCollection(ISimpleCosmosDbCollectionClient collectionClient, IRedisClient redisClient, ICosmosDbAutoscalerConfig config)
        {
            this.collectionClient = collectionClient;
            this.redisClient = redisClient;
            this.config = config;
        }

        private string Name => this.collectionClient.FriendlyName;

        private string LogComponentName => $"{nameof(ScaledCollection)}-{Name}";

        /// <summary>
        /// Continually scale the collection.
        /// </summary>
        public async Task ContinuallyScaleAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                TimeSpan delay = TimeSpan.FromSeconds(RandomHelper.Next(MinScalingDelaySeconds, 2 * MinScalingDelaySeconds));
                await Task.Delay(delay);

                List<string> logs = new List<string>();

                try
                {
                    await this.ScaleCollectionAsync(logs);
                }
                catch (Exception ex)
                {
                    logs.Add($"Error scaling collection {this.Name}. {ex}");
                }

                // Keep logs together in output.
                lock (LogLock)
                {
                    DualLogger.Instance.Information(LogComponentName, $"Examining collection {this.Name}");

                    foreach (var line in logs)
                    {
                        DualLogger.Instance.Information(LogComponentName, line);
                    }
                }
            }
        }

        /// <summary>
        /// Performs one invocation of the autoscaler on this collection.
        /// </summary>
        public async Task ScaleCollectionAsync(List<string> logs)
        {
            if (!this.config.Enabled || FlightingUtilities.IsStringValueEnabled(FlightingNames.AutoScalerDisabled, this.Name.ToUpperInvariant()))
            {
                // Bail if not explicitly enabled.
                logs.Add($"Unable to process '{this.Name}' due to flight or disabled status in config.");
                return;
            }

            // Use Redis cache to make sure we do not perform auto scaling on the same collection more frequent than MinScalingDelaySeconds
            DateTime now = DateTime.UtcNow;
            var lastExecutionTime = this.redisClient.GetDataTime(this.Name);
            if ((lastExecutionTime == default) || (now - lastExecutionTime.ToUniversalTime() > TimeSpan.FromSeconds(MinScalingDelaySeconds)))
            {
                redisClient.SetDataTime(this.Name, now, TimeSpan.FromSeconds(MinScalingDelaySeconds));
            }
            else
            {
                logs.Add($"Min interval for '{this.Name}' has not been reached yet, last auto scale time was {lastExecutionTime}. Skipping.");
                return;
            }

            DateTimeOffset endTime = NowToNearestMinute();

            // Summary contains stats for two different time ranges. The first one called 'recent'
            // contains recent data. We use this to make adjustments upwards. The second called 'older'
            // contains several more minutes of data. This is used for adjusting downwards. This allows
            // us to be conservative when decreasing while still reacting quickly while increasing.
            ThrottleSummary summary = await this.collectionClient.GetThrottleStatsAsync(endTime);

            int currentProvisioning = await this.collectionClient.GetCurrentThroughputAsync();
            int partitionCount = await this.collectionClient.GetPartitionCountAsync();

            PerfCounterUtility.GetOrCreate(PerformanceCounterType.Number, "TotalRequests").Set(this.Name, (int)summary.TotalRecentRequests);
            PerfCounterUtility.GetOrCreate(PerformanceCounterType.Number, "ThrottledRequests").Set(this.Name, (int)summary.TotalRecentThrottledRequests);

            logs.Add("Stats Summary:");
            logs.Add($"DB: {this.Name}");
            logs.Add($"Total Recent Requests: {summary.TotalRecentRequests}");
            logs.Add($"Total Recent Throttled Requests: {summary.TotalRecentThrottledRequests}");
            logs.Add($"Recent Success Rate: {summary.RecentSuccessRate}");
            logs.Add($"Total Older Requests: {summary.TotalOlderRequests}");
            logs.Add($"Total Older Throttled Requests: {summary.TotalOlderThrottledRequests}");
            logs.Add($"Older Success Rate: {summary.OlderSuccessRate}");
            logs.Add($"Partition Count: {partitionCount}");
            logs.Add($"Current Provisioning: {currentProvisioning}");

            int adjustedThroughput = currentProvisioning;

            if (summary.RecentSuccessRate < this.config.TargetSuccessRate)
            {
                // try to incresae RUs within reason.
                double maxRus = currentProvisioning * (1 + this.config.MaxIncrementalIncreasePercent);
                double percentScaledIncrease = currentProvisioning * this.config.TargetSuccessRate / summary.RecentSuccessRate;

                adjustedThroughput = (int)Math.Min(percentScaledIncrease, maxRus);

                // round up to next 100.
                adjustedThroughput = ((adjustedThroughput / 100) * 100) + 100;

                // Always increase by at least this amount of RU when we are below goal. This keeps us from creeping slowly up to the goal.
                adjustedThroughput = Math.Max(currentProvisioning + this.config.MinRuIncrease, adjustedThroughput);

                logs.Add($"OSR ({summary.RecentSuccessRate}) < target ({this.config.TargetSuccessRate}); Increasing to {adjustedThroughput} = MIN({maxRus}, {percentScaledIncrease})");
            }
            else if (summary.OlderSuccessRate > this.config.TargetSuccessRate)
            {
                // decrease a bit.
                double minRus = currentProvisioning * (1 - this.config.MaxIncrementalDecreasePercent);
                double percentScaledDecrease = currentProvisioning * this.config.TargetSuccessRate / summary.OlderSuccessRate;

                if (summary.TotalOlderThrottledRequests == 0)
                {
                    // if we didn't get any throttles, then back off a bit more aggressively.
                    adjustedThroughput = (int)minRus;
                }
                else
                {
                    adjustedThroughput = (int)Math.Max(minRus, percentScaledDecrease);
                }

                // round down to next 100.
                adjustedThroughput = (adjustedThroughput / 100) * 100;

                logs.Add($"OSR ({summary.OlderSuccessRate}) > target ({this.config.TargetSuccessRate}); Decreasing to {adjustedThroughput} = MAX({minRus}, {percentScaledDecrease})");
            }
            else
            {
                logs.Add("No RU adjustment necessary.");
            }

            PerfCounterUtility.GetOrCreate(PerformanceCounterType.Number, "ProvisionedThroughput").Set(this.Name, adjustedThroughput);

            if (adjustedThroughput != currentProvisioning)
            {
                // Clamp throughput between 100 and 10K rus / partition, and 500 RUs overall.
                adjustedThroughput = Math.Min(10000 * partitionCount, Math.Max(500, Math.Max(100 * partitionCount, adjustedThroughput)));

                // Further clamp throughput to the max collection throughput RUs
                adjustedThroughput = Math.Min(MaxCollectionThroughput, adjustedThroughput);

                await this.collectionClient.ReplaceThroughputAsync(adjustedThroughput);
            }
        }

        private static DateTimeOffset NowToNearestMinute()
        {
            return DateTimeOffset.FromUnixTimeSeconds((DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 60) * 60);
        }
    }
}
