// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.DataModel
{
    using Microsoft.PrivacyServices.Common.Context;

    using Newtonsoft.Json;

    /// <summary>
    ///     string explaining how to get a value from a model for use in adding it to another model
    /// </summary>
    /// <remarks>
    ///     Both SelectMany and Select must not both be specified
    ///     if both SelectMany and Const are specified, then Const will only be used if SelectMany results in an empty collection
    ///     if both Select and Const are specified, then Const will only be used if Select results in a null value
    /// </remarks>
    public class ModelValue : IValidatable
    {
        /// <summary>
        ///     Gets or sets a selector to extract an array of objects from the source model
        /// </summary>
        public string SelectMany { get; set; }

        /// <summary>
        ///     Gets or sets a selector to extract a data value from a model object
        /// </summary>
        public string Select { get; set; }

        /// <summary>
        ///     Gets or sets a constant value
        /// </summary>
        public object Const { get; set; }

        /// <summary>
        ///     Gets the mode to use when adding the found value into the target model
        /// </summary>
        [JsonIgnore]
        public virtual MergeMode MergeMode => MergeMode.ReplaceExisting;

        /// <summary>
        ///     Validates the object
        /// </summary>
        /// <param name="context">parse context to log errors into</param>
        /// <returns>resulting value</returns>
        public bool ValidateAndNormalize(IContext context)
        {
            bool result = true;

            if (string.IsNullOrWhiteSpace(this.SelectMany) &&
                string.IsNullOrWhiteSpace(this.Select) &&
                this.Const == null)
            {
                context.LogError("at least one of SelectMany, Select, or Const must be specified");
                result = false;
            }

            if (string.IsNullOrWhiteSpace(this.SelectMany) == false &&
                string.IsNullOrWhiteSpace(this.Select) == false)
            {
                context.LogError("if SelectMany is specified, Select should not be specified");
                result = false;
            }

            return result;
        }
    }
}
