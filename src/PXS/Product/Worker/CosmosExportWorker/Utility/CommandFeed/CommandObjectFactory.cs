// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility
{
    using System;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Identity.Client;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Data;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Tasks;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.PcfAdapter;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.Common.Azure;

    using IDependencyManager = Microsoft.Membership.MemberServices.Privacy.CosmosExport.IDependencyManager;
    using IPcfHttpClientFactory = Microsoft.PrivacyServices.CommandFeed.Client.IHttpClientFactory;

    /// <summary>
    ///     creates object for the PCF export task
    /// </summary>
    public class CommandObjectFactory : ICommandObjectFactory
    {
        private readonly X509Certificate2 authCert;

        private readonly IDependencyManager dependencyManager;

        /// <summary>
        ///     Initializes a new instance of the CommandObjectFactory class
        /// </summary>
        /// <param name="dependencyManager">dependency manager</param>
        public CommandObjectFactory(IDependencyManager dependencyManager)
        {
            ICertificateConfiguration certConfig;
            ICommandMonitorConfig pcfConfig;
            ICertificateProvider certProvider;

            this.dependencyManager = dependencyManager ?? throw new ArgumentNullException(nameof(dependencyManager));

            pcfConfig = this.dependencyManager.GetType<ICommandMonitorConfig>();
            certProvider = this.dependencyManager.GetType<ICertificateProvider>();

            if (pcfConfig.AuthMode == PcfAuthMode.Aad)
            {
                IAadTokenAuthConfiguration aadConfig = this.dependencyManager.GetType<IAadTokenAuthConfiguration>();
                certConfig = aadConfig.RequestSigningCertificateConfiguration;
                this.authCert = certProvider.GetClientCertificate(certConfig.Subject);
            }
            else
            {
                certConfig = pcfConfig.PcfMsaCertificate;
                this.authCert = certProvider.GetClientCertificate(certConfig);
            }
        }

        /// <summary>
        ///     Creates the command feed client
        /// </summary>
        /// <returns>resulting value</returns>
        public ICommandClient CreateCommandFeedClient()
        {
            ICosmosExportAgentConfig mainConfig = this.dependencyManager.GetType<ICosmosExportAgentConfig>();
            ICommandMonitorConfig pcfConfig = this.dependencyManager.GetType<ICommandMonitorConfig>();
            ICommandClient baseInstance;
            IPcfAdapter pcfClient = this.dependencyManager.GetType<IPcfAdapter>();
            ILogger logger = this.dependencyManager.GetType<ILogger>();

            CommandFeedEndpointConfiguration endpointConfig = pcfConfig.StockEndpointType == CommandFeedStockEndpointType.Prod
                ? CommandFeedEndpointConfiguration.Production
                : CommandFeedEndpointConfiguration.Preproduction;

            // TODO: determine why using PXS client factory cannot successfully auth against PCF

            if (pcfConfig.AuthMode == PcfAuthMode.Aad)
            {
                IAadTokenAuthConfiguration aadConfig = this.dependencyManager.GetType<IAadTokenAuthConfiguration>();

                baseInstance = new CommandFeedClientWrapper(
                    commandMonitorConfig: pcfConfig,
                    pcfClient: pcfClient,
                    logger: logger,
                    commandClient: new CommandFeedClient(
                        Guid.Parse(pcfConfig.AgentId),
                        aadConfig.AadAppId,
                        this.authCert,
                        this.CreateLogger(),
                        null,
                        endpointConfig,
                        sendX5c: true,
                        azureRegion: Environment.GetEnvironmentVariable("MONITORING_DATACENTER") ?? ConfidentialClientApplication.AttemptRegionDiscovery));
            }
            else
            {
                baseInstance = new CommandFeedClientWrapper(
                    commandMonitorConfig: pcfConfig,
                    pcfClient: pcfClient,
                    logger: logger,
                    commandClient: new CommandFeedClient(
                        Guid.Parse(pcfConfig.AgentId),
                        pcfConfig.PcfMsaSiteId,
                        this.authCert,
                        this.CreateLogger(),
                        null,
                        endpointConfig));
            }

            return new CommandFeedClientInstrumented(
                this.dependencyManager.GetType<ICounterFactory>(),
                new CommandFeedClientRetry(mainConfig.CommandFeedRetryStrategy, baseInstance, logger));
        }

        /// <summary>
        ///     Creates a command receiver
        /// </summary>
        /// <param name="taskId">task id</param>
        /// <returns>resulting value</returns>
        public ICommandReceiver CreateCommandReceiver(string taskId)
        {
            return new CommandReceiver(this, taskId);
        }

        /// <summary>
        ///     Creates the data agent
        /// </summary>
        /// <param name="taskId">task id</param>
        /// <returns>resulting value</returns>
        public IPrivacyDataAgent CreateDataAgent(string taskId)
        {
            ArgumentCheck.ThrowIfNullEmptyOrWhiteSpace(taskId, nameof(taskId));

            return new CosmosDataAgent(
                this.dependencyManager.GetType<ICommandMonitorConfig>(),
                this.dependencyManager.GetType<ITable<CommandFileState>>(),
                this.dependencyManager.GetType<ITable<CommandState>>(),
                this.dependencyManager.GetType<ICounterFactory>(),
                this.CreateCommandFeedClient(),
                this.dependencyManager.GetType<ILogger>(),
                this.dependencyManager.GetType<IClock>(),
                taskId);
        }

        /// <summary>
        ///     Creates a logger that the PCF client will invoke when certain events occur
        /// </summary>
        /// <returns>resulting value</returns>
        public CommandFeedLogger CreateLogger()
        {
            return this.dependencyManager.GetType<CommandFeedLogger>();
        }

        /// <summary>
        ///     CommandReceiver wrapper
        /// </summary>
        private class CommandReceiver : ICommandReceiver
        {
            private readonly PrivacyCommandReceiver receiver;

            /// <summary>
            ///     Initializes a new instance of the CommandReceiver class
            /// </summary>
            /// <param name="factory">factory</param>
            /// <param name="taskId">task id</param>
            public CommandReceiver(
                ICommandObjectFactory factory,
                string taskId)
            {
                this.receiver = new PrivacyCommandReceiver(
                    factory.CreateDataAgent(taskId),
                    factory.CreateCommandFeedClient(),
                    factory.CreateLogger());
            }

            /// <summary>
            ///     Begins receiving commands
            /// </summary>
            /// <param name="token">cancel token</param>
            /// <returns>resulting value</returns>
            public Task BeginReceivingAsync(CancellationToken token)
            {
                return this.receiver.BeginReceivingAsync(token);
            }
        }
    }
}
