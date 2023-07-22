using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.PrivacyServices.UX.Core.Flighting
{
    /// <summary>
    /// Provides mocked implementation of <see cref="IGroundControl"/>.
    /// </summary>
    /// <remarks>
    /// This implementation exists to allow dynamic control of certain mocked behaviors.
    /// </remarks>
    public class DynamicMockGroundControl : IGroundControl
    {
        private readonly MockGroundControlScenarioHelper scenarioHelper;

        public DynamicMockGroundControl(MockGroundControlScenarioHelper scenarioHelper)
        {
            this.scenarioHelper = scenarioHelper ?? throw new ArgumentNullException(nameof(scenarioHelper));
        }

        public Task<IEnumerable<string>> GetUserFlights(IReadOnlyDictionary<string, string> additionalProps = null)
        {
            return (Task<IEnumerable<string>>)scenarioHelper.GetMethodMock("GroundControl.GetUserFlights").DynamicInvoke();
        }

        public async Task<bool> IsUserInFlight(string flightName, IReadOnlyDictionary<string, string> additionalProps = null)
        {
            return (await GetUserFlights(additionalProps)).Contains(flightName, StringComparer.OrdinalIgnoreCase);
        }
    }
}
