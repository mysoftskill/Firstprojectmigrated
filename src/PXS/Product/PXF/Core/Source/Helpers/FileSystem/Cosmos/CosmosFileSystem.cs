// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.Common.Cosmos;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem.Cosmos;


    /// <summary>
    ///     implements the file system interface atop Cosmos
    /// </summary>
    public class CosmosFileSystem : ICosmosFileSystem
    {
        private readonly IAppConfiguration appConfig;
        /// <summary>
        ///     Initializes a new instance of the CosmosFileSystem class
        /// </summary>
        /// <param name="client">client accessor</param>
        /// <param name="rootDirectory">Cosmos directory path representing the root of this file system</param>
        /// <param name="tag">client tag</param>
        /// <param name="defaultLifetime">default lifetime</param>
        /// <param name="appConfig">AppConfig to read dynamic settings</param>
        public CosmosFileSystem(
            ICosmosClient client,
            string rootDirectory,
            string tag,
            TimeSpan? defaultLifetime,
            IAppConfiguration appConfig)
        {
            this.RootDirectory = ArgumentCheck.ReturnIfNotNullEmptyOrWhiteSpace(rootDirectory, nameof(rootDirectory));
            this.Client = client ?? throw new ArgumentNullException(nameof(client));
            this.Tag = ArgumentCheck.ReturnIfNotNullEmptyOrWhiteSpace(tag, nameof(tag));

            if (rootDirectory[rootDirectory.Length - 1] != '/')
            {
                this.RootDirectory += "/";
            }

            this.DefaultLifetime = defaultLifetime;
            this.appConfig = appConfig;
        }

        /// <summary>
        ///      Gets the supported operations
        /// </summary>
        public SupportedFileSystemOperations SupportedOperations =>
            SupportedFileSystemOperations.Create |
            SupportedFileSystemOperations.CreateWithLifetime |
            SupportedFileSystemOperations.Delete |
            SupportedFileSystemOperations.Open;

        /// <summary>
        ///     Gets the default lifetime for an object in the file system
        /// </summary>
        /// <remarks>null indicates no default lifetime</remarks>
        public TimeSpan? DefaultLifetime { get; }

        /// <summary>
        ///     Gets the Cosmos client to access the file system
        /// </summary>
        public ICosmosClient Client { get; }

        /// <summary>
        ///     Gets the root directory for the file system
        /// </summary>
        public string RootDirectory { get; }

        /// <summary>
        ///     Gets a tag for the file system
        /// </summary>
        public string Tag { get; }

        /// <summary>
        ///     Opens the specified directory
        /// </summary>
        /// <param name="path">file system path</param>
        /// <returns>resulting file system object or null if the file does not exist</returns>
        public async Task<IDirectory> OpenExistingDirectoryAsync(string path)
        {
            path = AdjustStreamPathForCosmosClient(path);
            return await this.Client.DirectoryExistsAsync(path).ConfigureAwait(false) ? 
                    new CosmosDirectory(path, this, this.appConfig) : 
                    null;
        }

        /// <summary>
        ///     Opens the specified file
        /// </summary>
        /// <param name="path">file system path</param>
        /// <returns>resulting file system object or null if the file does not exist</returns>
        public async Task<IFile> OpenExistingFileAsync(string path)
        {
            path = AdjustStreamPathForCosmosClient(path);
            CosmosStreamInfo info = await this.Client.GetStreamInfoAsync(path, false, true).ConfigureAwait(false);
                return info != null ? new CosmosFile(info, this, this.appConfig) : null;
        }

        /// <summary>
        ///      Creates a file
        /// </summary>
        /// <param name="path">file system path</param>
        /// <param name="lifetime">max lifetime for the file before it is automatically deleted</param>
        /// <param name="mode">file creation mode when the file already exists</param>
        /// <returns>resulting file system object</returns>
        /// <remarks>
        ///     not all file systems support lifetimes and the lifetimes are only applied on actual file creates, and not if the
        ///      FileCreateMode.OpenExisting flag is specified and the file already exists
        /// </remarks>
        public async Task<IFile> CreateFileAsync(
            string path,
            TimeSpan? lifetime,
            FileCreateMode mode)
        {
            path = AdjustStreamPathForCosmosClient(path);
            CosmosCreateStreamMode TranslateMode(FileCreateMode input)
            {
                switch (input)
                {
                    case FileCreateMode.OpenExisting: return CosmosCreateStreamMode.OpenExisting;
                    case FileCreateMode.CreateAlways: return CosmosCreateStreamMode.CreateAlways;
                    case FileCreateMode.FailIfExists: return CosmosCreateStreamMode.ThrowIfExists;
                    default: throw new ArgumentOutOfRangeException(nameof(mode));
                }
            }
                await this.Client.CreateAsync(path, lifetime, TranslateMode(mode)).ConfigureAwait(false);

                return new CosmosFile(
                    await this.Client.GetStreamInfoAsync(path, true, false).ConfigureAwait(false),
                    this, this.appConfig);
        }

        /// <summary>
        ///      Creates a writer that buffers writes to the target file
        /// </summary>
        /// <param name="path">file system path</param>
        /// <param name="lifetime">max lifetime for the file before it is automatically deleted</param>
        /// <param name="mode">file creation mode when the file already exists</param>
        /// <returns>resulting file system object</returns>
        /// <remarks>
        ///     not all file systems support lifetimes and the lifetimes are only applied on actual file creates, and not if the
        ///      FileCreateMode.OpenExisting flag is specified and the file already exists
        /// </remarks>
        public async Task<IQueuedFileWriter> CreateQueuedFileWriterAsync(
            string path,
            TimeSpan? lifetime,
            FileCreateMode mode)
        {
            path = AdjustStreamPathForCosmosClient(path);
            IFile file = await this.CreateFileAsync(path, lifetime, mode).ConfigureAwait(false);
            return new QueuedFileWriter(file, 2 * 1024 * 1024);
        }

        /// <summary>
        ///      Deletes a file
        /// </summary>
        /// <param name="path">full path to file</param>
        /// <param name="ignoreNotFound">true to ignore cases where the file is not found</param>
        /// <returns>resulting value</returns>
        public async Task DeleteAsync(
            string path, 
            bool ignoreNotFound)
        {
            path = AdjustStreamPathForCosmosClient(path);
            await this.Client.DeleteAsync(path, ignoreNotFound).ConfigureAwait(false);
        }

        // This is needed as paths are coded into the items added to queues, reading those items will fail.
        private string AdjustStreamPathForCosmosClient(string stream)
        {
            // We must include vccpath.

            // If path was cached when VcClient was used get relative path to /local dir.
            // all cosmos vccpath starts with /local
            var relativeRootDirPathIndex = stream.IndexOf("local/");
            if (relativeRootDirPathIndex > 0)
            {
                return $"{stream.Substring(relativeRootDirPathIndex)}";
            }

            return stream;
        }
    }
}
