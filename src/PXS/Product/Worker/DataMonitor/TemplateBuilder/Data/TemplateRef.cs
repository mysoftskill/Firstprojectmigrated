// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.TemplateBuilder
{
    using System;
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;

    /// <summary>
    ///     a template reference
    /// </summary>
    public class TemplateRef
    {
        /// <summary>
        ///     Gets or sets the parameter collection
        /// </summary>
        public IDictionary<string, ModelValue> Parameters { get; set; }

        /// <summary>
        ///     Gets or sets a tag to use to fetch the template text from the template store
        /// </summary>
        /// <remarks>
        ///     exactly one of TemplateTag or Inline must be specified. If both are specified, Inline will be used
        /// </remarks>
        public string TemplateTag { get; set; }

        /// <summary>
        ///     Gets or sets the the actual template text
        /// </summary>
        /// <remarks>
        ///     exactly one of TemplateTag or Inline must be specified. If both are specified, Inline will be used
        /// </remarks>
        public string Inline { get; set; }

        /// <summary>
        ///     Validates the specified template ref
        /// </summary>
        /// <param name="context">parse context to log errors into</param>
        /// <returns>true if the object validated successfully; false otherwise</returns>
        public bool ValidateAndNormalize(IContext context)
        {
            bool result = true;
            int hasInline = string.IsNullOrWhiteSpace(this.Inline) ? 0 : 1;
            int hasTag = string.IsNullOrWhiteSpace(this.TemplateTag) ? 0 : 1;

            // use xor to ensure at least one is specified and the other is not
            if ((hasInline ^ hasTag) != 1)
            {
                context.LogError("exactly one of TemplateTag or Inline must be specified");
                result = false;
            }

            result = ModelUtilites.ValidateModelValueMap(context, this.Parameters) && result;

            return result;
        }
    }
}
