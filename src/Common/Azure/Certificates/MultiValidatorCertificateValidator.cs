//--------------------------------------------------------------------------------
// <copyright file="MultiValidatorCertificateValidator.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.PrivacyServices.Common.Azure
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;

    public class MultiValidatorCertificateValidator : ICertificateValidator
    {
        private readonly List<ICertificateValidator> validators;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MultiValidatorCertificateValidator" /> class.
        /// </summary>
        /// <param name="validators">The validators to use for validation.</param>
        public MultiValidatorCertificateValidator(params ICertificateValidator[] validators)
        {
            this.validators = new List<ICertificateValidator>(validators);
        }

        /// <inheritdoc />
        public bool IsAuthorized(X509Certificate2 cert)
        {
            return this.validators.All(validator => validator.IsAuthorized(cert));
        }
    }
}
