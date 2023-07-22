namespace Microsoft.PrivacyServices.CommandFeed.Service.Autoscaler
{
    /// <summary>
    /// Statistics summary for the collection. Contains two data points, the set of "recent"
    /// throttle stats, and the set of "older" throttle stats.
    /// </summary>
    public class ThrottleSummary
    {
        /// <summary>
        /// Total requests across the whole period.
        /// </summary>
        public long TotalRecentRequests { get; set; }

        /// <summary>
        /// Total throttled requests across the whole period.
        /// </summary>
        public long TotalRecentThrottledRequests { get; set; }

        /// <summary>
        /// The overall success rate.
        /// </summary>
        public double RecentSuccessRate => (double)(this.TotalRecentRequests - this.TotalRecentThrottledRequests) / (double)this.TotalRecentRequests;

        /// <summary>
        /// Total requests across the whole period.
        /// </summary>
        public long TotalOlderRequests { get; set; }

        /// <summary>
        /// Total throttled requests across the whole period.
        /// </summary>
        public long TotalOlderThrottledRequests { get; set; }

        /// <summary>
        /// The overall success rate.
        /// </summary>
        public double OlderSuccessRate => (double)(this.TotalOlderRequests - this.TotalOlderThrottledRequests) / (double)this.TotalOlderRequests;
    }
}
