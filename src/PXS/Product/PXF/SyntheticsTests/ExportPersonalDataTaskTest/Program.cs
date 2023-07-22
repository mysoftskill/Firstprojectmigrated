namespace Microsoft.Membership.MemberServices.PrivacyExperience.SyntheticsTests.ExportPersonalDataTaskTest
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Membership.MemberServices.PrivacyExperience.SyntheticsTests.Common;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;

    /// <summary>
    /// This is a simple console app to test the ExportPersonalData synthetics task.
    /// It is not deployed to Geneva.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            IReadOnlyDictionary<string, string> parameters = new Dictionary<string, string>()
            {
                { "ApiUrlEndpoint", "https://graph.microsoft.com/v1.0" },
                { "ApiPathTemplate", "users/{0}/exportPersonalData" },
                { "ClientId", "90b23419-a7ce-4459-95e1-8f251ea7f606" }, // meepxsfunctionaltest
                { "KeyVaultUrl", "https://pxs-test-ame.vault.azure.net/" },
                { "UserPasswordSecretName", "user1-password"},
                { "UserUpnSecretName", "user1-upn"},
                { "BlobStorageConnectionStringSecretName", "pxs-test-blob-storage-connection-string" }
            };

            TelemetryClient telemetryClient = new TelemetryClient();

            try
            {
                ExportPersonalDataConfig config = new ExportPersonalDataConfig(telemetryClient, parameters);

                ExportPersonalDataTask.RunAsync(telemetryClient, config).Wait();

                Console.WriteLine("Export Created");
            }
            catch (Exception e)
            {
                // TrackTrace extension method for exceptions logs to both Trace and Exception streams to provide
                // debugging view (trace stream) and distributed tracing integration (exception stream).
                telemetryClient.TrackTrace(e.Message,  SeverityLevel.Error);

                Console.WriteLine($"Export Failed: {e}.");
            }

            Console.WriteLine("Press any key to exit.");
            Console.ReadLine();
        }
    }
}
