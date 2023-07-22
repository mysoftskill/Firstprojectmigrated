// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.PrivacySubject
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    /// <summary>
    ///     Defines MSA subject, when command is issued by the user and for the user, authenticated 
    ///     with their own proxy ticket.
    /// </summary>
    [Json.AllowSerialization]
    public class MsaSelfAuthSubject : IPrivacySubject
    {
        /// <summary>
        ///     A read-only storage for the proxy ticket. This ticket will not be serialized in request, service
        ///     should rely on the ticket value from header.
        /// </summary>
        private readonly string userProxyTicket;

        [JsonConstructor]
        protected MsaSelfAuthSubject()
        {
            //  Dummy constructor for use by JSON serializer.
        }

        public MsaSelfAuthSubject(string userProxyTicket)
        {
            this.userProxyTicket = userProxyTicket ?? throw new ArgumentNullException(nameof(userProxyTicket));
            if (string.IsNullOrWhiteSpace(userProxyTicket))
                throw new ArgumentException("User proxy ticket cannot be empty.", nameof(userProxyTicket));
        }

        /// <inheritdoc cref="IPrivacySubject.Validate"/>.
        public void Validate(SubjectUseContext useContext)
        {
        }

        void IPrivacySubject.Validate(SubjectUseContext delete, bool useEmailOnlyManadatoryRule)
        {
        }

        /// <summary>
        ///     Gets current user proxy ticket.
        /// </summary>
        public string GetUserProxyTicket() => this.userProxyTicket;


    }
}
