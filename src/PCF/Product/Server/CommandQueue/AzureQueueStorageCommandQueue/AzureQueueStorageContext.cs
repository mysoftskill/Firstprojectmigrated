// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue.QueueStorageCommandQueue
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Azure.Storage;
    using Microsoft.Azure.Storage.Queue;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    public class AzureQueueStorageContext
    {
        private readonly CloudQueueClient[] cloudQueueClients;

        /// <summary>
        /// Creates a new instance of <see cref="AzureQueueStorageContext"/>
        /// </summary>
        public AzureQueueStorageContext() : this(Config.Instance.AgentAzureQueues.StorageAccounts.Where(c => c != null && !string.IsNullOrWhiteSpace(c.ConnectionString)).Select(c => CloudStorageAccount.Parse(c.ConnectionString)))
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="AzureQueueStorageContext"/>
        /// </summary>
        public AzureQueueStorageContext(IEnumerable<CloudStorageAccount> cloudStorageAccounts)
        {
            this.cloudQueueClients = cloudStorageAccounts.Select(c => c.CreateCloudQueueClient()).ToArray();
        }

        /// <summary>
        /// Gets the current list of repositories.
        /// </summary>
        public IReadOnlyList<CloudQueueClient> GetQueueClients()
        {
            return this.cloudQueueClients;
        }

        /// <summary>
        /// Gets the storage account names
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<string> GetStorageAccountNames()
        {
            return this.cloudQueueClients.Select(c => c.Credentials.AccountName).ToArray();
        }
    }
}
