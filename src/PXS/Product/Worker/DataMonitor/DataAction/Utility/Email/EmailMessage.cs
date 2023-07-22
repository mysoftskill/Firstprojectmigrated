// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.Utility.Email
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Mail;

    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;

    /// <summary>
    ///     represents an email message
    /// </summary>
    public class EmailMessage : IValidatable
    {
        /// <summary>
        ///     Gets or sets the mail subject
        /// </summary>
        public string Subject { get; set; }

        /// <summary>
        ///     Gets or sets body
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        ///     Gets or sets the list of addresses on the to line
        /// </summary>
        public ICollection<string> ToAddresses { get; set; }

        /// <summary>
        ///     Gets or sets the list of addresses on the cc line
        /// </summary>
        public ICollection<string> CcAddresses { get; set; }

        /// <summary>
        ///     Gets or sets the from address
        /// </summary>
        public string FromDisplayText { get; set; }

        /// <summary>
        ///     Gets or sets the from address
        /// </summary>
        public string FromAddress { get; set; }

        /// <summary>
        ///     Gets or sets the reply to address
        /// </summary>
        public string ReplyTo { get; set; }

        /// <summary>
        ///     Gets or sets the from address
        /// </summary>
        public MailPriority Priority { get; set; }

        /// <summary>
        ///     Validates the email message is well formed
        /// </summary>
        public bool ValidateAndNormalize(IContext context)
        {
            bool result = true;

            if (this.ToAddresses == null || 
                this.ToAddresses.Count == 0 ||
                this.ToAddresses.Any(string.IsNullOrWhiteSpace))
            {
                context.LogError("at least one 'to' address must be specified and all must be non-empty");
                result = false;
            }

            if (this.CcAddresses != null && this.CcAddresses.Any(string.IsNullOrWhiteSpace))
            {
                context.LogError("all specified 'cc' addresses must be non-empty");
                result = false;
            }

            if (string.IsNullOrWhiteSpace(this.Subject))
            {
                context.LogError("non-empty Subject must be specified");
                result = false;
            }

            // body is intentionally not required as subject-only messages are not uncommon

            return result;
        }
    }
}
