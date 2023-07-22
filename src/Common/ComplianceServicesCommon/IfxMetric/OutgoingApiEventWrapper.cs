namespace Microsoft.Azure.ComplianceServices.Common.IfxMetric
{
    using System;
    using System.Text;
    using Microsoft.Cloud.InstrumentationFramework.Metrics.Extensions;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// OutgoingApiEvent wrapper for metrics
    /// </summary>
    public class OutgoingApiEventWrapper : ApiEvent
    {
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="OutgoingApiEventWrapper"/> class.
        /// </summary>
        /// <param name="metricContainer">Specifies the metric container.</param>
        /// <param name="dependencyName">Specifies the dependencyName for metric.</param>
        /// <param name="dependencyOperationName">Specifies the dependencyOperationName for metric.</param>
        /// <param name="logger">Logger</param>
        /// <param name="callerName">Specifies the callerName for metric.</param>
        public OutgoingApiEventWrapper(IMetricContainer metricContainer, string dependencyName, string dependencyOperationName, ILogger logger, string callerName = "NULL")
            : base(metricContainer)
        {
            this.ComponentName = nameof(OutgoingApiEventWrapper);
            this.EventType = "OutgoingApiEvent";
            this.DependencyName = dependencyName ?? throw new ArgumentNullException(this.ComponentName);
            this.DependencyOperationName = dependencyOperationName ?? throw new ArgumentNullException(this.ComponentName);
            this.CallerName = callerName;
            this.Stopwatch = new System.Diagnostics.Stopwatch();
            this.logger = logger;
            this.ErrorStatusCode = 0;
            this.ErrorType = string.Empty;
        }

        /// <summary>
        /// Indicates an dependency name.
        /// </summary>
        public string DependencyName { get; set; }

        /// <summary>
        /// Indicates an dependency operation name.
        /// </summary>
        public string DependencyOperationName { get; set; }

        /// <summary>
        ///     Perform operation telemetry commencement tasks.
        /// </summary>
        public override void Finish()
        {
            if (!this.IsStarted)
            {
                throw new InvalidOperationException("Start() must be called before Finish().");
            }

            this.Stopwatch.Stop();
            var elapsedTime = this.Stopwatch.ElapsedMilliseconds;
            this.logger.Information(this.ComponentName, $"Finish ElapsedTime: " + elapsedTime.ToString());
            StringBuilder dimensions = new StringBuilder("OutgoingApiEventWrapper => [Additional Dimensions]: ");
            dimensions.Append($"this.EventType: {this.EventType}, this.DependencyName: {this.DependencyName}, ");
            dimensions.Append($"this.DependencyOperationName:{this.DependencyOperationName}, this.Success: {this.Success}, this.CallerName: {this.CallerName}");
            var dimVal = DimensionValues.Create(this.EventType, this.DependencyName, this.DependencyOperationName, this.Success.ToString(), this.CallerName);
            var errorDimVal = DimensionValues.Create(this.ErrorStatusCode.ToString(), this.ErrorType, this.DependencyName, this.DependencyOperationName, this.CallerName);
            if (this.Success)
            {  
                this.MetricContainer.OutgoingMetric.Set(DateTime.UtcNow, 100, dimVal);
                var successDimVal = DimensionValues.Create(this.EventType, this.DependencyName, this.DependencyOperationName, this.CallerName);
                if (!this.MetricContainer.OutgoingSuccessLatencyMetric.Set(DateTime.UtcNow, (ulong)elapsedTime, successDimVal))
                {
                    this.logger.Error(this.ComponentName, $"Error setting successful Outgoing metric {dimensions}");
                }
                if (!this.MetricContainer.OutgoingApiErrorCountMetric.Set(DateTime.UtcNow, 0, errorDimVal))
                {
                    this.logger.Error(this.ComponentName, "Error setting OutgoingApiErrorCountMetric to 0");
                }
            }
            else
            {
                if (!this.MetricContainer.OutgoingMetric.Set(DateTime.UtcNow, 0, dimVal))
                {
                    this.logger.Error(this.ComponentName, "Error setting unsuccessful Outgoing metric");
                }
                if (!this.MetricContainer.OutgoingApiErrorCountMetric.Set(DateTime.UtcNow, 1, errorDimVal))
                {
                    this.logger.Error(this.ComponentName, "Error setting unsuccessful OutgoingApiErrorCountMetric to 1");
                }
            }
        }
    }
}
