// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///     Interface for aad s2s auth result
    /// </summary>
    public interface IAadS2SAuthResult
    {
        /// <summary>
        ///     Gets the calling app's display name
        /// </summary>
        /// <example>Graph explorer</example>
        string AppDisplayName { get; }

        /// <summary>
        ///     Gets the diagnostic logs
        /// </summary>
        IReadOnlyList<string> DiagnosticLogs { get; }

        /// <summary>
        ///     Gets the exception of the result, if any.
        /// </summary>
        Exception Exception { get; }

        /// <summary>
        ///     The Inbound App Id
        /// </summary>
        string InboundAppId { get; }

        /// <summary>
        ///     Gets the ObjectId
        /// </summary>
        Guid ObjectId { get; }

        /// <summary>
        ///     Gets the inbound subject ticket
        /// </summary>
        string SubjectTicket { get; }

        /// <summary>
        ///     Gets the succeeded status of the auth result
        /// </summary>
        bool Succeeded { get; }

        /// <summary>
        ///     Gets the TenantId
        /// </summary>
        Guid TenantId { get; }

        /// <summary>
        ///     Gets the User Principal Name (UPN)
        /// </summary>
        /// <remarks>This originates from the access token.</remarks>
        string UserPrincipalName { get; }

        /// <summary>
        ///     Gets the access token.
        /// </summary>
        string AccessToken { get; }
    }
}
