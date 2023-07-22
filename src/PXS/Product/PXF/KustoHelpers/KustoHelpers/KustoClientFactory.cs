// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.PrivacyServices.KustoHelpers
{
    using System;

    using global::Kusto.Data;
    using global::Kusto.Data.Common;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     Kusto client creator
    /// </summary>
    public class KustoClientFactory : IKustoClientFactory
    {
        private readonly ICertificateProvider certProvider;

        private readonly IAadTokenAuthConfiguration authConfig;
        private readonly IKustoConfig kustoConfig;
        private readonly ILogger logger;

        /// <summary>
        ///     Initializes a new instance of the KustoQuery class
        /// </summary>
        /// <param name="authConfig">authentication configuration</param>
        /// <param name="certProvider">certificate provider</param>
        /// <param name="kustoConfig">kusto configuration</param>
        /// <param name="logger">Geneva trace logger</param>
        public KustoClientFactory(
            IAadTokenAuthConfiguration authConfig,
            ICertificateProvider certProvider,
            IKustoConfig kustoConfig,
            ILogger logger)
        {
            this.certProvider = certProvider ?? throw new ArgumentNullException(nameof(certProvider));
            this.kustoConfig = kustoConfig ?? throw new ArgumentNullException(nameof(kustoConfig));
            this.authConfig = authConfig ?? throw new ArgumentNullException(nameof(authConfig));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        ///     Creates a Kusto client
        /// </summary>
        /// <param name="clusterUrl">Kusto cluster</param>
        /// <param name="databaseName">Kusto database</param>
        /// <param name="queryTag">query tag to be used for tracing / logging purposes</param>
        /// <returns>resulting value</returns>
        public IKustoClient CreateClient(
            string clusterUrl, 
            string databaseName, 
            string queryTag)
        {
            KustoConnectionStringBuilder connString;
            ICslQueryProvider clientActual;

            ArgumentCheck.ThrowIfNullEmptyOrWhiteSpace(queryTag, nameof(queryTag));

            databaseName = string.IsNullOrEmpty(databaseName) ? this.kustoConfig.DefaultDatabaseName : databaseName;
            clusterUrl = string.IsNullOrEmpty(clusterUrl) ? this.kustoConfig.DefaultClusterUrl : clusterUrl;

            connString = new KustoConnectionStringBuilder(clusterUrl, databaseName)
            {
                FederatedSecurity = true,
                ApplicationCertificateBlob = this.certProvider.GetClientCertificate(this.authConfig.RequestSigningCertificateConfiguration.Subject),
                ApplicationCertificateSendX5c = true,
                ApplicationClientId = this.authConfig.AadAppId,

                Accept = true,

                ApplicationNameForTracing = this.kustoConfig.DefaultKustoAppName,
                Authority = "72f988bf-86f1-41af-91ab-2d7cd011db47",

                AzureRegion = Environment.GetEnvironmentVariable("MONITORING_DATACENTER") ?? "TryAutoDetect",
            };

            clientActual = Kusto.Data.Net.Client.KustoClientFactory.CreateCslQueryProvider(connString);

            return
                new KustoClient(
                    new KustoClientInstrumented(
                        new KustoClientRetry(this.kustoConfig.RetryStrategy, clientActual, this.logger),
                        queryTag));
        }
    }
}
