// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Models.PrivacySubject
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.PrivacySubject;

    /// <summary>
    ///     Arguments for export command for privacy subject.
    /// </summary>
    public class ExportByTypesArgs : PrivacySubjectClientBaseArgs
    {
        /// <summary>
        ///     Gets or sets the Categories collection
        /// </summary>
        public IList<string> DataTypes { get; }

        /// <summary>
        ///     The end time for the exported data
        /// </summary>
        public DateTimeOffset EndTime { get; }

        /// <summary>
        ///     Optional flag to suggest the request is synthetic (not issued by real user or CELA) but still needs to be handled as a real request.
        /// </summary>
        public bool IsSynthetic { get; }

        /// <summary>
        ///     The start time for the exported data
        /// </summary>
        public DateTimeOffset StartTime { get; }

        /// <summary>
        ///     Optional storage location URI. If not provided, PXS will use default location.
        /// </summary>
        public Uri StorageLocationUri { get; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ExportByTypesArgs" /> class.
        /// </summary>
        public ExportByTypesArgs(IPrivacySubject subject, IList<string> types, DateTimeOffset startTime, DateTimeOffset endTime, Uri storageLocationUri, bool isSynthetic = false)
            : base(subject)
        {
            this.DataTypes = types ?? throw new ArgumentNullException(nameof(types));
            this.StartTime = startTime;
            this.EndTime = endTime;
            this.StorageLocationUri = storageLocationUri;
            this.IsSynthetic = isSynthetic;

            if (types.Count == 0)
                throw new ArgumentException("Value cannot be an empty collection.", nameof(types));
        }

        /// <summary>
        ///     Populates query string parameters collection, using args data.
        /// </summary>
        public QueryStringCollection CreateQueryStringCollection()
        {
            return this.PopulateCommonQueryStringCollection(
                new QueryStringCollection
                {
                    { "dataTypes", string.Join(",", this.DataTypes) },
                    { "startTime", this.StartTime.ToString("o") },
                    { "endTime", this.EndTime.ToString("o") },
                    { "isSynthetic", this.IsSynthetic.ToString() }
                });
        }
    }
}
