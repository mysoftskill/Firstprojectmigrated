// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility
{
    using System;

    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem.Cosmos;

    /// <summary>
    ///     contract for objects that can create periodic file writers
    /// </summary>
    public interface IPeriodicFileWriterFactory
    {
        /// <summary>
        ///     Creates the a new periodic
        /// </summary>
        /// <param name="period">period to generate new files for</param>
        /// <param name="fileSystem">file system</param>
        /// <param name="pathSuffix">path suffix</param>
        /// <param name="fileNameGenerator">file name generator</param>
        /// <returns>resulting value</returns>
        IPeriodicFileWriter Create(
            ICosmosFileSystem fileSystem,
            string pathSuffix,
            Func<DateTimeOffset, string> fileNameGenerator,
            TimeSpan period);
    }
}
