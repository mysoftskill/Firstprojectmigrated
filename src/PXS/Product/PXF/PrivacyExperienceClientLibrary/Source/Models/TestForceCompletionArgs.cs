// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Models
{
    using System;

    /// <summary>
    ///     Arguments for forcing command completion by command id
    /// </summary>
    public class TestForceCompletionArgs : PrivacyExperienceClientBaseArgs
    {
        /// <summary>
        ///     The command id
        /// </summary>
        public Guid CommandId { get; }

        /// <summary>
        ///     A constructor with obvious arguments but still a required xmldoc comment.
        /// </summary>
        public TestForceCompletionArgs(string userProxyTicket, Guid commandId)
            : base(userProxyTicket)
        {
            this.CommandId = commandId;
        }

        /// <summary>
        ///     Creates the query string for this argument
        /// </summary>
        public QueryStringCollection CreateQueryStringCollection()
        {
            return new QueryStringCollection
            {
                { "commandId", this.CommandId.ToString() }
            };
        }
    }
}
