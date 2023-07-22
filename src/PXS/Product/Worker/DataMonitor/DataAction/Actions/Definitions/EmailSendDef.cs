// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.Actions
{
    using System.Net.Mail;

    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.PrivacyServices.Common.TemplateBuilder;

    /// <summary>
    ///     definition of an email sending action
    /// </summary>
    public class EmailSendDef : IValidatable
    {
        /// <summary>
        ///     Gets or sets the template to be expanded into the email subject
        /// </summary>
        public TemplateRef Subject { get; set; }

        /// <summary>
        ///     Gets or sets the template to be expanded into the email body
        /// </summary>
        /// <remarks>the body is assumed to be HTML</remarks>
        public TemplateRef Body { get; set; }

        /// <summary>
        ///     Gets or sets the priority of the mail
        /// </summary>
        /// <remarks>
        ///     Some mail providers do not accept a priority.  In these cases, the priority is ignored.
        /// </remarks>
        public MailPriority Priority { get; set; }

        /// <summary>
        ///     Gets or sets the from address for the mail
        /// </summary>
        /// <remarks>
        ///     Some mail providers do not accept from addresses and instead use a fixed from address based on the authN
        ///      identity used to connect to the providers. In these cases, the reply to address is ignored.
        /// </remarks>
        public string ReplyToAddress { get; set; }

        /// <summary>
        ///     Gets or sets the from display name for the mail
        /// </summary>
        /// <remarks>
        ///     Some mail providers do not accept from addresses and instead use a fixed from address based on the authN
        ///      identity used to connect to the providers. In these cases, the from display name is ignored.
        /// </remarks>
        public string FromDisplayName { get; set; }

        /// <summary>
        ///     Gets or sets the from address for the mail
        /// </summary>
        /// <remarks>
        ///     Some mail providers do not accept from addresses and instead use a fixed from address based on the authN
        ///      identity used to connect to the providers. In these cases, the from address is ignored.
        /// </remarks>
        public string FromAddress { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether to ignore simulation mode and send email regardless
        /// </summary>
        /// <remarks>
        ///     This has no effect if the action is not running in simulation mode; in this case, email is sent as normal
        /// </remarks>
        public bool IgnoreSimulationMode { get; set; }

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

            VerifyTemplate(nameof(this.Subject), this.Subject);
            VerifyTemplate(nameof(this.Body), this.Body);

            return result;
        }
    }
}
