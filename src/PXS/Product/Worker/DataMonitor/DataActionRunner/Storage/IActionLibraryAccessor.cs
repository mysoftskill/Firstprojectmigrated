// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.Runner.Storage
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.Common.TemplateBuilder;
    using Microsoft.PrivacyServices.DataMonitor.Runner.Data;

    /// <summary>
    ///     contract to retrieve various template and action objects from storage
    /// </summary>
    public interface IActionLibraryAccessor : 
        ITemplateAccessor,
        DataAction.Store.IActionAccessor
    {
        /// <summary>
        ///     Retrieves the collection of references to actions to execute
        /// </summary>
        /// <returns>resulting value</returns>
        Task<ICollection<ActionRefRunnable>> RetrieveActionReferencesAsync();

        /// <summary>
        ///     Retrieves the collection of references to actions to execute
        /// </summary>
        /// <returns>resulting value</returns>
        Task WriteActionReferenceChangesAsync(
            ICollection<string> remove,
            ICollection<ActionRefRunnable> update,
            ICollection<ActionRefRunnable> add);
    }
}
