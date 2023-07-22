// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.Actions
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Data;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Store;

    /// <summary>
    ///     base class for actions that execute an ordered sequence of actions
    /// </summary>
    public abstract class ActionSet<TDef> : ActionOp<TDef>
        where TDef : ActionSetDef
    {
        private ICollection<KeyValuePair<string, ModelValue>> localModelTransform;
        private IList<InternalActionRef> actions;
        private bool useLocalModel;

        /// <summary>
        ///     Initializes a new instance of the ActionSet{TDef} class
        /// </summary>
        /// <param name="modelManipulator">model manipulator</param>
        protected ActionSet(IModelManipulator modelManipulator) : 
            base(modelManipulator)
        {
        }

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
            TDef definition)
        {
            bool result = base.ProcessAndStoreDefinition(context, factory, definition);

            if (definition.Actions == null || definition.Actions.Count == 0)
            {
                // set a default empty list so other parts of the class can just do a simple enumeration instead of having to worry
                //  about a null check
                this.actions = ListHelper.EmptyList<InternalActionRef>();
                return true;
            }

            this.actions = definition.Actions
                .OrderBy(o => o.ExecutionOrder)
                .Select(
                    aref =>
                    {
                        InternalActionRef arefInt = new InternalActionRef { Ref = aref };
                        ActionDef def = aref.Inline;
                        if (def != null)
                        {
                            arefInt.Action = factory.Create(def.Type);
                            if (arefInt.Action != null)
                            {
                                result = arefInt.Action.ParseAndProcessDefinition(context, factory, def.Tag, def.Def) && result;
                            }
                            else
                            {
                                context.LogError("[" + def.Type + "] is not a supported action type");
                                result = false;
                            }
                        }

                        return arefInt;
                    })
                .ToList();

            if (definition.LocalModelMode == ModelMode.Local)
            {
                this.localModelTransform = definition.LocalModelTransform;
                this.useLocalModel = true;
            }
            else
            {
                this.useLocalModel = false;
            }

            return result;
        }

        /// <summary>
        ///     Performs the second phase of building the action: once all actions are parsed and added to the store,
        ///      this instructs container actions to fetch referenced actions from the store and parse inline definitions
        /// </summary>
        /// <param name="context">parse context</param>
        /// <param name="store">action store</param>
        /// <returns>true if the expansion was successful, false if at least one error was found</returns>
        protected override bool ProcessReferenceExpansion(
            IParseContext context,
            IActionFetcher store)
        {
            bool result = base.ProcessReferenceExpansion(context, store);

            foreach (InternalActionRef actionRef in this.actions)
            {
                if (actionRef.Action == null)
                {
                    actionRef.Action = store.GetAction(actionRef.Ref.Tag);
                    if (actionRef.Action == null)
                    {
                        context.LogError($"Unable to find referenced action {actionRef.Ref.Tag} in {this.ObjText}");
                        result = false;
                        continue;
                    }
                }

                result = actionRef.Action.ExpandDefinition(context, store) && result;
            }

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
            bool result = base.ProcessValidation(context, missingArguments);

            foreach (InternalActionRef actionRef in this.actions)
            {
                result = actionRef.Action.Validate(context, actionRef.Ref.ArgTransform) && result;
            }

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
            bool result;

            model = this.GetActionSetLocalModel(context, actionRef, model);

            result = await this.ExecuteActionSetAsync(context, model).ConfigureAwait(false);

            return (result, model);
        }

        /// <summary>
        ///     Gets the action set model to use
        /// </summary>
        /// <param name="context">execution context</param>
        /// <param name="actionRef">action reference</param>
        /// <param name="model">model from previous actions in the containing action set</param>
        /// <returns>execution result</returns>
        protected object GetActionSetLocalModel(
            IExecuteContext context,
            ActionRefCore actionRef,
            object model)
        {
            if (actionRef?.ArgTransform?.Count > 0)
            {
                model = this.ModelManipulator.MergeModels(context, model, model, actionRef.ArgTransform);
            }

            return this.useLocalModel ?
                this.ModelManipulator.MergeModels(context, model, null, this.localModelTransform) :
                model;
        }

        /// <summary>
        ///     Executes the action set
        /// </summary>
        /// <param name="context">execution context</param>
        /// <param name="model">input data set</param>
        /// <returns>resulting value</returns>
        protected async Task<bool> ExecuteActionSetAsync(
            IExecuteContext context,
            object model)
        {
            foreach (InternalActionRef actionRef in this.actions)
            {
                ExecuteResult execResult;

                context.CancellationToken.ThrowIfCancellationRequested();
                    
                execResult = await actionRef.Action
                    .ExecuteAsync(context, actionRef.Ref, model)
                    .ConfigureAwait(false);

                if (execResult.Continue == false)
                {
                    context.Log(
                        "Child action [" + actionRef.Tag + "] has indicated that this action set should abort");

                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     internal class to store both an action ref and the associated action that will be executed with it
        /// </summary>
        private class InternalActionRef
        {
            public ActionRefCore Ref { get; set; }
            public IAction Action { get; set; }

            public string Tag => this.Ref?.Tag;
        }
    }

    /// <summary>
    ///     executes an ordered sequence of actions
    /// </summary>
    public sealed class ActionSet : ActionSet<ActionSetDef>
    {
        public const string ActionType = "ACTIONSET";

        /// <summary>
        ///     Initializes a new instance of the ActionSet class
        /// </summary>
        /// <param name="modelManipulator">model manipulator</param>
        public ActionSet(IModelManipulator modelManipulator) :
            base(modelManipulator)
        {
        }

        /// <summary>
        ///     Gets the action type
        /// </summary>
        public override string Type => ActionSet.ActionType;
    }
}
