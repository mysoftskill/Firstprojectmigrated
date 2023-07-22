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
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.PrivacyServices.Common.Exceptions;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Actions;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Data;

    /// <summary>
    ///     store for queries
    /// </summary>
    public class ActionStore : IActionStore
    {
        private const string DefaultContextHostName = "GenericActionHost";

        private readonly IModelManipulator modelManipulator;
        private readonly IActionAccessor accessor;
        private readonly IContextFactory contextFactory;
        private readonly IActionFactory actionFactory;
        private readonly SemaphoreSlim lockObj = new SemaphoreSlim(1, 1);

        private IDictionary<string, IAction> store;
        private ICollection<ActionDef> rawDefs;

        /// <summary>
        ///     Initializes a new instance of the QueryStore class
        /// </summary>
        /// <param name="accessor">query retriever</param>
        /// <param name="actionFactory">action factory</param>
        /// <param name="contextFactory">context factory</param>
        /// <param name="modelManipulator">model manipulator</param>
        public ActionStore(
            IActionAccessor accessor,
            IActionFactory actionFactory,
            IContextFactory contextFactory,
            IModelManipulator modelManipulator)
        {
            this.modelManipulator = modelManipulator ?? throw new ArgumentNullException(nameof(modelManipulator));
            this.contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            this.actionFactory = actionFactory ?? throw new ArgumentNullException(nameof(actionFactory));
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
        public ICollection<ActionDef> EnumerateActions() => this.rawDefs ?? ListHelper.EmptyList<ActionDef>();

        /// <summary>
        ///     Populates the store
        /// </summary>
        /// <param name="context">parse context</param>
        /// <param name="preserveDefinitions">true to preserve definitions; false otherwise</param>
        /// <returns>true if the store was refreshed successfully; false otherwise</returns>
        /// <remarks>
        ///     if preserveDefinitions is false, the EnumActions method will return no results as this requires the raw
        ///      definition data be preserved in order to generate the set of actions
        /// </remarks>
        public async Task<bool> RefreshAsync(
            IParseContext context,
            bool preserveDefinitions)
        {
            IDictionary<string, IAction> newStore = new Dictionary<string, IAction>(StringComparer.OrdinalIgnoreCase);
            ICollection<ActionDef> fetchedItems;
            bool result;
            int count;

            context.OnActionStart(ActionType.Parse, "ActionLoad");

            fetchedItems = await this.accessor.RetrieveActionsAsync().ConfigureAwait(false);

            (result, count) = this.UpdateStore(context, newStore, fetchedItems, false);

            // don't modify the store if the parse fails
            if (result)
            {
                this.store = newStore;

                this.rawDefs = preserveDefinitions && fetchedItems != null ?
                    new ReadOnlyCollection<ActionDef>(fetchedItems.ToList()) :
                    ListHelper.EmptyList<ActionDef>();
            }

            context.Log("Read and processed " + count.ToStringInvariant() + " actions from store");

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
            ICollection<ActionDef> toAddOrUpdate)
        {
            IDictionary<string, IAction> oldStore;
            ISet<string> toRemoveActual = new HashSet<string>();
            int countRemoved = 0;
            int countAdded = 0;
            bool result = true;

            if (this.store == null)
            {
                throw new InvalidOperationException("store has not been initialized");
            }

            context.OnActionStart(ActionType.Parse, "ActionUpdate");

            // need an awaitable lock because normal locks don't support doing an await while they are held (because they are
            //  thread aware and awaits can resume on different threads).  Ensure the lock body is in a try / finally so we
            //  release them even if exceptions occur.
            await this.lockObj.WaitAsync(-1).ConfigureAwait(false);

            try
            {
                IDictionary<string, IAction> newStore;

                oldStore = this.store;
                newStore = new Dictionary<string, IAction>(oldStore, StringComparer.OrdinalIgnoreCase);

                if (toRemove?.Count > 0)
                {
                    foreach (string tag in toRemove)
                    {
                        if (newStore.Remove(tag))
                        {
                            toRemoveActual.Add(tag);
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
                        .WriteActionChangesAsync(
                            toRemoveActual,
                            toAddOrUpdate?.Where(o => oldStore.ContainsKey(o.Tag)).ToList(),
                            toAddOrUpdate?.Where(o => oldStore.ContainsKey(o.Tag) == false).ToList())
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
        /// <param name="tag">action tag</param>
        /// <returns>requested query or null if the action does not exist</returns>
        public IAction GetAction(string tag)
        {
            IAction action;
            return this.store != null && this.store.TryGetValue(tag, out action) ? action : null;
        }

        /// <summary>
        ///     Retrieves an action from the store and executes it
        /// </summary>
        /// <param name="context">execution context</param>
        /// <param name="actionRef">action to invoke</param>
        /// <returns>result object or null if none found</returns>
        public async Task<object> ExecuteActionAsync(
            IExecuteContext context,
            ActionRef actionRef)
        {
            IAction action;
            object model;

            ArgumentCheck.ThrowIfNull(actionRef, nameof(actionRef));
            ArgumentCheck.ThrowIfNullEmptyOrWhiteSpace(actionRef.Tag, nameof(actionRef));

            if (this.store == null)
            {
                throw new InvalidOperationException("store has not been initialized");
            }

            if (this.store.TryGetValue(actionRef.Tag, out action) == false)
            {
                throw new ActionExecuteException("[" + actionRef.Tag + "] is not a tag for a known action in the store");
            }

            context = context ?? this.contextFactory.Create<IExecuteContext>(ActionStore.DefaultContextHostName);

            model = this.modelManipulator.CreateEmpty();

            await action.ExecuteAsync(context, actionRef, model);

            return model;
        }

        /// <summary>
        ///     Retrieves an action from the store and executes it
        /// </summary>
        /// <param name="context">execution context</param>
        /// <param name="tag">tag of the action to fetch</param>
        /// <returns>result object or null if none found</returns>
        public async Task ExecuteActionAsync(
            IExecuteContext context,
            string tag)
        {
            ArgumentCheck.ThrowIfNullEmptyOrWhiteSpace(tag, nameof(tag));

            if (this.store == null)
            {
                throw new InvalidOperationException("store has not been initialized");
            }

            await this.ExecuteActionAsync(
                context, 
                new ActionRef { Tag = tag, Description = "store launched action" });
        }

        /// <summary>
        ///     Validates the action reference exists and has the correct parameters
        /// </summary>
        /// <param name="context">parse context</param>
        /// <param name="actionRef">action reference</param>
        /// <returns>true if it validates successfully, false otherwise</returns>
        public bool ValidateReference(
            IParseContext context, 
            ActionRef actionRef)
        {
            IAction action;

            ArgumentCheck.ThrowIfNull(actionRef, nameof(actionRef));
            ArgumentCheck.ThrowIfNullEmptyOrWhiteSpace(actionRef.Tag, nameof(actionRef) + ".Tag");
            ArgumentCheck.ThrowIfNullEmptyOrWhiteSpace(actionRef.Id, nameof(actionRef) + ".Id");
            ArgumentCheck.ThrowIfNull(context, nameof(context));

            if (this.store == null)
            {
                return false;
            }

            action = this.GetAction(actionRef.Tag);
            if (action == null)
            {
                context.LogError($"Action with tag [{actionRef.Tag}] in action ref [{actionRef.Id}] was not found");
                return false;
            }

            return action.Validate(context, actionRef.ArgTransform);
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
            IDictionary<string, IAction> store,
            ICollection<ActionDef> newItems,
            bool allowOverwrite)
        {
            ICollection<string> duplicates = null;
            RefreshFetcher fetcher;
            bool result = true;
            int count = 0;

            // retrieve the actions and do a basic parse
            foreach (ActionDef def in newItems)
            {
                IAction item;
                string itemTag;

                if (def == null)
                {
                    context.LogError("Store contains a null action definition");
                    result = false;
                    continue;
                }

                itemTag = $"[{def.Type ?? "<UNKNOWN>"}].[{def.Tag ?? "<UNKNOWN>"}]";

                context.OnActionStart(ActionType.Parse, itemTag);
                context.LogVerbose("Processing definition for " + itemTag);

                item = this.actionFactory.Create(def.Type);
                if (item != null)
                {
                    if (item.ParseAndProcessDefinition(context, this.actionFactory, def.Tag, def.Def))
                    {
                        try
                        {
                            if (allowOverwrite)
                            {
                                if (store.ContainsKey(item.Tag))
                                {
                                    context.LogVerbose("Replaced existing action [" + def.Tag + "]");
                                }

                                store[item.Tag] = item;
                            }
                            else
                            {
                                store.Add(item.Tag, item);
                            }

                            ++count;
                        }
                        catch (ArgumentException)
                        {
                            duplicates = duplicates ?? new List<string>();
                            duplicates.Add(def.Tag);
                        }
                    }
                    else
                    {
                        result = false;
                    }
                }
                else
                {
                    context.LogError("[" + def.Type + "] is not a supported action type");
                    result = false;
                }

                context.OnActionEnd();
            }

            if (duplicates != null)
            {
                context.LogError("Action store contains duplicates of the following tags: " + string.Join(", ", duplicates));
                result = false;
            }

            fetcher = new RefreshFetcher(store);

            // tell the actions to expand non-inlined references into action objects (i.e. pull them from the store)
            foreach (IAction action in store.Values)
            {
                result = action.ExpandDefinition(context, fetcher) && result;
            }

            return (result, count);
        }

        /// <summary>
        ///     used to provide fetch support during store refreshes so that we do not have to change the
        ///      existing store until the refresh succeeds completely
        /// </summary>
        private class RefreshFetcher : IActionFetcher
        {
            private readonly IDictionary<string, IAction> actions;

            /// <summary>
            ///     Initializes a new instance of the RefreshFetcher class
            /// </summary>
            /// <param name="actionMap">action store</param>
            public RefreshFetcher(IDictionary<string, IAction> actionMap) => this.actions = actionMap;

            /// <summary>
            ///     Retrieves an action from the store
            /// </summary>
            /// <param name="tag">tag of the action to fetch</param>
            /// <returns>requested action</returns>
            public IAction GetAction(string tag)
            {
                IAction action;
                return this.actions.TryGetValue(tag, out action) ? action : null;
            }
        }
    }
}
