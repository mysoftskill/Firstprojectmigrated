// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Models
{
    using System;

    /// <summary>
    ///     Arguments for getting agent debug info by agent id
    /// </summary>
    public class TestGetAgentStatisticsArgs : PrivacyExperienceClientBaseArgs
    {
        /// <summary>
        ///     The command id
        /// </summary>
        public Guid AgentId { get; }

        /// <summary>
        ///     A constructor with obvious arguments but still a required xmldoc comment.
        /// </summary>
        public TestGetAgentStatisticsArgs(string userProxyTicket, Guid agentId)
            : base(userProxyTicket)
        {
            this.AgentId = agentId;
        }

        /// <summary>
        ///     Creates the query string for this argument
        /// </summary>
        public QueryStringCollection CreateQueryStringCollection()
        {
            return new QueryStringCollection
            {
                { "agentId", this.AgentId.ToString() }
            };
        }
    }
}
