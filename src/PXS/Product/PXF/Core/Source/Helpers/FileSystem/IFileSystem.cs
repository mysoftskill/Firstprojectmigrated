// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    ///     mode CreateFile operates in if a file already exists
    /// </summary>
    public enum FileCreateMode
    {
        /// <summary>
        ///      throw if the stream exists
        /// </summary>
        FailIfExists = 0,

        /// <summary>
        ///      treat an existing file as a successful create
        /// </summary>
        OpenExisting,

        /// <summary>
        ///      delete an existing file and recreate or truncate existing file
        /// </summary>
        CreateAlways,
    }

    /// <summary>
    ///     the set of operations supported by the file system
    /// </summary>
    [Flags]
    public enum SupportedFileSystemOperations
    {
        CreateWithLifetime,

        Create,

        Open,

        Delete
    }

    /// <summary>
    ///     contract for objects that implement a file system
    /// </summary>
    public interface IFileSystem
    {
        /// <summary>
        ///     Gets the supported operations
        /// </summary>
        SupportedFileSystemOperations SupportedOperations { get; }

        /// <summary>
        ///     Gets the root directory for the file system
        /// </summary>
        string RootDirectory { get; }

        /// <summary>
        ///     Gets a tag for the file system
        /// </summary>
        string Tag { get; }

        /// <summary>
        ///     Opens the specified existing directory
        /// </summary>
        /// <param name="path">file system path</param>
        /// <returns>resulting file system object</returns>
        Task<IDirectory> OpenExistingDirectoryAsync(string path);

        /// <summary>
        ///     Opens the specified existing file
        /// </summary>
        /// <param name="path">file system path</param>
        /// <returns>resulting file system object</returns>
        Task<IFile> OpenExistingFileAsync(string path);

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
        Task<IFile> CreateFileAsync(
            string path,
            TimeSpan? lifetime,
            FileCreateMode mode);

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
        Task<IQueuedFileWriter> CreateQueuedFileWriterAsync(
            string path,
            TimeSpan? lifetime,
            FileCreateMode mode);

        /// <summary>
        ///     Deletes a file
        /// </summary>
        /// <param name="path">full path to file</param>
        /// <param name="ignoreNotFound">true to ignore cases where the file is not found</param>
        Task DeleteAsync(
            string path,
            bool ignoreNotFound);
    }
}
