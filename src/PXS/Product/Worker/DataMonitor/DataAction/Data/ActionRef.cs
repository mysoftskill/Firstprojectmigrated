// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.Data
{
    using System;

    using Microsoft.PrivacyServices.Common.Context;

    /// <summary>
    ///     an action reference
    /// </summary>
    public class ActionRef : ActionRefCore
    {
        /// <summary>
        ///     Gets or sets a tag to use to fetch the action from the action store
        /// </summary>
        public override string Tag
        {
            get => this.Inline?.Tag ?? base.Tag;

            set
            {
                if (this.Inline == null)
                {
                    base.Tag = value?.Trim();
                    return;
                }

                throw new InvalidOperationException(
                    "Cannot set the 'Tag' property directly on a ActionRef with an inline action");
            }
        }

        /// <summary>
        ///     Gets or sets the unique id for this runnable reference
        /// </summary>
        /// <remarks>
        ///     only one instance of a task with a given id can run at the same time. If two references refer to the same
        ///      action definition but have different ids, then are allowed to run at the same time
        /// </remarks>
        public string Id { get; set; }

        /// <summary>
        ///     Gets or sets inline action
        /// </summary>
        public ActionDef Inline { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating the relative execution order when multple actions exist in a single
        ///      action container
        /// </summary>
        public int ExecutionOrder { get; set; }

        /// <summary>
        ///     Validates the specified ActionRef
        /// </summary>
        /// <param name="context">parse context to log errors into</param>
        /// <returns>true if the object validated ok; false otherwise</returns>
        public override bool ValidateAndNormalize(IContext context)
        {
            bool result;

            context.OnActionStart(ActionType.Validate, "REF##" + this.Tag);

            result = base.ValidateAndNormalize(context);

            if (this.Inline?.ValidateAndNormalize(context) == false)
            {
                result = false;
            }

            context.OnActionEnd();

            return result;
        }
        
        /// <summary>
        ///     returns a string that represents this instance
        /// </summary>
        /// <returns>string that represents this instance</returns>
        public override string ToString()
        {
            string result = this.Inline != null ?
                "Reference to inline action " + this.Inline.ToString() :
                "Reference to store action " + this.Tag;

            if (string.IsNullOrWhiteSpace(this.Description) == false)
            {
                result += ": " + this.Description;
            }

            return result;
        }
    }
}
