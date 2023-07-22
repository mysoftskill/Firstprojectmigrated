namespace Microsoft.Azure.ComplianceServices.Common.IfxMetric
{
    using System.Collections.Generic;
    using Microsoft.Cloud.InstrumentationFramework.Metrics.Extensions;

    /// <summary>
    /// Defines all the Metrics used in the Azure Function
    /// </summary>
    public interface IMetricContainer
    {
        /// <summary>
        /// Factory
        /// </summary>
        MdmMetricFactory Factory { get; set; }

        /// <summary>
        /// Incoming Test Metric
        /// </summary>
        IMdmMetric<DimensionValues4D, ulong> IncomingMetric { get; set; }

        /// <summary>
        /// Outgoing Test Metric
        /// </summary>
        IMdmMetric<DimensionValues5D, ulong> OutgoingMetric { get; set; }

        /// <summary>
        /// Incoming Test Success Latency Metric
        /// </summary>
        IMdmMetric<DimensionValues3D, ulong> IncomingSuccessLatencyMetric { get; set; }

        /// <summary>
        /// Outgoing Test Success Latency Metric
        /// </summary>
        IMdmMetric<DimensionValues4D, ulong> OutgoingSuccessLatencyMetric { get; set; }

        /// <summary>
        /// Incoming Api Error Count Metric
        /// </summary>
        IMdmMetric<DimensionValues4D, ulong> IncomingApiErrorCountMetric { get; set; }

        /// <summary>
        /// Outgoing Api Error count Metric
        /// </summary>
        IMdmMetric<DimensionValues5D, ulong> OutgoingApiErrorCountMetric { get; set; }

        /// <summary>
        /// Custom Metric Directory
        /// </summary>
        Dictionary<string, IMetric> CustomMetricDictionary { get; set; }

        /// <summary>
        /// Create all metrics
        /// </summary>
        /// <param name="metricAccount">Metric Account</param>
        /// <param name="prefix">Prefix for metrics</param>
        void CreateMetric(string metricAccount, string prefix);

        /// <summary>
        /// Add metric to container
        /// </summary>
        /// <param name="metricName">Metric Name</param>
        /// <param name="metric">Metric value</param>
        void AddMetric(string metricName, IMetric metric);
    }
}