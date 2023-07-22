namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.Azure.ComplianceServices.Common.Interfaces;

    /// <summary>
    /// Defines utility methods for creating, indexing, and retrieving performance counters.
    /// </summary>
    public static class PerfCounterUtility
    {
        /// <summary>
        /// A legacy shim that pipes through to the current hosting environment.
        /// </summary>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public static IPerformanceCounter GetOrCreate(PerformanceCounterType type, string name)
        {
            if (string.IsNullOrWhiteSpace(EnvironmentInfo.ServiceName))
            {
                throw new InvalidOperationException("EnvironmentInfo.Initialize must be called.");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("name");
            }

            return EnvironmentInfo.HostingEnvironment.GetOrCreatePerformanceCounter(type, name);
        }
    }
}
