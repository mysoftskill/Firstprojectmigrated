// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Azure.Storage.Blob;

    /// <summary>
    ///     Interface for dealing with export storage.
    /// </summary>
    public interface IExportStorageManager
    {
        /// <summary>
        ///     Gets a list of managed storage uris.
        /// </summary>
        IReadOnlyCollection<Uri> AccountUris { get; }

        /// <summary>
        ///     Cleanup a given container from the export storage.
        /// </summary>
        /// <param name="containerUri">The Uri for the container</param>
        /// <param name="commandId">The commandId (for logging)</param>
        Task CleanupContainerAsync(Uri containerUri, CommandId commandId);

        /// <summary>
        ///     Gets a list of async functions to execute to cleanup old containers from export storage.
        /// </summary>
        IEnumerable<Func<Task>> CleanupOldContainersAsync();

        /// <summary>
        ///     Takes a given container uri, and assuming it is an ExportStorageManager managed location, returns a
        ///     full access <see cref="CloudBlobContainer" /> for the location.
        /// </summary>
        /// <param name="containerUri">The Uri for the container</param>
        /// <returns>A full permissions <see cref="CloudBlobContainer" /></returns>
        CloudBlobContainer GetFullAccessContainer(Uri containerUri);

        /// <summary>
        ///     Gets or creates the final drop container for a given command and target storage account Uri.
        /// </summary>
        /// <param name="storageUri">The storage account Uri, such as https://blobstorename.core.windows.net</param>
        /// <param name="commandId">The commandId to transform into a container name</param>
        Task<Uri> GetOrCreateFinalContainerAsync(Uri storageUri, CommandId commandId);

        /// <summary>
        ///     Gets or creates the staging container for a given command/agent and target storage account Uri.
        /// </summary>
        /// <param name="storageUri">The storage account Uri, such as https://blobstorename.core.windows.net</param>
        /// <param name="commandId">The commandId to transform into a container name</param>
        /// <param name="agentId">The agentId</param>
        /// <param name="assetGroupId">The assetGroupId</param>
        Task<Uri> GetOrCreateStagingContainerAsync(Uri storageUri, CommandId commandId, AgentId agentId, AssetGroupId assetGroupId);

        /// <summary>
        ///     Gets a readonly container Uri
        /// </summary>
        Uri GetReadOnlyContainerUri(Uri containerUri);

        /// <summary>
        ///     Verifies a container is valid.
        /// </summary>
        /// <remarks>
        ///     The only way to verify a container is valid is attempt to write to it, which is why this method takes as input the filename
        ///     to write and the contents to write. Recommended that the caller writes somethign semi-useful here, as this will be visible the
        ///     user.
        /// </remarks>
        /// <param name="containerUri">The Uri for the container.</param>
        /// <param name="fileName">The blob name to write to.</param>
        /// <param name="fileContent">The contents to write.</param>
        /// <returns>A string indicating the reason why the container is invalid, or null if the container is valid.</returns>
        Task<string> GetContainerErrorCodeAsync(Uri containerUri, string fileName, string fileContent);

        /// <summary>
        ///     Returns whether or not the given Uri is a blob storage account this export storage manager manages.
        /// </summary>
        /// <param name="storageUri">The storage Uri to check</param>
        /// <returns>True if the export storage manager is managing that account, otherwise false</returns>
        bool IsManaged(Uri storageUri);

        /// <summary>
        ///     Returns a URI that "IsManaged" will accept.
        /// </summary>
        Uri GetManagedStorageUri();
    }
}
