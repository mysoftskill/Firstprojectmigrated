// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.NgpProxy.PcfDataAgent
{
    using System;
    using System.Threading;
    using Microsoft.Identity.Client;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyHost;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.Common.Azure;

    public class PcfDataAgentDecorator : HostDecorator
    {
        private readonly ILogger logger;
        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly PrivacyCommandReceiver receiver;

        public PcfDataAgentDecorator(IPcfDataAgentConfiguration pcfReceiverConfig, ICertificateProvider certificateProvider, ILogger logger)
        {
            if (pcfReceiverConfig.Enabled)
            {
                this.logger = logger;
                var cflogger = new TraceCommandFeedLogger(pcfReceiverConfig.AgentId, logger);
                this.cancellationTokenSource = new CancellationTokenSource();
                CommandFeedEndpointConfiguration endpointConfiguration = GetEndpointConfiguration(pcfReceiverConfig.PcfEndpoint);

                var privacyDataAgent = new PcfDataAgent(logger, pcfReceiverConfig.AgentId, endpointConfiguration);

                this.logger.Information(nameof(PcfDataAgentDecorator), $"Using {pcfReceiverConfig.PcfEndpoint} endpoint configuration. AgentId->{pcfReceiverConfig.AgentId}. AppId->{pcfReceiverConfig.PxsFirstPartyPortalAadAppId}.");

                var client = new CommandFeedClient(
                    agentId: Guid.Parse(pcfReceiverConfig.AgentId),
                    aadClientId: pcfReceiverConfig.PxsFirstPartyPortalAadAppId,
                    clientCertificate: certificateProvider.GetClientCertificate(pcfReceiverConfig.CertificateConfiguration.Subject),
                    logger: cflogger,
                    endpointConfiguration: endpointConfiguration,
                    sendX5c: true,
                    azureRegion: Environment.GetEnvironmentVariable("MONITORING_DATACENTER") ?? ConfidentialClientApplication.AttemptRegionDiscovery);

                this.receiver = new PrivacyCommandReceiver(
                    privacyDataAgent,
                    client,
                    cflogger);

                this.logger.Information(nameof(PcfDataAgentDecorator), "PcfDataAgentDecorator initialized.");
            }
        }

        public override ConsoleSpecialKey? Execute()
        {
            this.logger.Information(nameof(PcfDataAgentDecorator), "PcfDataAgentDecorator will start receiving commands.");
            this.receiver?.BeginReceivingAsync(this.cancellationTokenSource.Token);
            return base.Execute();
        }

        private CommandFeedEndpointConfiguration GetEndpointConfiguration(string pcfEndpointConfiguration)
        {
            switch (pcfEndpointConfiguration)
            {
                case "Production":
                    return CommandFeedEndpointConfiguration.Production;
                case "ProductionAME":
                    return CommandFeedEndpointConfiguration.ProductionAME;
                case "Preproduction":
                    return CommandFeedEndpointConfiguration.Preproduction;
                case "PreproductionAME":
                    return CommandFeedEndpointConfiguration.PreproductionAME;
                case "Fairfax":
                    return CommandFeedEndpointConfiguration.Fairfax;
                case "Mooncake":
                    return CommandFeedEndpointConfiguration.Mooncake;
                default:
                    throw new ArgumentException($"Invalid argument for: {nameof(pcfEndpointConfiguration)}");
            }
        }
    }
}
