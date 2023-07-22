namespace Microsoft.Azure.ComplianceServices.Common.IfxMetric
{
    /// <summary>
    /// Defines the Metric elements
    /// </summary>
    public interface IMetric
    {
        /// <summary>
        ///   Wrapper to set the metric with UInt value with 2 dimensions
        /// </summary>
        /// <param name="val">Metric value.</param>
        /// <param name="dimensionList">List of dimensions</param>
        void SetUInt64Metric(uint val, string[] dimensionList);
    }
}
