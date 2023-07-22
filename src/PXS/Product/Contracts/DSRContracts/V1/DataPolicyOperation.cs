// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.PrivacyServices.DataSubjectRight.Contracts.V1
{
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    ///     DataPolicyOperation status.
    /// </summary>
    public enum DataPolicyOperationStatus
    {
        /// <summary>
        ///     Not started.
        /// </summary>
        NotStarted,

        /// <summary>
        ///     Running.
        /// </summary>
        Running,

        /// <summary>
        ///     Succeeded.
        /// </summary>
        Complete,

        /// <summary>
        ///     Failed.
        /// </summary>
        Failed,

        /// <summary>
        ///     Unknown future value.
        /// </summary>
        UnknownFutureValue
    }

    /// <summary>
    ///     Public class for DataPolicyOperation.
    /// </summary>
    public class DataPolicyOperation
    {
        /// <summary>
        ///     Completed Date Time.
        /// </summary>
        public DateTimeOffset? CompletedDateTime { get; set; }

        /// <summary>
        ///     The progress of operation.
        /// </summary>
        public double Progress { get; set; }

        /// <summary>
        ///     Request Id.
        /// </summary>
        [Key]
        public string Id { get; set; }

        /// <summary>
        ///     Status of the request.
        /// </summary>
        public DataPolicyOperationStatus Status { get; set; }

        /// <summary>
        ///     Storage location for export request.
        /// </summary>
        public string StorageLocation { get; set; }

        /// <summary>
        ///     Submitted Date Time.
        /// </summary>
        public DateTimeOffset SubmittedDateTime { get; set; }

        /// <summary>
        ///     Subject Id.
        /// </summary>
        [Required]
        public string UserId { get; set; }
    }
}
