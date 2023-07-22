// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///     An individual export status
    /// </summary>
    public class ExportStatus
    {
        /// <summary>
        ///     The list of data types the export is for
        /// </summary>
        public IList<string> DataTypes { get; set; }

        /// <summary>
        ///     The time that the download expires
        /// </summary>
        public DateTimeOffset ExpiresAt { get; set; }

        /// <summary>
        ///     The id of the export request
        /// </summary>
        public string ExportId { get; set; }

        /// <summary>
        ///     Whether or not the export is completed or failed
        /// </summary>
        public bool IsComplete { get; set; }

        /// <summary>
        ///     The last error that happened, or null if no error
        /// </summary>
        public string LastError { get; set; }

        /// <summary>
        ///     The time that the export request was made
        /// </summary>
        public DateTimeOffset RequestedAt { get; set; }

        /// <summary>
        ///     Gets the size of the zip file in bytes.
        /// </summary>
        public long ZipFileSize { get; set; }

        /// <summary>
        ///     Gets the Uri to azure for the zip file. This is a read only SAS uri.
        /// </summary>
        public Uri ZipFileUri { get; set; }

        /// <summary>
        ///     status of export archives delete request
        /// </summary>
        public ExportArchivesDeleteStatus ExportArchivesDeleteStatus { get; set; }

        /// <summary>
        ///     type of export: full, quick
        /// </summary>
        public ExportType ExportType { get; set; }

    }
}
