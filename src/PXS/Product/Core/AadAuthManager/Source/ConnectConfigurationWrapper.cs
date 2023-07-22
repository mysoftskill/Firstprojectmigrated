// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication
{
    using System;
    using System.Collections.Generic;

    using Microsoft.IdentityModel.Protocols.OpenIdConnect;
    using Microsoft.IdentityModel.Tokens;

    /// <summary>
    ///     ConnectConfigurationWrapper
    /// </summary>
    public class ConnectConfigurationWrapper : IConnectConfigurationWrapper
    {
        /// <inheritdoc />
        public string Issuer { get; }

        /// <inheritdoc />
        public ICollection<SecurityKey> SigningKeys { get; } 

        public ConnectConfigurationWrapper(OpenIdConnectConfiguration config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            this.Issuer = config.Issuer;
            this.SigningKeys = config.SigningKeys;
        }
    }
}
