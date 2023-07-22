// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Models.PrivacySubject
{
    using System;

    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.PrivacySubject;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Extensions;

    /// <summary>
    ///     Base class of arguments for commands for privacy subject.
    /// </summary>
    public abstract class PrivacySubjectClientBaseArgs : PrivacyExperienceClientBaseArgs
    {
        /// <summary>
        ///     Gets privacy subject definition.
        /// </summary>
        public IPrivacySubject Subject { get; }

        /// <summary>
        ///     Gets or sets optional command context. This is a free-form string, which is not interpreted 
        ///     by PXS in any way.
        /// </summary>
        public string Context { get; set; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="PrivacySubjectClientBaseArgs" /> class.
        /// </summary>
        protected PrivacySubjectClientBaseArgs(IPrivacySubject subject) 
            : base()
        {
            this.Subject = subject ?? throw new ArgumentNullException(nameof(subject));

            if (subject is MsaSelfAuthSubject msaSelfAuthSubject)
            {
                this.UserProxyTicket = msaSelfAuthSubject.GetUserProxyTicket();
            }
        }

        /// <summary>
        ///     Adds common query string parameters to collection.
        /// </summary>
        protected QueryStringCollection PopulateCommonQueryStringCollection(QueryStringCollection collection)
        {
            return collection;
        }
    }
}
