// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.PrivacyServices.MsaAgeOutFakeCommandWorker
{
    using System;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Logging.LoggingFilter;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Factory;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.PcfAdapter;
    using Microsoft.Membership.MemberServices.PrivacyHost;

    using Microsoft.PrivacyServices.Common.Azure;

    public class MsaAgeOutQueueProcessorDecorator : HostDecorator
    {
        private readonly bool enabled;

        private readonly ILogger logger;

        private readonly IPrivacyConfigurationManager configurationManager;

        public MsaAgeOutQueueProcessorDecorator(ILogger logger, IPrivacyConfigurationManager configurationManager)
        {
            this.enabled = configurationManager?.MsaAgeOutFakeCommandWorkerConfiguration?.EnableProcessing ?? false;
            this.logger = logger;
            this.configurationManager = configurationManager;
        }

        public override ConsoleSpecialKey? Execute()
        {
            if (!this.enabled)
            {
                this.logger.Information(nameof(MsaAgeOutQueueProcessorDecorator), "This worker is disabled in this environment.");
                return base.Execute();
            }

            var msaAgeOutQueue = new MsaAgeOutQueue(this.logger, this.configurationManager);
            var pcfAdapter = new PcfAdapter(
                this.configurationManager,
                new HttpClientFactoryPublic(new LoggingFilter(this.configurationManager), this.logger),
                new NoOpCounterFactory(),
                new AadAuthManager(
                    this.configurationManager,
                    new CertificateProvider(this.logger),
                    this.logger,
                    new InstrumentedAadTokenManager(new AadTokenManager()),
                    new AadJwtSecurityTokenHandler(this.logger, this.configurationManager),
                    new MiseTokenValidationUtility(this.logger, this.configurationManager),
                    AppConfigurationFactory.Create(this.configurationManager)));

            var processor = new MsaAgeOutQueueProcessor(msaAgeOutQueue, pcfAdapter, this.logger);
            processor.Start();

            try
            {
                return base.Execute();
            }
            finally
            {
                processor?.StopAsync().GetAwaiter().GetResult();
            }
        }
    }
}
