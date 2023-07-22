// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem.Cosmos
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.Common.Cosmos;
    using Microsoft.Membership.MemberServices.Common.Utilities;    

    /// <summary>
    ///     implements the file system directory interface atop Cosmos
    /// </summary>
    public class CosmosDirectory : IDirectory
    {
        private readonly ICosmosFileSystem fileSystem;
        private readonly IAppConfiguration appConfig;

        /// <summary>
        ///     Initializes a new instance of the CosmosDirectory class
        /// </summary>
        /// <param name="info">stream info for the directory</param>
        /// <param name="fileSystem">cosmos file system</param>
        /// <param name="appConfig">AppConfig to read dynamic settings</param>
        internal CosmosDirectory(
            CosmosStreamInfo info,
            ICosmosFileSystem fileSystem,
            IAppConfiguration appConfig)
        {
            ArgumentCheck.ThrowIfNull(info, nameof(info));

            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

            this.Path = CosmosFileSystemUtility.TrimAndRemoveTrailingSlashes(info.StreamName);

            (this.ParentDirectory, this.Name) = CosmosFileSystemUtility.SplitNameAndPath(this.Path);
            this.appConfig = appConfig;
        }

        /// <summary>
        ///     Initializes a new instance of the CosmosDirectory class
        /// </summary>
        /// <param name="path">directory path</param>
        /// <param name="fileSystem">cosmos file system</param>
        /// <param name="appConfig">AppConfig to read dynamic settings</param>
        internal CosmosDirectory(
            string path,
            CosmosFileSystem fileSystem,
            IAppConfiguration appConfig)
        {
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

            path = CosmosFileSystemUtility.TrimAndRemoveTrailingSlashes(path);

            ArgumentCheck.ThrowIfNullEmptyOrWhiteSpace(path, nameof(path));

            (this.ParentDirectory, this.Name) = CosmosFileSystemUtility.SplitNameAndPath(path);
            this.Path = path;
            this.appConfig = appConfig;
        }

        /// <summary>
        ///     Gets the object type
        /// </summary>
        public FileSystemObjectType Type => FileSystemObjectType.Directory;

        /// <summary>
        ///     Gets the parent of the directory
        /// </summary>
        public string ParentDirectory { get; }

        /// <summary>
        ///     Gets the full path of the directory
        /// </summary>
        public string Path { get; }

        /// <summary>
        ///     Gets the name of the object
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     Enumerates the sub-objects of the directory
        /// </summary>
        /// <returns>resulting value</returns>
        public async Task<ICollection<IFileSystemObject>> EnumerateAsync()
        {
            ICollection<CosmosStreamInfo> result = await this.fileSystem.Client
                .GetDirectoryInfoAsync(this.Path, true)
                .ConfigureAwait(false);
            if (result == null)
            {
                throw new DirectoryNotFoundException($"Cosmos path {this.Path} does not exist");
            }

            return new List<IFileSystemObject>(
                result.Where(o => o.IsDirectory).Select(o => new CosmosDirectory(o, this.fileSystem, this.appConfig) as IFileSystemObject)
                .Concat(
                    result
                        .Where(o => o.IsDirectory == false && o.IsComplete)
                        .Select(o => new CosmosFile(o, this.fileSystem, this.appConfig) as IFileSystemObject))
                .ToList());
        }
    }
}
