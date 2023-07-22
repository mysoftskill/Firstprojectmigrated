// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.TemplateBuilder
{
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.TemplateBuilder.Engine;

    /// <summary>
    ///     contact for classes that parses templates
    /// </summary>
    public interface ITemplateParser
    {
        /// <summary>
        ///     Parses the specified template and returns a parsed template object
        /// </summary>
        /// <param name="context">parse context</param>
        /// <param name="def">template to parse</param>
        /// <returns>parsed template object</returns>
        IParsedTemplate Parse(
            IContext context,
            TemplateDef def);
    }
}
