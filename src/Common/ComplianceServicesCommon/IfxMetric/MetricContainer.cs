namespace Microsoft.Azure.ComplianceServices.Common.IfxMetric
{
    using System.Collections.Generic;
    using Microsoft.Cloud.InstrumentationFramework.Metrics.Extensions;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// Stores the instances of all the metrics.
    /// </summary>
    public class MetricContainer : IMetricContainer
    {
        private const string ComponentName = nameof(MetricContainer);
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricContainer"/> class.
        /// </summary>
        /// <param name="environment">Environment for service</param>
        /// <param name="role"> Prefix for metric Name-[Service Role] </param>
        /// <param name="roleInstance">Role Instance</param>
        /// <param name="metricAccount">Metric Account</param>
        /// <param name="logger">Logger</param>
        public MetricContainer(string environment, string role, string roleInstance, string metricAccount, string MetricPrefixName, ILogger logger)
        {
            this.logger = logger;
            this.CustomMetricDictionary = new Dictionary<string, IMetric>();

            MdmMetricController.AddDefaultDimension("Environment", environment);
            MdmMetricController.AddDefaultDimension("Role", role);
            MdmMetricController.AddDefaultDimension("RoleInstance", roleInstance);

            if (!MdmMetricController.StartMetricPublication())
            {
                this.logger.Information(ComponentName, "Failed to start metric publication.");
                return;
            }

            this.Factory = new MdmMetricFactory();
            this.CreateMetric(metricAccount, MetricPrefixName);
        }

        /// <summary>
        /// Factory
        /// </summary>
        public MdmMetricFactory Factory { get; set; }

        /// <inheritdoc/>
        public IMdmMetric<DimensionValues4D, ulong> IncomingMetric { get; set; }

        /// <inheritdoc/>
        public IMdmMetric<DimensionValues5D, ulong> OutgoingMetric { get; set; }

        /// <inheritdoc/>
        public IMdmMetric<DimensionValues3D, ulong> IncomingSuccessLatencyMetric { get; set; }

        /// <inheritdoc/>
        public IMdmMetric<DimensionValues4D, ulong> OutgoingSuccessLatencyMetric { get; set; }

        /// <inheritdoc/>
        public IMdmMetric<DimensionValues4D, ulong> IncomingApiErrorCountMetric { get; set; }

        /// <inheritdoc/>
        public IMdmMetric<DimensionValues5D, ulong> OutgoingApiErrorCountMetric { get; set; }

        /// <summary>
        /// Custom 2D Metric
        /// </summary>
        public Dictionary<string, IMetric> CustomMetricDictionary { get; set; }

        /// <summary>
        /// Creating Metrics
        /// </summary>
        /// <param name="metricAccount">Metric Account</param>
        /// <param name="prefix">Prefix for metrics</param>
        public void CreateMetric(string metricAccount, string prefix)
        {
            this.logger.Information(ComponentName, $"Creating metric with prefix -> {prefix}");
            this.logger.Information(ComponentName, $"Metric Account used -> {metricAccount}");

            this.IncomingMetric = this.Factory.CreateUInt64Metric(MdmMetricFlags.IncludeDefaultDimensions, metricAccount, "ApplicationMetrics", $"{prefix}IncomingApiReliability", "TypeOfEvent", "OperationName", "Success", "CallerName");
            this.IncomingSuccessLatencyMetric = this.Factory.CreateUInt64Metric(MdmMetricFlags.IncludeDefaultDimensions, metricAccount, "ApplicationMetrics", $"{prefix}IncomingApiSuccessLatency", "TypeOfEvent", "OperationName", "CallerName");
            this.IncomingApiErrorCountMetric = this.Factory.CreateUInt64Metric(MdmMetricFlags.IncludeDefaultDimensions, metricAccount, "ApplicationMetrics", $"{prefix}IncomingApiErrorCount", "ErrorStatusCode", "ErrorType", "OperationName", "CallerName");

            this.OutgoingMetric = this.Factory.CreateUInt64Metric(MdmMetricFlags.IncludeDefaultDimensions, metricAccount, "ApplicationMetrics", $"{prefix}OutgoingApiReliability", "TypeOfEvent", "DependencyName", "DependencyOperationName", "Success", "CallerName");
            this.OutgoingSuccessLatencyMetric = this.Factory.CreateUInt64Metric(MdmMetricFlags.IncludeDefaultDimensions, metricAccount, "ApplicationMetrics", $"{prefix}OutgoingApiSuccessLatency", "TypeOfEvent", "DependencyName", "DependencyOperationName", "CallerName");
            this.OutgoingApiErrorCountMetric = this.Factory.CreateUInt64Metric(MdmMetricFlags.IncludeDefaultDimensions, metricAccount, "ApplicationMetrics", $"{prefix}OutgoingApiErrorCount", "ErrorStatusCode", "ErrorType", "DependencyName", "DependencyOperationName", "CallerName");

            if (this.OutgoingMetric == null || this.IncomingMetric == null || this.IncomingSuccessLatencyMetric == null || this.OutgoingSuccessLatencyMetric == null || this.IncomingApiErrorCountMetric == null || this.OutgoingApiErrorCountMetric == null)
            {
                this.logger.Error(ComponentName, "Failed to create metric.");
            }
        }

        /// <summary>
        /// Add metric to container
        /// </summary>
        /// <param name="metricName">Metric Name</param>
        /// <param name="metric">Metric value</param>
        public void AddMetric(string metricName, IMetric metric)
        {
            this.CustomMetricDictionary.Add(metricName, metric);
        }
    }
}
