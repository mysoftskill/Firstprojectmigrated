// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Adapters.MsaIdentityService
{
    using System;

    /// <summary>
    /// Holds a cached token and its expiry time.
    /// This class is guaranteed to have a non-null token and an expiry time.
    /// </summary>
    public class CachedToken
    {
        public CachedToken(string token, DateTime expiry)
        {
            if (token == null)
            {
                throw new ArgumentNullException("token");
            }

            this.Token = token;
            this.Expiry = expiry;
        }

        public string Token { get; private set; }

        public DateTime Expiry { get; private set; }
    }
}