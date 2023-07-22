// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    ///    the set of operations that a given IFile can support
    /// </summary>
    [Flags]
    public enum SupportedFileOperations
    {
        Read,

        Write,

        Delete,
    }

    /// <summary>
    ///    contact for file system files
    /// </summary>
    public interface IFile : IFileSystemObject
    {
        /// <summary>
        ///    Gets the set of operations this object can support
        /// </summary>
        SupportedFileOperations Operations { get; }

        /// <summary>
        ///     Gets the file creation date
        /// </summary>
        DateTimeOffset Created { get; }

        /// <summary>
        ///     Gets the size of the object in bytes
        /// </summary>
        long Size { get; }

        /// <summary>
        ///     Gets a stream containing the data for the object
        /// </summary>
        /// <returns>resulting value</returns>
        Stream GetDataReader();

        /// <summary>
        ///      Reads a chunk of a file
        /// </summary>
        /// <param name="offset">offset to start reading at</param>
        /// <param name="maxSize">maximum size</param>
        /// <returns>stream containing the requested chunk</returns>
        Task<Stream> ReadFileChunkAsync(
            long offset,
            int maxSize);

        /// <summary>
        ///     Deletes the file
        /// </summary>
        /// <returns>resulting value</returns>
        Task DeleteAsync();

        /// <summary>
        ///     Appends to the file
        /// </summary>
        /// <param name="contents">data to write</param>
        /// <returns>resulting value</returns>
        Task AppendAsync(string contents);

        /// <summary>
        ///     Moves the file to the target path with the same name
        /// </summary>
        /// <param name="targetPath">path relative to the file system root that the file should be moved to</param>
        /// <param name="overwriteExisting">true to overwrite existing file in the target path; false otherwise</param>
        /// <param name="ignoreMissingSource">true to not fail if the source file no longer exists; false otherwise</param>
        /// <returns>resulting value</returns>
        Task MoveRelativeAsync(
            string targetPath,
            bool overwriteExisting,
            bool ignoreMissingSource);

        /// <summary>
        ///     Sets the file life time
        /// </summary>
        /// <param name="lifetime">lifetime to set- a null value set the lifespan to infinite</param>
        /// <param name="ignoreMissing">true to not fail if the file no longer exists; false otherwise</param>
        /// <returns>resulting value</returns>
        /// <remarks>not all file systems support file lifetimes</remarks>
        Task SetLifetimeAsync(
            TimeSpan? lifetime,
            bool ignoreMissing);
    }
}
