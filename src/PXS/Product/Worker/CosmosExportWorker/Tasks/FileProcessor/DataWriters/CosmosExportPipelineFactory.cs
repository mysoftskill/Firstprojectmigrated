// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Tasks
{
    using System;
    using System.Globalization;
    using System.Reflection;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility;
    using Microsoft.PrivacyServices.CommandFeed.Client;

    /// <summary>
    ///     export pipeline factory
    /// </summary>
    public class CosmosExportPipelineFactory : ICosmosExportPipelineFactory
    {
        private readonly ICommandObjectFactory commandObjectFactory;

        /// <summary>
        ///    Initializes a new instance of the CosmosExportPipelineFactory class
        /// </summary>
        /// <param name="commandObjectFactory">command object factory</param>
        public CosmosExportPipelineFactory(ICommandObjectFactory commandObjectFactory)
        {
            this.commandObjectFactory = commandObjectFactory ?? throw new ArgumentNullException(nameof(commandObjectFactory));
        }

        /// <summary>
        ///      Creates the specified command id
        /// </summary>
        /// <param name="commandId">command id</param>
        /// <param name="command">export command</param>
        /// <returns>resulting value</returns>
        public IExportPipeline Create(
            string commandId, 
            IExportCommand command)
        {
            return new ExportPipelineWrapper(
                ExportPipelineFactory.CreateAzureExportPipeline(
                    this.commandObjectFactory.CreateLogger(),
                    command.AzureBlobContainerTargetUri,
                    command.AzureBlobContainerPath));
        }

        /// <summary>
        ///     wraps the ExportPipline in an interface
        /// </summary>
        internal sealed class ExportPipelineWrapper : IExportPipeline
        {
            internal const string MissingProductName = "CosmosExportMiscProduct";

            private ExportPipeline pipeline;

            /// <summary>
            ///     Initializes a new instance of the ExportPipelineWrapper class
            /// </summary>
            /// <param name="pipeline">export pipeline</param>
            public ExportPipelineWrapper(ExportPipeline pipeline)
            {
                this.pipeline = pipeline;
            }

            /// <summary>
            ///     Exports to a particular filename a particular record
            /// </summary>
            /// <param name="productId">productId this data is from</param>
            /// <param name="fileName">name of the file to export to</param>
            /// <param name="jsonData">piece of data to append to the file as a properly formatted JSON serialized string</param>
            /// <returns>task that completes when the export has been successful</returns>
            public Task ExportAsync(
                string productId,
                string fileName,
                string jsonData)
            {
                if (this.pipeline == null)
                {
                    throw new ObjectDisposedException("object is disposed");
                }

                return this.pipeline.ExportAsync(ExportPipelineWrapper.TranslateProductId(productId), fileName, jsonData);
            }

            /// <summary>
            ///     frees, releases, or resets unmanaged resources
            /// </summary>
            public void Dispose()
            {
                this.pipeline?.Dispose();
                this.pipeline = null;
            }

            /// <summary>Translates a string id into an ExportProductId</summary>
            /// <param name="id">source string</param>
            /// <returns>an ExportProductId</returns>
            internal static ExportProductId TranslateProductId(string id)
            {
                const BindingFlags CreateFlags = 
                    BindingFlags.CreateInstance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

                int productIdNumeric;

                if (int.TryParse(id, out productIdNumeric))
                {
                    ExportProductId productIdObj;

                    if (ExportProductId.ProductIds.TryGetValue(productIdNumeric, out productIdObj) == false)
                    {
                        try
                        {
                            productIdObj = (ExportProductId)Activator.CreateInstance(
                                type: typeof(ExportProductId),
                                bindingAttr: CreateFlags,
                                binder: null,
                                args: new object[] { productIdNumeric, ExportPipelineWrapper.MissingProductName },
                                culture: CultureInfo.InvariantCulture);
                        }
                        catch(ArgumentOutOfRangeException)
                        {
                            // should only hit this if we failed to add it because 2+ threads are attempting to add it at the same 
                            //  time, so the second call to fetch it work. If it doesn't, just return the unknown one.
                            if (ExportProductId.ProductIds.TryGetValue(productIdNumeric, out productIdObj) == false)
                            {
                                return ExportProductId.Unknown;
                            }
                        }
                    }

                    return productIdObj;
                }

                return ExportProductId.Unknown;
            }
        }
    }
}
