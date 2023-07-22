// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility
{
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem.Cosmos;

    /// <summary>
    ///     contract for objects that manage a collection of tagged file systems
    /// </summary>
    public interface IFileSystemManager
    {
        /// <summary>
        ///     Gets Cosmos paths and expiry times
        /// </summary>
        ICosmosRelativePathsAndExpiryTimes CosmosPathsAndExpiryTimes { get; }

        /// <summary>
        ///     Gets file size thresholds
        /// </summary>
        ICosmosFileSizeThresholds FileSizeThresholds { get; }

        /// <summary>
        ///     Gets the activity log store dedicated file system
        /// </summary>
        ICosmosFileSystem ActivityLog { get; }

        /// <summary>
        ///     Gets the dead letter store dedicated file system
        /// </summary>
        ICosmosFileSystem DeadLetter { get; }

        /// <summary>
        ///     Gets the statistics log dedicated file system
        /// </summary>
        ICosmosFileSystem StatsLog { get; }

        /// <summary>Gets the specified file system</summary>
        /// <param name="tag">file system tag</param>
        /// <returns>resulting value</returns>
        ICosmosFileSystem GetFileSystem(string tag);
    }
}
