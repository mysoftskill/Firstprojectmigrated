using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.PrivacyServices.UX.I9n.Scenario;

namespace Microsoft.PrivacyServices.UX.Core.Flighting
{
    /// <summary>
    /// Scenario helper for <see cref="IGroundControl"/> mocks.
    /// </summary>
    public class MockGroundControlScenarioHelper
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ScenarioConfigurator scenarioConfigurator;

        public MockGroundControlScenarioHelper(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            scenarioConfigurator = new ScenarioConfigurator(httpContextAccessor);

            ConfigureAllScenarios();
        }

        public Func<object> GetMethodMock(string methodName)
        {
            return scenarioConfigurator.GetMethodMock(methodName);
        }

        private void ConfigureAllScenarios()
        {
            scenarioConfigurator.ConfigureDefaultMethodMock(
                "GroundControl.GetUserFlights",
                () => Task.FromResult(GetListOfFlightsFromHeaders()));

            scenarioConfigurator.ConfigureMethodMock(
                Scenario.Flighting.NoFlights,
                "GroundControl.GetUserFlights",
                () => Task.FromResult(Enumerable.Empty<string>()));
        }

        private IEnumerable<string> GetListOfFlightsFromHeaders()
        {
            return httpContextAccessor.HttpContext.Request.Headers.GetCommaSeparatedValues("X-Flights");
        }
    }
}
