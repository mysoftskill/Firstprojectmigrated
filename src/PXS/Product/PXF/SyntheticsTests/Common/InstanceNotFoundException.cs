using System;

namespace Microsoft.Membership.MemberServices.PrivacyExperience.SyntheticsTests.Common
{
    public class InstanceNotFoundException : Exception
    {
        public InstanceNotFoundException(string instanceName)
            : base($"Error: Instance name '{instanceName}' not found.")
        {
        }
    }
}
