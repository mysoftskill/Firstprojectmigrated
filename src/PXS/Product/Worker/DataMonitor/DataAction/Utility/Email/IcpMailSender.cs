// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.Utility.Email
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.Communications.Client;
    using Microsoft.Membership.Communications.Common.Delivery;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication;

    /// <summary>
    ///     sends an email message via the MUCP messaging platform
    /// </summary>
    public class IcpMailSender : IMailSender
    {
        public const string SenderType = "MUCP";

        private readonly IAadAuthManager authManager;
        private readonly IMucpConfig config;
        private readonly TimeSpan sendTimeout;

        /// <summary>
        ///     Initializes a new instance of the MucpMailSender class
        /// </summary>
        /// <param name="config">MUCP configuration</param>
        /// <param name="authManager">authentication manager</param>
        public IcpMailSender(
            IMucpConfig config,
            IAadAuthManager authManager)
        {
            this.authManager = authManager ?? throw new ArgumentNullException(nameof(authManager));
            this.config = config ?? throw new ArgumentNullException(nameof(config));

            this.sendTimeout = TimeSpan.FromSeconds(config.TimeoutSeconds);
        }

        /// <summary>
        ///     Sends an email message
        /// </summary>
        /// <param name="cancellationToken">cancellation token</param>
        /// <param name="message">message to send</param>
        /// <param name="threadIndex">thread index header value</param>
        /// <returns>true if the message was sent successfully, false otherwise</returns>
        public async Task<bool> SendEmailAsync(
            CancellationToken cancellationToken,
            EmailMessage message, 
            string threadIndex)
        {
            EmailRecipients recipients;
            EmailDelivery delivery;
            Publisher sender;
            string subject;
            string body;

            IList<string> SanitizeList(IEnumerable<string> input)
            {
                return 
                    input?.Where(o => string.IsNullOrWhiteSpace(o) == false).Select(o => o.Trim()).ToList() ?? 
                    ListHelper.EmptyList<string>();
            }

            ArgumentCheck.ThrowIfNull(message, nameof(message));

            recipients = new EmailRecipients
            {
                To = SanitizeList(message.ToAddresses),
                Cc = SanitizeList(message.CcAddresses)
            };

            if (recipients.To.Count == 0 && recipients.Cc.Count == 0)
            {
                throw new ArgumentException("Must specify at least one 'to' or 'cc' address", nameof(message));
            }

            subject = message.Subject?.Trim() ?? string.Empty;
            body = message.Body ?? string.Empty;

            if (string.IsNullOrWhiteSpace(subject) && string.IsNullOrWhiteSpace(body))
            {
                throw new ArgumentException("Must specify at least one of the title or body", nameof(message));
            }

            delivery = new EmailDelivery
            {
                Engine = PassThroughEngine.Instance,
                Source = new InlineSource
                {
                    KeyValuePairs = new Dictionary<string, string>
                    {
                        { "Subject", subject },
                        { "HtmlBody", body },
                    }
                },
            };

            sender = new Publisher("NGP", endpoint: new Uri(this.config.Endpoint));

            await sender
                .CreateRequest()
                .WithAADBearerToken(await this.authManager.GetAccessTokenAsync(this.config.AuthResourceId))
                .WithEventName(this.config.EventId)
                .WithInstanceId(Guid.NewGuid())
                .WithDelivery(delivery)
                .WithRecipient(recipients)
                .SendAsync(this.sendTimeout, cancellationToken);

            return true;
        }
    }
}
