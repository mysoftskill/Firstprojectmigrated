namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi.Throttling
{
    using WebApiThrottle;

    /// <summary>
    /// Logging class for the throttle layer.
    /// </summary>
    public class ThrottleLogger : IThrottleLogger
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ThrottleLogger" /> class.
        /// </summary>
        public ThrottleLogger()
        {
        }

        /// <summary>
        /// Logs the throttle information.
        /// </summary>
        /// <param name="entry">The throttle information.</param>
        public void Log(ThrottleLogEntry entry)
        {
        }
    }
}