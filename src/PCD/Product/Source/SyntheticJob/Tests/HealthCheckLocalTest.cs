using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace PCD.SyntheticJob.Tests
{
    class HealthCheckLocalTest
    {
        static void Main()
        {
            TelemetryClient telemetryClient = new TelemetryClient();

            try
            {
                HealthCheckTest.RunAsync(telemetryClient, "https://manage.privacy.microsoft-ppe.com/keepalive").Wait();
            }
            catch (Exception e)
            {
                telemetryClient.TrackTrace(e.Message, SeverityLevel.Error);
            }

            Console.WriteLine("Press any key to exit.");
            Console.ReadLine();
        }
    }
}
