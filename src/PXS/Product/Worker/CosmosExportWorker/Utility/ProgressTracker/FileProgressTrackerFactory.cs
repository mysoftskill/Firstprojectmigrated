// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility
{
    using System;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;

    /// <summary>
    ///     creates file progress trackers
    /// </summary>
    public class FileProgressTrackerFactory : IFileProgressTrackerFactory
    {
        private readonly IFileSystemManager fileSysMgr;
        private readonly IClock clock;
        private readonly int maxPendingBufferSize;

        /// <summary>
        ///     Initializes a new instance of the FileProgressTrackerFactory class
        /// </summary>
        /// <param name="agentConfig">agent configuration</param>
        /// <param name="fileSystemManager">file system manager</param>
        /// <param name="clock">time clock</param>
        public FileProgressTrackerFactory(
            ICosmosExportAgentConfig agentConfig,
            IFileSystemManager fileSystemManager, 
            IClock clock)
        {
            ArgumentCheck.ThrowIfNull(agentConfig, nameof(agentConfig));

            this.fileSysMgr = fileSystemManager ?? throw new ArgumentNullException(nameof(fileSystemManager));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));

            this.maxPendingBufferSize = agentConfig.MaxActivityLogBufferSize;
        }

        /// <summary>
        ///     Creates a file progress tracker
        /// </summary>
        /// <param name="agentId">agent id</param>
        /// <param name="name">file name</param>
        /// <param name="errorLogger">error logger</param>
        /// <returns>file progress tracker</returns>
        public IFileProgressTracker Create(
            string agentId, 
            string name, 
            Utility.TraceLoggerAction errorLogger)
        {
            return new FileProgressTracker(this.fileSysMgr, this.clock, agentId, name, errorLogger, this.maxPendingBufferSize);
        }
    }
}
