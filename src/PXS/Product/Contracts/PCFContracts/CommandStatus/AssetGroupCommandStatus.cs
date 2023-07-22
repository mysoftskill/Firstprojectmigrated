// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.PrivacyServices.PXS.Command.CommandStatus
{
    using System;

    /// <summary>
    ///     The status of a command for a particular asset group.
    /// </summary>
    public class AssetGroupCommandStatus
    {
        /// <summary>
        ///     The number of affected rows by this asset group, or null if not reported.
        /// </summary>
        public int? AffectedRows { get; set; }

        /// <summary>
        ///     The ID of the asset group's agent.
        /// </summary>
        public Guid AgentId { get; set; }

        /// <summary>
        ///     The asset group's unique identifier.
        /// </summary>
        public Guid AssetGroupId { get; set; }

        /// <summary>
        ///     The asset group's qualifier.
        /// </summary>
        public string AssetGroupQualifier { get; set; }

        /// <summary>
        ///     The time at which the agent marked this command as complete.
        /// </summary>
        public DateTimeOffset? CompletedTime { get; set; }

        /// <summary>
        ///     Indicates if this command was force completed. Force Completion can happen either due to a test user clicking "force complete" or
        ///     an automated process for exports after 21 days.
        /// </summary>
        public bool ForceCompleted { get; set; }

        /// <summary>
        ///     Indicates what action the Privacy Command Feed took when processing this command. Values can be found here:
        ///     https://microsoft.visualstudio.com/Universal%20Store/_git/MEE.Privacy.CommandFeed.Service?path=%2FProduct%2FServer%2FCommon%2FModels%2FCommandIngestionAuditRecord.cs&amp;
        ///     version=GBdevelop&amp;_a=contents
        /// </summary>
        public string IngestionActionTaken { get; set; }

        /// <summary>
        ///     The assembly version of the Privacy Command Feed that acted on the ingestion of this command.
        /// </summary>
        public string IngestionAssemblyVersion { get; set; }

        /// <summary>
        ///     Indicates what data the Privacy Command Feed used to process the filtering for this command. Each data set version is based on a different dump of PDMS/DataGrid data,
        ///     so behaviors may be different for different snapshots of the underlying data.
        /// </summary>
        public long IngestionDataSetVersion { get; set; }

        /// <summary>
        ///     The debug text emitted by the Privacy Command Feed explaining what action it took for this command.
        /// </summary>
        public string IngestionDebugText { get; set; }

        /// <summary>
        ///     The time at which the command was inserted into this agent's queue.
        /// </summary>
        public DateTimeOffset? IngestionTime { get; set; }

        /// <summary>
        ///     The time at which the agent marked this command as soft-deleted.
        /// </summary>
        public DateTimeOffset? SoftDeleteTime { get; set; }
    }
}
