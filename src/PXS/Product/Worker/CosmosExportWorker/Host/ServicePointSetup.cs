// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport
{
    using System;
    using System.Linq;
    using System.Net;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyHost;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     Implements the task manager to start and stop the worker's tasks
    /// </summary>
    public class ServicePointSetup : HostDecorator
    {
        private readonly ICosmosExportAgentConfig appConfig;

        private readonly ILogger logger;

        /// <summary>
        ///     Initializes a new instance of the ServicePointSetup class
        /// </summary>
        /// <param name="config">task configuration</param>
        /// <param name="logger">Geneva trace logger</param>
        public ServicePointSetup(
            ICosmosExportAgentConfig config,
            ILogger logger)
        {
            this.appConfig = config ?? throw new ArgumentNullException(nameof(config));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        ///     Executes this instance
        /// </summary>
        /// <returns>resulting value</returns>
        public override ConsoleSpecialKey? Execute()
        {
            this.logger.Information(nameof(TaskSetManager), "Service point setup module starting");
            
            try
            {
                this.SetupCommandFeedServicePointConfig();
                this.SetupCosmosServicePointConfig();

                return base.Execute();
            }
            finally
            {
                this.logger.Information(nameof(TaskSetManager), "Service point setup module terminating");
            }
        }

        /// <summary>
        ///     Sets up the service point config for Cosmos
        /// </summary>
        private void SetupCosmosServicePointConfig()
        {
            foreach (ITaggedCosmosVcConfig cosmosConfig in this.appConfig.CosmosVcs.Where(o => o.ServicePointConfiguration != null))
            {
                // VCClient
                ServicePointSetup.ApplyConfig(cosmosConfig.CosmosVcPath, cosmosConfig.ServicePointConfiguration);

                // adls
                string adlsAccountUrl = $"https://{cosmosConfig.CosmosAdlsAccountName}.{appConfig.AdlsConfiguration.AdlsAccountSuffix}";

                if(!string.IsNullOrEmpty(cosmosConfig.RootDir))
                {
                    adlsAccountUrl = $"{adlsAccountUrl}{cosmosConfig.RootDir}";
                }

                if(cosmosConfig.ApplyRelativeBasePath)
                {
                    adlsAccountUrl = $"{adlsAccountUrl}/{appConfig.CosmosPathsAndExpiryTimes.BasePath}";
                }
                ServicePointSetup.ApplyConfig(adlsAccountUrl, cosmosConfig.ServicePointConfiguration);
            }
        }

        /// <summary>
        ///     Sets up the service point config for command feed
        /// </summary>
        private void SetupCommandFeedServicePointConfig()
        {
            IPrivacyPartnerAdapterConfiguration pcfConfig = this.appConfig.PcfEndpointConfig;
            ServicePointSetup.ApplyConfig(pcfConfig.BaseUrl, pcfConfig.ServicePointConfiguration);
        }

        /// <summary>
        ///      Applies the service point configuration for the specified URI
        /// </summary>
        /// <param name="uri">URI to apply config to</param>
        /// <param name="config">config to apply</param>
        private static void ApplyConfig(
            string uri,
            IServicePointConfiguration config)
        {
            if (string.IsNullOrWhiteSpace(uri) == false && config != null)
            {
                ServicePoint sp = ServicePointManager.FindServicePoint(new Uri(uri));
                sp.ConnectionLeaseTimeout = config.ConnectionLeaseTimeout;
                sp.UseNagleAlgorithm = config.UseNagleAlgorithm;
                sp.ConnectionLimit = config.ConnectionLimit;
                sp.MaxIdleTime = config.MaxIdleTime;
            }
        }
    }
}
