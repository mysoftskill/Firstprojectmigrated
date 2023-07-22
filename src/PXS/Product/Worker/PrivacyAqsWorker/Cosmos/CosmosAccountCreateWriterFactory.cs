// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.Cosmos
{
    using Microsoft.PrivacyServices.Common.Cosmos;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.CosmosHelpers;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.Common;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.LeaseId;
    using Microsoft.PrivacyServices.Common.Azure;
    using System;
    using System.Net;
    using System.Security.Policy;

    internal static class CosmosAccountCreateWriterFactory
    {
        public static IAccountCreateWriter Create(IPrivacyConfigurationManager config, ICosmosClient client, ILogger logger, IDistributedIdFactory idFactory)
        {
            return new CosmosAccountCreateWriter(client, logger, config.AqsWorkerConfiguration.MappingConfig, idFactory);
        }
    }
}
