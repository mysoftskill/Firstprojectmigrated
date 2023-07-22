namespace Microsoft.Membership.MemberServices.PrivacyExperience.SyntheticsTests.DeviceDeleteTaskTest
{
    using System;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.PrivacyExperience.SyntheticsTests.Common;

    class Program
    {
        public static async Task Main()
        {
            TelemetryClient telemetryClient = new TelemetryClient();
            try
            {
                // Test cert configured for auth in PPE
                string testCertCN = "cloudtest.privacy.microsoft-int.ms";

                // PPE Deveice Delete Test - Local Cert Store
                Console.WriteLine("Testing PPE with Local Cert");
                string pxsApiPPE = "https://pxs.api.account.microsoft-ppe.com";
                await DeviceDeleteTask.RunAsync(telemetryClient, GetClientCertificate(testCertCN),
                    new Uri(pxsApiPPE), 1, 1).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                // TrackTrace extension method for exceptions logs to both Trace and Exception streams to provide
                // debugging view (trace stream) and distributed tracing integration (exception stream).
                telemetryClient.TrackTrace(e.Message, SeverityLevel.Error);
            }

            Console.WriteLine("Device Delete Requests Created. Check the event log to verify sucess. Press any key to exit.");
            Console.ReadLine();
        }

        // Retrieve an X509Certificate from the local machine store
        private static X509Certificate2 GetClientCertificate(string subjectName)
        {
            using (X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadOnly);
                X509Certificate2Collection certs = store.Certificates.Find(X509FindType.FindBySubjectName, subjectName, false);
                if (certs.Count == 0)
                {
                    throw new InvalidOperationException($"{subjectName} not installed on the machine.");
                }
                return CertHelper.GetCertWithMostRecentIssueDate(CertHelper.GetCertsWithExactSubjectName(certs.Cast<X509Certificate2>(), subjectName));
            }
        }
    }
}
