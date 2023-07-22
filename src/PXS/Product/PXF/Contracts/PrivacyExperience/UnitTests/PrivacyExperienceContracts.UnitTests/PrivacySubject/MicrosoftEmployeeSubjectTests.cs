// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperienceContracts.UnitTests.PrivacySubject
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Linq;

    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.PrivacySubject;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class MicrosoftEmployeeSubjectTests : AltSubjectTestCaseBase
    {
        [TestMethod]
        public void MicrosoftEmployeeSubject_Validate_Throws_EmailsNull()
        {
            var subject = new MicrosoftEmployeeSubject
            {
                EmployeeId = "employee id",
                EmploymentStart = DateTimeOffset.UtcNow.AddDays(-10)
            };

            this.Assert_PrivacySubjectInvalidException_PropertyName(nameof(MicrosoftEmployeeSubject.Emails), subject);
        }

        [TestMethod]
        public void MicrosoftEmployeeSubject_Validate_Throws_EmailsEmpty()
        {
            var subject = new MicrosoftEmployeeSubject
            {
                Emails = new List<string>(),
                EmployeeId = "employee id",
                EmploymentStart = DateTimeOffset.UtcNow.AddDays(-10)
            };

            this.Assert_PrivacySubjectInvalidException_PropertyName(nameof(MicrosoftEmployeeSubject.Emails), subject);
        }

        [TestMethod]
        public void MicrosoftEmployeeSubject_Validate_Throws_EmployeeIdNull()
        {
            var subject = new MicrosoftEmployeeSubject
            {
                Emails = new List<string>() { "qwe@asd.com" },
                EmploymentStart = DateTimeOffset.UtcNow.AddDays(-10)
            };

            this.Assert_PrivacySubjectInvalidException_PropertyName(nameof(MicrosoftEmployeeSubject.EmployeeId), subject);
        }

        [TestMethod]
        public void MicrosoftEmployeeSubject_Validate_Throws_EmployeeIdEmpty()
        {
            var subject = new MicrosoftEmployeeSubject
            {
                Emails = new List<string>() { "qwe@asd.com" },
                EmployeeId = string.Empty,
                EmploymentStart = DateTimeOffset.UtcNow.AddDays(-10)
            };

            this.Assert_PrivacySubjectInvalidException_PropertyName(nameof(MicrosoftEmployeeSubject.EmployeeId), subject);
        }

        [TestMethod]
        public void MicrosoftEmployeeSubject_Validate_Throws_EmploymentStartTooEarly()
        {
            var subject = new MicrosoftEmployeeSubject
            {
                Emails = new List<string>() { "qwe@asd.com" },
                EmployeeId = "employee id",
                EmploymentStart = new DateTimeOffset(1975, 4, 3, 0, 0, 0, TimeSpan.Zero)
            };

            this.Assert_PrivacySubjectInvalidException_PropertyName(nameof(MicrosoftEmployeeSubject.EmploymentStart), subject);
        }

        [TestMethod]
        public void MicrosoftEmployeeSubject_Validate_Throws_EmploymentStartAfterEnd()
        {
            var subject = new MicrosoftEmployeeSubject
            {
                Emails = new List<string>() { "qwe@asd.com" },
                EmployeeId = "employee id",
                EmploymentStart = DateTimeOffset.UtcNow,
                EmploymentEnd = DateTimeOffset.UtcNow.AddDays(-1)
            };

            this.Assert_PrivacySubjectInvalidException_PropertyName(nameof(MicrosoftEmployeeSubject.EmploymentStart), subject);
        }

        [TestMethod]
        public void MicrosoftEmployeeSubject_Validate_Throws_EmploymentEndLaterThanNow()
        {
            var subject = new MicrosoftEmployeeSubject
            {
                Emails = new List<string>() { "qwe@asd.com" },
                EmployeeId = "employee id",
                EmploymentStart = DateTimeOffset.UtcNow.AddYears(-3),
                EmploymentEnd = DateTimeOffset.UtcNow.AddDays(1)
            };

            this.Assert_PrivacySubjectInvalidException_PropertyName(nameof(MicrosoftEmployeeSubject.EmploymentEnd), subject);
        }
    }
}
