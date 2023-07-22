namespace PCF.UnitTests.Pdms
{
    using System;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.Policy;
    
    public static class PcfTestCapabilityExtension
    {
        public static CapabilityId GetCapabilityId(this PcfTestCapability pcfTestCapability)
        {
            CapabilityId capabilityId;

            switch (pcfTestCapability)
            {
                case PcfTestCapability.AccountClose:
                    capabilityId = Policies.Current.Capabilities.Ids.AccountClose;
                    break;
                case PcfTestCapability.Delete:
                    capabilityId = Policies.Current.Capabilities.Ids.Delete;
                    break;
                case PcfTestCapability.Export:
                    capabilityId = Policies.Current.Capabilities.Ids.Export;
                    break;
                default:
                    throw new ArgumentException($"Unknown privacy command type: {pcfTestCapability}.");
            }

            return capabilityId;
        }

        public static PrivacyCommandType GetPrivacyCommandType(this PcfTestCapability pcfTestCapability)
        {
            PrivacyCommandType privacyCommandType;

            switch (pcfTestCapability)
            {
                case PcfTestCapability.AccountClose:
                    privacyCommandType = PrivacyCommandType.AccountClose;
                    break;
                case PcfTestCapability.Delete:
                    privacyCommandType = PrivacyCommandType.Delete;
                    break;
                case PcfTestCapability.Export:
                    privacyCommandType = PrivacyCommandType.Export;
                    break;
                default:
                    throw new ArgumentException($"Unknown privacy command type: {pcfTestCapability}.");
            }

            return privacyCommandType;
        }
    }
}
