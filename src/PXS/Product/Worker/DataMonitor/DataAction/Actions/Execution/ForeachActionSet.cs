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
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Data;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Utility;

    /// <summary>
    ///     an action that runs a query and loops over the result set, performing the same action
    /// </summary>
    /// <remarks>
    ///     the action does the following:
    ///     - executes a query and for each rowset in the query,
    ///       1. it checks to ensure that the action is applicable (based on time of day, day of week, date, etc)
    ///       2. it attempts to acquire a lock allowing it to perform the actions inside the loop for the row
    ///       3. if the lock was acquired, it performs a sequence of actions
    ///       4. if successful, it adds the row results to the loop result set
    ///       5. if successful, it relocks the row for the periodicity of the loop (e.g. run once per row per day)
    /// </remarks>
    public sealed class ForeachActionSet : ActionSet<ForeachActionSetDef>
    {
        public const string ActionType = "LOOP-DATASET";

        private ICollection<KeyValuePair<string, ModelValueUpdate>> loopResultTransform;
        private ICollection<KeyValuePair<string, ModelValue>> loopModelTransform;
        private LoopResultCondition loopResultCondition;
        private bool returnNotContinueOnEmpty;
        private bool useLoopModel;

        /// <summary>
        ///     Initializes a new instance of the ForeachAction class
        /// </summary>
        /// <param name="modelManipulator">model manipulator</param>
        public ForeachActionSet(IModelManipulator modelManipulator) :
            base(modelManipulator)
        {
        }

        /// <summary>
        ///     Gets the action type
        /// </summary>
        public override string Type => ForeachActionSet.ActionType;

        /// <summary>
        ///     Gets the action's required parameters
        /// </summary>
        protected override ICollection<string> RequiredParams => ForeachActionSet.Args.Required;

        /// <summary>
        ///     Allows a derived type to perform validation on the definition object created during parsing
        /// </summary>
        /// <param name="context">parse context</param>
        /// <param name="factory">action factory</param>
        /// <param name="definition">definition object</param>
        /// <returns>null or empty string to indicate no errors or a non-empty string containing a description of the errors</returns>
        protected override bool ProcessAndStoreDefinition(
            IParseContext context,
            IActionFactory factory,
            ForeachActionSetDef definition)
        {
            bool result = base.ProcessAndStoreDefinition(context, factory, definition);

            result = ModelUtilites.ValidateModelValueMap(context, definition.LoopModelTransform) && result;
            result = ModelUtilites.ValidateModelValueMap(context, definition.LoopResultTransform) && result;

            if (definition.LoopModelMode == ModelMode.Local)
            {
                this.loopModelTransform = definition.LoopModelTransform;
                this.useLoopModel = true;
            }
            else if (definition.LoopModelMode == ModelMode.Input)
            {
                if (definition.LoopModelTransform != null)
                {
                    context.LogError("input model mode is Input, but a local loop model transform was found");
                    result = false;
                }

                this.useLoopModel = false;
            }
            else
            {
                context.LogError("unknown loop model mode [{" + definition.LoopModelMode.ToString() + "}] found");
                result = false;
            }

            this.returnNotContinueOnEmpty = definition.ReturnNotContinueOnEmpty;
            this.loopResultTransform = definition.LoopResultTransform;
            this.loopResultCondition = definition.LoopResultCondition;

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
            object args = this.ModelManipulator.MergeModels(context, model, null, actionRef.ArgTransform);
            Args argsActual = Utility.ExtractObject<Args>(context, this.ModelManipulator, args);
            bool result = true;
            int countContinue = 0;
            int total = 0;

            model = this.GetActionSetLocalModel(context, null, model);

            if (argsActual.Collection != null)
            {
                context.Log("Beginning iteration over object collection");

                foreach (object item in this.ModelManipulator.ToEnumerable(argsActual.Collection))
                {
                    bool iterationResult;

                    context.CancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        iterationResult = await this
                            .ExcecuteLoopIterationAsync(context, argsActual, item, model, total)
                            .ConfigureAwait(false);
                    }
                    catch (DataActionException e) when (e.IsFatal == false)
                    {
                        iterationResult = false;
                        context.LogError(
                            $"Iteration {total} failed with ActionException: {e.GetMessageAndInnerMessages()} ");
                    }

                    ++total;

                    if (iterationResult)
                    {
                        ++countContinue;
                    }
                }

                context.Log(
                    $"Iteration over object collection completed: {countContinue} of {total} iterations reported continue");
            }

            if (total == 0)
            {
                result = this.returnNotContinueOnEmpty == false;
            }
            else if (this.loopResultCondition == LoopResultCondition.FalseIfAny)
            {
                result = countContinue == total;
            }
            else if (this.loopResultCondition == LoopResultCondition.FalseIfAll)
            {
                result = countContinue > 0;
            }

            return (result, model);
        }

        /// <summary>
        ///     Excecutes executes a single iteration of the foreach loop
        /// </summary>
        /// <param name="context">execution context</param>
        /// <param name="args">foreach loop arguments</param>
        /// <param name="loopItem">loop item</param>
        /// <param name="model">overall action local model</param>
        /// <param name="iterationIndex">iteration index</param>
        /// <returns>resulting value</returns>
        private async Task<bool> ExcecuteLoopIterationAsync(
            IExecuteContext context,
            Args args,
            object loopItem,
            object model,
            int iterationIndex)
        {
            object loopLocalModel;
            string fullTag;
            bool iterationResult;

            loopLocalModel = this.useLoopModel ?
                this.ModelManipulator.MergeModels(context, model, null, this.loopModelTransform) :
                model;

            if (this.ModelManipulator.TryExtractValue(
                    context,
                    loopItem,
                    args.CollectionItemKeyPropertyName,
                    null,
                    out string keyText) &&
                string.IsNullOrWhiteSpace(keyText) == false)
            {
                fullTag = this.Tag + "['" + keyText + "']";
            }
            else
            {
                fullTag = this.Tag + "[" + iterationIndex.ToStringInvariant() + "]";
            }

            context.OnActionUpdate(fullTag);

            context.Log("Processing loop item [" + fullTag + "]");

            // add (or replace) the object that represents the row of data we're iterating over
            this.ModelManipulator.AddSubmodel(
                context,
                loopLocalModel,
                args.DataRowPropertyName,
                loopItem,
                MergeMode.ReplaceExisting);

            // execute a single instance of the action set
            try
            {
                iterationResult = await this.ExecuteActionSetAsync(context, loopLocalModel).ConfigureAwait(false);

                // do this before we remove the data row to allow that to be extracted from the local model
                if (this.loopResultTransform?.Count > 0)
                {
                    this.ModelManipulator.MergeModels(context, loopLocalModel, model, this.loopResultTransform);
                }
            }
            finally
            {
                // remove the object we added for the data row
                this.ModelManipulator.RemoveSubmodel(loopLocalModel, args.DataRowPropertyName);
            }
            
            return iterationResult;
        }

        /// <summary>
        ///     Action arguments
        /// </summary>
        internal class Args : IValidatable
        {
            public static readonly string[] Required = { "DataRowPropertyName", "Collection" };

            public string CollectionItemKeyPropertyName { get; set; }
            public string DataRowPropertyName { get; set; }
            public object Collection { get; set; }

            /// <summary>
            ///     Validates and normalizes the argument object and logs any errors to the context
            /// </summary>
            /// <param name="context">execution context</param>
            /// <returns>true if the object validated successfully; false otherwise</returns>
            public bool ValidateAndNormalize(IContext context)
            {
                bool result = true;

                this.CollectionItemKeyPropertyName = this.CollectionItemKeyPropertyName?.Trim();
                this.DataRowPropertyName = this.DataRowPropertyName?.Trim();

                if (string.IsNullOrWhiteSpace(this.DataRowPropertyName))
                {
                    context.LogError("must specify a non-empty DataRowPropertyName");
                    result = false;
                }

                return result;
            }
        }
    }
}
