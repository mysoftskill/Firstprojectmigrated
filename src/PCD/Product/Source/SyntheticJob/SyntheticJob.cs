namespace PCD.SyntheticJob
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.Azure.Geneva.Synthetics.Contracts;
    using Microsoft.Azure.Geneva.Synthetics.Logging.OneDS;

    public class SyntheticJob : ISyntheticJob, IDisposable
    {
        private const string metricNamespaceValue = "adgcsMsAzGsProd";
        private const string endpointKey = "EndPoint";

        public SyntheticJob(ISyntheticsEnvironment environment)
        {
            Environment = environment;
            TelemetryClient = SyntheticsTelemetryClientCache.Instance.GetOrAdd(environment, metricNamespace: metricNamespaceValue);
            ServiceCallMetric = TelemetryClient.GetMetric(metricId: "ServiceCallTest", dimension1Name: "Result");
        }

        private Metric ServiceCallMetric { get; }
        private ISyntheticsEnvironment Environment { get; }
        private TelemetryClient TelemetryClient { get; }

        public void Dispose()
        {
            SyntheticsTelemetryClientCache.Instance.FlushAll();
        }

        public async Task RunAsync(IReadOnlyDictionary<string, string> parameters)
        {
            using (var operation = TelemetryClient.StartSyntheticTransaction(parameters))
            {
                TelemetryClient.TrackTrace("SyntheticJob RunAsync method started!", SeverityLevel.Information);
                bool success = false;
                Stopwatch sw = Stopwatch.StartNew();

                try
                {
                    string value;
                    if (!parameters.TryGetValue(endpointKey, out value))
                    {
                        throw new Exception("Endpoint not defined");
                    }
                    await HealthCheckTest.RunAsync(TelemetryClient, value);
                    success = true;
                }
                catch (Exception e)
                {
                    TelemetryClient.TrackTrace(e, "Exception during healthcheck call : " + e.Message, SeverityLevel.Error);
                }

                sw.Stop();
                ServiceCallMetric.TrackValue(sw.ElapsedMilliseconds, success ? "Success" : "Failure");
            }
        }
    }
}
