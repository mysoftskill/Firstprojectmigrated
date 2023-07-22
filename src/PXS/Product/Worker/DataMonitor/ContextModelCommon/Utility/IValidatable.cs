// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.DataModel
{
    using Microsoft.PrivacyServices.Common.Context;

    /// <summary>
    ///     contract for action arg or definition class that want to be able to be validated
    /// </summary>
    public interface IValidatable
    {
        /// <summary>
        ///     Validates the argument object and logs any errors to the context
        /// </summary>
        /// <param name="context">execution context</param>
        /// <returns>true if the object validated successfully; false otherwise</returns>
        bool ValidateAndNormalize(IContext context);
    }
}
