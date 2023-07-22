namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    
    public interface IDefender
    {
        /// <summary>
        /// Get scan results 
        /// </summary>
        /// <param name="sha">SHA256 to lookup in File Metadata Service</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>returns null in case of failures or when retry is required. Scan result is returned when it is available.</returns>
        Task<DefenderScanResult> GetScanResultAsync(string sha, CancellationToken cancellationToken);

        /// <summary>
        /// Compute SHA 256
        /// </summary>
        /// <param name="rawData">Raw data to calculate SHA 256</param>
        /// <returns>SHA 256</returns>
        string ComputeSha256Hash(Stream rawData);
    }
}
