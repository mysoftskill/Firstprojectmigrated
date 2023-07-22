namespace Microsoft.PrivacyServices.CommandFeed.Client.Helpers
{
    using System;

    /// <summary>
    /// General backoff interface for all backoff implementations
    /// </summary>
    public interface IBackOff
    {
        /// <summary>
        /// Get a delay time
        /// </summary>
        /// <returns></returns>
        TimeSpan Delay();

        /// <summary>
        /// Reset the backoff
        /// </summary>
        void Reset();
    }
}