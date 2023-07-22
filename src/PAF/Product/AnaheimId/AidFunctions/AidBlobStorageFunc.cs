namespace Microsoft.PrivacyServices.AnaheimId.AidFunctions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Azure.ComplianceServices.Common.IfxMetric;
    using Microsoft.PrivacyServices.AnaheimId.Avro;
    using Microsoft.PrivacyServices.AnaheimId.Icm;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// Anaheim id Blob Storage Function.
    /// </summary>
    public class AidBlobStorageFunc : IAidBlobStorageFunc
    {
        private const string ComponentName = nameof(AidBlobStorageFunc);
        private const int MaxRequestIds = 5;
        private readonly IMetricContainer metricContainer;
        private readonly IMissingRequestFileHelper requestFileHelper;
        private readonly IMissingSignalIcmConnector missingSignalIcmConnector;

        /// <summary>
        /// Initializes a new instance of the <see cref="AidBlobStorageFunc"/> class.
        /// The instance of this class is used to create missing signal ICMs from blob storage files.
        /// </summary>
        /// <param name="metricContainer">Container to hold Metrics.</param>
        /// <param name="requestFileHelper">Used to read in request ids from the file in blob storage.</param>
        /// <param name="missingSignalIcmConnector">ICM connector for missing signal alerts.</param>
        public AidBlobStorageFunc(IMetricContainer metricContainer, IMissingRequestFileHelper requestFileHelper, IMissingSignalIcmConnector missingSignalIcmConnector)
        {
            this.metricContainer = metricContainer ?? throw new ArgumentException(nameof(metricContainer));
            this.requestFileHelper = requestFileHelper ?? throw new ArgumentException(nameof(requestFileHelper));
            this.missingSignalIcmConnector = missingSignalIcmConnector ?? throw new ArgumentException(nameof(missingSignalIcmConnector));
        }

        /// <summary>
        /// Runs the Anaheim ID Blob Storage Function.
        /// </summary>
        /// <param name="inBlob">The memory stream for the input blob.</param>
        /// <param name="name">The name of the blob.</param>
        /// <param name="logger">The logger instance.</param>
        public void Run(Stream inBlob, string name, ILogger logger)
        {
            IApiEvent outgoingApi = new OutgoingApiEventWrapper(this.metricContainer, "AID_BlobStorage", "AID_BlobStorageDetection", logger, "Anaheim");
            try
            {
                outgoingApi.Start();
                logger.Information(ComponentName, $"Collecting request ids from {name}");
                List<long> sampleRequestIds = this.requestFileHelper.CollectRequestIds(inBlob, MaxRequestIds);

                // No missing signal is found in missingsignalscontainer
                if (sampleRequestIds == null || sampleRequestIds.Count == 0)
                {
                    logger.Information(ComponentName, $"No request ids found in {name}");
                    outgoingApi.Success = true;
                }

                // Missing signal is found in missingsignalscontainer, create an ICM
                else
                {
                    logger.Error(ComponentName, $"Extracted the following sample request ids: {string.Join(", ", sampleRequestIds)}");
                    outgoingApi.Success = this.missingSignalIcmConnector.CreateMissingSignalIncident(name, sampleRequestIds, logger);
                }
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
    }
}
