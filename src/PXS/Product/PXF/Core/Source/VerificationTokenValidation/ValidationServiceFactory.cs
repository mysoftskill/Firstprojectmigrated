// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.VerificationTokenValidation
{
    using System;

    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.PrivacyServices.CommandFeed.Validator;
    using Microsoft.PrivacyServices.CommandFeed.Validator.Configuration;

    /// <summary>
    ///     ValidationServiceFactory
    /// </summary>
    public class ValidationServiceFactory : IValidationServiceFactory
    {
        /// <summary>
        ///     Creates a new ValidationServiceFactory
        /// </summary>
        /// <param name="configurationManager">configuration manager</param>
        public ValidationServiceFactory(IPrivacyConfigurationManager configurationManager)
        {
            if (configurationManager?.PrivacyExperienceServiceConfiguration == null)
            {
                throw new ArgumentNullException(nameof(configurationManager.PrivacyExperienceServiceConfiguration));
            }
        }

        /// <inheritdoc />
        public IValidationService Create(PcvEnvironment pcvEnvironment)
        {
            return new ValidationService(pcvEnvironment);
        }
    }
}
