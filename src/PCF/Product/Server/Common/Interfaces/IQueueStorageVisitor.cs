// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    /// <summary>
    /// A highly generic visitor pattern implementation for queue storage.
    /// </summary>
    /// <typeparam name="TReturn"></typeparam>
    public interface IQueueStorageVisitor<out TReturn>
    {
        /// <summary>
        /// Visits Azure-Cosmos-Db
        /// </summary>
        TReturn VisitAzureCosmosDb();

        /// <summary>
        /// Visits Azure-Queue-Storage
        /// </summary>
        TReturn VisitAzureQueueStorage();

        /// <summary>
        /// Visits Default
        /// </summary>
        TReturn VisitDefault();
    }
}
