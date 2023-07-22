namespace Microsoft.Membership.MemberServices.PrivacyAdapters.AnaheimIdAdapter
{
    using System;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using global::Azure.Identity;
    using global::Azure.Core;

    public class EventHubProducerFactory
    {
        public static IEventHubProducer Create(IPrivacyConfigurationManager config, IAppConfiguration appConfiguration)
        {
            return new AidEventHubProducer(config, appConfiguration);
        }
    }
}