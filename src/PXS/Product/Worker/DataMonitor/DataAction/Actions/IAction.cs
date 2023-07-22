// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.Actions
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Data;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Store;

    /// <summary>
    ///     the result of executing an action
    /// </summary>
    public class ExecuteResult
    {
        /// <summary>
        ///     Initializes a new instance of the ExecuteResult class
        /// </summary>
        /// <param name="continue">
        ///     true if processing should continue for the current action set or false to stop and return to the parent action
        ///     false if it should stop
        /// </param>
        public ExecuteResult(
            bool @continue)
        {
            this.Continue = @continue;
        }

        /// <summary>
        ///     Gets a value indicating whether execution should stop after this action or continue
        /// </summary>
        /// <remarks>
        ///     true if processing should continue for the current action set or false to stop and return to the parent action
        ///     false if it should stop
        /// </remarks>
        public bool Continue { get; }
    }

    /// <summary>
    ///     contact for data action objects
    /// </summary>
    public interface IAction
    {
        /// <summary>
        ///     Gets the action type
        /// </summary>
        string Type { get; }

        /// <summary>
        ///     Gets the action tag
        /// </summary>
        string Tag { get; }

        /// <summary>
        ///     Extracts the first phase of building the action: parses the specified action definition and does static
        ///      validation of the definition
        /// </summary>
        /// <param name="context">parse context</param>
        /// <param name="factory">action factory</param>
        /// <param name="tag">action tag</param>
        /// <param name="definition">action definition</param>
        /// <remarks>
        /// The action definition can either be a JToken containing the semi-deserialized representation of the action
        /// definition or a ActionDef structure containing a JSON text representation of the definition
        /// </remarks>
        bool ParseAndProcessDefinition(
            IParseContext context,
            IActionFactory factory,
            string tag,
            object definition);

        /// <summary>
        ///     Extracts the second phase of building the action: once all actions are parsed and added to the store,
        ///      this instructs container actions to fetch referenced actions from the store and parse inline definitions
        /// </summary>
        /// <param name="context">parse context</param>
        /// <param name="store">action store</param>
        /// <remarks>
        /// The action definition can either be a JToken containing the semi-deserialized representation of the action
        /// definition or a ActionDef structure containing a JSON text representation of the definition
        /// This method is called after all store-based actions have been parsed, so it is an error if any referenced
        /// actions cannot be found
        /// </remarks>
        bool ExpandDefinition(
            IParseContext context,
            IActionFetcher store);

        /// <summary>
        ///     Validates that the collection of a reference's parameter set for this action is correct. Recursively
        ///      validates contained actions
        /// </summary>
        /// <param name="context">parse context</param>
        /// <param name="parameters">parameters found in reference</param>
        /// <returns>list containing the set of errors found</returns>
        /// <remarks>
        /// This does not do any variable expansion or other runtime actions- it just validates that the parameter
        /// set is well formed and contains expected parameter names
        /// </remarks>
        bool Validate(
            IParseContext context,
            IDictionary<string, ModelValue> parameters);

        /// <summary>
        ///     Executes the action using the specified input
        /// </summary>
        /// <param name="context">execution context</param>
        /// <param name="actionRef">action reference triggering the call</param>
        /// <param name="model">input data set</param>
        /// <returns>execution result</returns>
        Task<ExecuteResult> ExecuteAsync(
            IExecuteContext context,
            ActionRefCore actionRef,
            object model);
    }
}
