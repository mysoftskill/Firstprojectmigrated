// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.Aqs
{
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.Membership.MemberServices.Configuration;

    internal interface IAsyncQueueService2ClientFactory
    {
        /// <summary>
        ///     Creates an instance of <see cref="IAsyncQueueService2" />.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="logger">logging system</param>
        /// <returns>A new instance of <see cref="IAsyncQueueService2" /></returns>
        IAsyncQueueService2 Create(IAqsConfiguration config, ILogger logger);
    }
}
