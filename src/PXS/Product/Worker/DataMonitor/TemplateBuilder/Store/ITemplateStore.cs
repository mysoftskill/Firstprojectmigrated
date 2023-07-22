// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.TemplateBuilder
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.Common.Context;

    /// <summary>
    ///     contract for storage of templates
    /// </summary>
    public interface ITemplateStore
    {
        /// <summary>
        ///     Enumerates the list of contained templates
        /// </summary>
        /// <returns>list of contained templates</returns>
        ICollection<TemplateDef> EnumerateTemplates();

        /// <summary>
        ///     Refreshes the store
        /// </summary>
        /// <param name="context">parse context</param>
        /// <param name="preserveDefinitions">true to preserve definitions; false otherwise</param>
        /// <returns>true if the store refreshed successfully, false otherwise</returns>
        /// <remarks>
        ///     if preserveDefinitions is false, the EnumerateTemplates method will return an empty list as EnumerateTemplates
        ///      cannot reconstruct the definitions from the parsed and processed templates and the original definitions are not
        ///      preserved if by default as they would otherwise not be required
        /// </remarks>
        Task<bool> RefreshAsync(
            IContext context,
            bool preserveDefinitions);

        /// <summary>
        ///     Updates the templates in the store by adding, updated, or removing them
        /// </summary>
        /// <param name="context">context</param>
        /// <param name="toRemove">set of templates to remove</param>
        /// <param name="toAddOrUpdate">set of templates to add or update</param>
        /// <returns>resulting value</returns>
        Task<bool> UpdateAsync(
            IContext context,
            ICollection<string> toRemove,
            ICollection<TemplateDef> toAddOrUpdate);

        /// <summary>
        ///     Validates the template reference exists
        /// </summary>
        /// <param name="context">parse context</param>
        /// <param name="actionRef">action reference</param>
        /// <returns>true if it validates successfully, false otherwise</returns>
        bool ValidateReference(
            IContext context,
            TemplateRef actionRef);

        /// <summary>
        ///     Renders a text blob by applying the supplied data to the template
        /// </summary>
        /// <param name="context">render context</param>
        /// <param name="templateRef">template reference</param>
        /// <param name="data">collection of data items</param>
        /// <returns>rendered text</returns>
        string Render(
            IContext context,
            TemplateRef templateRef,
            object data);
    }
}
