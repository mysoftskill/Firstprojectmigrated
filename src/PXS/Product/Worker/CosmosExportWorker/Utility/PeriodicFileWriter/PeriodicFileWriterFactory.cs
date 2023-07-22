// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility
{
    using System;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem.Cosmos;

    /// <summary>
    ///     contract for objects that can create periodic file writers
    /// </summary>
    public class PeriodicFileWriterFactory : IPeriodicFileWriterFactory
    {
        private readonly IClock clock;

        /// <summary>
        ///     Initializes a new instance of the PeriodicFileWriterFactory class
        /// </summary>
        /// <param name="clock">time clock</param>
        public PeriodicFileWriterFactory(IClock clock)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>
        ///     Creates the a new periodic
        /// </summary>
        /// <param name="period">period to generate new files for</param>
        /// <param name="fileSystem">file system</param>
        /// <param name="pathSuffix">path suffix</param>
        /// <param name="fileNameGenerator">file name generator</param>
        /// <returns>resulting value</returns>
        public IPeriodicFileWriter Create(
            ICosmosFileSystem fileSystem,
            string pathSuffix,
            Func<DateTimeOffset, string> fileNameGenerator,
            TimeSpan period)
        {
            return new PeriodicFileWriter(fileSystem, pathSuffix, fileNameGenerator, period, this.clock);
        }
    }
}
