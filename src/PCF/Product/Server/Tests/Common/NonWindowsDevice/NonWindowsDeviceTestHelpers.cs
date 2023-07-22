namespace Microsoft.Azure.ComplianceServices.Test.Common
{
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Newtonsoft.Json.Linq;
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    public static class NonWindowsDeviceTestHelpers
    {
        public static NonWindowsDeviceSubject CreateNonWindowsDeviceSubject(DeviceOS platform)
        {
            NonWindowsDeviceSubject subject = new NonWindowsDeviceSubject();

            // Get device id
            switch (platform)
            {
                case DeviceOS.MacOS:
                    // Random UUID
                    subject.MacOsPlatformDeviceId = Guid.NewGuid();
                    break;
                default:
                    throw new IndexOutOfRangeException($"Invalid platform: {platform}.");
            }

            return subject;
        }

        /// <summary>
        /// Generate OneDS Json NonWindowsDeviceEvent.
        /// </summary>
        /// <param name="deviceId">Common schema device Id. Format example "u:GUID"</param>
        /// <param name="userId">Common schema userId.</param>
        /// <param name="cV">Common schema cV.</param>
        /// <param name="time">Common Schema time.</param>
        /// <returns></returns>
        public static string CreateNonWindowsDeviceEvent(string deviceId, string userId, string cV, DateTimeOffset time)
        {
            string json = GetNonWindowsDeviceEventSampleJson();
            JObject parsedJson = JObject.Parse(json);

            // See CommonSchema spec for details: https://github.com/microsoft/common-schema/blob/master/v3.0/README.md
            parsedJson["ext"]["device"]["localId"] = deviceId;
            parsedJson["ext"]["user"]["id"] = userId;
            parsedJson["cV"] = cV;
            parsedJson["time"] = time.ToString("o");

            return parsedJson.ToString();
        }

        private static string GetNonWindowsDeviceEventSampleJson()
        {
            Assembly current = typeof(NonWindowsDeviceTestHelpers).Assembly;
            string configFile = current.GetManifestResourceNames().Single(x => x.IndexOf("NonWindowsDeviceDeleteSample.json", StringComparison.OrdinalIgnoreCase) >= 0);
            string content;

            using (var streamReader = new StreamReader(current.GetManifestResourceStream(configFile)))
            {
                content = streamReader.ReadToEnd();
            }

            return content;
        }
    }
}