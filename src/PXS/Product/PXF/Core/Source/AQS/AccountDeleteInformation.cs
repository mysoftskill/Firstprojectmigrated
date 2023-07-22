// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.AQS
{
    using System;

    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Privacy.Core.PrivacyCommand;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    using SocialAccessorV4;

    /// <summary>
    ///     Contains information from a User Delete event
    /// </summary>
    public class AccountDeleteInformation
    {
        private long puid;

        public string Anid { get; private set; }

        /// <summary>
        ///     Gets or sets the CID
        /// </summary>
        public long Cid { get; set; }

        /// <summary>
        ///     Gets the auto-generated Command ID for the Delete
        /// </summary>
        public Guid CommandId { get; }

        /// <summary>
        ///     Gets or sets the correlation vector.
        /// </summary>
        public CorrelationVector CorrelationVector { get; }

        /// <summary>
        ///     Gets the GDPR Verifier token
        /// </summary>
        public string GdprVerifierToken { get; set; }

        /// <summary>
        ///     Gets or sets the pre verifier token
        /// </summary>
        public string PreVerifierToken { get; set; }

        /// <summary>
        ///     Gets the last login if the account was aged out
        /// </summary>
        public DateTimeOffset? LastLogin { get; set; }

        public string Opid { get; private set; }

        /// <summary>
        ///     Gets or sets the PUID
        /// </summary>
        public long Puid
        {
            get => this.puid;
            set
            {
                this.puid = value;
                this.Anid = IdConverter.AnidFromPuid((ulong)value);
                this.Opid = IdConverter.OpidFromPuid((ulong)value);
            }
        }

        /// <summary>
        ///     Gets or sets the reason that the account was deleted
        /// </summary>
        public AccountCloseReason Reason { get; set; }

        /// <summary>
        ///     Gets or sets the request unique identifier.
        /// </summary>
        public Guid RequestGuid { get; }

        /// <summary>
        ///     Gets or sets a value indicating whether this <see cref="AccountDeleteInformation" /> is suspended.
        /// </summary>
        public bool IsSuspended { get; set; } = false;

        /// <summary>
        ///     Gets the time stamp of when the event was dequeued
        /// </summary>
        public DateTimeOffset TimeStamp { get; }

        /// <summary>
        ///     Gets or sets the XUID
        /// </summary>
        public string Xuid { get; set; }

        /// <summary>
        ///     Gets or sets whether or not a successful attempt on adding a xuid occurred or not for this account.
        /// </summary>
        /// <remarks>This is a status bit to keep track of if xbox returned a successful call on this item before.</remarks>
        public bool AddXuidAttemptSucceeded { get; set; } = false;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AccountDeleteInformation" /> class.
        /// </summary>
        public AccountDeleteInformation()
        {
            // Values are created new when they don't exist.
            // They may already exist if the item in queue was updated previously.
            // If it already existed, and a verifier token is present then the command id would not match inside the verifier token, if a new command id is created.
            if (this.CommandId.Equals(default(Guid)))
            {
                this.CommandId = Guid.NewGuid();
            }

            if (this.TimeStamp.Equals(default(DateTimeOffset)))
            {
                this.TimeStamp = DateTimeOffset.UtcNow;
            }

            this.CorrelationVector = Sll.Context?.Vector ?? new CorrelationVector();

            if (this.RequestGuid.Equals(default(Guid)))
            {
                this.RequestGuid = Guid.NewGuid(); // Account closes should always be treated as separate "requests"
            }
        }

        /// <summary>
        ///     Transforms the <see cref="AccountDeleteInformation" /> into a account close request for Event Grid
        /// </summary>
        /// <param name="requester">The requester.</param>
        /// <returns>
        ///     A request to send to PCF
        /// </returns>
        public PrivacyRequest ToAccountCloseRequest(string requester = "Aqs")
        {
            // TODO: this should be constructed through PrivacyRequestConverter in some way, so all the things we need
            // to do, such as the request applicability below, can be centralized better.
            PrivacyRequest request;
            if (this.Reason == AccountCloseReason.UserAccountAgedOut)
            {
                request = new AgeOutRequest { LastActive = this.LastLogin, IsSuspended = this.IsSuspended };
            }
            else
            {
                request = new AccountCloseRequest { AccountCloseReason = this.Reason };
            }

            request.AuthorizationId = $"p:{this.Puid}";
            request.RequestType = (this.Reason == AccountCloseReason.UserAccountAgedOut) ? RequestType.AgeOut : RequestType.AccountClose;
            request.Subject = new MsaSubject
            {
                Anid = this.Anid,
                Opid = this.Opid,
                Cid = this.Cid,
                Puid = this.Puid,
                Xuid = this.Xuid,
            };
            request.RequestId = this.CommandId;
            request.CorrelationVector = this.CorrelationVector.Value;
            request.Timestamp = this.TimeStamp;
            request.RequestGuid = this.RequestGuid;
            request.VerificationToken = this.GdprVerifierToken;
            request.Requester = requester;
            request.Portal = Portals.MsaAccountCloseEventSource;

            PrivacyRequestConverter.UpdateRequestApplicability(request);

            return request;
        }
    }
}
