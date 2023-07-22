// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.NgpProxy.PcfDataAgentV2
{
    using System;
    using System.Threading;
    using Microsoft.Identity.Client;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyHost;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.Common.Azure;

    public class PcfDataAgentV2Decorator : HostDecorator
    {
        private readonly ILogger logger;
        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly PrivacyCommandReceiverV2 receiver;

        public PcfDataAgentV2Decorator(IPcfDataAgentConfiguration pcfReceiverConfig, ICertificateProvider certificateProvider, ILogger logger)
        {
            if (pcfReceiverConfig.Enabled)
            {
                this.logger = logger;
                var cflogger = new TraceCommandFeedLogger(pcfReceiverConfig.AgentId, logger);
                this.cancellationTokenSource = new CancellationTokenSource();
                CommandFeedEndpointConfiguration endpointConfiguration = GetEndpointConfiguration(pcfReceiverConfig.PcfEndpoint);

                var privacyDataAgent = new PcfDataAgentV2(logger);

                this.logger.Information(nameof(PcfDataAgentV2Decorator), $"Using {pcfReceiverConfig.PcfEndpoint} endpoint configuration. AgentId->{pcfReceiverConfig.AgentId}. AppId->{pcfReceiverConfig.PxsFirstPartyPortalAadAppId}.");

                var client = new CommandFeedClient(
                    agentId: Guid.Parse(pcfReceiverConfig.AgentId),
                    aadClientId: pcfReceiverConfig.PxsFirstPartyPortalAadAppId,
                    clientCertificate: certificateProvider.GetClientCertificate(pcfReceiverConfig.CertificateConfiguration.Subject),
                    logger: cflogger,
                    endpointConfiguration: endpointConfiguration,
                    sendX5c: true,
                    azureRegion: Environment.GetEnvironmentVariable("MONITORING_DATACENTER") ?? ConfidentialClientApplication.AttemptRegionDiscovery);

                this.receiver = new PrivacyCommandReceiverV2(
                    privacyDataAgent,
                    client,
                    cflogger);

                this.logger.Information(nameof(PcfDataAgentV2Decorator), "PcfDataAgentDecorator initialized.");
            }
        }

        public override ConsoleSpecialKey? Execute()
        {
            this.logger.Information(nameof(PcfDataAgentV2Decorator), "PcfDataAgentDecorator will start receiving commands.");
            this.receiver?.BeginReceivingAsync(cancellationToken: this.cancellationTokenSource.Token);
            return base.Execute();
        }

        private CommandFeedEndpointConfiguration GetEndpointConfiguration(string pcfEndpointConfiguration)
        {
            switch (pcfEndpointConfiguration)
            {
                case "Production":
                    return CommandFeedEndpointConfiguration.ProductionV2;
                case "ProductionAME":
                    return CommandFeedEndpointConfiguration.ProductionAMEV2;
                case "Preproduction":
                    return CommandFeedEndpointConfiguration.PreproductionV2;
                case "PreproductionAME":
                    return CommandFeedEndpointConfiguration.PreproductionAMEV2;
                case "Fairfax":
                    return CommandFeedEndpointConfiguration.FairfaxV2;
                case "Mooncake":
                    return CommandFeedEndpointConfiguration.MooncakeV2;
                default:
                    throw new ArgumentException($"Invalid argument for: {nameof(pcfEndpointConfiguration)}");
            }
        }
    }
}
