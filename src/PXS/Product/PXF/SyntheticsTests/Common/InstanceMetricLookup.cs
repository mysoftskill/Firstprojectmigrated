using System;
using System.Collections.Generic;

namespace Microsoft.Membership.MemberServices.PrivacyExperience.SyntheticsTests.Common
{
    public static class InstanceMetricLookup
    {
        private static readonly Dictionary<string, string> _instanceMetricMap;

        static InstanceMetricLookup()
        {
            _instanceMetricMap = new Dictionary<string, string>
        {
            { "Fairfax", "NGPProxy" },
            { "AME", "adgcsMsAzGsProd" },
            { "PPE", "adgcsMsAzGsProd" }
            // Add more instance names and metric namespaces as needed
        };
        }

        public static string GetMetricNamespace(string instanceNamePrefix)
        {
            if (_instanceMetricMap.TryGetValue(instanceNamePrefix, out string metricNamespace))
            {
                return metricNamespace;
            }
            else
            {
                // Handle the case when the instance name is not found in the dictionary
                Console.WriteLine($"Error: Instance name prefix'{instanceNamePrefix}' not found.");
                throw new InstanceNotFoundException(instanceNamePrefix);
            }
        }
    }
}
