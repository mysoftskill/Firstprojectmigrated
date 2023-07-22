// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.PXS.Command.CommandStatus
{
    using System;

    /// <summary>
    ///     The status of a PCF command queue for a particular asset group and subject type.
    /// </summary>
    public class AssetGroupQueueStatistics
    {
        /// <summary>
        ///     The ID of the asset group's agent.
        /// </summary>
        public Guid AgentId { get; set; }

        /// <summary>
        ///     The asset group's unique identifier.
        /// </summary>
        public Guid AssetGroupId { get; set; }

        /// <summary>
        ///     The type of subject.
        /// </summary>
        public string SubjectType { get; set; }

        /// <summary>
        ///     The asset group's qualifier.
        /// </summary>
        public string AssetGroupQualifier { get; set; }
        
        /// <summary>
        ///     The timestamp at which the oldest pending command was created.
        /// </summary>
        public DateTimeOffset? OldestPendingCommand { get; set; }

        /// <summary>
        ///     The earliest timestamp at which a lease became available. This is an indication of whether an agent is pulling from its queue in real time.
        /// </summary>
        public DateTimeOffset? MinimumLeaseAvailableTime { get; set; }

        /// <summary>
        ///     The number of commands that are in the pending state.
        /// </summary>
        public long? PendingCommandCount { get; set; }

        /// <summary>
        ///     The number of commands that have a lease available.
        /// </summary>
        public long? UnleasedCommandCount { get; set; }
    }
}
