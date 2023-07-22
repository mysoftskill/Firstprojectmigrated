// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.PrivacyServices.DataMonitor.Runner.Storage
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;
    using Microsoft.Practices.Unity;
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.TemplateBuilder;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Data;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Store;
    using Microsoft.PrivacyServices.DataMonitor.Runner.Data;
    using Microsoft.PrivacyServices.DataMonitor.Runner.Utility;

    /// <summary>
    ///     a store managers
    /// </summary>
    public class ActionManager :
        IActionAccessor,
        IActionExecutor
    {
        private IActionRefStore actionRefStore;

        private IActionStore actionStore;

        private volatile IUnityContainer localContainer;

        private long references;

        private ITemplateStore templateStore;

        /// <summary>
        ///     Gets a count of references
        /// </summary>
        public long References => this.references;

        /// <summary>
        ///     Initializes a new instance of the ActionAccessor class
        /// </summary>
        /// <param name="rootContainer">root container to create objects with</param>
        /// <param name="localUnityRegistrar">local unity registrar</param>
        /// <param name="accessor">action and template retriever</param>
        public ActionManager(
            IUnityContainer rootContainer,
            ILocalUnityRegistrar localUnityRegistrar,
            IActionLibraryAccessor accessor)
        {
            ArgumentCheck.ThrowIfNull(localUnityRegistrar, nameof(localUnityRegistrar));
            ArgumentCheck.ThrowIfNull(rootContainer, nameof(rootContainer));
            ArgumentCheck.ThrowIfNull(accessor, nameof(accessor));

            this.localContainer = localUnityRegistrar.SetupLocalContainer(rootContainer, accessor);
        }

        /// <summary>
        ///     Gets the actions to execute
        /// </summary>
        /// <param name="queue">queue to add work items to</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>resulting value</returns>
        public async Task EnqueueActionsToExecuteAsync(
            IQueue<JobWorkItem> queue,
            CancellationToken cancellationToken)
        {
            this.VerifyObjectIsUsable();

            // add an extra reference for the duration of our enqueuing- this allows the runner to execute the action and
            //  decrement the reference count without risking the object being cleaned up while we are still enqueueing things
            Interlocked.Increment(ref this.references);

            try
            {
                foreach (ActionRefRunnable aReference in this.actionRefStore.EnumerateReferences())
                {
                    IDictionary<string, IDictionary<string, string>> extProps =
                        new Dictionary<string, IDictionary<string, string>>();

                    if (aReference.Templates?.Count > 0)
                    {
                        extProps.Add(nameof(aReference.Templates), new ReadOnlyDictionary<string, string>(aReference.Templates));
                    }

                    await queue
                        .EnqueueAsync(
                            new JobWorkItem(this, aReference, new ReadOnlyDictionary<string, IDictionary<string, string>>(extProps)),
                            cancellationToken)
                        .ConfigureAwait(false);

                    Interlocked.Increment(ref this.references);
                }
            }
            finally
            {
                this.DecrementRef();
            }
        }

        /// <summary>
        ///     Retrieves the set of actions, templates, and action references from the store
        /// </summary>
        /// <returns>resulting value</returns>
        public ActionStoreContents EnumerateContents()
        {
            this.VerifyObjectIsUsable();

            return new ActionStoreContents
            {
                ActionReferences = this.actionRefStore.EnumerateReferences() ?? ListHelper.EmptyList<ActionRefRunnable>(),
                Templates = this.templateStore?.EnumerateTemplates() ?? ListHelper.EmptyList<TemplateDef>(),
                Actions = this.actionStore?.EnumerateActions() ?? ListHelper.EmptyList<ActionDef>()
            };
        }

        /// <summary>
        ///     Executes the specified action
        /// </summary>
        /// <param name="context">execution context</param>
        /// <param name="actionRef">reference to action to execute</param>
        /// <returns>data object produced by the execution</returns>
        public async Task<object> ExecuteActionAsync(
            IExecuteContext context,
            ActionRef actionRef)
        {
            this.VerifyObjectIsUsable();

            object result = await this.actionStore.ExecuteActionAsync(context, actionRef).ConfigureAwait(false);

            this.DecrementRef();

            return result;
        }

        /// <summary>
        ///     Retrieves the actions from storage
        /// </summary>
        /// <param name="context">parse context</param>
        /// <param name="preserveDefinitions">true to preserve definitions; false otherwise</param>
        /// <returns>resulting value</returns>
        public async Task<bool> InitializeAndRetrieveActionsAsync(
            IParseContext context,
            bool preserveDefinitions)
        {
            ITemplateStore localTemplateStore;
            IActionStore localActionStore;
            bool result;

            if (this.actionRefStore != null)
            {
                throw new InvalidOperationException("object has already been initialized");
            }

            if (this.localContainer == null)
            {
                throw new InvalidOperationException("last reference on object was released and object self-cleaned up");
            }

            var localActionRefStore = this.localContainer.Resolve<IActionRefStore>();
            localTemplateStore = this.localContainer.Resolve<ITemplateStore>();
            localActionStore = this.localContainer.Resolve<IActionStore>();

            // run the verification on later stores even if the earlier ones fail so that we collect as many failures as possible
            //  to enable callers to fix their provided data in the fewest number of retries

            result = await localTemplateStore.RefreshAsync(context, preserveDefinitions).ConfigureAwait(false);

            result = await localActionStore.RefreshAsync(context, preserveDefinitions).ConfigureAwait(false) && result;

            result = await localActionRefStore.RefreshAsync(context).ConfigureAwait(false) && result;

            if (result)
            {
                this.actionRefStore = localActionRefStore;
                this.templateStore = localTemplateStore;
                this.actionStore = localActionStore;
            }

            return result;
        }

        /// <summary>
        ///     Updates the store
        /// </summary>
        /// <param name="context">parse context</param>
        /// <param name="changes">changes to update store with</param>
        /// <returns>resulting value</returns>
        public async Task<bool> UpdateAsync(
            IParseContext context,
            ActionStoreUpdate changes)
        {
            bool result;

            this.VerifyObjectIsUsable();

            // run the verification on later stores even if the earlier ones fail so that we collect as many failures as possible
            //  to enable callers to fix their provided data in the fewest number of retries

            result = await this.templateStore
                .UpdateAsync(context, changes.TemplateDeletes, changes.TemplateUpdates)
                .ConfigureAwait(false);

            result = await this.actionStore
                         .UpdateAsync(context, changes.ActionDeletes, changes.ActionUpdates)
                         .ConfigureAwait(false) && result;

            result = await this.actionRefStore
                         .UpdateAsync(context, changes.ActionReferenceDeletes, changes.ActionReferenceUpdates)
                         .ConfigureAwait(false) && result;

            return result;
        }

        /// <summary>
        ///     Decrements the reference count
        /// </summary>
        private void DecrementRef()
        {
            if (Interlocked.Decrement(ref this.references) <= 0)
            {
                this.localContainer?.Dispose();
                this.localContainer = null;
            }
        }

        /// <summary>
        ///     Verifies that the object is usable and throws an InvalidOperationException if not
        /// </summary>
        private void VerifyObjectIsUsable()
        {
            if (this.actionStore == null)
            {
                throw new InvalidOperationException("object has not yet been initialized");
            }

            if (this.localContainer == null)
            {
                throw new InvalidOperationException("last reference on object was reserved and object self-cleaned up");
            }
        }
    }
}
