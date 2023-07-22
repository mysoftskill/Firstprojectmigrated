namespace Microsoft.PrivacyServices.Common.Azure
{
    using System;
    using System.Diagnostics;

    using Microsoft.Cloud.InstrumentationFramework;

    /// <summary>
    /// Helper to correctly setup and use Geneva.
    /// </summary>
    public class GenevaHelper
    {
        /// <summary>
        /// Initailzes Geneva to report telemetry with the correct names. 
        /// </summary>
        /// <param name="tagPrefix">Tag Prefix.</param>
        public static void Initialize(TraceTagPrefixes tagPrefix)
        {
            Trace.TraceInformation($"Executing method: {nameof(GenevaHelper)}.{nameof(Initialize)}");
            Trace.TraceInformation("dump all environment variables:");
            foreach (System.Collections.DictionaryEntry de in Environment.GetEnvironmentVariables())
            {
                Trace.TraceInformation("  {0} = {1}", de.Key, de.Value);
            }

            // get env variables
            string dataCenterName = Environment.GetEnvironmentVariable("REGION_Name");
            if (dataCenterName == null)
            {
                dataCenterName = "UnknownDC";
            }

            string serviceName = Environment.GetEnvironmentVariable("Fabric_ServiceName");
            if (string.IsNullOrEmpty(serviceName))
            {
                serviceName = "UnknownService";
            }

            string dcServiceName = dataCenterName + "-" + serviceName;

            string appName = Environment.GetEnvironmentVariable("Fabric_ApplicationName");
            if (string.IsNullOrEmpty(appName))
            {
                appName = "UnknownApplication";
            }

            string instanceName = Environment.GetEnvironmentVariable("Fabric_NodeName");
            if (string.IsNullOrEmpty(instanceName))
            {

                instanceName = Environment.MachineName;
                if (string.IsNullOrEmpty(instanceName))
                {
                    instanceName = "UnknownInstance";
                }
            }

            ILogger logger = IfxTraceLogger.Instance;
            IfxTraceLogger.TagIdPrefix = $"{tagPrefix}_{appName}";

            // Initialize Geneva
            Trace.TraceInformation("Initializing Geneva, dcServiceName={0}, appName={1},instanceName={2}", dcServiceName, appName, instanceName);
            Trace.TraceInformation($"Executing method: {nameof(IfxInitializer)}.{nameof(IfxInitializer.IfxInitialize)}");
            IfxInitializer.IfxInitialize("AdgcsSesssion");
        }

        /// <summary>
        /// Initailzes Geneva to report telemetry with the correct names. 
        /// Used for initilization in Azure Functions or Web apps.
        /// </summary>
        /// <param name="tagPrefix">Tag Prefix.</param>
        /// <param name="monitoringTenant">Monitoring Tenant.</param>
        /// <param name="monitoringRole">Monitoring Role.</param>
        public static void Initialize(TraceTagPrefixes tagPrefix, string monitoringTenant, string monitoringRole, string appName)
        {
            Trace.TraceInformation($"Executing method: {nameof(GenevaHelper)}.{nameof(Initialize)}");

            System.Net.IPAddress[] addresses = System.Net.Dns.GetHostAddresses(Environment.MachineName);
            string ipAddress = null;

            foreach (var addr in addresses)
            {
                if (addr.ToString() == "127.0.0.1")
                {
                    continue;
                }
                else if (addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    ipAddress = addr.ToString();
                    break;
                }
            }

            ILogger logger = IfxTraceLogger.Instance;
            IfxTraceLogger.TagIdPrefix = $"{TraceTagPrefixes.ADGCS_PAF}_{appName}";

            // Initialize Geneva
            var initialized = $"Initializing Geneva, MonitoringTenant={monitoringTenant}, role={monitoringRole}, role_instance={ipAddress}";
            Trace.TraceInformation(initialized);
            IfxInitializer.IfxInitialize(monitoringTenant, monitoringRole, ipAddress);
            logger.Information(nameof(GenevaHelper), initialized);
        }
    }
}
