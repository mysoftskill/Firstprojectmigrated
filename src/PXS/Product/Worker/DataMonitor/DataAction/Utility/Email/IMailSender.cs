// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.Utility.Email
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    ///     contract for objects that can send email messages
    /// </summary>
    public interface IMailSender
    {
        /// <summary>
        ///     Sends an email message
        /// </summary>
        /// <param name="cancellationToken">cancellation token</param>
        /// <param name="message">message to send</param>
        /// <param name="threadIndex">thread index header value</param>
        /// <returns>true if the message was sent successfully, false otherwise</returns>
        Task<bool> SendEmailAsync(
            CancellationToken cancellationToken,
            EmailMessage message,
            string threadIndex);
    }
}