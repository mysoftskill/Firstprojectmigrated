namespace Microsoft.Membership.MemberServices.PrivacyExperience.SyntheticsTests.Common
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Extensions;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.DeviceDeleteHelper;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.Utilities;
    using HttpClient = Microsoft.OSGS.HttpClientCommon.HttpClient;

    /// <summary>
    /// This sample shows how to query the device delete api.
    /// </summary>
    public static class DeviceDeleteTask
    {
        private const int MaxDeviceIdEntriesPerLogStatement = 60;

        public static async Task RunAsync(TelemetryClient telemetryClient, X509Certificate2 certificate, Uri serviceEndpoint, int totalNumberOfRequests = 5, int minutesPerExecution = 5)
        {
            // Create the http client
            var certHandler = new WebRequestHandler();
            var httpClient = new HttpClient(certHandler) { BaseAddress = serviceEndpoint };
            httpClient.MessageHandler.AttachClientCertificate(certificate);

            // Generate a list of ids for test device delete requests
            List<string> deviceIdsStrings = new List<string>(new string[totalNumberOfRequests]).Select(item => $"g:{GlobalDeviceIdGenerator.Generate()}").ToList();

            // collect all the device delete ids sent sucessfully and the device delete ids that failed to send
            List<string> deviceIdsDeliveredSuccessfully = new List<string>();
            List<string> deviceIdsNotDelivered = new List<string>();

            // setup a timer
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            try
            {
                int requestTransmitCount = 0;
                foreach (List<string> deviceIdsStringBatch in BuildStringChunksWithLinq(deviceIdsStrings, (int)Math.Ceiling((double)totalNumberOfRequests/minutesPerExecution)))
                {
                    HttpResponseMessage response = await DeviceDeleteHelper.SendRandomDeviceDeleteRequests(httpClient, deviceIdsStringBatch).ConfigureAwait(false);
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        deviceIdsDeliveredSuccessfully.AddRange(deviceIdsStringBatch);
                    }
                    else
                    {
                        deviceIdsNotDelivered.AddRange(deviceIdsStringBatch);
                    }
                    requestTransmitCount += 1;

                    // Do not delay ending the program after the last request
                    if (requestTransmitCount < minutesPerExecution)
                    {
                        long nextRunInMilliseconds = requestTransmitCount * 60 * 1000;
                        long difference = nextRunInMilliseconds - stopWatch.ElapsedMilliseconds;
                        if (difference > 0)
                        {
                            // wait for the minute to complete before sending another request
                            await Task.Delay((int)difference).ConfigureAwait(false);
                        }
                    }
                }
            }
            catch
            {
                telemetryClient.TrackTrace("Device delete requests could not be sent from synthetics runner.", SeverityLevel.Error);
            }
            finally
            {
                if (deviceIdsDeliveredSuccessfully.Count > 0)
                {
                    LogSuccess(telemetryClient, deviceIdsDeliveredSuccessfully);
                }
                if (deviceIdsNotDelivered.Count > 0)
                {
                    LogFailure(telemetryClient, deviceIdsNotDelivered);
                }
            }
            stopWatch.Stop();
        }

        private static void LogSuccess(TelemetryClient telemetryClient, List<string> deviceIdsDeliveredSuccessfully)
        {
            foreach (List<string> batchDeviceIds in BuildStringChunksWithLinq(deviceIdsDeliveredSuccessfully, MaxDeviceIdEntriesPerLogStatement))
            {
                telemetryClient.TrackTrace($"Device delete requests sent from synthetics runner" +
                    $" with test ids: {string.Join(",", batchDeviceIds)}", SeverityLevel.Information);
            }
        }

        private static void LogFailure(TelemetryClient telemetryClient, List<string> deviceIdsNotDelivered)
        {
            foreach (List<string> batchDeviceIds in BuildStringChunksWithLinq(deviceIdsNotDelivered, MaxDeviceIdEntriesPerLogStatement))
            {
                telemetryClient.TrackTrace($"Device delete requests failed to send from synthetics runner" +
                    $" with test ids: {string.Join(",", batchDeviceIds)}", SeverityLevel.Error);
            }
        }

        private static List<List<string>> BuildStringChunksWithLinq(List<string> fullList, int batchSize)
        {
            int total = 0;
            var chunkedList = new List<List<string>>();
            while (total < fullList.Count)
            {
                var chunk = fullList.Skip(total).Take(batchSize);
                chunkedList.Add(chunk.ToList());
                total += batchSize;
            }

            return chunkedList;
        }
    }
}

