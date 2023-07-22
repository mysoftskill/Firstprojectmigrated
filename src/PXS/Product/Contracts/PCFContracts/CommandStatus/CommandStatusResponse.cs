// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.PXS.Command.CommandStatus
{
    using System;
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;

    /// <summary>
    ///     PCF command status response.
    /// </summary>
    public class CommandStatusResponse
    {
        /// <summary>
        ///     The set of asset group status records for this command.
        /// </summary>
        public List<AssetGroupCommandStatus> AssetGroupStatuses { get; set; }

        /// <summary>
        ///     The unique identifier of the command.
        /// </summary>
        public Guid CommandId { get; set; }

        /// <summary>
        ///     The type of command (Delete/Export/AccountClose).
        /// </summary>
        public string CommandType { get; set; }

        /// <summary>
        ///     The context of the command. Opaque to NGP.
        /// </summary>
        public string Context { get; set; }

        /// <summary>
        ///     The final export destination URI.
        /// </summary>
        public Uri FinalExportDestinationUri { get; set; }

        /// <summary>
        ///     A flag indicating if the command is globally complete, which indicates that all agents to which the command was sent have processed to completion.
        /// </summary>
        public bool IsGloballyComplete { get; set; }

        /// <summary>
        ///     The requester of the command, such as the dashboard or AAD tenant ID. Opaque to NGP.
        /// </summary>
        public string Requester { get; set; }

        /// <summary>
        ///     The subject of the command.
        /// </summary>
        public IPrivacySubject Subject { get; set; }

        /// <summary>
        ///     A flag indicating if the command was synthetically generated for end-to-end testing.
        /// </summary>
        public bool IsSyntheticCommand { get; set; }

        /// <summary>
        ///     Indicates the type of the subject of the command.
        /// </summary>
        public string SubjectType { get; set; }

        /// <summary>
        ///     For delete commands, indicates the type of the command's predicate.
        /// </summary>
        public string PredicateType { get; set; }

        /// <summary>
        ///     Indicates the data type(s) of this command.
        /// </summary>
        public IEnumerable<string> DataTypes { get; set; }

        /// <summary>
        ///     Indicates when the command was created in DocDb.
        /// </summary>
        public DateTimeOffset CreatedTime { get; set; }

        /// <summary>
        ///     Indicates when the command was completed.
        /// </summary>
        public DateTimeOffset CompletedTime { get; set; }

        /// <summary>
        ///     The ratio between the number of asset groups for which agents reported completion of this command and
        ///     the total number of asset groups for which this command is applicable.
        ///     A valid success rate is a number between 0 and 1. A success rate calculation error is represented as a negative number such as -1.
        /// </summary>
        public double CompletionSuccessRate { get; set; }

        /// <summary>
        ///     The total number of commands that were sent to all agent/asset group pairs.
        /// </summary>
        public long? TotalCommandCount { get; set; }

        /// <summary>
        ///     The PDMS config version PCF used to perform the filtering.
        /// </summary>
        public long? IngestionDataSetVersion { get; set; }

        /// <summary>
        ///     The PCF assembly version that handled the filtering.
        /// </summary>
        public string IngestionAssemblyVersion { get; set; }


        /// <summary>
        ///     status of export archives delete request
        /// </summary>
        public ExportArchivesDeleteStatus ExportArchivesDeleteStatus { get; set; }
    }
}
