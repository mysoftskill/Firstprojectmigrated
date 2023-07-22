// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Security.Cryptography.X509Certificates;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.Common.Cosmos;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.CosmosHelpers;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem.Cosmos;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     manages a collection of Cosmos file system objects
    /// </summary>
    public class CosmosFileSystemManager : IFileSystemManager
    {
        public const string ActivityLogTag = "ACTIVITYLOG";
        public const string DeadLetterTag = "DEADLETTER";
        public const string StatsLogTag = "STATISTICSLOG";

        private readonly IDictionary<string, ICosmosFileSystem> systems = 
            new Dictionary<string, ICosmosFileSystem>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     Initializes a new instance of the CosmosFileSystemManager class
        /// </summary>
        /// <param name="agentConfig">agent configuration</param>
        /// <param name="logger">Geneva trace logger</param>
        /// <param name="cosmosClientFactory">Factory for creating cosmos client</param>
        /// <param name="appConfig">App configuration for reading config/feautre flags</param>
        public CosmosFileSystemManager(
            ICosmosExportAgentConfig agentConfig,
            ILogger logger,
            ICosmosResourceFactory cosmosClientFactory,
            IAppConfiguration appConfig)
        {
            ICosmosFileSystem GetDedicatedFileSystem(
                string prop,
                string tagExisting,
                string tagDedicated,
                string suffix,
                int lifetimeHours)
            {
                ICosmosFileSystem fsDedicated;
                ICosmosFileSystem fsExisting;
                string rootDedicated;

                try
                {
                    fsExisting = this.systems[tagExisting];
                }
                catch (KeyNotFoundException)
                {
                    throw new ArgumentException(
                        $"Could not find tag {tagExisting} specified for dedicated file system {prop}",
                        "agentConfig." + prop);
                }

                rootDedicated = fsExisting.RootDirectory + Utility.EnsureHasTrailingSlashButNoLeadingSlash(suffix);

                fsDedicated = new CosmosFileSystem(fsExisting.Client, rootDedicated, tagDedicated, TimeSpan.FromHours(lifetimeHours), appConfig);

                try
                {
                    this.systems.Add(tagDedicated, fsDedicated);
                }
                catch (ArgumentException)
                {
                    throw new ArgumentException(
                        $"A configured Cosmos file system is using a tag ({tagDedicated}) reserved for a dedicated file system",
                        "agentConfig." + prop);
                }

                return fsDedicated;
            }

            ArgumentCheck.ThrowIfNull(agentConfig, nameof(agentConfig));
            ArgumentCheck.ThrowIfEmptyOrNull(agentConfig.CosmosVcs, "agentConfig.CosmosVcs");

            this.CosmosPathsAndExpiryTimes = agentConfig.CosmosPathsAndExpiryTimes ?? throw new ArgumentNullException("agentConfig.CosmosPathsAndExpiryTimes");

            this.FileSizeThresholds = agentConfig.FileSizeThresholds ?? throw new ArgumentNullException("agentConfig.FileSizeThresholds");

            ICertificateProvider certProvider = new CertificateProvider(logger);

            foreach (ITaggedCosmosVcConfig config in agentConfig.CosmosVcs)
            {

                CosmosFileSystem fs = CreateCosmosFileSystem(agentConfig, config, logger, certProvider, cosmosClientFactory, appConfig);
                
                this.systems.Add(
                    config.CosmosTag,
                    fs);
            }

            this.DeadLetter = GetDedicatedFileSystem(
                "DeadLetterCosmosTag",
                agentConfig.DeadLetterCosmosTag,
                CosmosFileSystemManager.DeadLetterTag,
                agentConfig.CosmosPathsAndExpiryTimes.DeadLetter,
                agentConfig.CosmosPathsAndExpiryTimes.DeadLetterExpiryHours);

            this.ActivityLog = GetDedicatedFileSystem(
                "ActivityLogCosmosTag",
                agentConfig.ActivityLogCosmosTag,
                CosmosFileSystemManager.ActivityLogTag,
                agentConfig.CosmosPathsAndExpiryTimes.ActivityLog,
                agentConfig.CosmosPathsAndExpiryTimes.ActivityLogExpiryHours);

            this.StatsLog = GetDedicatedFileSystem(
                "StatsLogCosmosTag",
                agentConfig.StatsLogCosmosTag,
                CosmosFileSystemManager.StatsLogTag,
                agentConfig.CosmosPathsAndExpiryTimes.StatsLog,
                agentConfig.CosmosPathsAndExpiryTimes.StatsLogExpiryHours);
        }

        /// <summary>
        ///     Gets Cosmos paths and expiry times
        /// </summary>
        public ICosmosRelativePathsAndExpiryTimes CosmosPathsAndExpiryTimes { get; }

        /// <summary>
        ///     Gets file size thresholds
        /// </summary>
        public ICosmosFileSizeThresholds FileSizeThresholds { get; }

        /// <summary>
        ///     Gets the activity log store dedicated file system
        /// </summary>
        public ICosmosFileSystem ActivityLog { get; }

        /// <summary>
        ///     Gets the dead letter store dedicated file system
        /// </summary>
        public ICosmosFileSystem DeadLetter { get; }

        /// <summary>
        ///     Gets the statistics log dedicated file system
        /// </summary>
        public ICosmosFileSystem StatsLog { get; }

        /// <summary>
        ///     Gets the specified file system
        /// </summary>
        /// <param name="tag">file system tag</param>
        /// <returns>resulting value</returns>
        public ICosmosFileSystem GetFileSystem(string tag)
        {
            return this.systems[tag];
        }

        private CosmosFileSystem CreateCosmosFileSystem(
            ICosmosExportAgentConfig agentConfig,
            ITaggedCosmosVcConfig vcConfig,
            ILogger logger,
            ICertificateProvider certProvider,
            ICosmosResourceFactory cosmosClientFactory,
            IAppConfiguration appConfig)
        {
            ICosmosClient cosmosClient = null;

            string root = null;

            logger.Information(nameof(CosmosFileSystemManager), "Initializing cosmos client using adls");
            string dir = String.IsNullOrEmpty(vcConfig.RootDir) ? agentConfig.CosmosPathsAndExpiryTimes.BasePath : vcConfig.RootDir;
            root = Utility.EnsureTrailingSlash(dir);
            X509Certificate2 cosmosCert = certProvider.GetClientCertificate(agentConfig.AdlsConfiguration.ClientAppCertificateSubjectAdls);
            cosmosClient = cosmosClientFactory.CreateCosmosAdlsClient( new AdlsConfig(vcConfig.CosmosAdlsAccountName,
                agentConfig.AdlsConfiguration.ClientAppId,
                                agentConfig.AdlsConfiguration.AdlsAccountSuffix,
                                agentConfig.AdlsConfiguration.TenantId,cosmosCert));

            ICosmosClient client =
            new CosmosClientRetry(
                agentConfig.CosmosRetryStrategy,
                logger,
                new CosmosClientInstrumented(
                    cosmosClient));
            
            return new CosmosFileSystem(client, root, vcConfig.CosmosTag, null, appConfig);
        }
    }
}
