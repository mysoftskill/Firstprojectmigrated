namespace Microsoft.PrivacyServices.AnaheimId.AidFunctions
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.ComplianceServices.Common.IfxMetric;
    using Microsoft.ComplianceServices.AnaheimIdLib.Schema;
    using Microsoft.ComplianceServices.Common.Queues;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// Anaheim id EventHub Function.
    /// </summary>
    public class AidEventHubFunc : IAidFunction
    {
        private const string ComponentName = nameof(AidEventHubFunc);
        private readonly ICloudQueueBase<AnaheimIdRequest> cloudQueuePool;
        private readonly IMetricContainer metricContainer;
        private readonly IAppConfiguration appConfiguration;
        private readonly IRedisClient redisClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="AidEventHubFunc"/> class.
        /// The instance of this class is used to forward EventHub events into Azure Queue.
        /// </summary>
        /// <param name="cloudQueuePool">Anaheim Id Azure Queue.</param>
        /// <param name="metricContainer">Container to hold Metrics.</param>
        /// <param name="appConfiguration">appConfiguration</param>
        /// <param name="redisClient">RedisClient</param>
        public AidEventHubFunc(ICloudQueueBase<AnaheimIdRequest> cloudQueuePool, IMetricContainer metricContainer, IAppConfiguration appConfiguration, IRedisClient redisClient)
        {
            this.cloudQueuePool = cloudQueuePool ?? throw new ArgumentException(nameof(cloudQueuePool));
            this.metricContainer = metricContainer ?? throw new ArgumentException(nameof(metricContainer));
            this.appConfiguration = appConfiguration ?? throw new ArgumentException(nameof(appConfiguration));
            this.redisClient = redisClient ?? throw new ArgumentException(nameof(redisClient));
            this.redisClient.SetDatabaseNumber(RedisDatabaseId.AnaheimIdRequestDedup);
        }

        /// <inheritdoc />
        public async Task RunAsync(AnaheimIdRequest anaheimIdRequest, ILogger logger)
        {
            IApiEvent outgoingApi = new OutgoingApiEventWrapper(this.metricContainer, "AID_AzureQueue", "AID_EventHubToQueue", logger, "Anaheim");

            try
            {
                outgoingApi.Start();
                var requestId = anaheimIdRequest.DeleteDeviceIdRequest.RequestId;

                if (!this.IsDuplicate(requestId.ToString(), DateTime.UtcNow, 1440))
                {
                    // set random timeout for each request so the traffic can flow into PXS then PCF evenly in the next 24 hours
                    int maxVisibilityTimeout = this.appConfiguration.GetConfigValue<int>(ConfigNames.PAF.PAF_AID_AnaheimIdRequestMaxVisibilityTimeoutInMinutes, defaultValue: 60 * 24);

                    // messages will be available for pick-up after invisibilityDelay time
                    var invisibilityDelay = TimeSpan.FromMinutes(RandomHelper.Next(0, maxVisibilityTimeout));

                    await this.cloudQueuePool.EnqueueAsync(anaheimIdRequest, invisibilityDelay: invisibilityDelay).ConfigureAwait(false);

                    logger.Information(ComponentName, $"Enqueued unique request with RequestId={anaheimIdRequest.DeleteDeviceIdRequest.RequestId}, maxVisibilityTimeout={maxVisibilityTimeout}, invisibilityDelay={invisibilityDelay}");
                }

                outgoingApi.Success = true;
            }
            catch (Exception ex)
            {
                outgoingApi.Success = false;
                logger.Error(ComponentName, $"Unhandled Exception: {ex.Message}");
                throw;
            }
            finally
            {
                outgoingApi.Finish();
            }
        }

        private bool IsDuplicate(string requestId, DateTime eventTimeStamp, int timeoutInMintues)
        {
            if (string.IsNullOrEmpty(requestId))
            {
                throw new ArgumentNullException(nameof(requestId));
            }

            var lastSeen = this.redisClient.GetDataTime(requestId);
            if ((lastSeen == default) || (eventTimeStamp - lastSeen.ToUniversalTime() > TimeSpan.FromMinutes(timeoutInMintues)))
            {
                this.redisClient.SetDataTime(requestId, eventTimeStamp, TimeSpan.FromMinutes(timeoutInMintues));
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
