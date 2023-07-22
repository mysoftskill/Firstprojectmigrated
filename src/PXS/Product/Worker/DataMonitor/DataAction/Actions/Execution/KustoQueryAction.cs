// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;

    using Kusto.Data.Common;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.PrivacyServices.KustoHelpers;
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.PrivacyServices.Common.TemplateBuilder;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Data;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Utility;

    using Newtonsoft.Json;

    /// <summary>
    ///     action that can query Kusto for data
    /// </summary>
    public class KustoQueryAction : ActionOp<KustoQueryDef>
    {
        public const string ActionType = "MODELBUILD-QUERY-KUSTO";

        private const string ResultNamePrefix = "Table";

        private static readonly string AppId;

        private readonly IKustoClientFactory kustoFactory;
        private readonly ITemplateStore templateStore;

        private KustoQueryDef def;

        /// <summary>
        ///     Initializes static members of the KustoQueryAction class
        /// </summary>
        static KustoQueryAction()
        {
            string processId = Process.GetCurrentProcess().Id.ToStringInvariant();
            string machine = Environment.MachineName;
            string entry = Assembly.GetEntryAssembly()?.FullName ?? Assembly.GetCallingAssembly().FullName;

            KustoQueryAction.AppId = $"{machine}.{processId}.{entry}";
        }

        /// <summary>
        ///     Initializes a new instance of the KustoQueryAction class
        /// </summary>
        /// <param name="modelManipulator">model manipulator</param>
        /// <param name="kustoClientFactory">kusto client factory</param>
        /// <param name="templateStore">template store</param>
        public KustoQueryAction(
            IModelManipulator modelManipulator,
            IKustoClientFactory kustoClientFactory,
            ITemplateStore templateStore)
            :
            base(modelManipulator)
        {
            this.templateStore = templateStore ?? throw new ArgumentNullException(nameof(templateStore));
            this.kustoFactory = kustoClientFactory ?? throw new ArgumentNullException(nameof(kustoClientFactory));
        }

        /// <summary>
        ///     Gets the action type
        /// </summary>
        public override string Type => KustoQueryAction.ActionType;

        /// <summary>
        ///     Allows a derived type to perform validation on the definition object created during parsing
        /// </summary>
        /// <param name="context">context</param>
        /// <param name="factory">action factory</param>
        /// <param name="definition">definition object</param>
        /// <returns>true if the parse was successful, false if at least one error was found</returns>
        protected override bool ProcessAndStoreDefinition(
            IParseContext context,
            IActionFactory factory,
            KustoQueryDef definition)
        {
            bool result = base.ProcessAndStoreDefinition(context, factory, definition);

            result = this.templateStore.ValidateReference(context, definition.Query) && result;

            this.def = definition;

            return result;
        }

        /// <summary>
        ///     Executes the action using the specified input
        /// </summary>
        /// <param name="context">execution context</param>
        /// <param name="actionRef">action reference</param>
        /// <param name="model">model from previous actions in the containing action set</param>
        /// <returns>execution result</returns>
        protected override async Task<(bool Continue, object Result)> ExecuteInternalAsync(
            IExecuteContext context,
            ActionRefCore actionRef,
            object model)
        {
            IDictionary<string, string> queryArgsToKusto = new Dictionary<string, string>();
            TemplateRef queryTemplate = this.def.Query;
            DataSet dataSet = new DataSet();
            string queryText;
            object result;
            object args;
            Args argsActual;

            args = this.ModelManipulator.MergeModels(context, model, null, actionRef.ArgTransform);
            argsActual = Utility.ExtractObject<Args>(context, this.ModelManipulator, args);

            if (argsActual?.QueryParameters != null)
            {
                foreach (KeyValuePair<string, object> kvp in argsActual.QueryParameters.Where(o => o.Value != null))
                {
                    queryArgsToKusto[kvp.Key] = JsonConvert.SerializeObject(kvp.Value);
                }
            }

            if (string.IsNullOrWhiteSpace(argsActual?.QueryTagOverride) == false)
            {
                queryTemplate = new TemplateRef
                {
                    TemplateTag = argsActual.QueryTagOverride,
                    Parameters = queryTemplate.Parameters,
                };
            }

            queryText = this.templateStore.Render(context, queryTemplate, model);

            using (IKustoClient client = this.kustoFactory.CreateClient(this.def.ClusterUrl, this.def.Database, context.Tag))
            {
                KustoQueryOptions options;
                IDataReader reader;

                options = new KustoQueryOptions
                {
                    ClientRequestId = context.Tag + "." + Guid.NewGuid().ToString("N"),
                    ApplicationId = KustoQueryAction.AppId,
                    Parameters = queryArgsToKusto,
                    Options = new Dictionary<string, object>
                    {
                        { ClientRequestProperties.OptionServerTimeout, TimeSpan.FromMinutes(10) }
                    }
                };

                try
                {
                    reader = await client.ExecuteQueryAsync(queryText, options).ConfigureAwait(false);
                    dataSet = client.ConvertToDataSet(reader);

                    context.IncrementCounter("Kusto Queries Executed", this.Tag, argsActual?.CounterSuffix, 1);
                    context.ReportActionEvent("success", this.Type, this.Tag, null);
                }
                catch (Exception e)
                {
                    context.IncrementCounter("Kusto Query Errors", this.Tag, argsActual?.CounterSuffix, 1);
                    context.ReportActionError("error", this.Type, this.Tag, e.GetMessageAndInnerMessages(), null);
                    
                    DataTable errorTable = dataSet.Tables.Add("KustoQueryErrors");

                    DataColumn errorTag = errorTable.Columns.Add("Tag", typeof(string));
                    errorTable.Columns.Add("Message", typeof(string));

                    errorTable.PrimaryKey = new DataColumn[] { errorTag };
                    
                    // Add the error message to the data table
                    var row = errorTable.NewRow();
                    row["Tag"] = queryTemplate.TemplateTag ?? this.Tag;
                    row["Message"] = e.GetMessageAndInnerMessages();
                    errorTable.Rows.Add(row);
                }
            }

            result = this.ModelManipulator.CreateEmpty();

            for (int i = 0; i < dataSet.Tables.Count; ++i)
            {
                string propName = KustoQueryAction.ResultNamePrefix + i.ToString("D2", CultureInfo.InvariantCulture);
                this.ModelManipulator.AddSubmodel(context, result, propName, dataSet.Tables[i], MergeMode.ReplaceExisting);
            }

            return (true, result);
        }

        /// <summary>
        ///     Action arguments
        /// </summary>
        internal class Args : IValidatable
        {
            public IDictionary<string, object> QueryParameters { get; set; }
            public string QueryTagOverride { get; set; }
            public string CounterSuffix { get; set; }

            /// <summary>
            ///     Validates the argument object and logs any errors to the context
            /// </summary>
            /// <param name="context">execution context</param>
            /// <returns>true if the object validated successfully; false otherwise</returns>
            public bool ValidateAndNormalize(IContext context)
            {
                if (this.CounterSuffix != null && string.IsNullOrWhiteSpace(this.CounterSuffix))
                {
                    context.LogError("null or non-empty counter suffix must be specified");
                    return false;
                }

                if (this.QueryParameters != null && this.QueryParameters.Any(o => o.Value == null))
                {
                    context.LogError("Kusto query parameters must be non-null");
                    return false;
                }

                return true;
            }
        }
    }
}