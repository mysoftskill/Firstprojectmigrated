// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.Actions
{
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Data;

    /// <summary>
    ///     the action extracts a template ref from the query body and 
    /// </summary>
    public sealed class ConstQueryAction : ActionOp<object>
    {
        public const string ActionType = "MODELBUILD-CONST";

        private Task<(bool Continue, object Result)> cachedResult;

        /// <summary>
        ///     Initializes a new instance of the ConstJsonQueryAction class
        /// </summary>
        /// <param name="modelManipulator">model manipulator</param>
        public ConstQueryAction(IModelManipulator modelManipulator) : 
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
            object definition)
        {
            this.cachedResult = Task.FromResult((true, this.ModelManipulator.TransformFrom(definition)));
            return base.ProcessAndStoreDefinition(context, factory, definition);
        }

        /// <summary>
        ///     Gets the action type
        /// </summary>
        public override string Type => ConstQueryAction.ActionType;

        /// <summary>
        ///     Executes the action using the specified input
        /// </summary>
        /// <param name="context">execution context</param>
        /// <param name="actionRef">action reference</param>
        /// <param name="model">model from previous actions in the containing action set</param>
        /// <returns>execution result</returns>
        protected override Task<(bool Continue, object Result)> ExecuteInternalAsync(
            IExecuteContext context,
            ActionRefCore actionRef,
            object model)
        {
            return this.cachedResult;
        }
    }
}
