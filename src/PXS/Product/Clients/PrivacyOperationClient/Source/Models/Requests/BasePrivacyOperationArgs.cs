﻿// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.PrivacyOperation.Client.Models
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Identity.Client;
    using Microsoft.PrivacyServices.PrivacyOperation.Contracts.PrivacySubject;

    /// <summary>
    ///     Base Privacy Request Args.
    /// </summary>
    public class BasePrivacyOperationArgs
    {
        /// <summary>
        ///     Gets or sets the context.
        /// </summary>
        public string Context { get; set; }

        /// <summary>
        ///     Gets or sets the end time.
        /// </summary>
        public DateTimeOffset? EndTime { get; set; }

        /// <summary>
        ///     Gets or sets the start time.
        /// </summary>
        public DateTimeOffset? StartTime { get; set; }

        /// <summary>
        ///     Gets or sets the subject.
        /// </summary>
        public IPrivacySubject Subject { get; set; }

        /// <summary>
        ///     Gets or sets the access token. This can be:
        ///         1. A PFT token that Office/another AAD partner passes to PXS
        ///         2. A MSA user proxy ticket generated by PCR in PCD cases
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        ///     Gets or sets the correlation vector.
        /// </summary>
        public string CorrelationVector { get; set; }

        /// <summary>
        ///     Gets or sets the user assertion.
        /// </summary>
        public UserAssertion UserAssertion { get; set; }

        /// <summary>
        ///     Gets or sets the data types.
        /// </summary>
        public IList<string> DataTypes { get; set; }
    }
}