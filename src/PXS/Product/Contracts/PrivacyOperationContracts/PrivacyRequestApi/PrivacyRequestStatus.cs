// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.PrivacyServices.PrivacyOperation.Contracts
{
    using System;
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;

    /// <summary>
    ///     A privacy request status object.
    /// </summary>
    public class PrivacyRequestStatus
    {
        /// <summary>
        ///     When the request was completed
        /// </summary>
        public DateTimeOffset CompletedTime { get; }

        /// <summary>
        ///     The percentage of agents that completed successfully.
        /// </summary>
        public double CompletionSuccessRate { get; }

        /// <summary>
        ///     The context of the request
        /// </summary>
        public string Context { get; }

        /// <summary>
        ///     The data types for the request.
        /// </summary>
        public IList<string> DataTypes { get; }

        /// <summary>
        ///     The destination uri of the request
        /// </summary>
        public Uri DestinationUri { get; }

        /// <summary>
        ///     The id of the request
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        ///     The type of request.
        /// </summary>
        public PrivacyRequestType RequestType { get; }

        /// <summary>
        ///     The state of the request
        /// </summary>
        public PrivacyRequestState State { get; }

        /// <summary>
        ///     The subject of the request.
        /// </summary>
        public IPrivacySubject Subject { get; }

        /// <summary>
        ///     The time the request was submitted.
        /// </summary>
        public DateTimeOffset SubmittedTime { get; }

        /// <summary>
        ///     Create a new PrivacyRequest
        /// </summary>
        public PrivacyRequestStatus(
            Guid id,
            PrivacyRequestType requestType,
            DateTimeOffset submittedTime,
            DateTimeOffset completedTime,
            IPrivacySubject subject,
            IList<string> dataTypes,
            string context,
            PrivacyRequestState state,
            Uri destinationUri,
            double completionSuccessRate)
        {
            this.Id = id;
            this.RequestType = requestType;
            this.SubmittedTime = submittedTime;
            this.CompletedTime = completedTime;
            this.Subject = subject;
            this.DataTypes = dataTypes;
            this.Context = context;
            this.State = state;
            this.DestinationUri = destinationUri;
            this.CompletionSuccessRate = completionSuccessRate;
        }
    }
}
