namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System.Collections.Generic;

    /// <summary>
    /// flighting context.
    /// </summary>
    public interface IFlightContext
    {
        /// <summary>
        /// Determines whether a feature is enabled.
        /// </summary>
        /// <param name="flightName">The name of the feature.</param>
        /// <param name="parameters">The list of flighting parameters. Feature needs to be enabled for all parameters</param>
        /// <returns>True if enabled.</returns>
        bool IsEnabledAll<TContext>(string flightName, IEnumerable<TContext> parameters);

        /// <summary>
        /// Determines whether a feature is enabled.
        /// </summary>
        /// <param name="flightName">The name of the feature.</param>
        /// <param name="parameters">The list of flighting parameters.</param>
        /// <returns>True if enabled.</returns>
        bool IsEnabledAny<TContext>(string flightName, IEnumerable<TContext> parameters);

        /// <summary>
        /// Determines whether a feature is enabled.
        /// </summary>
        /// <param name="flightName">The name of the feature.</param>
        /// <param name="parameters">flighting parameters.</param>
        /// <param name="useCached">boolean indicating whether cache should be checked first for the feature.</param>
        /// <returns>True if enabled.</returns>
        bool IsEnabled<TContext>(string flightName, TContext parameters, bool useCached = true);

        /// <summary>
        /// Determines whether a feature is enabled.
        /// </summary>
        /// <param name="flightName">The name of the feature.</param>
        /// <param name="useCached">boolean indicating whether cache should be checked first for the feature.</param>
        /// <returns>True if enabled.</returns>
        bool IsEnabled(string flightName, bool useCached = true);
    }
}
