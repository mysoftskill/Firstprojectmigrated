namespace Microsoft.Azure.ComplianceServices.Common.IfxMetric
{
    using System;
    using System.Diagnostics;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// ApiEvent wrapper for metrics
    /// </summary>
    public class ApiEvent : IApiEvent
    {
        /// <summary>
        ///    Initializes a new instance of the <see cref="ApiEvent"/> class.
        /// </summary>
        /// <param name="metricContainer">Specifies the metric container</param>
        public ApiEvent(IMetricContainer metricContainer)
        {
            this.MetricContainer = metricContainer;
            this.IsStarted = false;
        }

        /// <summary>
        ///     Event start time
        /// </summary>
        public string  ComponentName { get; set; }

        /// <summary>
        ///     Event start time
        /// </summary>
        protected bool IsStarted { get; set; }
        /// <summary>
        ///     Event start time
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        ///     Metric container
        /// </summary>
        public IMetricContainer MetricContainer { get; }

        /// <summary>
        ///     Specifies the CallerName
        /// </summary>
        public string CallerName { get; set; }

        /// <summary>
        ///     Specifies the eventType Incoming/Outgoing
        /// </summary>
        public string EventType { get; set; }

        /// <summary>
        ///     True if the operation was a success; false otherwise.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        ///     Error Type
        /// </summary>
        public string ErrorType { get; set; }

        /// <summary>
        ///     Error Status Code
        /// </summary>
        public int ErrorStatusCode { get; set; }

        /// <summary>
        ///     Gets or sets the stopwatch used to calculate duration
        /// </summary>
        public Stopwatch Stopwatch { get; set; }

        /// <summary>
        ///     This method starts the wrapper event
        /// </summary>
        public virtual void Start()
        {
            if (this.IsStarted)
            {
                throw new InvalidOperationException("Finish() must be called after Start().");
            }

            this.IsStarted = true;
            this.StartTime = DateTime.Now;
            this.Stopwatch.Start();
        }

        /// <summary>
        ///     This method Ends the wrapper event
        /// </summary>
        public virtual void Finish()
        {
        }
    }
}
