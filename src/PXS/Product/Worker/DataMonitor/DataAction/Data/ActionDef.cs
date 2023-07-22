// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.Data
{
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;

    /// <summary>
    ///     a template definition
    /// </summary>
    public class ActionDef : IValidatable
    {
        /// <summary>
        ///     Gets or sets the action type
        /// </summary>
        /// <remarks>
        ///     the action type indicates how the Json field is to be parsed, so each type defines the valid syntax
        ///      of the field
        /// </remarks>
        public string Type { get; set; }

        /// <summary>
        ///     Gets or sets the unique tag for this action
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        ///     Gets or sets the action definition
        /// </summary>
        public object Def { get; set; }

        /// <summary>
        ///     Validates the specified ActionRef
        /// </summary>
        /// <param name="context">parse context to log errors into</param>
        /// <returns>true if no errors were found; false otherwise</returns>
        public bool ValidateAndNormalize(IContext context)
        {
            bool result = true;

            this.Type = this.Type?.Trim();
            if (string.IsNullOrWhiteSpace(this.Type))
            {
                context.LogError("the action type is null or empty");
                result = false;
            }

            this.Tag = this.Tag?.Trim();
            if (string.IsNullOrWhiteSpace(this.Tag))
            {
                context.LogError("the action tag is null or empty");
                result = false;
            }

            return result;
        }

        /// <summary>
        ///     returns a string that represents this instance
        /// </summary>
        /// <returns>string that represents this instance</returns>
        public override string ToString()
        {
            return $"{this.Type}: {this.Tag}";
        }
    }
}