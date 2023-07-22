// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.VerificationTokenValidation
{
    using System.Threading.Tasks;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    /// <summary>
    /// Defines the contract for the service to validate the verificationtoken
    /// </summary>
    public interface IVerificationTokenValidationService
    {
        /// <summary>
        /// Validate the VerificationToken and the claims in the PrivacyRequest
        /// </summary>
        /// <param name="privacyRequest">PrivacyRequest that contains the claims to be validated</param>
        /// <param name="verificationToken">VerificationToken issued by the authenticating authority</param>
        /// <returns>true if the verification succeeds else false</returns>
        Task<AdapterResponse> ValidateVerifierAsync(PrivacyRequest privacyRequest, string verificationToken);
    }
}
