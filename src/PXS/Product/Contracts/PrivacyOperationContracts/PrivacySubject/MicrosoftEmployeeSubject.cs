// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.PrivacyOperation.Contracts.PrivacySubject
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    ///     Defines Microsoft employee subject.
    /// </summary>
    [Json.AllowSerialization]
    public class MicrosoftEmployeeSubject : IPrivacySubject
    {
        /// <summary>
        ///     Date of company foundation. Noone can be employed earlier than that.
        /// </summary>
        private static readonly DateTimeOffset microsoftFoundationDate = new DateTimeOffset(1975, 4, 4, 0, 0, 0, TimeSpan.Zero);

        /// <summary>
        ///     Gets or sets list of employee emails.
        /// </summary>
        public IList<string> Emails { get; set; }

        /// <summary>
        ///     Gets or sets employee ID.
        /// </summary>
        public string EmployeeId { get; set; }

        /// <summary>
        ///     Gets or sets the date when user's employment had started.
        /// </summary>
        public DateTimeOffset EmploymentStart { get; set; } = DateTimeOffset.MinValue;

        /// <summary>
        ///     Gets or sets the date when user's employment had ended. 
        ///     If set to null, employee is currently working at the company.
        /// </summary>
        public DateTimeOffset? EmploymentEnd { get; set; } = null;

        /// <inheritdoc cref="IPrivacySubject.Validate"/>.
        public void Validate(SubjectUseContext useContext)
        {
            if (this.Emails == null || !this.Emails.Any())
            {
                throw new PrivacySubjectInvalidException(nameof(this.Emails));
            }

            if (string.IsNullOrWhiteSpace(this.EmployeeId))
            {
                throw new PrivacySubjectInvalidException(nameof(this.EmployeeId));
            }

            if (this.EmploymentStart < microsoftFoundationDate 
                || (this.EmploymentEnd.HasValue && this.EmploymentStart.UtcDateTime.Date > this.EmploymentEnd.Value.UtcDateTime.Date))
            {
                throw new PrivacySubjectInvalidException(nameof(this.EmploymentStart));
            }

            if (this.EmploymentEnd.HasValue && this.EmploymentEnd.Value.UtcDateTime.Date > DateTimeOffset.UtcNow.Date)
            {
                throw new PrivacySubjectInvalidException(nameof(this.EmploymentEnd));
            }
        }

        public void Validate(SubjectUseContext useContext, bool useEmailOnlyManadatoryRule)
        { }
    }
}
