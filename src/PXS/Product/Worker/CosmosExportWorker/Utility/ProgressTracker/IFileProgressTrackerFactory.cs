// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility
{
    /// <summary>
    ///     contract for objects that create file progress trackers
    /// </summary>
    public interface IFileProgressTrackerFactory
    {
        /// <summary>
        ///     Creates a file progress tracker
        /// </summary>
        /// <param name="agentId">agent id</param>
        /// <param name="name">file name</param>
        /// <param name="errorLogger">error logger</param>
        /// <returns>file progress tracker</returns>
        IFileProgressTracker Create(
            string agentId,
            string name,
            Utility.TraceLoggerAction errorLogger);
    }
}
