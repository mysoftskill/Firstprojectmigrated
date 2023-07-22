namespace Microsoft.PrivacyServices.DataManagement.Common
{
    using System;

    /// <summary>
    /// Interface of creating date time. This is used so that we can mock specific DateTimeOffset for testing.
    /// </summary>
    public interface IDateFactory
    {
        /// <summary>
        /// Get the current time.
        /// </summary>
        /// <returns>A DateTimeOffset object having the value set as the current date time offset.</returns>
        DateTimeOffset GetCurrentTime();
    }

    /// <summary>
    /// An implementation of the IDateFactory interface to get the actual time.
    /// </summary>
    public class DateFactory : IDateFactory
    {
        /// <summary>
        /// Get the current time.
        /// </summary>
        /// <returns>A DateTimeOffset object having the value set as the current date time offset.</returns>
        public DateTimeOffset GetCurrentTime()
        {
            return DateTimeOffset.UtcNow;
        }
    }
}