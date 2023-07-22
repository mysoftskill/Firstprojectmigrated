namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using Microsoft.PrivacyServices.Common.Azure;
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    public static class DefenderExtensions
    {
        /// <summary>
        /// Extension method to Scan for malware given a stream
        /// </summary>
        /// <returns>True if malware found, else false</returns>
        public static async Task<DefenderScanResult> ScanForMalwareAsync(
            this IDefender avScanner, 
            Stream streamForScan,
            CancellationToken cancellationToken)
        {
            // We are going with assumption that agents are not generating net new malware files
            // As long as AVaaS malware database doesn't have the file SHA, its assumed to be clean.
            // This step is done to avoid excess load on AVaaS system, which currently takes couple of minutes to scan new files
            string sha = avScanner.ComputeSha256Hash(streamForScan);

            int i = 1;

            // Loop until one of the below conditions are met
            // 1. Scan result is available
            // 2. Wait time for scan result exceeds 15 mins
            // 3. Cancellation is requested
            while (i <= 30)
            {
                DefenderScanResult scanResult = await avScanner.GetScanResultAsync(sha, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                if (scanResult != null)
                {
                    return scanResult;
                }

                DualLogger.Instance.Information(nameof(DefenderExtensions), $"Waiting for scan result on file with SHA:[{sha}], Iteration:{i++}");

                // Delay further processing for 30 seconds
                await Task.Delay(30000, cancellationToken);
            }

            throw new TimeoutException($"Could not complete AV Scan on file with SHA:[{sha}]");
        }
    }
}
