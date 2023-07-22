// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.Actions
{
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;

    public enum LoopResultCondition
    {
        /// <summary>
        ///     always return true regardless of what any iteration of the loop returns
        /// </summary>
        AlwaysTrue = 0,

        /// <summary>
        ///     return true unless any iteration of the loop returns false
        /// </summary>
        FalseIfAny,

        /// <summary>
        ///     return true unless all iterations of the loop return false
        /// </summary>
        FalseIfAll,
    }

    /// <summary>
    ///     definition of a foreach action
    /// </summary>
    public class ForeachActionSetDef : ActionSetDef
    {
        /// <summary>
        ///     Gets or sets the model mode to use for each iteration of the loop
        /// </summary>
        /// <remarks>
        ///     for the purposes of each loop iteration, the 'input' model is what was defined as the input from 
        /// </remarks>
        public ModelMode LoopModelMode { get; set; }

        /// <summary>
        ///     Gets or sets the parameter collection
        /// </summary>
        /// <remarks>
        ///     this must be null or empty if LoopModelModel is 'Input'
        ///     this may be null or empty if LoopModelModel is 'Local', which indicates an empty model is to be used for the action
        ///       set
        /// </remarks>
        public IDictionary<string, ModelValue> LoopModelTransform { get; set; }

        /// <summary>
        ///     Gets or sets the name of the field in the data model that the actions results will be saved to
        /// </summary>
        public IDictionary<string, ModelValueUpdate> LoopResultTransform { get; set; }

        /// <summary>
        ///     Gets or sets loop result condition
        /// </summary>
        /// <remarks>
        ///     ReturnNotContinueOnEmpty has precedence over LoopResultCondition, so if ReturnNotContinueOnEmpty is set to true
        ///      and the loop collection is empty, Execute will return false even if LoopResultCondition is AlwaysTrue
        /// </remarks>
        public LoopResultCondition LoopResultCondition { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether to return false for Continue if the input collection was empty
        /// </summary>
        /// <remarks>
        ///     ReturnNotContinueOnEmpty has precedence over LoopResultCondition, so if ReturnNotContinueOnEmpty is set to true
        ///      and the loop collection is empty, Execute will return false even if LoopResultCondition is AlwaysTrue
        /// </remarks>
        public bool ReturnNotContinueOnEmpty { get; set; }

        /// <summary>
        ///     Validates the argument object and logs any errors to the context
        /// </summary>
        /// <param name="context">execution context</param>
        /// <returns>true if the object validated successfully; false otherwise</returns>
        public override bool ValidateAndNormalize(IContext context)
        {
            bool result = base.ValidateAndNormalize(context);

            if (this.LoopModelMode == ModelMode.Input)
            {
                if (this.LocalModelTransform?.Count > 0)
                {
                    context.LogError("input model mode is Input, but a local model transform was found");
                    result = false;
                }
            }
            else if (this.LoopModelMode != ModelMode.Local)
            {
                context.LogError("unknown model mode [{" + this.LocalModelMode.ToString() + "}] found");
                result = false;
            }

            context.PushErrorIntroMessage(() => "Errors were found validating the LoopModelTransform:\n");
            result = ModelUtilites.ValidateModelValueMap(context, this.LoopModelTransform) && result;
            context.PopErrorIntroMessage();

            return result;
        }
    }
}
