namespace Microsoft.PrivacyServices.AnaheimId.AidFunctions
{
    using global::Azure.Core;
    using Microsoft.PrivacyServices.AnaheimId.Config;

    /// <summary>
    /// Anaheim Id Func Factory.
    /// </summary>
    public interface IAidFunctionsFactory
    {
        /// <summary>
        /// Get EventHub Func.
        /// </summary>
        /// <returns>Aid EventHub Func</returns>
        IAidFunction GetAidEventHubFunc();

        /// <summary>
        /// Get Blob Storage Func.
        /// </summary>
        /// <returns>Aid Blob Storage Func</returns>
        IAidBlobStorageFunc GetAidBlobStorageFunc();

        /// <summary>
        /// AID Telemetry Func.
        /// </summary>
        /// <returns>Aid Azure Queue Func</returns>
        AidTelemetryFunc GetAidTelemetryFunc();

        /// <summary>
        /// Get Azure token credentials.
        /// </summary>
        /// <param name="config">AiID Config.</param>
        /// <returns>Azure token credentials.</returns>
        TokenCredential GetAzureTokenCredentials(IAidConfig config);

        /// <summary>
        /// Anaheim Id mock.
        /// </summary>
        /// <returns>AidMockEventHubFunc.</returns>
        AidMockFunc GetAidMockFunc();
    }
}
