// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.Utility.Email
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Mail;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.PrivacyServices.Common.Exceptions;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     sends an email mesage via SMTP
    /// </summary>
    public class SmtpMailSender : IMailSender
    {
        public const string SenderType = "SMTP";

        private const string ThreadIndexHeaderName = "Thread-Index";

        private readonly ISmtpConfig config;
        private readonly TimeSpan timeout;
        private readonly ILogger logger;

        /// <summary>
        ///     Initializes a new instance of the SmtpMailSender class
        /// </summary>
        public SmtpMailSender(
            ISmtpConfig config,
            ILogger logger)
        {
            ArgumentCheck.ThrowIfNull(config, nameof(config));
            ArgumentCheck.ThrowIfNullEmptyOrWhiteSpace(config.TransmitTimeout, nameof(config.TransmitTimeout));

            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            this.config = config;

            try
            {
                this.timeout = TimeSpan.Parse(config.TransmitTimeout);
            }
            catch (FormatException)
            {
                this.timeout = TimeSpan.FromSeconds(30);
            }
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
            ICollection<string> toAddrs;
            ICollection<string> ccAddrs;
            string fromAddr;
            string fromName;
            string replyTo;
            string subject;
            string body;

            ICollection<string> SanitizeList(IEnumerable<string> input)
            {
                return input?.Where(o => o?.Length > 0).Select(o => o.Trim()).ToList() ?? ListHelper.EmptyList<string>();
            }

            ArgumentCheck.ThrowIfNull(message, nameof(message));
            ArgumentCheck.ThrowIfNullEmptyOrWhiteSpace(message.FromAddress, "message.FromAddress");

            toAddrs = SanitizeList(message.ToAddresses);
            ccAddrs = SanitizeList(message.ToAddresses);

            if (toAddrs.Count == 0 && ccAddrs.Count == 0)
            {
                throw new InvalidPathException("Must specify at least one 'to' or 'cc' address");
            }

            fromAddr = message.FromAddress?.Trim() ?? this.config.DefaultFromAddress.Trim();
            fromName = string.IsNullOrWhiteSpace(message.FromDisplayText) ? fromAddr : message.FromDisplayText.Trim();

            replyTo = string.IsNullOrWhiteSpace(message.ReplyTo) ? fromAddr : message.ReplyTo.Trim();

            subject = message.Subject?.Trim() ?? string.Empty;
            body = message.Body ?? string.Empty;
            
            using (MailMessage mailMsg = new MailMessage())
            {
                if (string.IsNullOrWhiteSpace(threadIndex) == false)
                {
                    mailMsg.Headers.Add(SmtpMailSender.ThreadIndexHeaderName, threadIndex);
                }

                this.PopulateAddressGroup(toAddrs, mailMsg.To);
                this.PopulateAddressGroup(ccAddrs, mailMsg.CC);

                mailMsg.ReplyToList.Add(this.CreateMailAddress(replyTo, null));
                mailMsg.IsBodyHtml = true;
                mailMsg.Priority = message.Priority;
                mailMsg.Subject = SmtpMailSender.SanitizeSubject(subject);
                mailMsg.From = this.CreateMailAddress(fromAddr, fromName);
                mailMsg.Body = body;

                foreach (ISmtpServer server in this.config.Servers)
                {
                    if (await this.TransmitToServerAsync(server, mailMsg).ConfigureAwait(false))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        ///     Attempts to send the email via one of the configured servers
        /// </summary>
        /// <param name="server">server</param>
        /// <param name="msg">MSG</param>
        /// <returns>resulting value</returns>
        public async Task<bool> TransmitToServerAsync(
            ISmtpServer server,
            MailMessage msg)
        {
            using (SmtpClient client = new SmtpClient(server.Server, server.Port))
            {
                client.EnableSsl = this.config.UseHttps;
                client.Timeout = this.timeout.Milliseconds;

                if (this.config.UseSspi)
                {
                    client.UseDefaultCredentials = true;
                }
                
                try
                {
                    await client.SendMailAsync(msg).ConfigureAwait(false);
                    return true;
                }
                catch (SmtpException e)
                {
                    this.logger.Error(
                        nameof(SmtpClient),
                        $"Attempt to send to SMTP server {server.Server} failed: {e}");
                }
            }

            return false;
        }
        
        /// <summary>Sanitizes the title</summary>
        /// <param name="title">title to sanitize</param>
        /// <returns>resulting value</returns>
        private static string SanitizeSubject(
            string title)
        {
            return (title != null) ?
                title.Trim().Replace("\r", string.Empty).Replace("\n", string.Empty) :
                string.Empty;
        }

        /// <summary>Creates the mail address</summary>
        /// <param name="addrText">address text</param>
        /// <param name="name">name to use</param>
        /// <returns>resulting value or null</returns>
        private MailAddress CreateMailAddress(
            string addrText,
            string name)
        {
            try
            {
                return new MailAddress(addrText, name ?? addrText, Encoding.ASCII);
            }
            catch (FormatException)
            {
                // TODO: logging + retries
            }

            return null;
        }

        /// <summary>Populates the address group</summary>
        /// <param name="addresses">addresses list</param>
        /// <param name="addressGroup">address group</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Globalization", 
            "CA1308:NormalizeStringsToUppercase",
            Justification = "The address is intended to be an email address, which we want lowercase")]
        private void PopulateAddressGroup(
            ICollection<string> addresses,
            MailAddressCollection addressGroup)
        {
            if (addresses != null && addresses.Count > 0)
            {
                SortedList<string, bool> set = new SortedList<string, bool>();

                // remove duplicates and bogus email addresses
                foreach (string addrText in addresses)
                {
                    if (string.IsNullOrWhiteSpace(addrText) == false)
                    {
                        string addrLower = addrText.ToLowerInvariant();
                        set[addrLower] = true;
                    }
                }

                foreach (string addrText in set.Keys)
                {
                    MailAddress addr = this.CreateMailAddress(addrText, null);
                    if (addr != null)
                    {
                        addressGroup.Add(addr);
                    }
                }
            }
        }
    }
}
