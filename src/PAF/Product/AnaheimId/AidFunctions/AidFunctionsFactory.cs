namespace Microsoft.PrivacyServices.AnaheimId.AidFunctions
{
    using System;
    using System.Collections.Generic;
    using global::Azure.Core;
    using global::Azure.Identity;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.ComplianceServices.Common.IfxMetric;
    using Microsoft.ComplianceServices.AnaheimIdLib.Schema;
    using Microsoft.ComplianceServices.Common.Queues;
    using Microsoft.PrivacyServices.AnaheimId.Avro;
    using Microsoft.PrivacyServices.AnaheimId.Config;
    using Microsoft.PrivacyServices.AnaheimId.Icm;

    /// <summary>
    /// AidFunctionsFactory.
    /// </summary>
    public class AidFunctionsFactory : IAidFunctionsFactory
    {
        private static TokenCredential credentials = null;
        private readonly AidMockFunc aidMockFunc;
        private readonly IAidFunction aidEventHubFunc;
        private readonly AidTelemetryFunc aidTelemetryFunc;
        private readonly IAidBlobStorageFunc aidBlobStorageFunc;

        /// <summary>
        /// Initializes a new instance of the <see cref="AidFunctionsFactory"/> class.
        /// </summary>
        /// <param name="config">AID Config.</param>
        /// <param name="metricContainer">Container to hold Metrics.</param>
        /// <param name="missingRequestFileHelper">Helper for collecting request ids from a file.</param>
        /// <param name="missingSignalIcmConnector">ICM connector for missing signal alerts.</param>
        /// <param name="appConfiguration">appConfiguration</param>
        /// <param name="redisClient">RedisClient</param>
        public AidFunctionsFactory(
            IAidConfig config,
            IMetricContainer metricContainer,
            IMissingRequestFileHelper missingRequestFileHelper,
            IMissingSignalIcmConnector missingSignalIcmConnector,
            IAppConfiguration appConfiguration,
            IRedisClient redisClient)
        {
            if (config == null)
            {
                throw new ArgumentException(nameof(config));
            }

            if (metricContainer == null)
            {
                throw new ArgumentException(nameof(metricContainer));
            }

            if (missingRequestFileHelper == null)
            {
                throw new ArgumentException(nameof(missingRequestFileHelper));
            }

            if (missingSignalIcmConnector == null)
            {
                throw new ArgumentException(nameof(missingSignalIcmConnector));
            }

            if (appConfiguration == null)
            {
                throw new ArgumentException(nameof(appConfiguration));
            }

            if (redisClient is null)
            {
                throw new ArgumentNullException(nameof(redisClient));
            }

            var cloudQueuePool = this.CreateCloudQueue(config);
            var aidQueueMonitoringClientList = this.CreateAidQueueMonitoringClientList(config);
            this.aidMockFunc = new AidMockFunc(config, metricContainer, this.GetAzureTokenCredentials(config));
            this.aidEventHubFunc = new AidEventHubFunc(cloudQueuePool, metricContainer, appConfiguration, redisClient);
            this.aidTelemetryFunc = new AidTelemetryFunc(aidQueueMonitoringClientList, metricContainer);
            this.aidBlobStorageFunc = new AidBlobStorageFunc(metricContainer, missingRequestFileHelper, missingSignalIcmConnector);
        }

        /// <inheritdoc />
        public AidMockFunc GetAidMockFunc()
        {
            return this.aidMockFunc;
        }

        /// <inheritdoc />
        public IAidFunction GetAidEventHubFunc()
        {
            return this.aidEventHubFunc;
        }

        /// <inheritdoc />
        public IAidBlobStorageFunc GetAidBlobStorageFunc()
        {
            return this.aidBlobStorageFunc;
        }

        /// <inheritdoc />
        public AidTelemetryFunc GetAidTelemetryFunc()
        {
            return this.aidTelemetryFunc;
        }

        /// <inheritdoc />
        public TokenCredential GetAzureTokenCredentials(IAidConfig config)
        {
            if (credentials != null)
            {
                return credentials;
            }

            if (config.DeploymentEnvironment == DeploymentEnvironment.ONEBOX)
            {
                var cert = CertificateFinder.FindCertificateByName(config.OneBoxCertSubjectName);
                credentials = new ConfidentialCredential(
                    tenantId: config.OneBoxTenantId,
                    clientId: config.ClientAppId,
                    certificate: cert);
            }
            else
            {
                // MSI
                credentials = new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = config.AidUamiId });
            }

            return credentials;
        }

        private ICloudQueueBase<AnaheimIdRequest> CreateCloudQueue(IAidConfig config)
        {
            List<ICloudQueue<AnaheimIdRequest>> cloudQueuesList = new List<ICloudQueue<AnaheimIdRequest>>();

            foreach (var account in config.AidQueuesStorageAccounts)
            {
                ICloudQueue<AnaheimIdRequest> cloudQueue;

                if (config.DeploymentEnvironment == DeploymentEnvironment.ONEBOX)
                {
                    // Emulator
                    cloudQueue = new CloudQueue<AnaheimIdRequest>(account.QueueName);
                }
                else
                {
                    var credentials = this.GetAzureTokenCredentials(config);

                    cloudQueue = new CloudQueue<AnaheimIdRequest>(
                        account.StorageAccountName,
                        account.QueueName,
                        credentials);
                }

                cloudQueuesList.Add(cloudQueue);
            }

            return new CloudQueuePool<AnaheimIdRequest>(cloudQueuesList.ToArray());
        }

        private IList<IAidQueueMonitoringClient> CreateAidQueueMonitoringClientList(IAidConfig config)
        {
            var aidQueueMonitoringClientList = new List<IAidQueueMonitoringClient>();

            foreach (var account in config.AidMonitoringQueuesStorageAccounts)
            {
                IAidQueueMonitoringClient aidQueueMonitoringClient;

                if (config.DeploymentEnvironment == DeploymentEnvironment.ONEBOX)
                {
                    aidQueueMonitoringClient = new AidQueueMonitoringClient(account.QueueName);
                }
                else
                {
                    var credentials = this.GetAzureTokenCredentials(config);

                    aidQueueMonitoringClient = new AidQueueMonitoringClient(
                        account.StorageAccountName,
                        account.QueueName,
                        credentials);
                }

                aidQueueMonitoringClientList.Add(aidQueueMonitoringClient);
            }

            return aidQueueMonitoringClientList;
        }
    }
}
