// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.Cosmos
{
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.Common.Cosmos;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.CosmosHelpers;
    using Microsoft.PrivacyServices.Common.Azure;
    using System.Security.Cryptography.X509Certificates;
    using System;

    internal static class AqsCosmosClientFactory
    {
        public static ICosmosClient CreateCosmosClient(IPrivacyConfigurationManager config,
            IAppConfiguration appConfig,
            ICosmosResourceFactory factory,
            ILogger logger)
        {
            var certProvider = new CertificateProvider(logger);

            logger.Information(nameof(AqsCosmosClientFactory), "Initializing cosmos client using adls");
            var vcConfig = config.AqsWorkerConfiguration;
            X509Certificate2 cosmosCert = certProvider.GetClientCertificate(config.AqsWorkerConfiguration.AdlsConfiguration.ClientAppCertificateSubjectAdls, StoreLocation.LocalMachine);
            ICosmosClient cosmosClient = factory.CreateCosmosAdlsClient( new AdlsConfig(vcConfig.MappingConfig.CosmosAdlsAccountName, 
                config.AqsWorkerConfiguration.AdlsConfiguration.ClientAppId,
                config.AqsWorkerConfiguration.AdlsConfiguration.AdlsAccountSuffix,
                config.AqsWorkerConfiguration.AdlsConfiguration.TenantId,
                cosmosCert));
            return new CosmosClientInstrumented(cosmosClient);
        }
    }
}
