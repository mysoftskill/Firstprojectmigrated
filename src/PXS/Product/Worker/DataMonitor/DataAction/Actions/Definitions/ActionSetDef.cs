// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.Actions
{
    using System.Collections.Generic;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Data;

    public enum ModelMode
    {
        /// <summary>
        ///     the action set should create a local model to 
        /// </summary>
        Input = 0,

        /// <summary>
        ///     the action set should create a local model for all the actions in the set and use the local initializer to populate
        ///      it before running the actions
        /// </summary>
        Local,
    }

    /// <summary>
    ///     definition of an action set
    /// </summary>
    public class ActionSetDef : IValidatable
    {
        /// <summary>
        ///     Gets or sets an ordered set of actions to execute for the action
        /// </summary>
        public IList<ActionRef> Actions { get; set; }

        /// <summary>
        ///     Gets or sets the model mode to use for the local model that will be fed to each actions in the set
        /// </summary>
        public ModelMode LocalModelMode { get; set; }

        /// <summary>
        ///     Gets or sets the parameter collection
        /// </summary>
        /// <remarks>
        ///     this must be null or empty if LocalModelMode is 'Input'
        ///     this may be null or empty if LocalModelMode is 'Local', which indicates an empty model is to be used for the action
        ///       set
        /// </remarks>
        public IDictionary<string, ModelValue> LocalModelTransform { get; set; }

        /// <summary>
        ///     Validates the argument object and logs any errors to the context
        /// </summary>
        /// <param name="context">execution context</param>
        /// <returns>true if the object validated successfully; false otherwise</returns>
        public virtual bool ValidateAndNormalize(IContext context)
        {
            IDictionary<int, string> indexMap = new Dictionary<int, string>();
            bool result = true;

            if (this.Actions?.Count > 0)
            {
                foreach (ActionRef aref in this.Actions)
                {
                    context.PushErrorIntroMessage(
                        () => "Errors were found validating the " +
                              (aref.Tag ?? "<UNKNOWN>") +
                              " action ref:\n");

                    result = aref.ValidateAndNormalize(context) && result;

                    context.PopErrorIntroMessage();

                    if (indexMap.TryGetValue(aref.ExecutionOrder, out string tagAtIndex))
                    {
                        const string Msg =
                            "the two action refs with tags [{0}] and [{1}] use the same execution order index of {2}";

                        context.LogError(Msg.FormatInvariant(tagAtIndex, aref.Tag, aref.ExecutionOrder));
                        result = false;
                    }
                    else
                    {
                        indexMap[aref.ExecutionOrder] = aref.Tag;
                    }
                }
            }

            if (this.LocalModelMode == ModelMode.Input)
            {
                if (this.LocalModelTransform?.Count > 0)
                {
                    context.LogError("input model mode is Input, but a local model transform was found");
                    result = false;
                }
            }
            else if (this.LocalModelMode != ModelMode.Local)
            {
                context.LogError("unknown model mode [{" + this.LocalModelMode.ToString() + "}] found");
                result = false;
            }

            context.PushErrorIntroMessage(() => "Errors were found validating the LocalModelTransform:\n");
            result = ModelUtilites.ValidateModelValueMap(context, this.LocalModelTransform) && result;
            context.PopErrorIntroMessage();

            return result;
        }
    }
}
