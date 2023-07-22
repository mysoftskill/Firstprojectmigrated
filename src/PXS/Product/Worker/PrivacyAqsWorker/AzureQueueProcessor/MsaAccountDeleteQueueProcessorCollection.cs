// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.AzureQueueProcessor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Worker;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.AQS;
    using Microsoft.Membership.MemberServices.Privacy.Core.VerificationTokenValidation;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts;
    using Microsoft.PrivacyServices.Common.Azure;

    public class MsaAccountDeleteQueueProcessorCollection : IWorker
    {
        /// <summary>
        ///     Sub processors that were created
        /// </summary>
        private readonly List<IWorker> processors;

        public MsaAccountDeleteQueueProcessorCollection(
            IPrivacyConfigurationManager config,
            IMsaAccountDeleteQueue queue,
            IXboxAccountsAdapter xboxAccountsAdapter,
            IMsaIdentityServiceAdapter msaIdentityServiceAdapter,
            IVerificationTokenValidationService tokenValidationService,
            IAccountDeleteWriter pcfWriter,
            ICounterFactory counterFactory,
            ILogger logger)
        {
            if (config?.AqsWorkerConfiguration?.MsaAccountDeleteQueueProcessorConfiguration == null) throw new ArgumentException(nameof(config.AqsWorkerConfiguration.MsaAccountDeleteQueueProcessorConfiguration));

            int processorCount = config.AqsWorkerConfiguration.MsaAccountDeleteQueueProcessorConfiguration.ProcessorCount;

            this.processors = new List<IWorker>(processorCount);
            for (int x = 0; x < processorCount; ++x)
            {
                this.processors.Add(
                    new MsaAccountDeleteQueueProcessor(
                        config.AqsWorkerConfiguration.MsaAccountDeleteQueueProcessorConfiguration,
                        queue,
                        xboxAccountsAdapter,
                        msaIdentityServiceAdapter,
                        tokenValidationService,
                        pcfWriter,
                        counterFactory,
                        logger));
            }
        }

        /// <inheritdoc />
        public void Start() => this.processors.ForEach(p => p.Start());

        /// <inheritdoc />
        public void Start(TimeSpan delay) => this.processors.ForEach(p => p.Start(delay));

        /// <inheritdoc />
        public Task StopAsync() => Task.WhenAll(this.processors.Select(p => p.StopAsync()));
    }
}
