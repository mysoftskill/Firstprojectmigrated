namespace Microsoft.PrivacyServices.AnaheimId.AidFunctions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using global::Azure.Core;
    using global::Azure.Storage.Queues;
    using global::Azure.Storage.Queues.Models;
    using Microsoft.Azure.ComplianceServices.Common.IfxMetric;
    using Microsoft.ComplianceServices.AnaheimIdLib.Schema;
    using Microsoft.ComplianceServices.Common.Queues;
    using Microsoft.PrivacyServices.AnaheimId.Config;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// Anaheim Id Telemetry function.
    /// </summary>
    public class AidTelemetryFunc
    {
        private const string ComponentName = nameof(AidTelemetryFunc);
        private readonly IMetricContainer metricContainer;
        private readonly IList<IAidQueueMonitoringClient> aidQueueMonitoringClientList;

        /// <summary>
        /// Initializes a new instance of the <see cref="AidTelemetryFunc"/> class.
        /// </summary>
        /// <param name="aidQueueMonitoringClientList">List of Aid Queue Monitoring Clients.</param>
        /// <param name="metricContainer">Metric container.</param>
        public AidTelemetryFunc(IList<IAidQueueMonitoringClient> aidQueueMonitoringClientList, IMetricContainer metricContainer)
        {
            this.aidQueueMonitoringClientList = aidQueueMonitoringClientList ?? throw new ArgumentNullException(nameof(aidQueueMonitoringClientList));
            this.metricContainer = metricContainer ?? throw new ArgumentNullException(nameof(metricContainer));
        }

        /// <summary>
        /// Run async.
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task RunAsync(ILogger logger)
        {
            await Task.Yield();

            if (this.aidQueueMonitoringClientList.Count == 0)
            {
                throw new ArgumentException(ComponentName, $"{nameof(this.aidQueueMonitoringClientList)} empty");
            }

            foreach (var aidQueueMonitoringClient in this.aidQueueMonitoringClientList)
            {
                string queueName = aidQueueMonitoringClient.GetQueueName();
                string accountName = aidQueueMonitoringClient.GetStorageAccountName();

                if (string.IsNullOrEmpty(accountName))
                {
                    throw new ArgumentException(ComponentName, $"{nameof(QueueAccountInfo.StorageAccountName)} not found");
                }

                if (string.IsNullOrEmpty(queueName))
                {
                    throw new ArgumentException(ComponentName, $"{nameof(QueueAccountInfo.QueueName)} not found");
                }

                IApiEvent outgoingApi = new OutgoingApiEventWrapper(this.metricContainer, ComponentName, "GetQueueDepth", logger, $"{accountName}.{queueName}");
                try
                {
                    // Start API Event
                    outgoingApi.Start();

                    logger.Verbose(ComponentName, $"{nameof(this.RunAsync)}: StorageAccountName={accountName};QueueName={queueName}");
                    int approximateMessagesCount = await aidQueueMonitoringClient.GetQueueSizeAsync().ConfigureAwait(false);
                    logger.Information(ComponentName, $"{nameof(this.RunAsync)}: StorageAccountName={accountName};QueueName={queueName};ApproximateMessagesCount={approximateMessagesCount}");
                    outgoingApi.Success = true;

                    string[] dimVal = new string[2] { accountName, queueName };
                    string key = "PAF.FunctionAnaheimQueueDepth";
                    if (this.metricContainer.CustomMetricDictionary.TryGetValue(key, out var metric))
                    {
                        metric.SetUInt64Metric((uint)approximateMessagesCount, dimVal);
                    }
                    else
                    {
                        throw new ArgumentException(ComponentName, $"{nameof(this.RunAsync)}: CustomMetricDictionary does not contain metric with key {key}");
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ComponentName, $"{nameof(this.RunAsync)}: Error running telemetry function: {ex.Message}");
                    outgoingApi.Success = false;
                }
                finally
                {
                    // Finish API event
                    outgoingApi.Finish();
                }
            }
        }
    }
}
