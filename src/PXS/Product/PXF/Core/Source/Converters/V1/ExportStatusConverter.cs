// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Converters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.DataContracts.ExportTypes;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Telemetry;

    /// <summary>
    ///     Export Status Converter
    /// </summary>
    public static class ExportStatusConverter
    {
        public const string NotYetProcessed = "Not Yet Processed";

        public static ExportStatusHistoryRecord CreateHistoryRecordFromStatus(ExportStatusRecord status, DateTimeOffset submitDate)
        {
            var history = new ExportStatusHistoryRecord
            {
                Error = status.LastError,
                ExportId = status.ExportId,
                RequestedAt = submitDate,
                DataTypes = status.DataTypes,
                StartTime = status.StartTime,
                EndTime = status.EndTime,
                ZipFileUri = status.ZipFileUri,
                ZipFileExpires = status.ZipFileExpires,
                ZipFileSize = status.ZipFileSize
            };
            return history;
        }

        /// <summary>
        ///     Convert an external export request to an internal export status record
        /// </summary>
        public static ExportStatusRecord FromExportRequest(string puid, string requestId, string ticket, IList<string> dataTypes, DateTimeOffset startTime, DateTimeOffset endTime, string[] flights)
        {
            return new ExportStatusRecord(requestId)
            {
                UserId = puid,
                Ticket = ticket,
                DataTypes = dataTypes,
                StartTime = startTime,
                EndTime = endTime,
                Flights = flights
            };
        }

        public static ExportStatus ToExportStatus(ExportStatusHistoryRecord exportStatusHistoryRecord)
        {
            var status = new ExportStatus
            {
                ExportId = exportStatusHistoryRecord.ExportId,
                RequestedAt = exportStatusHistoryRecord.RequestedAt,
                IsComplete = exportStatusHistoryRecord.Completed.HasValue,
                DataTypes = exportStatusHistoryRecord.DataTypes,
                ExpiresAt = exportStatusHistoryRecord.ZipFileExpires,
                LastError = exportStatusHistoryRecord.Error,
                ZipFileUri = exportStatusHistoryRecord.ZipFileUri,
                ZipFileSize = exportStatusHistoryRecord.ZipFileSize,
                ExportArchivesDeleteStatus = (ExperienceContracts.ExportArchivesDeleteStatus)exportStatusHistoryRecord.ExportArchiveDeleteStatus,
                ExportType = (ExperienceContracts.ExportType)exportStatusHistoryRecord.ExportType,
            };
            return status;
        }
    }
}
