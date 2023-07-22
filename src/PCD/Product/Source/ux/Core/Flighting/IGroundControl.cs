using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.PrivacyServices.UX.Core.Flighting
{
    /// <summary>
    /// Provides access to flight information.
    /// </summary>
    public interface IGroundControl
    {
        /// <summary>
        /// Evaluates whether the user is in a particular flight.
        /// </summary>
        /// <param name="flightName">Flight name to check.</param>
        /// <param name="additionalProps">Optional additional properties to use when evaluating flights.</param>
        Task<bool> IsUserInFlight(string flightName, IReadOnlyDictionary<string, string> additionalProps = null);

        /// <summary>
        /// Gets all flights the user is part of.
        /// </summary>
        /// <param name="additionalProps">Optional additional properties to use when evaluating flights.</param>
        Task<IEnumerable<string>> GetUserFlights(IReadOnlyDictionary<string, string> additionalProps = null);
    }
}
