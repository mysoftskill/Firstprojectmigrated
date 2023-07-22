// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Models.PrivacySubject
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.PrivacySubject;

    /// <summary>
    ///     Arguments for delete command for privacy subject.
    /// </summary>
    public class DeleteByTypesArgs : PrivacySubjectClientBaseArgs
    {
        /// <summary>
        ///     Delete these types of data, these should come from Microsoft.PrivacyServices.Policy.DataTypes.KnownIds, for
        ///     example <code>Policies.Current.DataTypes.Ids.ProductAndServiceUsage</code>.
        /// </summary>
        public IList<string> Types { get; }

        /// <summary>
        ///     The end time for the deleted data
        /// </summary>
        public DateTimeOffset EndTime { get; }

        /// <summary>
        ///     The start time for the deleted data
        /// </summary>
        public DateTimeOffset StartTime { get; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DeleteByTypesArgs" /> class.
        /// </summary>
        public DeleteByTypesArgs(IPrivacySubject subject, IList<string> types, DateTimeOffset startTime, DateTimeOffset endTime)
            : base(subject)
        {
            this.Types = types ?? throw new ArgumentNullException(nameof(types));
            this.StartTime = startTime;
            this.EndTime = endTime;

            if (types.Count == 0)
                throw new ArgumentException("Value cannot be an empty collection.", nameof(types));
        }

        /// <summary>
        ///     Populates query string parameters collection, using args data.
        /// </summary>
        public QueryStringCollection CreateQueryStringCollection()
        {
            return base.PopulateCommonQueryStringCollection(new QueryStringCollection
            {
                { "dataTypes", string.Join(",", this.Types) },
                { "startTime", this.StartTime.ToString("o") },
                { "endTime", this.EndTime.ToString("o") }
            });
        }
    }
}
