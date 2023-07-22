namespace Microsoft.Membership.MemberServices.PrivacyExperience.SyntheticsTests.DeviceDeleteSyntheticsJob
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    using global::Azure.Identity;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.Azure.Geneva.Synthetics.Contracts;
    using Microsoft.Azure.Geneva.Synthetics.Logging.OneDS;
    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.Services.AppAuthentication;
    using Microsoft.Membership.MemberServices.PrivacyExperience.SyntheticsTests.Common;

    /// <summary>
    /// Synthetic job which implements synthetics platform interface to test the Device Delete Api.
    /// </summary>
    public class DeviceDeleteSyntheticsJob : ISyntheticJob, IDisposable
    {
        // The total number of minutes between geneva synthetics runs (ExecutionIntervalSeconds/60)
        const int ExecutionIntervalMinutes = 5;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="environment">Synthetics environment, will be automatically injected by runtime</param>
        public DeviceDeleteSyntheticsJob(ISyntheticsEnvironment environment)
        {
            Environment = environment ?? throw new ArgumentNullException(nameof(environment));

            // Telemetry clients created by SyntheticsTelemetryClientCache automatically add columns for the current Synthetics environment values
            // (Environment, Region, JobGroupName, JobName, InstanceName, ...) to all log entries and metrics.
            TelemetryClient = SyntheticsTelemetryClientCache.Instance.GetOrAdd(environment, metricNamespace: "adgcsMsAzGsProd");

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
                bool success = false;
                Stopwatch sw = Stopwatch.StartNew();
                TelemetryClient.TrackTrace($"{Environment.JobGroupName} RunAsync method started!", SeverityLevel.Information);

                if (parameters == null)
                {
                    throw new ArgumentNullException(nameof(parameters));
                }

                // Check if the app settings feature flag to run is enabled
                var appSettings = new SyntheticsAppSettings(parameters["AppConfigUrl"], new ManagedIdentityCredential(clientId: parameters["ManagedIdentityClientId"]));
                string enableFeatureFlagName = parameters["EnableFeatureFlagName"];
                bool runnerEnabled = await appSettings.IsFeatureFlagEnabledAsync(enableFeatureFlagName).ConfigureAwait(false);
                TelemetryClient.TrackTrace($"AppConfig {enableFeatureFlagName}={runnerEnabled}.", SeverityLevel.Information);

                // Only execute task if the runner is enabled based on the feature flag value set in the azure portal
                if (runnerEnabled)
                {
                    // Extract the paramaters needed to retrieve the certificate from azure keyvault
                    string keyVaultUrl = parameters["KeyVaultUrl"];
                    string certificateName = parameters["CertificateName"];

                    // Create a keyvault client using msi
                    var azureServiceTokenProvider = new AzureServiceTokenProvider();
                    var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));

                    // Accessing full certificate including private key from the keyault (Creating X509 objects)
                    X509Certificate2 certificate = null;
                    try
                    {
                        // Get the X509Certificate from keyvault using the keyvault client
                        var certSecret = await keyVaultClient.GetSecretAsync(keyVaultUrl, certificateName).ConfigureAwait(false);
                        certificate = new X509Certificate2(Convert.FromBase64String(certSecret.Value), string.Empty, X509KeyStorageFlags.UserKeySet);

                        // Log the certificate identifier and the certificate thumbprint
                        // Note: NEVER log your keyvault secrets, keys or certificate private keys. Just logging the thumbprint here.
                        TelemetryClient.TrackTrace($"Created x509 object from {certSecret.SecretIdentifier}. The thumbprint is {certificate.Thumbprint}", SeverityLevel.Information);
                    }
                    catch (Exception e)
                    {
                        // TrackTrace extension method for exceptions logs to both Trace and Exception streams to provide
                        // debugging view (trace stream) and distributed tracing integration (exception stream).
                        TelemetryClient.TrackTrace(e, "Exception during service call", SeverityLevel.Error);
                    }

                    // Check to insure we have the x509 certificate before attempting to authenticate with the API
                    if (certificate != null)
                    {
                        try
                        {
                            // Get the PXS API URL from the parameters file
                            string pxsApiEndpoint = parameters["PXSApiEndpoint"];

                            // Retrieve the effective request rate per minute for device delete requests from app settings.
                            string rpmConfigName = parameters["RPMConfigName"];
                            int deviceDeleteRPM = appSettings.GetConfigIntValue(rpmConfigName);
                            TelemetryClient.TrackTrace($"AppConfig {rpmConfigName}={deviceDeleteRPM}.", SeverityLevel.Information);

                            // Retrieve the max number of requests which should be sent per-thread from app settings.
                            string numParallelRequestsName = parameters["NumParallelRequestsName"];
                            int numParallelRequests = appSettings.GetConfigIntValue(numParallelRequestsName);
                            TelemetryClient.TrackTrace($"AppConfig {numParallelRequestsName}={numParallelRequests}.", SeverityLevel.Information);

                            // Create a Batch of parallel tasks to execute 
                            int totalRequestPerThread = ExecutionIntervalMinutes * (int)Math.Ceiling(deviceDeleteRPM / (double)numParallelRequests);
                            TelemetryClient.TrackTrace($"Starting {numParallelRequests} threads with {totalRequestPerThread} each to send over {ExecutionIntervalMinutes} minutes.", SeverityLevel.Information);
                            List<Task> tasks = new List<Task>(numParallelRequests);
                            for (int j = 0; j < numParallelRequests; j++)
                            {
                                tasks.Add(DeviceDeleteTask.RunAsync(TelemetryClient, certificate, new Uri(pxsApiEndpoint), totalRequestPerThread, ExecutionIntervalMinutes));
                            }

                            // Start all the tasks and wait for all of them to complete
                            await Task.WhenAll(tasks).ConfigureAwait(false);
                            success = true;
                        }
                        catch (Exception e)
                        {
                            // TrackTrace extension method for exceptions logs to both Trace and Exception streams to provide
                            // debugging view (trace stream) and distributed tracing integration (exception stream).
                            TelemetryClient.TrackTrace(e, "Exception during service call", SeverityLevel.Error);
                        }
                    }
                }
                else
                {
                    TelemetryClient.TrackTrace($"{Environment.JobGroupName} runner is not enabled.", SeverityLevel.Information);
                }

                sw.Stop();
                ServiceCallMetric.TrackValue(sw.ElapsedMilliseconds, success ? "Success" : "Failure");
                TelemetryClient.TrackTrace($"{Environment.JobGroupName} RunAsync method finished!", SeverityLevel.Information);

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

        private TelemetryClient TelemetryClient { get; }

        private Metric ServiceCallMetric { get; }

        private ISyntheticsEnvironment Environment { get; }
    }
}