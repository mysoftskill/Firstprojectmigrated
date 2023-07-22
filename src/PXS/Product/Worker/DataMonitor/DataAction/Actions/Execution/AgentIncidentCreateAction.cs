// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.PrivacyServices.Common.Exceptions;
    using Microsoft.PrivacyServices.Common.TemplateBuilder;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Data;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Utility;

    using Microsoft.PrivacyServices.DataMonitor.DataAction.Utility.Incidents;

    /// <summary>
    ///     action that files incidents in IcM
    /// </summary>
    public class AgentIncidentCreateAction : ActionOp<AgentIncidentCreateDef>
    {
        public const string ActionType = "TRANSMIT-INCIDENT";
        
        private readonly IIncidentCreator creator;
        private readonly ITemplateStore templateStore;

        private AgentIncidentCreateDef def;

        /// <summary>
        ///     Initializes a new instance of the AgentIncidentFileAction class
        /// </summary>
        /// <param name="modelManipulator">model manipulator</param>
        /// <param name="templateStore">template store</param>
        /// <param name="incidentCreator">incident filer</param>
        public AgentIncidentCreateAction(
            IModelManipulator modelManipulator,
            ITemplateStore templateStore,
            IIncidentCreator incidentCreator) :
            base(modelManipulator)
        {
            this.templateStore = templateStore ?? throw new ArgumentNullException(nameof(templateStore));
            this.creator = incidentCreator ?? throw new ArgumentNullException(nameof(incidentCreator));
        }

        /// <summary>
        ///     Gets the action type
        /// </summary>
        public override string Type => AgentIncidentCreateAction.ActionType;

        /// <summary>
        ///     Gets the action's required parameters
        /// </summary>
        protected override ICollection<string> RequiredParams => AgentIncidentCreateAction.Args.Required;

        /// <summary>
        ///     Allows a derived type to perform validation on the definition object created during parsing
        /// </summary>
        /// <param name="context">parse context</param>
        /// <param name="factory">action factory</param>
        /// <param name="definition">definition object</param>
        /// <returns>true if the parse was successful, false if at least one error was found</returns>
        protected override bool ProcessAndStoreDefinition(
            IParseContext context,
            IActionFactory factory,
            AgentIncidentCreateDef definition)
        {
            bool result = base.ProcessAndStoreDefinition(context, factory, definition);

            result = this.templateStore.ValidateReference(context, definition.Body) && result;
            result = this.templateStore.ValidateReference(context, definition.Title) && result;

            this.def = definition;

            return result;
        }

        /// <summary>     
        ///     Validates that the collection of a reference's parameter set for this action is correct.
        /// </summary>
        /// <param name="context">parse context</param>
        /// <param name="missingArguments">missing arguments</param>
        /// <returns>true if the validation was successful, false if at least one error was found</returns>
        protected override bool ProcessValidation(
            IParseContext context,
            ISet<string> missingArguments)
        {
            // do not call the base class as it does the common "if any props are missing, it's a failure" case and
            //  we do not want that for this action
            return AgentIncidentCreateAction.Args.AreMissingArgumentsOkAtParse(context, missingArguments);
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
            object args = this.ModelManipulator.MergeModels(context, model, null, actionRef.ArgTransform);
            Args argsActual = Utility.ExtractObject<Args>(context, this.ModelManipulator, args);

            IncidentCreateResult createResult;
            TemplateRef bodyTemplate = this.def.Body;
            AgentIncident incident;
            string keywords;
            string eventName = this.def.EventName;
            object result;

            if (string.IsNullOrWhiteSpace(argsActual.BodyTagOverride) == false)
            {
                bodyTemplate = new TemplateRef
                {
                    TemplateTag = argsActual.BodyTagOverride,
                    Parameters = bodyTemplate.Parameters,
                };
            }

            if (string.IsNullOrWhiteSpace(argsActual.EventNameOverride) == false)
            {
                eventName = argsActual.EventNameOverride;
            }

            keywords = string.IsNullOrWhiteSpace(argsActual.KeywordsOverride) == false ?
                argsActual.KeywordsOverride :
                this.templateStore.Render(context, this.def.Keywords, model);

            incident = new AgentIncident
            {
                AssetGroupId = argsActual.AssetGroupId,
                AgentId = argsActual.AgentId,
                OwnerId = argsActual.OwnerId,

                Keywords = keywords,
                Title = this.templateStore.Render(context, this.def.Title, model),
                Body = this.templateStore.Render(context, bodyTemplate, model),

                EventName = eventName,
                Severity = argsActual.Severity,
            };

            if (incident.ValidateAndNormalize(context) == false)
            {
                throw new ActionExecuteException(
                    $"Errors found validating incident for {this.ObjText} [tag: {context.Tag}]");
            }

            try
            {
                if (context.IsSimulation == false)
                {
                    createResult = await this.creator.CreateIncidentAsync(context.CancellationToken, incident);
                }
                else
                {
                    context.Log("Running in simulation mode.  Incident will NOT be filed.");
                    createResult = new IncidentCreateResult(0, IncidentFileStatus.SimulatedFiling);
                }

                context.IncrementCounter("Incidents Filed", this.Tag, argsActual.CounterSuffix, 1);

                context.ReportActionEvent(
                    "success",
                    this.Type,
                    this.Tag,
                    new Dictionary<string, string>
                    {
                        { DataActionConsts.ExceptionDataIncidentAgent, incident.AgentId },
                        { DataActionConsts.ExceptionDataIncidentTitle, incident.Title },
                        { DataActionConsts.ExceptionDataIncidentEvent, incident.EventName },
                        { DataActionConsts.ExceptionDataIncidentSev, incident.Severity.ToStringInvariant() },
                    });
            }
            catch (Exception e)
            {
                string rawResponse = e.Data.Contains(DataActionConsts.ExceptionDataIncidentRawResponse) ?
                    e.Data[DataActionConsts.ExceptionDataIncidentRawResponse].ToString() :
                    string.Empty;

                context.IncrementCounter("Incident Filing Errors", this.Tag, argsActual.CounterSuffix, 1);

                context.ReportActionError(
                    "error",
                    this.Type,
                    this.Tag,
                    e.GetMessageAndInnerMessages(),
                    new Dictionary<string, string>
                    {
                        { DataActionConsts.ExceptionDataIncidentAgent, incident.AgentId },
                        { DataActionConsts.ExceptionDataIncidentTitle, incident.Title },
                        { DataActionConsts.ExceptionDataIncidentEvent, incident.EventName },
                        { DataActionConsts.ExceptionDataIncidentSev, incident.Severity.ToStringInvariant() },
                        { DataActionConsts.ExceptionDataIncidentRawResponse, rawResponse }
                    });

                throw;
            }

            context.Log(
            "{0} severity {1} incident ({2}) [{3}] : {4}".FormatInvariant(
                createResult.Id.HasValue ? "Successfully filed" : "Failed to file",
                createResult.Id?.ToStringInvariant() ?? "NONE",
                incident.Severity,
                incident.Title,
                createResult.Status.ToString()));

            result = this.ModelManipulator
                .TransformFrom(
                    new
                    {
                        AssetGroupId = incident.AssetGroupId?.ToString(),
                        OwnerId = incident.OwnerId?.ToString(),
                        AgentId = incident.AgentId?.ToString(),

                        Title = incident.Title,

                        IncidentStatusText = createResult.Status.ToStringInvariant(),
                        IncidentStatus = createResult.Status,
                        IncidentId = createResult.Id,
                    });

            return (true, result);
        }

        /// <summary>
        ///     Action arguments
        /// </summary>
        internal class Args : IValidatable
        {
            public static readonly string[] Required = { "AssetGroupId", "AgentId", "OwnerId", "Severity" };

            public string EventNameOverride { get; set; }
            public string KeywordsOverride { get; set; }
            public string BodyTagOverride { get; set; }
            public string CounterSuffix { get; set; }
            public string AssetGroupId { get; set; }
            public string AgentId { get; set; }
            public string OwnerId { get; set; }
            public int Severity { get; set; }

            public static bool AreMissingArgumentsOkAtParse(
                IParseContext context,
                ISet<string> missing)
            {
                bool result = true;

                if (missing.Contains("Severity"))
                {
                    context.LogError("Severity argment must be specified");
                    result = false;
                }

                if (missing.Contains("AssetGroupId") &&
                    missing.Contains("AgentId") &&
                    missing.Contains("OwnerId"))
                {
                    context.LogError("At least one of AssetGroupId, AgentId, or OwnerId must be specified");
                    result = false;
                }

                return result;
            }

            /// <summary>
            ///     Validates the argument object and logs any errors to the context
            /// </summary>
            /// <param name="context">execution context</param>
            /// <returns>true if the object validated successfully; false otherwise</returns>
            public bool ValidateAndNormalize(IContext context)
            {
                bool result = true;

                if (this.CounterSuffix != null && string.IsNullOrWhiteSpace(this.CounterSuffix))
                {
                    context.LogError("null or non-empty counter suffix must be specified");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(this.AssetGroupId) &&
                    string.IsNullOrWhiteSpace(this.AgentId) &&
                    string.IsNullOrWhiteSpace(this.OwnerId))
                {
                    context.LogError("At least one of AssetGroupId, AgentId, or OwnerId must be non-empty");
                    result = false;
                }

                if (this.Severity < AgentIncident.MinSev || this.Severity > AgentIncident.MaxSev)
                {
                    context.LogError($"severity must be in the range [{AgentIncident.MinSev}..{AgentIncident.MaxSev}]");
                    result = false;
                }

                return result;
            }
        }
    }
}
