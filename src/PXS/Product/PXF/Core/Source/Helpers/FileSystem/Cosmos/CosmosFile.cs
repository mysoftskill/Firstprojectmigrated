// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem.Cosmos
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.Common.Cosmos;
    using Microsoft.Membership.MemberServices.Common.Utilities;


    /// <summary>
    ///     implements the file system file interface atop Cosmos
    /// </summary>
    public class CosmosFile : IFile
    {
        private readonly CosmosStreamInfo info;

        private readonly ICosmosFileSystem fileSystem;

        private readonly IAppConfiguration appConfig;

        /// <summary>
        ///     Initializes a new instance of the CosmosFile class
        /// </summary>
        /// <param name="info">stream info for the file</param>
        /// <param name="fileSystem">cosmos file system</param>
        /// <param name="appConfig">AppConfig to read dynamic settings</param>
        internal CosmosFile(
            CosmosStreamInfo info,
            ICosmosFileSystem fileSystem,
            IAppConfiguration appConfig)
        {
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            this.info = info ?? throw new ArgumentNullException(nameof(info));

            (this.ParentDirectory, this.Name) = CosmosFileSystemUtility.SplitNameAndPath(info.StreamName);

            this.Created = new DateTimeOffset(info.CreateTime.ToUniversalTime());

            this.Path = info.StreamName;
            this.appConfig = appConfig;
        }

        /// <summary>
        ///     Gets the object type
        /// </summary>
        public FileSystemObjectType Type => FileSystemObjectType.File;

        /// <summary>
        ///     Gets the set of operations this object can support
        /// </summary>
        public SupportedFileOperations Operations => 
            SupportedFileOperations.Read | 
            SupportedFileOperations.Delete | 
            SupportedFileOperations.Write;

        /// <summary>
        ///     Gets the file creation date
        /// </summary>
        public DateTimeOffset Created { get; }

        /// <summary>
        ///     Gets the parent of the directory
        /// </summary>
        public string ParentDirectory { get; }

        /// <summary>
        ///     Gets the full path of the file
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        ///     Gets the file name
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     Gets the size of the file in bytes
        /// </summary>
        public long Size => this.info.Length;

        /// <summary>
        ///     Gets a stream containing the data for the object
        /// </summary>
        /// <returns>resulting value</returns>
        public Stream GetDataReader()
        {
            return new CosmosChunkedReadStream(this.info, this.fileSystem.Client, this.appConfig);
        }

        /// <summary>
        ///      Reads a chunk of a file
        /// </summary>
        /// <param name="offset">offset to start reading at</param>
        /// <param name="maxSize">maximum size</param>
        /// <returns>stream containing the requested chunk</returns>
        public async Task<Stream> ReadFileChunkAsync(
            long offset, 
            int maxSize)
        {
                Stream finalResult = new MemoryStream();

                DataInfo result = await this.fileSystem.Client
                    .ReadStreamAsync(this.Path, offset, maxSize, true)
                    .ConfigureAwait(false);

                if (result != null && result.Length > 0)
                {
                    finalResult.Write(result.Data, 0, result.Length);
                    finalResult.Seek(0, SeekOrigin.Begin);
                }

                return finalResult;   
        }

        /// <summary>
        ///     Deletes the file
        /// </summary>
        /// <returns>resulting value</returns>
        public async Task DeleteAsync()
        {
            
                await this.fileSystem.Client.DeleteAsync(this.Path, true).ConfigureAwait(false);
        }

        /// <summary>
        ///     Appends data to the file
        /// </summary>
        /// <param name="contents">data to append</param>
        /// <returns>resulting value</returns>
        public async Task AppendAsync(string contents)
        {
            if (string.IsNullOrEmpty(contents) == false)
            {
                byte[] data = Encoding.UTF8.GetBytes(contents);
                    await this.fileSystem.Client.AppendAsync(this.Path, data).ConfigureAwait(false);
            }
        }

        /// <summary>
        ///     Moves the file to the target path with the same name
        /// </summary>
        /// <param name="targetPath">path relative to the file system root that the file should be moved to</param>
        /// <param name="overwriteExisting">true to overwrite existing file in the target path; false otherwise</param>
        /// <param name="ignoreMissingSource">true to not fail if the source file no longer exists; false otherwise</param>
        /// <returns>resulting value</returns>
        public async Task MoveRelativeAsync(
            string targetPath, 
            bool overwriteExisting, 
            bool ignoreMissingSource)
        {
            ArgumentCheck.ThrowIfNullEmptyOrWhiteSpace(targetPath, nameof(targetPath));

            targetPath = targetPath.Trim();

            if (targetPath[0] == '/')
            {
                targetPath = targetPath.Substring(1);
            }

            if (targetPath[targetPath.Length - 1] != '/')
            {
                targetPath += "/";
            }

                string fullTargetPath = this.fileSystem.RootDirectory + targetPath + this.Name;

                await this.fileSystem.Client.RenameAsync(this.Path, fullTargetPath, overwriteExisting, ignoreMissingSource);

                this.Path = fullTargetPath; 
        }

        /// <summary>
        ///     Sets the file life time
        /// </summary>
        /// <param name="lifetime">lifetime to set- a null value set the lifespan to infinite</param>
        /// <param name="ignoreMissing">true to not fail if the file no longer exists; false otherwise</param>
        /// <returns>resulting value</returns>
        /// <remarks>not all file systems support file lifetimes</remarks>
        public async Task SetLifetimeAsync(
            TimeSpan? lifetime,
            bool ignoreMissing)
        {
                await this.fileSystem.Client.SetLifetimeAsync(this.Path, lifetime, ignoreMissing).ConfigureAwait(false);   
        }
    }
}
