// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Models
{
    using System.Collections.Generic;

    /// <summary>
    ///     Delete timeline data by ids
    /// </summary>
    public class DeleteTimelineByIdsArgs : PrivacyExperienceClientBaseArgs
    {
        /// <summary>
        ///     The list of ids to delete from the timeline
        /// </summary>
        public IList<string> Ids { get; }

        /// <summary>
        ///     Constructs the delete arguments
        /// </summary>
        public DeleteTimelineByIdsArgs(string userProxyTicket, IList<string> ids)
            : base(userProxyTicket)
        {
            this.Ids = ids;
        }

        /// <summary>
        ///     Creates query string collection
        /// </summary>
        public QueryStringCollection CreateQueryStringCollection()
        {
            // Ids are passed in the body
            return new QueryStringCollection();
        }
    }
}
