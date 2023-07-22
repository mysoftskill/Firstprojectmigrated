// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.Store
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataMonitor.DataAction.Data;

    /// <summary>
    ///     contract for objects that accesses action information
    /// </summary>
    public interface IActionAccessor
    {
        /// <summary>
        ///     Retrieves the collection of actions to populate the store with
        /// </summary>
        /// <returns>resulting value</returns>
        Task<ICollection<ActionDef>> RetrieveActionsAsync();

        /// <summary>
        ///     Writes the action changes to the store
        /// </summary>
        /// <param name="remove">actions to remove from the store</param>
        /// <param name="update">actions to update in the store</param>
        /// <param name="add">actions to add to the store</param>
        /// <returns>resulting value</returns>
        Task WriteActionChangesAsync(
            ICollection<string> remove,
            ICollection<ActionDef> update,
            ICollection<ActionDef> add);
    }
}
