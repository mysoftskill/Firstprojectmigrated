// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.PrivacyOperation.Contracts.PrivacySubject
{
    using System;

    using Microsoft.PrivacyServices.PrivacyOperation.Contracts.Json;

    using Newtonsoft.Json;

    /// <summary>
    ///     Defines MSA subject, when command is issued by the user and for the user, authenticated
    ///     with their own proxy ticket.
    /// </summary>
    [AllowSerialization]
    public class MsaSelfAuthSubject : IPrivacySubject
    {
        /// <summary>
        ///     A read-only storage for the proxy ticket. This ticket will not be serialized in request, service
        ///     should rely on the ticket value from header.
        /// </summary>
        private readonly string userProxyTicket;

        public MsaSelfAuthSubject(string userProxyTicket)
        {
            this.userProxyTicket = userProxyTicket ?? throw new ArgumentNullException(nameof(userProxyTicket));
            if (string.IsNullOrWhiteSpace(userProxyTicket))
                throw new ArgumentException("User proxy ticket cannot be empty.", nameof(userProxyTicket));
        }

        /// <summary>
        ///     Gets current user proxy ticket.
        /// </summary>
        public string GetUserProxyTicket() => this.userProxyTicket;

        /// <inheritdoc cref="IPrivacySubject.Validate" />
        /// .
        public void Validate(SubjectUseContext useContext)
        {
        }

        public void Validate(SubjectUseContext useContext, bool useEmailOnlyManadatoryRule)
        {
        }

        [JsonConstructor]
        protected MsaSelfAuthSubject()
        {
            //  Dummy constructor for use by JSON serializer.
        }
    }
}
