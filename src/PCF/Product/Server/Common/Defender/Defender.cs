namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.PrivacyServices.Common.Azure;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Implement the antivirus scanning.
    /// </summary>
    public class Defender : IDefender
    {
        public const string MalwareReplacementText = @"Microsoft cares about your security, We have detected a file we believe could exploit a potential security vulnerability."
                                                   + " Your security is our priority and have therefore replaced the infected file with this message. If you have any questions about the action taken, please contact our privacy team https://privacy.microsoft.com/en-US/privacy-questions?ref=amc.privacy-signedout";

        private readonly string fileMetaDataServiceUri;

        private readonly string defenderApiKey;

        private readonly DefenderScanResult clean = new DefenderScanResult { IsMalware = false };
        private readonly DefenderScanResult malwareFound = new DefenderScanResult { IsMalware = true };

        /// <summary>
        /// Create an instance of Defender Client
        /// </summary>
        public Defender()
        {
            this.defenderApiKey = Config.Instance.Worker.DefenderAPIKey;
            this.fileMetaDataServiceUri = Config.Instance.Worker.DefenderFileMetaDataServiceUri;
        }

        /// <inheritdoc/>
        public async Task<DefenderScanResult> GetScanResultAsync(string sha, CancellationToken cancellationToken)
        {
            var uri = $"{this.fileMetaDataServiceUri}/{sha}";

            DefenderScanResult defenderScanResult = null;
            await Logger.InstrumentAsync(
                new OutgoingEvent(SourceLocation.Here()),
                async ev =>
                {
                    using (HttpClient client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", this.defenderApiKey);
                        using (HttpResponseMessage httpResponse = await client.GetAsync(uri))
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            defenderScanResult = await this.ProcessScanResponseAsync(httpResponse, cancellationToken);
                        }
                    }

                    ev["Step"] = $"Defender:Get scan result";
                    ev["AVaaSURI:"] = $"{this.fileMetaDataServiceUri}";
                    ev["SHA"] = $"[{sha}]";
                    ev["ScanResult"] = $"IsMalware: [{defenderScanResult?.IsMalware}]";
                });

            return defenderScanResult;
        }

        /// <summary>
        /// Method to process scan response. Logic specific to AVaaS scan result
        /// </summary>
        /// <param name="httpResponse">Respose to parse</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Null if not successful or needs retries; Clean when no malware; malwareFound when malware tags found in response</returns>
        internal async Task<DefenderScanResult> ProcessScanResponseAsync(HttpResponseMessage httpResponse, CancellationToken cancellationToken)
        {
            if (httpResponse.IsSuccessStatusCode)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var responseString = await httpResponse.Content.ReadAsStringAsync();

                // Certain times, If Sha not found, AVaaS returns 404 in response string, Assume clean
                if (responseString == "404")
                {
                    this.clean.DeterminationType = DeterminationType.ShaNotFound;
                    return this.clean;
                }

                var response = JObject.Parse(responseString);

                var shaFromResponse = response["Key"]?.ToString();

                // Check if malware status is readily available in response
                // For cases where file is not uploaded by us, 
                // we should mostly have the result available or declare clean when not found in AVaaS Database
                var resultReadilyAvailable = this.LookupMalwareTags(response);
                if (resultReadilyAvailable != null)
                {
                    return resultReadilyAvailable;
                }

                // Scan result is not readily available, wait for the Static Scan State to turn PROCESSED
                // STATES
                // NONE = 0,
                // FILE PROCESSING HAS BEEN REQUESTED
                // REQUESTED = 1,
                // FILE PROCESSING IS CURRENTLY IN PROGRESS
                // INPROGRESS = 2,
                // FILE HAS BEEN SUCCESSFULLY PROCESSED
                // PROCESSED = 3,
                // FILE PROCESSING HAS FAILED
                // PROCESSFAILED = 4
                var states = response["States"];
                var staticScan = response.SelectToken("States.StaticScan");

                DualLogger.Instance.Information(nameof(Defender), $"ProcessScanResponseAsync: SHA=[{shaFromResponse}] States: [{states}]");
                DualLogger.Instance.Information(nameof(Defender), $"ProcessScanResponseAsync: SHA=[{shaFromResponse}] StaticScan: [{staticScan}]");

                if (states == null || staticScan == null)
                {
                    // States or StaticScan not found. Retry
                    return null;
                }
                else if (response.SelectToken("States.StaticScan.State") != null)
                {
                    if (response.SelectToken("States.StaticScan.State")?.ToString() == "4")
                    {
                        // Scan failed, assume clean
                        this.clean.DeterminationType = DeterminationType.ScanFailed;
                        return this.clean;
                    }
                    else if (response.SelectToken("States.StaticScan.State")?.ToString() != "3")
                    {
                        // Scan result not yet availble for processing. Retry again after some time
                        return null;
                    }
                    else
                    {
                        // Lookup for malware flags again; after StaticScan State==3
                        return this.LookupMalwareTags(response);
                    }
                }

                return null;
            }
            else if (httpResponse.StatusCode == HttpStatusCode.NotFound)
            {
                // If SHA not found in AVaaS, assume its not malware. 
                // We are going with assumption that agents are not generate net new malware files
                // As long as AVaaS malware database doesn't have the file SHA, its assumed to be clean.
                // This step is done to avoid excess load on AVaaS system, which currently takes couple of minutes to scan new files
                this.clean.DeterminationType = DeterminationType.ShaNotFound;
                return this.clean;
            }

            // Default to retry; when other Status codes are present in response
            return null;
        }

        private DefenderScanResult LookupMalwareTags(JObject response)
        {
            var shaFromResponse = response["Key"]?.ToString();
            string determinationValue = response.SelectToken("V1.DeterminationValue")?.ToString();

            DualLogger.Instance.Information(nameof(Defender), $"ProcessScanResponseAsync: SHA=[{shaFromResponse}] V1.DeterminationValue=[{determinationValue}]");

            // First Resort: If Determination Found 
            if (!string.IsNullOrEmpty(determinationValue) && !StringComparer.OrdinalIgnoreCase.Equals(determinationValue, "NoDetermination"))
            {
                string[] malwareFlags = new string[] { "Malware", "MalwareContainer", "AutomationMalware", "AutomationPUA", "UWS", "AutomationGrayware", "AutomationUWS", "ClassifiedPUA", "Spyware", "SpywareContainer", "PUAContainer" };
                if (StringComparer.OrdinalIgnoreCase.Equals(determinationValue, "clean"))
                {
                    this.clean.DeterminationType = DeterminationType.V1DeterminationValueFound;
                    return this.clean;
                }
                else if (Array.FindIndex(malwareFlags, p => p.Equals(determinationValue, StringComparison.OrdinalIgnoreCase)) > -1)
                {
                    this.malwareFound.ScanDetermination = determinationValue;
                    this.malwareFound.DeterminationType = DeterminationType.V1DeterminationValueFound;
                    return this.malwareFound;
                }
            }

            // Second Resort: If determination not found rely on Static Scan result
            var staticScanResult = response.SelectToken("V1.StaticScanResult.MsAmPreRelRel.Result")?.ToString();

            DualLogger.Instance.Information(nameof(Defender), $"ProcessScanResponseAsync: SHA=[{shaFromResponse}] V1.StaticScanResult.MsAmPreRelRel.Result=[{staticScanResult}]");

            // If Static Scan Result Found 
            if (staticScanResult != null)
            {
                if (!string.IsNullOrEmpty(staticScanResult))
                {
                    this.malwareFound.ScanDetermination = staticScanResult;
                    this.malwareFound.DeterminationType = DeterminationType.V1StaticScanResultMsAmPreRelRelFound;
                    return this.malwareFound;
                }
                else
                {
                    this.clean.DeterminationType = DeterminationType.V1StaticScanResultMsAmPreRelRelFound;
                    return this.clean;
                }
            }

            // Third Resort: If static Scan result not present, rely on VT(Virus Total) Scan result
            else
            {
                string vtScanResult = response.SelectToken("EX.Feeds.VT.StaticScanResults.Microsoft")?.ToString();

                DualLogger.Instance.Information(nameof(Defender), $"ProcessScanResponseAsync: SHA=[{shaFromResponse}] EX.Feeds.VT.StaticScanResult.Microsoft=[{vtScanResult}]");

                // If VT Scan result Found
                if (vtScanResult != null)
                {
                    if (!string.IsNullOrEmpty(vtScanResult))
                    {
                        this.malwareFound.ScanDetermination = vtScanResult;
                        this.malwareFound.DeterminationType = DeterminationType.EXFeedsVTFound;
                        return this.malwareFound;
                    }
                    else
                    {
                        this.clean.DeterminationType = DeterminationType.EXFeedsVTFound;
                        return this.clean;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Compute SHA 256 Hash from the raw byte array
        /// </summary>
        /// <param name="rawData">raw data to generate Hash</param>
        /// <returns>SHA256</returns>
        public string ComputeSha256Hash(Stream rawData)
        {
            rawData.Position = 0;

            // Create a SHA256
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(rawData);

                // Convert byte array to a string
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }

        /// <summary>
        /// Replacement text for files with malware
        /// </summary>
        /// <returns>replacement byte array</returns>
        public static byte[] GetMalwareReplacementFileText()
        {
            return Encoding.UTF8.GetBytes(MalwareReplacementText);
        }
    }
}
