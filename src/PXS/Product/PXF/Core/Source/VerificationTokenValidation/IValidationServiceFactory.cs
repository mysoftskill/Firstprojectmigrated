// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.VerificationTokenValidation
{
    using Microsoft.PrivacyServices.CommandFeed.Validator;
    using Microsoft.PrivacyServices.CommandFeed.Validator.Configuration;

    /// <summary>
    ///     Interface for validation service factory
    /// </summary>
    public interface IValidationServiceFactory
    {
        /// <summary>
        ///     Creates the Validation Service from <see cref="PcvEnvironment" />
        /// </summary>
        /// <param name="pcvEnvironment"></param>
        /// <returns>A new instance of validation service</returns>
        IValidationService Create(PcvEnvironment pcvEnvironment);
    }
}
