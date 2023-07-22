namespace Microsoft.Azure.ComplianceServices.Common.IfxMetric
{
    using System;
    using System.Text;
    using Microsoft.Cloud.InstrumentationFramework.Metrics.Extensions;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// IncomingApiEvent wrapper for metrics
    /// </summary>
    public class IncomingApiEventWrapper : ApiEvent
    {
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="IncomingApiEventWrapper"/> class.
        /// </summary>
        /// <param name="metricContainer">Specifies the metric container.</param>
        /// <param name="operationName">Specifies the operationName for metric.</param>
        /// <param name="logger">Logger</param>
        /// <param name="callerName">Specifies the callerName for metric.</param>
        public IncomingApiEventWrapper(IMetricContainer metricContainer, string operationName, ILogger logger, string callerName = "NULL")
            : base(metricContainer)
        {
            this.ComponentName = nameof(IncomingApiEventWrapper);
            this.EventType = "IncomingApiEvent";
            this.OperationName = operationName ?? throw new ArgumentNullException(this.ComponentName);
            this.CallerName = callerName;
            this.Stopwatch = new System.Diagnostics.Stopwatch();
            this.logger = logger;
            this.ErrorStatusCode = 0;
            this.ErrorType = string.Empty;    
        }

        /// <summary>
        /// Indicates an operation.
        /// </summary>
        public string OperationName { get; set; }

        /// <summary>
        ///      Perform operation telemetry commencement tasks.
        /// </summary>
        public override void Finish()
        {
            if (!this.IsStarted)
            {
                throw new InvalidOperationException("Start() must be called before Finish().");
            }

            this.Stopwatch.Stop();
            var elapsedTime = this.Stopwatch.ElapsedMilliseconds;
            this.logger.Information(this.ComponentName, $"Finished with ElapsedTime: " + elapsedTime.ToString());
            StringBuilder dimensions = new StringBuilder("IncomingApiEventWrapper => [Additional Dimensions]: ");
            dimensions.Append($"this.EventType: {this.EventType}, this.OpertionName: {this.OperationName},  this.Success: {this.Success}, this.CallerName : {this.CallerName}");
            var dimVal = DimensionValues.Create(this.EventType, this.OperationName, this.Success.ToString(), this.CallerName);
            var errorDimVal = DimensionValues.Create(this.ErrorStatusCode.ToString(), this.ErrorType, this.OperationName, this.CallerName);
            if (this.Success)
            {
                if (!this.MetricContainer.IncomingMetric.Set(DateTime.UtcNow, 100, dimVal))
                {
                    this.logger.Error(this.ComponentName, $"Error setting successful incomingMetric {dimensions}");
                }

                var successDimVal = DimensionValues.Create(this.EventType, this.OperationName, this.CallerName);
                if (!this.MetricContainer.IncomingSuccessLatencyMetric.Set(DateTime.UtcNow, (ulong)elapsedTime, successDimVal))
                {
                    this.logger.Error(this.ComponentName, "Error setting incomingSuccessMetric");
                }
                if (!this.MetricContainer.IncomingApiErrorCountMetric.Set(DateTime.UtcNow, 0, errorDimVal))
                {
                    this.logger.Error(this.ComponentName, "Error setting IncomingApiErrorCountMetric to 0");
                }
            }
            else
            {
                if (!this.MetricContainer.IncomingMetric.Set(DateTime.UtcNow, 0, dimVal))
                {
                    throw new Exception("Error setting unsuccessful Incoming metric");
                }
                if (!this.MetricContainer.IncomingApiErrorCountMetric.Set(DateTime.UtcNow, 1, errorDimVal))
                {
                    this.logger.Error(this.ComponentName, "Error setting IncomingApiErrorCountMetric to 1");
                }
            }
        }
    }
}
