// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Contracts.Adapter.MsaTokenProvider
{
    using System;
    using System.Globalization;
    using Microsoft.Membership.MemberServices.Contracts.Exposed;

    /// <summary>
    /// Response type for the GetToken method containing either the token or error information.
    /// </summary>
    public class GetTokenResponse
    {
        public string Token { get; set; }

        public DateTime Expiry { get; set; }

        public ErrorInfo Error { get; set; }

        /// <summary>
        /// Returns whether this request was successful or not.
        /// </summary>
        public bool IsSuccess
        {
            get
            {
                return this.Error == null;
            }
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture,
                                 "(Token={0}, Expiry={1}, Error={2})",
                                 this.Token, this.Expiry, this.Error);
        }
    }
}
