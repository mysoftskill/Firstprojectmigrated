// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.PrivacyOperation.Client.Models.Requests
{
    using System;

    /// <summary>
    ///     RequestExtensions.
    /// </summary>
    public static class RequestExtensions
    {
        /// <summary>
        ///     Validate a request object.
        /// </summary>
        /// <param name="args"></param>
        public static void Validate(this BasePrivacyOperationArgs args)
        {
            if (args.UserAssertion == null)
                throw new ArgumentNullException(nameof(args.UserAssertion));
            if (string.IsNullOrEmpty(args.CorrelationVector))
                throw new ArgumentNullException(nameof(args.CorrelationVector));
        }
    }
}
