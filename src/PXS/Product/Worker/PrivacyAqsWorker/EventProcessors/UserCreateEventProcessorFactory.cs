// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.EventProcessors
{
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService;

    internal class UserCreateEventProcessorFactory : IUserCreateEventProcessorFactory
    {
        private readonly IMsaIdentityServiceAdapter msaAdapter;

        private readonly ICounterFactory counterFactory;

        public UserCreateEventProcessorFactory(IMsaIdentityServiceAdapter msaAdapter, ICounterFactory counterFactory)
        {
            this.msaAdapter = msaAdapter;
            this.counterFactory = counterFactory;
        }

        public IUserCreateEventProcessor Create(IAqsQueueProcessorConfiguration config) => new UserCreateEventProcessor(this.msaAdapter, this.counterFactory);
    }
}
