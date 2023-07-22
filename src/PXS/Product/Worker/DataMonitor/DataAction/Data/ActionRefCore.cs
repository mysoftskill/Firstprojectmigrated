// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.Data
{
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;

    /// <summary>
    ///     a parsed action reference
    /// </summary>
    public class ActionRefCore : IValidatable
    {
        /// <summary>
        ///     Gets or sets a tag to use to fetch the action from the action store
        /// </summary>
        public virtual string Tag { get; set; }

        /// <summary>
        ///     Gets or sets a description to be emitted into the operation log when this action is executed
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        ///     Gets or sets a transform that will copy properties from the action result to the input model
        /// </summary>
        public IDictionary<string, ModelValueUpdate> ResultTransform { get; set; }

        /// <summary>
        ///     Gets or sets the parameter collection
        /// </summary>
        public IDictionary<string, ModelValue> ArgTransform { get; set; }

        /// <summary>
        ///     Validates the specified ActionRef
        /// </summary>
        /// <param name="context">parse context to log errors into</param>
        /// <returns>true if the object validated ok; false otherwise</returns>
        public virtual bool ValidateAndNormalize(IContext context)
        {
            bool result = true;

            if (string.IsNullOrWhiteSpace(this.Tag))
            {
                context.LogError("the action tag is null or empty");
                result = false;
            }

            // allow null to mean 'no description', but if it's non-null, require something other than whitespace
            this.Description = this.Description?.Trim();
            if (this.Description != null && this.Description.Length == 0)
            {
                context.LogError("the action description is empty or just whitespace");
                result = false;
            }

            context.PushErrorIntroMessage(() => "Error validating reference arguments:");

            result = ModelUtilites.ValidateModelValueMap(context, this.ArgTransform) && result;

            context.PopErrorIntroMessage();
            context.PushErrorIntroMessage(() => "Error validating reference result transform:");

            result = ModelUtilites.ValidateModelValueMap(context, this.ResultTransform) && result;

            context.PopErrorIntroMessage();

            return result;
        }

        /// <summary>
        ///     returns a string that represents this instance
        /// </summary>
        /// <returns>string that represents this instance</returns>
        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(this.Description) ?
                $"[{this.Tag}] reference" :
                $"[{this.Tag}] reference: {this.Description}";
        }
    }
}
