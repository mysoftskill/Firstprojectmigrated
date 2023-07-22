// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.TemplateBuilder
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    ///     contract for objects that reterieve template information
    /// </summary>
    public interface ITemplateAccessor
    {
        /// <summary>
        ///     Retrieves the collection of templates to populate the store with
        /// </summary>
        /// <returns>resulting value</returns>
        Task<ICollection<TemplateDef>> RetrieveTemplatesAsync();

        /// <summary>
        ///     Writes template changes to the store
        /// </summary>
        /// <param name="remove">templates to remove from the store</param>
        /// <param name="update">templates to update in the store</param>
        /// <param name="add">templates to add to the store</param>
        /// <returns>resulting value</returns>
        Task WriteTemplateChangesAsync(
            ICollection<string> remove,
            ICollection<TemplateDef> update,
            ICollection<TemplateDef> add);
    }
}
