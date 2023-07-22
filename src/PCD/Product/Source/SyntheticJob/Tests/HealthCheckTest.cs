namespace PCD.SyntheticJob
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;

    public static class HealthCheckTest
    {
        public static async Task RunAsync(TelemetryClient telemetryClient, string url)
        {
            var httpClient = new HttpClient();
            HttpResponseMessage responseMessage = await httpClient.GetAsync(url);
            telemetryClient.TrackTrace("Status: " + responseMessage.StatusCode.ToString());
            if (responseMessage.StatusCode != HttpStatusCode.OK)
            {
                telemetryClient.TrackTrace("Message: " + responseMessage.Content.ToString());
                throw new Exception("Healthcheck API is failing");
            }
        }
    }
}
