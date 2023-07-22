namespace Microsoft.Membership.MemberServices.PrivacyExperience.SyntheticsTests.ExportPersonalDataSyntheticsJob
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.Azure.Geneva.Synthetics.Contracts;
    using Microsoft.Azure.Geneva.Synthetics.Logging.OneDS;
    using Microsoft.Membership.MemberServices.PrivacyExperience.SyntheticsTests.Common;

    /// <summary>
    /// Synthetic job which implements synthetics platform interface to test the ExportPersonalData Graph Api.
    /// </summary>
    public class ExportPersonalDataSyntheticsJob : ISyntheticJob, IDisposable
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="environment">Synthetics environment, will be automatically injected by runtime</param>
        public ExportPersonalDataSyntheticsJob(ISyntheticsEnvironment environment)
        {
            Environment = environment ?? throw new ArgumentNullException(nameof(environment));

            // Telemetry clients created by SyntheticsTelemetryClientCache automatically add columns for the current Synthetics environment values
            // (Environment, Region, JobGroupName, JobName, InstanceName, ...) to all log entries and metrics.
            TelemetryClient = SyntheticsTelemetryClientCache.Instance.GetOrAdd(environment, metricNamespace: InstanceMetricLookup.GetMetricNamespace(environment.InstanceNamePrefix));

            // Once you deployed the job and it is emitting metrics, don't forget to configure preaggregates to be able to pivot on the defined dimensions.
            ServiceCallMetric = TelemetryClient.GetMetric(metricId: "ServiceCallTest", dimension1Name: "Result");
        }

        /// <summary>
        /// Function which is called on a specific schedule provided in the config.
        /// This is the entry point for the ExportPersonalData test code.
        /// </summary>
        /// <param name="parameters">Dictionary of custom parameters which have been defined in the config</param>
        /// <returns>Awaitable task</returns>
        public async Task RunAsync(IReadOnlyDictionary<string, string> parameters)
        {
            // This will set the trace ID on the operation_Id column in all logs emitted with the App Insights / OneDS SDK within this using block.
            // The ID is stored on System.Diagnostics.Activity.Current and will also apply to other TelemetryClient objects used within this block.
            // It also emits a special message to the RequestTelemetry table for Distributed Tracing integration.
            using (var operation = TelemetryClient.StartSyntheticTransaction(parameters))
            {
                TelemetryClient.TrackTrace("ExportPersonalDataSyntheticsJob RunAsync method started!", SeverityLevel.Information);

                bool success = false;
                Stopwatch sw = Stopwatch.StartNew();

                try
                {
                    ExportPersonalDataConfig config = new ExportPersonalDataConfig(TelemetryClient, parameters);

                    await ExportPersonalDataTask.RunAsync(TelemetryClient, config);
                }
                catch (Exception e)
                {
                    // TrackTrace extension method for exceptions logs to both Trace and Exception streams to provide
                    // debugging view (trace stream) and distributed tracing integration (exception stream).
                    TelemetryClient.TrackTrace(e, "Exception during service call", SeverityLevel.Error);
                }

                sw.Stop();
                ServiceCallMetric.TrackValue(sw.ElapsedMilliseconds, success ? "Success" : "Failure");

                TelemetryClient.TrackTrace("ExportPersonalDataSyntheticsJob RunAsync method finished!", SeverityLevel.Information);

                // Success property is set to false by default. Should set to true at end of iteration if all went well.
                // Setting this at the end of the using block has the benefit that transactions which fail with an unhandled exception
                // will have Success = false in the logs. That allows easily identifying this scenario in the logs.
                // This only affects the RequestTelemetry event and has no impact on the Synthetics platform. You cannot alert on this.
                operation.Telemetry.Success = true;
            }
        }

        /// <summary>
        /// Dispose method is called by Synthetics platform after RunAsync, before the job shuts down.
        /// It will be called even if RunAsync throws an unhandled exception.
        /// </summary>
        public void Dispose()
        {
            // IMPORTANT: Necessary to flush all clients before process exits or events may be lost.
            //            All clients created by SyntheticsTelemetryClientCache.Instance are covered by the below method.
            //            If you create custom TelemetryClient objects in your code, make sure to flush them as well.
            //            You must implement IDisposable in your job for this Dispose method to be called.
            //            Just copying the method into your job without implementing IDisposable will not work!
            SyntheticsTelemetryClientCache.Instance.FlushAll();
        }

        private HttpClient HttpClient { get; } = new HttpClient();  // lgtm [cs/httpclient-checkcertrevlist-disabled]

        private TelemetryClient TelemetryClient { get; }

        private Metric ServiceCallMetric { get; }

        private ISyntheticsEnvironment Environment { get; }
    }
}