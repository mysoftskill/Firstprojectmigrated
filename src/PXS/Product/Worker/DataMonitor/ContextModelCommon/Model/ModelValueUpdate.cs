// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.DataModel
{
    using Newtonsoft.Json;

    /// <summary>
    ///     Instructions for how to fetch a model value from a source model and the mode for how to add it to another model
    /// </summary>
    public class ModelValueUpdate : ModelValue
    {
        /// <summary>
        ///     Gets or sets how to merge the result into the model
        /// </summary>
        public MergeMode Mode { get; set; }

        /// <summary>
        ///     Gets the mode to use when writing to the target model
        /// </summary>
        /// <returns>resulting value</returns>
        [JsonIgnore]
        public override MergeMode MergeMode => this.Mode;
    }
}
