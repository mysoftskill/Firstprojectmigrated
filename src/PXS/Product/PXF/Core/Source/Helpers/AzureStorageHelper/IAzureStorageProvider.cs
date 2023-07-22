// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper
{
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Azure.Storage;

    /// <summary>
    ///     Azure Storage Provider class establishes connection to Azure Storage and hands out base classes
    /// </summary>
    public interface IAzureStorageProvider
    {
        /// <summary>
        ///     Gets the account name of the storage account
        /// </summary>
        string AccountName { get; }

        /// <summary>
        ///     Gets the endpoints for the Queue service at the primary and secondary location, as configured for the storage account.
        /// </summary>
        StorageUri QueueStorageUri { get; }

        /// <summary>
        ///     Get the cloud table wrapper for the queue
        /// </summary>
        /// <param name="name">name of the queue to fetch</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns></returns>
        Task<ICloudQueue> GetCloudQueueAsync(string name, CancellationToken cancellationToken);

        /// <summary>
        ///     Get the cloud table wrapper for the table
        /// </summary>
        /// <param name="name">name of the table to fetch</param>
        /// <returns></returns>
        CloudTableWrapper GetCloudTable(string name);

        /// <summary>
        ///     Get the cloud table wrapper for the table
        /// </summary>
        /// <param name="name">name of the table to fetch</param>
        /// <returns></returns>
        Task<ICloudTable> GetCloudTableAsync(string name);

        /// <summary>
        ///     Initialize Azure Storage using the privacy experience configuration
        /// </summary>
        /// <param name="serviceConfig">service configuration</param>
        /// <returns></returns>
        Task InitializeAsync(IPrivacyExperienceServiceConfiguration serviceConfig);

        /// <summary>
        ///     Initialize Azure Storage using the IAzureStorageConfiguration
        /// </summary>
        /// <param name="config">IAzureStorageConfiguration</param>
        /// <returns></returns>
        Task InitializeAsync(IAzureStorageConfiguration config);
    }
}
