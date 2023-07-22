// ---------------------------------------------------------------------------
// <copyright file="ModelBuildAction.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.Actions
{
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Data;

    /// <summary>
    ///     action that allows you to modify the current model without performing any other operation
    /// </summary>
    public class ModelBuildAction : ActionOp<object>
    {
        public const string ActionType = "MODELBUILD-TRANSFORM";

        /// <summary>
        ///     Initializes a new instance of the Action{TDef} class
        /// </summary>
        /// <param name="modelManipulator">model manipulator</param>
        public ModelBuildAction(IModelManipulator modelManipulator) : 
            base(modelManipulator)
        {
        }

        /// <summary>
        ///     Gets the action type
        /// </summary>
        public override string Type => ModelBuildAction.ActionType;

        /// <summary>
        ///     Gets a value indicating whether the action has a definition object
        /// </summary>
        protected override DefinitionMode DefinitionMode => DefinitionMode.Forbidden;

        /// <summary>
        ///     Initializes a new instance of the Action class
        /// </summary>
        /// <param name="context">execution context</param>
        /// <param name="actionRef">action reference</param>
        /// <param name="model">model from previous actions in the containing action set</param>
        /// <returns>resulting value</returns>
        protected override Task<(bool Continue, object Result)> ExecuteInternalAsync(
            IExecuteContext context, 
            ActionRefCore actionRef, 
            object model)
        {
            return Task.FromResult((true, model));
        }
    }
}
