// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Models
{
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Extensions;

    /// <summary>
    ///     Privacy Experience Client Base Args
    /// </summary>
    public class PrivacyExperienceClientBaseArgs
    {
        /// <summary>
        ///     Gets or sets the correlation vector. The value should already be incremented.
        /// </summary>
        public string CorrelationVector { get; set; }

        /// <summary>
        ///     Gets or sets the family ticket
        /// </summary>
        public string FamilyTicket { get; set; }

        /// <summary>
        ///     Gets or sets the request identifier.
        /// </summary>
        public string RequestId { get; set; }

        /// <summary>
        ///     Gets or sets the user proxy ticket.
        /// </summary>
        public string UserProxyTicket { get; protected set; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="PrivacyExperienceClientBaseArgs" /> class.
        /// </summary>
        /// <param name="userProxyTicket">The user proxy ticket.</param>
        public PrivacyExperienceClientBaseArgs(string userProxyTicket)
            : this()
        {
            userProxyTicket.ThrowOnNull("userProxyTicket");
            this.UserProxyTicket = userProxyTicket;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="PrivacyExperienceClientBaseArgs" /> class.
        /// </summary>
        protected PrivacyExperienceClientBaseArgs()
        {
            this.RequestId = string.Empty;
            this.CorrelationVector = string.Empty;
        }
    }
}
