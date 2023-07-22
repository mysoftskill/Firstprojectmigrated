// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.Store
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.TemplateBuilder;
    using Microsoft.PrivacyServices.DataMonitor.Runner.Data;
    using Microsoft.PrivacyServices.DataMonitor.Runner.Storage;

    /// <summary>
    ///     store for queries
    /// </summary>
    public class ActionRefStore : IActionRefStore
    {
        private readonly IActionLibraryAccessor accessor;
        private readonly ITemplateStore templateStore;
        private readonly SemaphoreSlim lockObj = new SemaphoreSlim(1, 1);
        private readonly IActionStore actionStore;

        private IDictionary<string, ActionRefRunnable> store;
        private ICollection<ActionRefRunnable> rawDefs;

        /// <summary>
        ///     Initializes a new instance of the QueryStore class
        /// </summary>
        /// <param name="accessor">query retriever</param>
        /// <param name="templateStore">template store</param>
        /// <param name="actionStore">action store</param>
        public ActionRefStore(
            IActionLibraryAccessor accessor,
            ITemplateStore templateStore,
            IActionStore actionStore)
        {
            this.templateStore = templateStore ?? throw new ArgumentNullException(nameof(templateStore));
            this.actionStore = actionStore ?? throw new ArgumentNullException(nameof(actionStore));
            this.accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
        }

        /// <summary>
        ///     Gets the number of queries in the store
        /// </summary>
        public int Count => this.store?.Count ?? 0;

        /// <summary>
        ///     Enumerates the list of contained actions
        /// </summary>
        /// <returns>list of contained actions</returns>
        public ICollection<ActionRefRunnable> EnumerateReferences() => this.rawDefs ?? ListHelper.EmptyList<ActionRefRunnable>();

        /// <summary>
        ///     Populates the store
        /// </summary>
        /// <param name="context">parse context</param>
        /// <returns>true if the store was refreshed successfully; false otherwise</returns>
        public async Task<bool> RefreshAsync(IParseContext context)
        {
            IDictionary<string, ActionRefRunnable> newStore;
            ICollection<ActionRefRunnable> fetchedItems;
            bool result;
            int count;

            newStore = new Dictionary<string, ActionRefRunnable>(StringComparer.OrdinalIgnoreCase);

            context.OnActionStart(ActionType.Parse, "ActionLoad");

            fetchedItems = await this.accessor.RetrieveActionReferencesAsync().ConfigureAwait(false);

            (result, count) = this.UpdateStore(context, newStore, fetchedItems, false);

            // don't modify the store if the parse fails
            if (result)
            {
                this.store = newStore;
                this.rawDefs = new ReadOnlyCollection<ActionRefRunnable>(newStore.Values.ToList());
            }

            context.Log("Read and processed " + count.ToStringInvariant() + " action references from store");

            context.OnActionEnd();

            return result;
        }

        /// <summary>
        ///     Updates the actions in the store by adding, updated, or removing them
        /// </summary>
        /// <param name="context">context</param>
        /// <param name="toRemove">set of actions to remove</param>
        /// <param name="toAddOrUpdate">set of actions to add or update</param>
        /// <returns>resulting value</returns>
        public async Task<bool> UpdateAsync(
            IParseContext context,
            ICollection<string> toRemove,
            ICollection<ActionRefRunnable> toAddOrUpdate)
        {
            IDictionary<string, ActionRefRunnable> oldStore;
            ISet<string> toRemoveActual = new HashSet<string>();
            int countRemoved = 0;
            int countAdded = 0;
            bool result = true;

            if (this.store == null)
            {
                throw new InvalidOperationException("store has not been initialized");
            }

            context.OnActionStart(ActionType.Parse, "ActionUpdate");

            // need an awaitable lock becuase normal locks don't support doing an await while they are held (because they are
            //  thread aware and awaits can resume on different threads).  Ensure the lock body is in a try / finally so we
            //  release them even if exceptions occur.
            await this.lockObj.WaitAsync(-1).ConfigureAwait(false);

            try
            {
                IDictionary<string, ActionRefRunnable> newStore;

                oldStore = this.store;
                newStore = new Dictionary<string, ActionRefRunnable>(oldStore, StringComparer.OrdinalIgnoreCase);

                if (toRemove?.Count > 0)
                {
                    foreach (string id in toRemove)
                    {
                        if (newStore.Remove(id))
                        {
                            toRemoveActual.Add(id);
                            ++countRemoved;
                        }
                    }
                }

                if (toAddOrUpdate?.Count > 0)
                {
                    (result, countAdded) = this.UpdateStore(context, newStore, toAddOrUpdate, true);
                }

                context.Log(
                    $"Updated store: added or updated {countAdded} actions and removed {countRemoved} actions. Committing changes.");

                if (result)
                {
                    await this.accessor
                        .WriteActionReferenceChangesAsync(
                            toRemoveActual,
                            toAddOrUpdate?.Where(o => oldStore.ContainsKey(o.Id)).ToList(),
                            toAddOrUpdate?.Where(o => oldStore.ContainsKey(o.Id) == false).ToList())
                        .ConfigureAwait(false);

                    this.store = newStore;
                }
            }
            finally
            {
                this.lockObj.Release();
            }

            context.LogVerbose("Action store changes committed");

            context.OnActionEnd();

            return result;
        }

        /// <summary>
        ///     Gets the action associated with the provided tag
        /// </summary>
        /// <param name="id">action reference id</param>
        /// <returns>requested query or null if the action does not exist</returns>
        public ActionRefRunnable GetReference(string id)
        {
            ActionRefRunnable actionRef;
            return this.store != null && this.store.TryGetValue(id, out actionRef) ? actionRef : null;
        }

        /// <summary>
        ///     Updates a provided store
        /// </summary>
        /// <param name="context">parse context</param>
        /// <param name="store">store to add to</param>
        /// <param name="newItems">items to add or udpate</param>
        /// <param name="allowOverwrite">true to allow overwrite; false otherwise</param>
        /// <returns>resulting value</returns>
        private (bool Success, int CountAdded) UpdateStore(
            IParseContext context,
            IDictionary<string, ActionRefRunnable> store,
            ICollection<ActionRefRunnable> newItems,
            bool allowOverwrite)
        {
            ICollection<string> duplicates = null;
            bool result = true;
            int count = 0;

            // retrieve the actions and do a basic parse
            foreach (ActionRefRunnable aref in newItems)
            {
                string itemTag;

                if (aref == null)
                {
                    context.LogError("Store contains a null action reference");
                    result = false;
                    continue;
                }

                itemTag = $"[{aref.Id}].[{aref.Tag}]";

                context.OnActionStart(ActionType.Parse, itemTag);
                context.LogVerbose("Processing action reference " + itemTag);

                try
                {
                    if (allowOverwrite)
                    {
                        if (store.ContainsKey(aref.Id))
                        {
                            context.LogVerbose("Replaced existing action [" + aref.Tag + "]");
                        }

                        store[aref.Id] = aref;
                    }
                    else
                    {
                        store.Add(aref.Id, aref);
                    }

                    ++count;
                }
                catch (ArgumentException)
                {
                    duplicates = duplicates ?? new List<string>();
                    duplicates.Add(aref.Tag);
                }

                context.OnActionEnd();
            }

            if (duplicates != null)
            {
                context.LogError(
                    "Action reference store contains duplicates of the following references: " + string.Join(", ", duplicates));
                result = false;
            }

            if (result)
            {
                foreach (ActionRefRunnable aref in newItems)
                {
                    result = this.actionStore.ValidateReference(context, aref) && result;

                    if (aref.Templates?.Count > 0)
                    {
                        foreach (KeyValuePair<string, string> kvp in aref.Templates)
                        {
                            result = this.templateStore.ValidateReference(
                                context,
                                new TemplateRef { TemplateTag = kvp.Value }) && result;
                        }
                    }
                }
            }

            return (result, count);
        }
    }
}
