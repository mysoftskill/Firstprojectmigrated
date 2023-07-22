// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.DataContracts.ExportTypes
{
    using System;
    using System.Collections.Generic;

    public class ExportStatusHistoryRecord
    {
        /// <summary>
        ///     Gets or sets the date the request was Completed
        /// </summary>
        public DateTimeOffset? Completed { get; set; }

        /// <summary>
        ///     Gets or sets the data types
        /// </summary>
        public IList<string> DataTypes { get; set; }

        /// <summary>
        ///     Gets or sets the end time of the exported data
        /// </summary>
        public DateTimeOffset EndTime { get; set; }

        /// <summary>
        ///     Gets or sets the error string, null when incomplete or completed successfully
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        ///     Gets or sets the export id
        /// </summary>
        public string ExportId { get; set; }

        /// <summary>
        ///     Gets or sets the date the request was submitted
        /// </summary>
        public DateTimeOffset RequestedAt { get; set; }

        /// <summary>
        ///     Gets or sets the start time of the exported data
        /// </summary>
        public DateTimeOffset StartTime { get; set; }

        /// <summary>
        ///     Gets or sets the time the zip file expires
        /// </summary>
        public DateTimeOffset ZipFileExpires { get; set; }

        /// <summary>
        ///     Gets or set the size of the zip file
        /// </summary>
        public long ZipFileSize { get; set; }

        /// <summary>
        ///     Gets or sets the completed zip file Uri
        /// </summary>
        public Uri ZipFileUri { get; set; }

        /// <summary>
        ///     Gets or sets the export archives delete status
        /// </summary>
        public ExportArchivesDeleteStatus ExportArchiveDeleteStatus { get; set; }

        /// <summary>
        ///     Gets or sets the Export Archives Delete requested time
        /// </summary>
        public DateTimeOffset ExportArchiveDeleteRequestedTime { get; set; }


        /// <summary>
        ///     Gets or sets the Export Archives Delete requester id
        /// </summary>
        public string ExportArchiveDeleteRequesterId { get; set; }

        /// <summary>
        ///     type of export: full, quick
        /// </summary>
        public ExportType ExportType { get; set; }
    }
}
