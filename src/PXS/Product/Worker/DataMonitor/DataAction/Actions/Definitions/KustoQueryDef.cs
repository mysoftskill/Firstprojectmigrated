// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.Actions
{
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.PrivacyServices.Common.TemplateBuilder;

    /// <summary>
    ///     definition of a Kusto query action
    /// </summary>
    public class KustoQueryDef : IValidatable
    {
        /// <summary>
        ///     Gets or sets the query to execute
        /// </summary>
        public TemplateRef Query { get; set; }

        /// <summary>
        ///     Gets or sets cluster URL to use
        /// </summary>
        public string ClusterUrl { get; set; }

        /// <summary>
        ///     Gets or sets the database to use
        /// </summary>
        public string Database { get; set; }

        /// <summary>
        ///     Validates the argument object and logs any errors to the context
        /// </summary>
        /// <param name="context">execution context</param>
        /// <returns>true if the object validated successfully; false otherwise</returns>
        public bool ValidateAndNormalize(IContext context)
        {
            bool result;

            if (this.Query == null)
            {
                context.LogError("query template must be specified");
                result = false;
            }
            else
            {
                context.PushErrorIntroMessage(() => "Errors were found validating the Query template:\n");
                result = this.Query.ValidateAndNormalize(context);
                context.PopErrorIntroMessage();
            }

            if (string.IsNullOrWhiteSpace(this.ClusterUrl))
            {
                context.LogError("non-empty cluster URL must be specified");
                result = false;
            }

            if (string.IsNullOrWhiteSpace(this.Database))
            {
                context.LogError("non-empty database must be specified");
                result = false;
            }

            return result;
        }
    }
}
