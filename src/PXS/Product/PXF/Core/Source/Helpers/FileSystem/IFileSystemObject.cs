// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem
{
    /// <summary>
    ///    contact for the base file system object
    /// </summary>
    public enum FileSystemObjectType
    {
        /// <summary>
        ///     default option
        /// </summary>
        Default = 0,

        /// <summary>
        ///     directory option
        /// </summary>
        Directory,

        /// <summary>
        ///     file option
        /// </summary>
        File
    }

    /// <summary>
    ///    contact for the base file system object
    /// </summary>
    public interface IFileSystemObject
    {
        /// <summary>
        ///     Gets the object type
        /// </summary>
        FileSystemObjectType Type { get; }

        /// <summary>
        ///     Gets the parent of the directory
        /// </summary>
        string ParentDirectory { get; }

        /// <summary>
        ///     Gets the full path of the object
        /// </summary>
        string Path { get; }

        /// <summary>
        ///     Gets the name of the object
        /// </summary>
        string Name { get; }
    }
}
