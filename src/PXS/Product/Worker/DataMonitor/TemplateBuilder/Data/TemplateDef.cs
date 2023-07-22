// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.TemplateBuilder
{
    /// <summary>
    ///     a raw template
    /// </summary>
    public class TemplateDef
    {
        /// <summary>
        ///     Gets or sets the template text (with placeholders)
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        ///     Gets or sets the template tag
        /// </summary>
        public string Tag { get; set; }
    }
}
