// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.Runner.Storage
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.PrivacyServices.Common.TemplateBuilder;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Data;
    using Microsoft.PrivacyServices.DataMonitor.Runner.Data;

    /// <summary>
    ///     retrieves a list of Actions from Azure storage
    /// </summary>
    public class TableAccessor : IActionLibraryAccessor
    {
        private readonly ITable<TemplateDefState> templateDefTable;
        private readonly ITable<ActionDefState> actionDefTable;
        private readonly ITable<ActionRefState> actionRefTable;

        /// <summary>
        ///     Initializes a new instance of the TableRetriever class
        /// </summary>
        /// <param name="templateDefTable">template definition table</param>
        /// <param name="actionDefTable">action definition table</param>
        /// <param name="actionRefTable">action reference table</param>
        public TableAccessor(
            ITable<TemplateDefState> templateDefTable,
            ITable<ActionDefState> actionDefTable,
            ITable<ActionRefState> actionRefTable)
        {
            this.templateDefTable = templateDefTable ?? throw new ArgumentNullException(nameof(templateDefTable));
            this.actionDefTable = actionDefTable ?? throw new ArgumentNullException(nameof(actionDefTable));
            this.actionRefTable = actionRefTable ?? throw new ArgumentNullException(nameof(actionRefTable));
        }

        /// <summary>
        ///     Writes the action reference changes to the store
        /// </summary>
        /// <returns>resulting value</returns>
        public async Task<ICollection<ActionRefRunnable>> RetrieveActionReferencesAsync()
        {
            return (await this.actionRefTable.QueryAsync(null)).Select(o => o.ActionRef).ToList();
        }

        /// <summary>
        ///     Retrieves the collection of references to actions to execute
        /// </summary>
        /// <returns>resulting value</returns>
        public Task WriteActionReferenceChangesAsync(
            ICollection<string> remove,
            ICollection<ActionRefRunnable> update,
            ICollection<ActionRefRunnable> add)
        {
            // TODO: Implement when update support added!

            return Task.CompletedTask;
        }

        /// <summary>
        ///     Retrieves the collection of Actions to populate the store with
        /// </summary>
        /// <returns>resulting value</returns>
        public async Task<ICollection<ActionDef>> RetrieveActionsAsync()
        {
            return (await this.actionDefTable.QueryAsync(null)).Select(o => o.Action).ToList();
        }

        /// <summary>
        ///     Writes the action changes to the store
        /// </summary>
        /// <param name="remove">remove actions</param>
        /// <param name="update">update actions</param>
        /// <param name="add">add actions</param>
        /// <returns>resulting value</returns>
        public Task WriteActionChangesAsync(
            ICollection<string> remove, 
            ICollection<ActionDef> update, 
            ICollection<ActionDef> add)
        {
            // TODO: Implement when update support added!

            return Task.CompletedTask;
        }

        /// <summary>
        ///     Retrieves the collection of templates to populate the store with
        /// </summary>
        /// <returns>resulting value</returns>
        public async Task<ICollection<TemplateDef>> RetrieveTemplatesAsync()
        {
            return (await this.templateDefTable.QueryAsync(null)).Select(o => o.Template).ToList();
        }

        /// <summary>
        ///     Writes template changes to the store
        /// </summary>
        /// <param name="remove">templates to remove from the store</param>
        /// <param name="update">templates to update in the store</param>
        /// <param name="add">templates to add to the store</param>
        /// <returns>resulting value</returns>
        public Task WriteTemplateChangesAsync(
            ICollection<string> remove,
            ICollection<TemplateDef> update,
            ICollection<TemplateDef> add)
        {
            // TODO: Implement when update support added!

            return Task.CompletedTask;
        }
    }
}
