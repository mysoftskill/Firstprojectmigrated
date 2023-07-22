using System;

namespace Microsoft.PrivacyServices.UX.I9n.Scenario
{
    /// <summary>
    /// Represents each scenario entity.
    /// </summary>
    public class ScenarioName
    {
        public ScenarioName(string name)
        {
             Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public string Name { get; }
    }
}
