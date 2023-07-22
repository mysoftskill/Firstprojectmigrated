// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication
{
    /// <summary>
    ///     Interface IJwtOutboundPolicy.
    /// </summary>
    public interface IJwtOutboundPolicy
    {
        /// <summary>
        ///     Gets or sets the AppId.
        /// </summary>
        string AppId { get; }

        /// <summary>
        ///     Gets or sets the Authority.
        /// </summary>
        string Authority { get; }

        /// <summary>
        ///     Gets or sets the Resource.
        /// </summary>
        string Resource { get; }

        /// <summary>
        ///     Gets or sets the TokenEndpoint.
        /// </summary>
        string TokenEndpoint { get; }

        /// <summary>
        ///     Gets or sets the TargetTenantId.
        /// </summary>
        string TargetTenantId { get; }
    }
}
