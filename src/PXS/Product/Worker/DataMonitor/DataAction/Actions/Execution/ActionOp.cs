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
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Store;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    
    public enum DefinitionMode
    {
        Required,
        Forbidden
    }

    /// <summary>
    ///     base class for actions
    /// </summary>
    public abstract class ActionOp<TDef> : IAction
        where TDef : class
    {
        private string context;

        /// <summary>
        ///     Initializes a new instance of the Action{TDef} class
        /// </summary>
        /// <param name="modelManipulator">model manipulator</param>
        protected ActionOp(IModelManipulator modelManipulator)
        {
            this.ModelManipulator = modelManipulator ?? throw new ArgumentNullException(nameof(modelManipulator));
        }

        /// <summary>
        ///     Gets the action type
        /// </summary>
        public abstract string Type { get; }

        /// <summary>
        ///     Gets the action tag
        /// </summary>
        public string Tag { get; private set; }

        /// <summary>
        ///     Gets a value to emit when the action's name is needed
        /// </summary>
        protected string ObjText => 
            this.context ?? 
            (string.IsNullOrWhiteSpace(this.Tag) ?
                $"action [{this.Type}] with UNKNOWN tag" :
                (this.context = $"action [{this.Type}] with tag [{this.Tag ?? "<NONE>"}]"));

        /// <summary>
        ///     Gets or sets a value indicating whether this instance is valid
        /// </summary>
        public bool IsValid { get; private set; }

        /// <summary>
        ///     Gets the model manipulator
        /// </summary>
        protected IModelManipulator ModelManipulator { get; }

        /// <summary>
        ///     Gets the action's required parameters
        /// </summary>
        protected virtual ICollection<string> RequiredParams => null;

        /// <summary>
        ///     Gets a value indicating whether the action has a definition object
        /// </summary>
        protected virtual DefinitionMode DefinitionMode => DefinitionMode.Required;

        /// <summary>
        ///     Extracts the specified JSON node into the action's inner data
        /// </summary>
        /// <param name="context">parse context</param>
        /// <param name="factory">action factory</param>
        /// <param name="tag">action tag</param>
        /// <param name="definition">action definition</param>
        /// <returns>true if the definition was parsed successfully, false if at least one error was found</returns>
        /// <remarks>
        ///     The action definition can either be a JToken containing the semi-deserialized representation of the action
        ///      definition or a ActionDef structure containing a JSON text representation of the definition
        /// </remarks>
        public bool ParseAndProcessDefinition(
            IParseContext context,
            IActionFactory factory,
            string tag, 
            object definition)
        {
            const string PushedErrMsg =
                "Actions of type [{0}] require a definition object of type [{1}]. One was was supplied for {2}, but was " +
                "found to contain the following errors:";

            TDef defObj = null;

            ArgumentCheck.ThrowIfNull(context, nameof(context));
            ArgumentCheck.ThrowIfNull(factory, nameof(factory));

            if (this.Tag != null)
            {
                throw new InvalidOperationException(
                    $"{this.Type} object has already been initialized with as object with action [{this.Tag}]");
            }

            if (string.IsNullOrWhiteSpace(tag))
            {
                context.LogError("All actions of type [" + this.Type + "] must contain have a non-empty tag");
                return false;
            }

            context.OnActionStart(ActionType.Parse, tag);

            this.Tag = tag;

            if (definition is ActionDef actionDef)
            {
                definition = actionDef.Def;
            }

            if (definition != null)
            {
                if (definition is TDef actualObjectDef)
                {
                    defObj = actualObjectDef;
                }
                else if (definition is JToken nodeDef)
                {
                    try
                    {
                        // the final cast at the end is just to make C# happy- the condition occurs only when TDef is 'object',
                        //  so that cast cannot fail (as of course everything derives from 'object' and the definition is of type
                        //  'object')
                        // ReSharper disable once PossibleInvalidCastException
                        defObj = typeof(object) != typeof(TDef) ? nodeDef.ToObject<TDef>() : (TDef)definition;
                    }
                    catch (JsonSerializationException e)
                    {
                        context.LogError(e, "Parse failure deserializing JToken for " + this.ObjText);
                        return false;
                    }
                }
                else if (definition is string textDef)
                {
                    if (string.IsNullOrWhiteSpace(textDef) == false)
                    {
                        try
                        {
                            defObj = JsonConvert.DeserializeObject<TDef>(textDef);
                        }
                        catch (JsonSerializationException e)
                        {
                            context.LogError(e, "Parse failure deserializing JSON for " + this.ObjText);
                            return false;
                        }
                    }
                }
                else
                {
                    const string ErrMsg =
                        "An action definition for a [{0}] action must be either an JToken, an ActionDef, a string containing " +
                        "JSON, or an instance of the definition type [{1}]. [{2}] objects are not supported";

                    context.LogError(
                        ErrMsg.FormatInvariant(this.Type, typeof(TDef).FullName, definition.GetType().FullName));
                    return false;
                }
            }

            if (this.DefinitionMode == DefinitionMode.Required && defObj == null)
            {
                const string ErrMsg =
                    "Actions of type [{0}] require a definition object of type [{1}]. No definition was supplied for {2}";

                context.LogError(ErrMsg.FormatInvariant(this.Type, typeof(TDef).FullName, this.ObjText));
                return false;
            }

            if (this.DefinitionMode == DefinitionMode.Forbidden && defObj != null)
            {
                // the {0} replacement parameter is used twice for the object type
                const string ErrMsg =
                    "Actions of type [{0}] require no action definition, but a definition of type [{1}] was supplied for {2}";

                context.LogError(ErrMsg.FormatInvariant(this.Type, definition.GetType().FullName, this.ObjText));
                return false;
            }

            if (defObj is IValidatable validatableDefObj && validatableDefObj.ValidateAndNormalize(context) == false)
            {
                return false;
            }

            context.PushErrorIntroMessage(() => PushedErrMsg.FormatInvariant(this.Type, typeof(TDef).FullName, this.ObjText));

            this.IsValid = this.ProcessAndStoreDefinition(context, factory, defObj);

            context.PopErrorIntroMessage();

            context.OnActionEnd();

            return this.IsValid;
        }

        /// <summary>
        ///     Extracts the second phase of building the action: once all actions are parsed and added to the store,
        ///     this instructs container actions to fetch referenced actions from the store and parse inline definitions
        /// </summary>
        /// <param name="context">parse context</param>
        /// <param name="store">action store</param>
        /// <returns>true if the expansion was successful, false if at least one error was found</returns>
        /// <remarks>
        ///     The action definition can either be a JToken containing the semi-deserialized representation of the action
        ///      definition or a ActionDef structure containing a JSON text representation of the definition
        ///     This method is called after all store-based actions have been parsed, so it is an error if any referenced
        ///      actions cannot be found
        /// </remarks>
        public bool ExpandDefinition(
            IParseContext context,
            IActionFetcher store)
        {
            bool result;

            ArgumentCheck.ThrowIfNull(context, nameof(context));
            ArgumentCheck.ThrowIfNull(store, nameof(store));

            if (this.Tag == null || this.IsValid == false)
            {
                throw new InvalidOperationException("object has not been successfully initialized successfully");
            }

            // default implementation has no expansion
            context.OnActionStart(ActionType.Expand, this.Tag);
            result = this.ProcessReferenceExpansion(context, store);
            context.OnActionEnd();

            this.IsValid = result;

            return result;
        }

        /// <summary>
        ///     Validates that the collection of a reference's parameter set for this action is correct.
        /// </summary>
        /// <param name="context">parse context</param>
        /// <param name="parameters">parameters found in reference</param>
        /// <returns>true if the validation was successful, false if at least one error was found</returns>
        /// <remarks>
        ///     This does not do any variable expansion or other runtime actions- it just validates that the parameter
        ///      set is well formed and contains expected parameter names
        /// </remarks>
        public bool Validate(
            IParseContext context,
            IDictionary<string, ModelValue> parameters)
        {
            ISet<string> requiredArgs = null;
            bool result = true;

            ArgumentCheck.ThrowIfNull(context, nameof(context));

            if (this.Tag == null || this.IsValid == false)
            {
                throw new InvalidOperationException("object has not been successfully initialized");
            }

            context.OnActionStart(ActionType.Validate, this.Tag);

            if (this.RequiredParams?.Count > 0)
            {
                requiredArgs = new HashSet<string>(this.RequiredParams);
            }

            if (parameters?.Count > 0)
            {
                foreach (string name in parameters.Keys)
                {
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        context.LogError("parameter names must be non-empty");
                        result = false;
                    }

                    requiredArgs?.Remove(name);
                }
            }

            result = this.ProcessValidation(context, requiredArgs) && result;

            context.OnActionEnd();

            this.IsValid = result;

            return result;
        }

        /// <summary>
        ///     Executes the action using the specified input
        /// </summary>
        /// <param name="context">execution context</param>
        /// <param name="actionRef">action reference triggering the call</param>
        /// <param name="model">input data set</param>
        /// <returns>execution result</returns>
        public async Task<ExecuteResult> ExecuteAsync(
            IExecuteContext context,
            ActionRefCore actionRef,
            object model)
        {
            (bool Continue, object Result) localResult;

            ArgumentCheck.ThrowIfNull(actionRef, nameof(actionRef));
            ArgumentCheck.ThrowIfNull(context, nameof(context));

            if (this.Tag == null || this.IsValid == false)
            {
                throw new InvalidOperationException("object has not been successfully initialized successfully");
            }

            context.OnActionStart(ActionType.Execute, this.Tag);

            context.LogVerbose(
                string.IsNullOrWhiteSpace(actionRef.Description) ?
                    "Executing " + this.ObjText :
                    $"Executing {this.ObjText}: {actionRef.Description}");

            localResult = await this
                .ExecuteInternalAsync(context, actionRef, model)
                .ConfigureAwait(false);

            if (localResult.Result != null &&
                model != null &&
                actionRef.ResultTransform?.Count > 0)
            {
                this.ModelManipulator.MergeModels(context, localResult.Result, model, actionRef.ResultTransform);
            }

            context.OnActionEnd();

            return new ExecuteResult(localResult.Continue);
        }

        /// <summary>
        ///     Allows a derived type to perform validation on the definition object created during parsing
        /// </summary>
        /// <param name="context">parse context</param>
        /// <param name="factory">action factory</param>
        /// <param name="definition">definition object</param>
        /// <returns>true if the parse was successful, false if at least one error was found</returns>
        protected virtual bool ProcessAndStoreDefinition(
            IParseContext context,
            IActionFactory factory,
            TDef definition)
        {
            // default implementation does no processing
            return true;
        }

        /// <summary>
        ///     Performs the second phase of building the action: once all actions are parsed and added to the store,
        ///      this instructs container actions to fetch referenced actions from the store and parse inline definitions
        /// </summary>
        /// <param name="context">parse context</param>
        /// <param name="store">action store</param>
        /// <returns>true if the expansion was successful, false if at least one error was found</returns>
        protected virtual bool ProcessReferenceExpansion(
            IParseContext context,
            IActionFetcher store)
        {
            // default implementation does no processing
            return true;
        }

        /// <summary>     
        ///     Validates that the collection of a reference's parameter set for this action is correct.
        /// </summary>
        /// <param name="context">parse context</param>
        /// <param name="missingArguments">missing arguments</param>
        /// <returns>true if the validation was successful, false if at least one error was found</returns>
        protected virtual bool ProcessValidation(
            IParseContext context,
            ISet<string> missingArguments)
        {
            bool result = true;

            if (missingArguments?.Count > 0)
            {
                context.LogError(
                    "the following required parameters are not specified: " + string.Join(", ", missingArguments));
                result = false;
            }

            return result;
        }

        /// <summary>
        ///     Initializes a new instance of the Action class
        /// </summary>
        /// <param name="context">execution context</param>
        /// <param name="actionRef">action reference</param>
        /// <param name="model">model from previous actions in the containing action set</param>
        /// <returns>resulting value</returns>
        protected abstract Task<(bool Continue, object Result)> ExecuteInternalAsync(
            IExecuteContext context,
            ActionRefCore actionRef,
            object model);
    }
}
