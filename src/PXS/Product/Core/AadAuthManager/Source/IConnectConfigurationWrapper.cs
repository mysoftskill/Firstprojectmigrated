// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication
{
    using System.Collections.Generic;

    using Microsoft.IdentityModel.Tokens;

    /// <summary>
    ///     IConnectConfigurationWrapper
    /// </summary>
    public interface IConnectConfigurationWrapper
    {
        /// <summary>
        ///     Gets the issuer.
        /// </summary>
        string Issuer { get; }

        /// <summary>
        ///     Get the signing keys.
        /// </summary>
        ICollection<SecurityKey> SigningKeys { get; }
    }
}
