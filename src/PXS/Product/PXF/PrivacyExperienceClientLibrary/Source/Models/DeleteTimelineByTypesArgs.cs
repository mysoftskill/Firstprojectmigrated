// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///     Delete timeline data in bulk by type
    /// </summary>
    public class DeleteTimelineByTypesArgs : PrivacyExperienceClientBaseArgs
    {
        /// <summary>
        ///     The period to delete data, last X timespan
        /// </summary>
        public TimeSpan Period { get; }

        /// <summary>
        ///     Delete these types of data, these should come from Microsoft.PrivacyServices.Policy.DataTypes.KnownIds, for
        ///     example <code>Policies.Current.DataTypes.Ids.ProductAndServiceUsage</code>
        /// </summary>
        public IList<string> Types { get; }

        /// <summary>
        ///     Constructs the delete argument
        /// </summary>
        public DeleteTimelineByTypesArgs(string userProxyTicket, TimeSpan period, IList<string> types)
            : base(userProxyTicket)
        {
            if (types == null)
                throw new ArgumentNullException(nameof(types));
            if (types.Count == 0)
                throw new ArgumentException("Value cannot be an empty collection.", nameof(types));

            this.Period = period;
            this.Types = types;
        }

        /// <summary>
        ///     Constructs the query string collection
        /// </summary>
        public QueryStringCollection CreateQueryStringCollection()
        {
            return new QueryStringCollection
            {
                { "types", string.Join(",", this.Types) },
                { "period", this.Period.ToString() }
            };
        }
    }
}
