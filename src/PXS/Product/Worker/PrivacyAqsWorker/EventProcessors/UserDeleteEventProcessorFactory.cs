// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.EventProcessors
{
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.Common;
    using Microsoft.PrivacyServices.Common.Azure;

    internal class UserDeleteEventProcessorFactory : IUserDeleteEventProcessorFactory
    {
        private readonly IClock clock;

        private readonly ICounterFactory counterFactory;

        private readonly CdpEvent2Helper eventHelper;

        private readonly ILogger logger;

        public UserDeleteEventProcessorFactory(
            CdpEvent2Helper eventHelper,
            ICounterFactory counterFactory,
            ILogger logger,
            IClock clock)
        {
            this.eventHelper = eventHelper;
            this.counterFactory = counterFactory;
            this.logger = logger;
            this.clock = clock;
        }

        public IUserDeleteEventProcessor Create(IAqsQueueProcessorConfiguration config) =>
            new UserDeleteEventProcessorV2(this.eventHelper, this.clock, this.counterFactory, this.logger);
    }
}
