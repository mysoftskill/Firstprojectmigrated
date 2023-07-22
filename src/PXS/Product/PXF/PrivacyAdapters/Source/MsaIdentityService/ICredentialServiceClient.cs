// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    ///     ICredentialServiceClient
    /// </summary>
    public interface ICredentialServiceClient
    {
        /// <summary>
        ///     Target URI of the client
        /// </summary>
        Uri TargetUri { get; }

        /// <summary>
        ///     Get the GDPR Verifier from IDSAPI
        /// </summary>
        /// <param name="targetIdentifier">target identifier for the verifier token</param>
        /// <param name="operation">the operation to create a verifier token for</param>
        /// <param name="additionalClaims">Any additional claims to include in the verifier</param>
        /// <param name="unauthSessionID">Unauth session Id to help debugging</param>
        /// <param name="optionalParams">Any optional params to include in the request (ie. pre-verifier)</param>
        /// <param name="proxyTicket">user proxy ticket</param>
        /// <param name="requestPreverifier">Whether preverifier is needed for the MSA RVS request</param>
        /// <returns></returns>
        Task<string> GetGdprVerifierAsync(
            tagPASSID targetIdentifier,
            eGDPR_VERIFIER_OPERATION operation,
            IDictionary<string, string> additionalClaims,
            string unauthSessionID = "",
            string optionalParams = null,
            string proxyTicket = null,
            bool requestPreverifier = false);

        /// <summary>
        ///     Gets the signin names and CIDs for net id asynchronous.
        /// </summary>
        /// <param name="puid">The user net identifier as a X16 encoded long.</param>
        /// <param name="unauthSessionID">Unauth session Id to help debugging</param>
        /// <returns></returns>
        Task<string> GetSigninNamesAndCidsForNetIdAsync(string puid, string unauthSessionID = "");
    }
}
