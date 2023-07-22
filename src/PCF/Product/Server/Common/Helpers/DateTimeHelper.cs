namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;

    /// <summary>
    /// Defines extension methods for DateTime and DateTimeOffset.
    /// </summary>
    public static class DateTimeHelper
    {
        /// <summary>
        /// Normalizes the given date time offset to UTC and the nearest millisecond.
        /// </summary>
        public static DateTimeOffset ToNearestMsUtc(this DateTimeOffset dateTimeOffset)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(dateTimeOffset.ToUniversalTime().ToUnixTimeMilliseconds());
        }

        /// <summary>
        /// Truncate Hours, Minutes, Seconds of the given date time offset 
        /// </summary>
        public static DateTimeOffset GetDate(this DateTimeOffset dateTimeOffset)
        {
            return new DateTimeOffset(
                dateTimeOffset.Year,
                dateTimeOffset.Month,
                dateTimeOffset.Day,
                0,
                0,
                0,
                dateTimeOffset.Offset);
        }

        /// <summary>
        /// Calculate live TTL seconds from document created time
        /// Minimum live TTL seconds is rounded to 60secs in case of edge conditions
        /// </summary>
        public static int GetTimeToLiveSeconds(DateTimeOffset absoluteExpirationTime)
        {
            TimeSpan timeUntilExpiration = absoluteExpirationTime.ToNearestMsUtc() - DateTimeOffset.UtcNow;
            TimeSpan minimumTimeToLive = TimeSpan.FromMinutes(1);

            if (timeUntilExpiration <= minimumTimeToLive)
            {
                // Default to a low value for expiration in cases of edge conditions.
                timeUntilExpiration = minimumTimeToLive;
            }

            // Add one to avoid decreasing the TTL due to casting the double to int.
            return (int)timeUntilExpiration.TotalSeconds + 1;
        }
    }
}
