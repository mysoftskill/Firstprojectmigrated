// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.Actions
{
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.PrivacyServices.Common.TemplateBuilder;

    /// <summary>
    ///     definition of an incident filing action
    /// </summary>
    public class AgentIncidentCreateDef : IValidatable
    {
        /// <summary>
        ///     Gets or sets the template to be expanded into the incident keywords
        /// </summary>
        public TemplateRef Keywords { get; set; }

        /// <summary>
        ///     Gets or sets the template to be expanded into the incident title
        /// </summary>
        public TemplateRef Title { get; set; }

        /// <summary>
        ///     Gets or sets the template to be expanded into the incident summary / first description entry
        /// </summary>
        public TemplateRef Body { get; set; }

        /// <summary>
        ///     Gets or sets the event name
        /// </summary>
        /// <remarks>this is used by PDMS as the prefix for the incident correlation id</remarks>
        public string EventName { get; set; }

        /// <summary>
        ///     Validates the argument object and logs any errors to the context
        /// </summary>
        /// <param name="context">execution context</param>
        /// <returns>true if the object validated successfully; false otherwise</returns>
        public bool ValidateAndNormalize(IContext context)
        {
            bool result = true;

            void VerifyTemplate(
                string name,
                TemplateRef template)
            {
                if (template == null)
                {
                    context.LogError(name + " template must be specified");
                    result = false;
                }
                else
                {
                    context.PushErrorIntroMessage(() => "Errors were found validating the " + name + " template:\n");
                    result = template.ValidateAndNormalize(context) && result;
                    context.PopErrorIntroMessage();
                }
            }

            VerifyTemplate(nameof(this.Keywords), this.Keywords);
            VerifyTemplate(nameof(this.Title), this.Title);
            VerifyTemplate(nameof(this.Body), this.Body);

            if (string.IsNullOrWhiteSpace(this.EventName))
            {
                context.LogError("non-empty EventName must be provided");
                result = false;
            }

            return result;
        }
    }
}
