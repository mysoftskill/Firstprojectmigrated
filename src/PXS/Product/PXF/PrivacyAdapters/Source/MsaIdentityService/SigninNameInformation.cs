// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService
{
    /// <inheritdoc />
    public class SigninNameInformation : ISigninNameInformation
    {
        /// <inheritdoc />
        public long? Cid { get; set; }

        /// <inheritdoc />
        public int? CredFlags { get; set; }

        /// <inheritdoc />
        public long? Puid { get; set; }

        /// <inheritdoc />
        public string SigninName { get; set; }
    }
}
