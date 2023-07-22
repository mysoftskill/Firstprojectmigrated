// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Helpers
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Membership.MemberServices.Configuration;

    /// <summary>
    ///     Helps with filght and ring mappings
    /// </summary>
    public static class FlightRingHelper
    {
        /// <summary>
        ///     Calculates the target ring type, based on the request context.
        /// </summary>
        /// <param name="context">The request context</param>
        /// <param name="flightConfigurations">The flight configurations supported</param>
        /// <param name="defaultRingType">The default ring type to use, if no supported flights are found</param>
        /// <returns>The target <see cref="RingType" /></returns>
        public static RingType CalculateTargetRingType(IPxfRequestContext context, IList<IFlightConfiguration> flightConfigurations, RingType defaultRingType)
        {
            // If user has no flights, or there are no flight configurations defined, use the defaults.
            if (context.Flights == null || context.Flights.Length == 0 ||
                flightConfigurations == null || flightConfigurations.Count == 0)
            {
                return defaultRingType;
            }

            // Flights are in priority order. Find the first match with the user's request context.
            foreach (IFlightConfiguration flight in flightConfigurations)
            {
                if (context.Flights.Contains(flight.FlightName))
                {
                    return flight.Ring;
                }
            }

            // If no matches found, use defaults.
            return defaultRingType;
        }
    }
}
