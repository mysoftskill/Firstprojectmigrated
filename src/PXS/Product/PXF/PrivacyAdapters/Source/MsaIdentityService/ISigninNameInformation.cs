// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService
{
    /// <summary>
    ///     Represents the sign-in name information returned by a call to <see cref="IMsaIdentityServiceAdapter.GetSigninNameInformationAsync" />
    /// </summary>
    /// <remarks>
    ///     https://microsoft.sharepoint.com/teams/liveid/docs/idsapi/Credential_GetSigninNamesAndCIDsForNetIDsMethod.html
    /// </remarks>
    public interface ISigninNameInformation
    {
        /// <summary>
        ///     Gets the Cid for the sign-in name, <c>null</c> if the requested user could not be found.
        /// </summary>
        long? Cid { get; }

        /// <summary>
        ///     Gets the applicable credential flags for the sign-in name, <c>null</c> if the requested user could not be found.
        /// </summary>
        /// <remarks>
        ///     https://microsoft.sharepoint.com/teams/liveid/docs/idsapi/Credential_CredentialXmlBlobProperties.html
        /// </remarks>
        int? CredFlags { get; }

        /// <summary>
        ///     Gets the Puid for the sign-in name, <c>null</c> if the requested user could not be found.
        /// </summary>
        long? Puid { get; }

        /// <summary>
        ///     Gets the alias for the sign-in name, <c>null</c> if the requested user could not be found.
        /// </summary>
        string SigninName { get; }
    }
}
