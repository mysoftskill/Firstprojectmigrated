namespace Microsoft.Azure.ComplianceServices.Common.IfxMetric
{
    using System;
    using System.Text;
    using Microsoft.Cloud.InstrumentationFramework.Metrics.Extensions;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// Defines the Metric elements
    /// </summary>
    public class Metric2D : IMetric
    {
        private const string ComponentName = nameof(Metric2D);
        private readonly ILogger logger;

        /// <summary>
        ///    Initializes a new instance of the <see cref="Metric2D"/> class.
        /// </summary>
        /// <param name="factory">MdmMetricFactory</param>
        /// <param name="metricAccount">Metric Account</param>
        /// <param name = "metricName" > Metric Name </param>
        /// <param name="dimensionName1">Dimension 1</param>
        /// <param name = "dimensionName2" > Dimension 2 </param>
        /// <param name="logger">Logger</param>
        /// <param name = "metricNamespace" >Metric Namespace [default - ApplicationMetrics] </param>
        public Metric2D(MdmMetricFactory factory, string metricAccount, string metricName, string dimensionName1, string dimensionName2, ILogger logger, string metricNamespace = "ApplicationMetrics")
        {
            this.MetricContainerFactory = factory;
            this.MetricName = metricName;
            this.logger = logger;
            this.Metric = this.MetricContainerFactory.CreateUInt64Metric(MdmMetricFlags.IncludeDefaultDimensions, metricAccount, metricNamespace, metricName, dimensionName1, dimensionName2);
        }

        /// <summary>
        ///     Metric container
        /// </summary>
        private MdmMetricFactory MetricContainerFactory { get; }

        /// <summary>
        ///     Metric Name
        /// </summary>
        private string MetricName { get; }

        /// <summary>
        ///     Metric
        /// </summary>
        private IMdmMetric<DimensionValues2D, ulong> Metric { get; set; }

        /// <summary>
        ///   Wrapper to set the metric value
        /// </summary>
        /// <param name="val">Metric value.</param>
        /// <param name="dimensionList">List of dimensions</param>
        public void SetUInt64Metric(uint val, string[] dimensionList)
        {
            this.Set2DUInt64Metric(val, dimensionList[0], dimensionList[1]);
        }

        private void Set2DUInt64Metric(uint val, string dim1, string dim2)
        {
            if (string.IsNullOrEmpty(dim1) || string.IsNullOrEmpty(dim2))
            {
                this.logger.Error(ComponentName, "one or both dimensions are null or empty");
            }

            StringBuilder dimensions = new StringBuilder("Metric2D Set2DUInt64Metric => ");
            dimensions.Append($"AccountName: {dim1}, QueueName: {dim2}");
            var dimVal = DimensionValues.Create(dim1, dim2);
            if (!this.Metric.Set(DateTime.UtcNow, val, dimVal))
            {
                this.logger.Error(ComponentName, "Error setting 2DimensionUIntMetric");
            }

            this.logger.Information(ComponentName, $"Logged QueueDepth: {dimensions} with value {val}");
        }
    }
}
