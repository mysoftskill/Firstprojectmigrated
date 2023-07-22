// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.Runner.Storage
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.PrivacyServices.Common.TemplateBuilder;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Data;
    using Microsoft.PrivacyServices.DataMonitor.Runner.Data;
    using Microsoft.PrivacyServices.DataMonitor.Runner.Storage.FileSystem;

    using Newtonsoft.Json;

    /// <summary>
    ///     Retrieves a list of Actions from Azure storage
    /// </summary>
    public class FileSystemAccessor : IActionLibraryAccessor
    {
        private const string DefaultFilterTag = "PROD";

        private readonly string templateManifestPath;
        private readonly string actionRefSetPath;
        private readonly string actionSetPath;
        private readonly string filterTag;
        private readonly IActionRefExcludedAgentsOverrides overrides;

        private ICollection<ActionRefRunnable> refs;
        private ICollection<TemplateDef> templates;
        private ICollection<ActionDef> actions;

        /// <summary>
        ///     Initializes a new instance of the FileAccessor class
        /// </summary>
        /// <param name="config">configuration</param>
        /// <param name="appPath">application path</param>
        /// <param name="overrides">ActionRef excluded agents overrides</param>
        public FileSystemAccessor(
            IFileSystemActionLibraryConfig config,
            string appPath,
            IActionRefExcludedAgentsOverrides overrides)
        {
            ArgumentCheck.ThrowIfNull(config, nameof(config));
            ArgumentCheck.ThrowIfNullEmptyOrWhiteSpace(appPath, nameof(appPath));
            ArgumentCheck.ThrowIfNull(overrides, nameof(overrides));

            appPath = appPath.Trim();

            this.templateManifestPath = FileSystemAccessor.ValidatePathAndBuildFullPath(
                appPath, config.TemplateManifestPath, nameof(config.TemplateManifestPath));

            this.actionRefSetPath = FileSystemAccessor.ValidatePathAndBuildFullPath(
                appPath, config.ActionRefSetPath, nameof(config.ActionRefSetPath));

            this.actionSetPath = FileSystemAccessor.ValidatePathAndBuildFullPath(
                appPath, config.ActionSetPath, nameof(config.ActionSetPath));

            this.filterTag = string.IsNullOrWhiteSpace(config.LibraryFilterTag) == false ?
                config.LibraryFilterTag :
                FileSystemAccessor.DefaultFilterTag;

            this.overrides = overrides;
        }

        /// <summary>
        ///     Retrieves the collection of templates to populate the store with
        /// </summary>
        /// <returns>resulting value</returns>
        public async Task<ICollection<TemplateDef>> RetrieveTemplatesAsync()
        {
            if (this.templates == null)
            {
                IEnumerable<TemplateManifestEntry> templatePointers;
                List<TemplateDef> results = new List<TemplateDef>();
                string basePath = Path.GetDirectoryName(this.templateManifestPath) ?? string.Empty;

                templatePointers = await this
                    .GetFilteredFileContents<TemplateManifestEntry, TemplateManifestEntryFilterable>(this.templateManifestPath);

                templatePointers = templatePointers.Where(o => string.IsNullOrWhiteSpace(o.LocalName) == false);

                foreach (TemplateManifestEntry pointer in templatePointers)
                {
                    results.Add(
                        new TemplateDef
                        {
                            Tag = pointer.Tag,
                            Text = await this.ReadFileAsync(Path.Combine(basePath, pointer.LocalName)),
                        });
                }

                this.templates = results;
            }

            return this.templates;
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
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Retrieves the collection of actions to populate the store with
        /// </summary>
        /// <returns>resulting value</returns>
        public async Task<ICollection<ActionDef>> RetrieveActionsAsync()
        {
            return 
                this.actions ?? 
                (this.actions = await this.GetFilteredFileContents<ActionDef, ActionDefFilterable>(this.actionSetPath));
        }

        /// <summary>
        ///     Writes the action changes to the store
        /// </summary>
        /// <param name="remove">actions to remove from the store</param>
        /// <param name="update">actions to update in the store</param>
        /// <param name="add">actions to add to the store</param>
        /// <returns>resulting value</returns>
        public Task WriteActionChangesAsync(
            ICollection<string> remove, 
            ICollection<ActionDef> update, 
            ICollection<ActionDef> add)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Retrieves the collection of references to actions to execute
        /// </summary>
        /// <returns>resulting value</returns>
        public async Task<ICollection<ActionRefRunnable>> RetrieveActionReferencesAsync()
        {
            return 
                this.refs ?? 
                (this.refs = await this.GetFilteredFileContents<ActionRefRunnable, ActionRefFilterable>(this.actionRefSetPath));
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
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Reads a file as a string and returns the contents
        /// </summary>
        /// <param name="path">file to read</param>
        /// <returns>resulting value</returns>
        private async Task<string> ReadFileAsync(string path)
        {
            using (
                Stream stream =
                    new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, FileOptions.SequentialScan))
            using (TextReader reader = new StreamReader(stream))
            {
                return await reader.ReadToEndAsync();
            }
        }

        /// <summary>
        ///     Reads a file and parses it into a collection of objects of type T
        /// </summary>
        /// <typeparam name="T">type of object to parse the file into</typeparam>
        /// <param name="path">file to read and parse</param>
        /// <returns>resulting value</returns>
        private async Task<ICollection<T>> DeserializeFileAsync<T>(string path)
        {
            string contents = await this.ReadFileAsync(path);
            
            if (typeof(T) == typeof(ActionRefFilterable))
            {
                contents = this.overrides.MergeExcludedAgentsOverrides(contents);
            }

            return JsonConvert.DeserializeObject<ICollection<T>>(contents);
        }

        /// <summary>
        ///     Gets the filtered file contents
        /// </summary>
        /// <typeparam name="TActual">type of t actual</typeparam>
        /// <typeparam name="TFilterable">type of t filterable</typeparam>
        /// <param name="path">path</param>
        /// <returns>resulting value</returns>
        private async Task<ICollection<TActual>> GetFilteredFileContents<TActual, TFilterable>(string path)
            where TFilterable : IFilterableLibraryObject<TActual>
        {
            ICollection<TFilterable> allItems = await this.DeserializeFileAsync<TFilterable>(path);

            return allItems
                .Where(
                    o => o.FilterTags == null ||
                         o.FilterTags.Count == 0 ||
                         o.FilterTags.Contains(this.filterTag, StringComparison.OrdinalIgnoreCase))
                .Select(o => o.BaseObject)
                .ToList();
        }

        /// <summary>
        ///     Validates the path and build full path
        /// </summary>
        /// <param name="appPath">application path</param>
        /// <param name="subPath">sub path</param>
        /// <param name="name">parameter name</param>
        /// <returns>resulting value</returns>
        private static string ValidatePathAndBuildFullPath(
            string appPath,
            string subPath,
            string name)
        {
            ArgumentCheck.ThrowIfNullEmptyOrWhiteSpace(subPath, name);

            subPath = subPath.Trim();

            if (Path.IsPathRooted(subPath))
            {
                throw new ArgumentException("Library file path is expected to be relative", name);
            }

            // while this could filter out files and paths with two dots in a row, it should be safe to declare that a provided
            //  relative library path and filename MUST NOT contain two dots in a row.
            if (subPath.Contains(".."))
            {
                throw new ArgumentException("Path cannot contain a reference to a parent directory", name);
            }

            return Path.Combine(appPath, subPath);
        }
    }
}
