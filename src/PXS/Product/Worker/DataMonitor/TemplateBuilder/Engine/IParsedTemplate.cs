// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.TemplateBuilder.Engine
{
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;

    /// <summary>
    ///     contract for a parsed template
    /// </summary>
    public interface IParsedTemplate
    {
        /// <summary>
        ///     Gets the template tag
        /// </summary>
        string Tag { get; }

        /// <summary>
        ///     Gets the set of variables referenced by the template
        /// </summary>
        /// <returns>resulting value</returns>
        ICollection<string> GetVariables();

        /// <summary>
        ///     Renders a text blob by applying the supplied data to the template using the constant or data reference
        ///     parameters
        /// </summary>
        /// <param name="context">render context</param>
        /// <param name="parameters">template parameters</param>
        /// <param name="model">data</param>
        /// <returns>rendered text</returns>
        string Render(
            IContext context,
            ICollection<KeyValuePair<string, ModelValue>> parameters,
            object model);
    }
}
