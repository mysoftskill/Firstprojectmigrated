using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Microsoft.PrivacyServices.UX.I9n.Scenario
{
    public class ScenarioConfigurator
    {
        private readonly IDictionary<string, IDictionary<string, Func<object>>> scenarioMap = new Dictionary<string, IDictionary<string, Func<object>>>();
        private readonly IEnumerable<string> scenarios;

        public ScenarioConfigurator(IHttpContextAccessor httpContextAccessor)
        {
            if (null == httpContextAccessor)
            {
                throw new ArgumentNullException(nameof(httpContextAccessor));
            }

            HttpRequest request = httpContextAccessor.HttpContext.Request;
            if (request.Headers.Keys.Contains("X-Scenarios"))
            {
                scenarios = new List<string>(
                    request.Headers.GetCommaSeparatedValues("X-Scenarios"));
            }
            else
            {
                scenarios = new List<string>{ Scenario.Default.Name };
            }
        }

        public void ConfigureMethodMock(ScenarioName scenarioName, string methodKey, Func<object> func)
        {
            if (!scenarioMap.ContainsKey(scenarioName.Name))
            {
                scenarioMap[scenarioName.Name] = new Dictionary<string, Func<object>>()
                {
                    {methodKey, func}
                };
            }
            else
            {
                scenarioMap[scenarioName.Name][methodKey] = func;
            }
        }

        public void ConfigureDefaultMethodMock(string methodKey, Func<object> func)
        { 
            ConfigureMethodMock(Scenario.Default, methodKey, func);
        }

        public Func<object> GetMethodMock(string methodKey)
        {
            var defaultScenario = Scenario.Default.Name;

            //  First look for the exact match in the map.
            foreach (string scenarioName in scenarios)
            {
                if (scenarioMap.ContainsKey(scenarioName) && scenarioMap[scenarioName].ContainsKey(methodKey))
                {
                    return scenarioMap[scenarioName][methodKey];
                }
            }

            //  If not found, do part wise search on each scenario. This will attempt to find base scenarios.
            foreach (string scenarioName in scenarios)
            {
                var scenarioNamePart = scenarioName;
                while (scenarioNamePart.LastIndexOf(".") > 0)
                {
                    //  Partition on delimiter to extract parent scenarios.
                    scenarioNamePart = scenarioNamePart.Substring(0, scenarioNamePart.LastIndexOf("."));

                    //  Look for the parent scenario within map. 
                    if (scenarioMap.ContainsKey(scenarioNamePart) && scenarioMap[scenarioNamePart].ContainsKey(methodKey))
                    {
                        return scenarioMap[scenarioNamePart][methodKey];
                    }
                }
            }

            //  If not found, look up the default scenario.
            if (scenarioMap.ContainsKey(defaultScenario) && 
                scenarioMap[defaultScenario].ContainsKey(methodKey))
            {
                return scenarioMap[defaultScenario][methodKey];
            }

            //  If still not found, throw up.
            throw new NotImplementedException($"Mock of the method {methodKey} does not exist in scenarios {scenarios}.");
        }
    }
}
