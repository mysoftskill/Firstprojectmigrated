namespace Microsoft.Azure.ComplianceServices.Common.IfxMetric
{
    /// <summary>
    /// Defines the Metric parameters
    /// </summary>
    public interface IApiEvent
    {
        /// <summary>
        ///     Metric container
        /// </summary>
        IMetricContainer MetricContainer { get; }

        /// <summary>
        ///     Specifies the CallerName
        /// </summary>
        string CallerName { get; set; }

        /// <summary>
        ///     True if the operation was a success; false otherwise.
        /// </summary>
        bool Success { get; set; }

        /// <summary>
        ///     Error Type
        /// </summary>
        string ErrorType { get; set; }

        /// <summary>
        ///     Error Status Code
        /// </summary>
        int ErrorStatusCode { get; set; }

        /// <summary>
        ///     start Method
        /// </summary>
        void Start();

        /// <summary>
        ///     Finish Method
        /// </summary>
        void Finish();
    }
}
