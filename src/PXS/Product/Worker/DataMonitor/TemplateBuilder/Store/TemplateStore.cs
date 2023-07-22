// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.TemplateBuilder
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.TemplateBuilder.Engine;

    /// <summary>
    ///     store for templates
    /// </summary>
    public class TemplateStore : ITemplateStore
    {
        private readonly ITemplateAccessor accessor;
        private readonly SemaphoreSlim lockObj = new SemaphoreSlim(1, 1);
        private readonly ITemplateParser parser;

        private volatile IDictionary<string, IParsedTemplate> store;
        private ICollection<TemplateDef> rawDefs;

        /// <summary>
        ///     Initializes a new instance of the TemplateStore class
        /// </summary>
        /// <param name="accessor">template retriever</param>
        /// <param name="parser">template parser</param>
        public TemplateStore(
            ITemplateAccessor accessor,
            ITemplateParser parser)
        {
            this.accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
            this.parser = parser ?? throw new ArgumentNullException(nameof(parser));
        }

        /// <summary>
        ///     Gets the number of templates in the store
        /// </summary>
        public int Count => this.store?.Count ?? 0;

        /// <summary>
        ///     Enumerates the list of contained templates
        /// </summary>
        /// <returns>list of contained templates</returns>
        public ICollection<TemplateDef> EnumerateTemplates() => this.rawDefs ?? ListHelper.EmptyList<TemplateDef>();

        /// <summary>
        ///     populates the store
        /// </summary>
        /// <param name="context">parse context</param>
        /// <param name="preserveDefinitions">true to preserve definitions; false otherwise</param>
        /// <returns>true if the store refreshed successfully, false otherwise</returns>
        /// <remarks>
        ///     if preserveDefinitions is false, the EnumerateTemplates method will return an empty list as EnumerateTemplates
        ///      cannot reconstruct the definitions from the parsed and processed templates and the original definitions are not
        ///      preserved if by default as they would otherwise not be required
        /// </remarks>
        public async Task<bool> RefreshAsync(
            IContext context,
            bool preserveDefinitions)
        {
            IDictionary<string, IParsedTemplate> newStore = new Dictionary<string, IParsedTemplate>();
            ICollection<TemplateDef> fetchedItems;
            bool result;
            int count;

            context.OnActionStart(ActionType.Parse, "<<TemplateStoreRefresh>>");

            fetchedItems = await this.accessor.RetrieveTemplatesAsync().ConfigureAwait(false);

            (result, count) = this.UpdateStore(context, newStore, fetchedItems, false);

            if (result)
            {
                this.store = newStore;

                this.rawDefs = preserveDefinitions && fetchedItems != null ?
                    new ReadOnlyCollection<TemplateDef>(fetchedItems.ToList()) :
                    ListHelper.EmptyList<TemplateDef>();
            }

            context.Log("Read and parsed " + count.ToStringInvariant() + " templates from store");

            context.OnActionEnd();

            return result;
        }

        /// <summary>
        ///     Updates the templates in the store by adding, updated, or removing them
        /// </summary>
        /// <param name="context">context</param>
        /// <param name="toRemove">set of templates to remove</param>
        /// <param name="toAddOrUpdate">set of templates to add or update</param>
        /// <returns>resulting value</returns>
        public async Task<bool> UpdateAsync(
            IContext context,
            ICollection<string> toRemove,
            ICollection<TemplateDef> toAddOrUpdate)
        {
            IDictionary<string, IParsedTemplate> oldStore;
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
                IDictionary<string, IParsedTemplate> newStore;

                oldStore = this.store;
                newStore = new Dictionary<string, IParsedTemplate>(oldStore, StringComparer.OrdinalIgnoreCase);

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
                    $"Updated store: added or updated {countAdded} templates and removed {countRemoved} templates. Committing changes.");

                if (result)
                {
                    await this.accessor
                        .WriteTemplateChangesAsync(
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

            context.LogVerbose("Template store changes committed");

            context.OnActionEnd();

            return result;
        }

        /// <summary>
        ///     Gets the template associated with the provided tag
        /// </summary>
        /// <param name="tag">template tag</param>
        /// <returns>requested template or null if the template does not exist</returns>
        public IParsedTemplate GetTemplate(string tag)
        {
            IDictionary<string, IParsedTemplate> storeLocal = this.store;
            IParsedTemplate template;
            return storeLocal != null && storeLocal.TryGetValue(tag, out template) ? template : null;
        }

        /// <summary>
        ///     Validates the template reference exists
        /// </summary>
        /// <param name="context">parse context</param>
        /// <param name="templateRef">action reference</param>
        /// <returns>true if it validates successfully, false otherwise</returns>
        public bool ValidateReference(
            IContext context,
            TemplateRef templateRef)
        {
            ArgumentCheck.ThrowIfNull(templateRef, nameof(templateRef));
            ArgumentCheck.ThrowIfNull(context, nameof(context));

            if (this.store == null)
            {
                throw new InvalidOperationException("store has not been initialized");
            }

            if (string.IsNullOrWhiteSpace(templateRef.TemplateTag) == false)
            {
                IParsedTemplate template = this.GetTemplate(templateRef.TemplateTag);
                if (template == null)
                {
                    context.LogError("Template with tag [" + templateRef.TemplateTag + "] was not found");
                    return false;
                }
            }
            else if (string.IsNullOrWhiteSpace(templateRef.Inline))
            {
                context.LogError("Template has both missing tag and inline definition");
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Renders a text blob by applying the supplied data to the template
        /// </summary>
        /// <param name="context">render context</param>
        /// <param name="templateRef">template reference</param>
        /// <param name="data">collection of data items</param>
        /// <returns>rendered text</returns>
        public string Render(
            IContext context,
            TemplateRef templateRef, 
            object data)
        {
            IDictionary<string, IParsedTemplate> storeLocal = this.store;
            IParsedTemplate template;
            string result;

            ArgumentCheck.ThrowIfNull(templateRef, nameof(templateRef));
            
            if (string.IsNullOrWhiteSpace(templateRef.Inline) == false)
            {
                TemplateDef def = new TemplateDef
                {
                    Tag = Guid.NewGuid().ToString("N"),
                    Text = templateRef.Inline
                };

                context.OnActionStart(ActionType.Parse, "TemplateRender[Inline]");

                template = this.parser.Parse(context, def);

                context.OnActionEnd();
                context.OnActionStart(ActionType.Execute, "TemplateRender[Inline]");

                result = template.Render(context, templateRef.Parameters, data);
            }
            else
            {
                ArgumentCheck.ThrowIfNullEmptyOrWhiteSpace(templateRef.TemplateTag, nameof(templateRef) + ".TemplateTag");

                context.OnActionStart(ActionType.Execute, "TemplateRender[" + templateRef.TemplateTag  + "]");

                result = storeLocal != null && storeLocal.TryGetValue(templateRef.TemplateTag, out template) ?
                    template.Render(context, templateRef.Parameters, data) :
                    null;
            }
            
            context.OnActionEnd();

            return result;
        }

        /// <summary>
        ///     Updates a provided store
        /// </summary>
        /// <param name="context">parse context</param>
        /// <param name="store">store to add to</param>
        /// <param name="newItems">items to add or update</param>
        /// <param name="allowOverwrite">true to allow overwrite; false otherwise</param>
        /// <returns>resulting value</returns>
        private (bool Success, int CountAdded) UpdateStore(
            IContext context,
            IDictionary<string, IParsedTemplate> store,
            ICollection<TemplateDef> newItems,
            bool allowOverwrite)
        {
            ICollection<string> duplicates = null;
            bool result = true;
            int count = 0;

            foreach (TemplateDef def in newItems)
            {
                IParsedTemplate item;

                context.OnActionStart(ActionType.Parse, def.Tag);

                context.LogVerbose("Parsing template [" + (def.Tag ?? "<UNKNOWN>") + "]");

                item = this.parser.Parse(context, def);

                if (item == null)
                {
                    result = false;
                    continue;
                }

                try
                {
                    if (allowOverwrite)
                    {
                        if (store.ContainsKey(item.Tag))
                        {
                            context.LogVerbose("Replaced existing template [" + def.Tag + "]");
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

                context.OnActionEnd();
            }

            if (duplicates != null)
            {
                context.LogError(
                    "Template store contains duplicate of the following tags: " + string.Join(", ", duplicates));
                result = false;
            }

            return (result, count);
        }
    }
}
