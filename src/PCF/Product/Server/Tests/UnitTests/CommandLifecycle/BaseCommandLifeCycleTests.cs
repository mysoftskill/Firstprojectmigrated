using Microsoft.Azure.ComplianceServices.Common;
using Microsoft.PrivacyServices.CommandFeed.Service.Common;

namespace PCF.UnitTests.CommandLifecycle
{
    public class BaseCommandLifeCycleTests
    {
        public void Initialize()
        {
            // Currently, FlightingUtilities is not initialized for Test context if we need to read ConfigValues. Opened up a task for it: 1348710 
            FlightingUtilities.Initialize(new AppConfiguration("local.settings.test.json"));
        }
    }
}
