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
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <remarks>
    ///     These unit tests check demographic subject completeness using rules outlined in 
    ///     section "Fulfilling DSRs on Alt-Subject requests" in the document linked below.
    ///     Only minimal amount of permutations is tested, assuming that more data on input 
    ///     is always permitted.
    ///     https://microsoft.sharepoint.com/:w:/r/teams/ngphome/ngpx/execution/_layouts/15/Doc.aspx?sourcedoc=%7B044c0763-aaf4-4add-822b-3be4167e4400%7D&amp;action=edit
    /// </remarks>
    [TestClass]
    public class DemographicSubjectTests : AltSubjectTestCaseBase
    {
        private readonly IList<string> fakeNames = new List<string>() { "qwe" };

        private readonly IList<string> fakeEmails = new List<string>() { "qwe@example.com", "asd@contoso.com" };

        private readonly IList<string> fakePhones = new List<string>() { "1234567890", "+1 206 555 5555" };

        private readonly DemographicSubject.Address fakeAddress = new DemographicSubject.Address()
        {
            StreetNames = new List<string>() { "street1", "street2" },
            Cities = new List<string>() { "city" }
        };

        [TestMethod]
        [ExpectedException(typeof(PrivacySubjectInvalidException))]
        public void DemographicSubject_Validate_Throws_NoPropertiesSet()
        {
            var subject = new DemographicSubject();
            subject.Validate(SubjectUseContext.Any, true);
        }

        [TestMethod]
        public void DemographicSubject_Address_Validate_Throws_InvalidEmails()
        {
            var subject = new DemographicSubject();

            subject.Emails = new List<string>() { "qwe" };
            this.Assert_PrivacySubjectInvalidException_PropertyName(nameof(DemographicSubject.Emails), subject, true);

            subject.Emails = new List<string>() { "two@at@example.com" };
            this.Assert_PrivacySubjectInvalidException_PropertyName(nameof(DemographicSubject.Emails), subject, true);

            subject.Emails = new List<string>() { "@none" };
            this.Assert_PrivacySubjectInvalidException_PropertyName(nameof(DemographicSubject.Emails), subject, true);
        }

        [TestMethod]
        public void DemographicSubject_Address_Validate_Throws_InvalidPhones()
        {
            var subject = new DemographicSubject();

            subject.Emails = new List<string>() { "try@example.com" };
            subject.Phones = new List<string>() { "1234567890123456" };
            this.Assert_PrivacySubjectInvalidException_PropertyName(nameof(DemographicSubject.Phones), subject, true);

            subject.Phones = new List<string>() { "+EXAMPLE" };
            this.Assert_PrivacySubjectInvalidException_PropertyName(nameof(DemographicSubject.Phones), subject, true);
        }

        [TestMethod]
        public void DemographicSubject_Address_Validate_Throws_PostalAddressNoStreetNames()
        {
            var subject = new DemographicSubject()
            {
                Names = this.fakeNames,
                Emails = this.fakeEmails,
                PostalAddress = new DemographicSubject.Address()
                {
                    Cities = new List<string>() { "city" }
                }
            };

            this.Assert_PrivacySubjectInvalidException_PropertyName(nameof(DemographicSubject.Address.StreetNames), subject, true);
        }



        [TestMethod]
        public void DemographicSubject_Address_Validate_Throws_PostalAddressNoCityNames()
        {
            var subject = new DemographicSubject()
            {
                Names = this.fakeNames,
                Emails = this.fakeEmails,
                PostalAddress = new DemographicSubject.Address()
                {
                    StreetNames = new List<string>() { "street" }
                }
            };

            this.Assert_PrivacySubjectInvalidException_PropertyName(nameof(DemographicSubject.Address.Cities), subject, true);
        }

        [TestMethod]
        public void DemographicSubject_Validate_DoesNotThrow_Emails()
        {
            var subject = new DemographicSubject()
            {
                Emails = this.fakeEmails
            };

            subject.Validate(SubjectUseContext.Any, true);
        }

        [TestMethod]
        public void DemographicSubject_Validate_DoesNotThrow_EmailsNames()
        {
            var subject = new DemographicSubject()
            {
                Names = this.fakeNames,
                Emails = this.fakeEmails
            };

            subject.Validate(SubjectUseContext.Any, true);
        }

        [TestMethod]
        public void DemographicSubject_Validate_DoesNotThrow_EmailsPhonesNames()
        {
            var subject = new DemographicSubject()
            {
                Names = this.fakeNames,
                Emails = this.fakeEmails,
                Phones = this.fakePhones
            };

            subject.Validate(SubjectUseContext.Any, true);
        }

        [TestMethod]
        public void DemographicSubject_Validate_DoesNotThrow_PostalAddressEmails()
        {
            var subject = new DemographicSubject()
            {
                Emails = this.fakeEmails,
                PostalAddress = this.fakeAddress
            };

            subject.Validate(SubjectUseContext.Any, true);
        }

        [TestMethod]
        [ExpectedException(typeof(PrivacySubjectIncompleteException))]
        public void DemographicSubject_Validate_Old_Rule_Throws_OnlyPostalAddressSet()
        {
            var subject = new DemographicSubject()
            {
                PostalAddress = this.fakeAddress
            };

            subject.Validate(SubjectUseContext.Any);
        }

        [TestMethod]
        [ExpectedException(typeof(PrivacySubjectIncompleteException))]
        public void DemographicSubject_Validate_Old_Rule_Throws_OnlyNamesSet()
        {
            var subject = new DemographicSubject()
            {
                Names = this.fakeNames
            };

            subject.Validate(SubjectUseContext.Any);
        }

        [TestMethod]
        public void DemographicSubject_Validate_Old_Rule_DoesNotThrow_Phones()
        {
            var subject = new DemographicSubject()
            {
                Phones = this.fakePhones
            };

            subject.Validate(SubjectUseContext.Any);
        }

        [TestMethod]
        public void DemographicSubject_Validate_Old_Rule_DoesNotThrow_PhonesNames()
        {
            var subject = new DemographicSubject()
            {
                Names = this.fakeNames,
                Phones = this.fakePhones
            };

            subject.Validate(SubjectUseContext.Any);
        }

        [TestMethod]
        public void DemographicSubject_Validate_Old_Rule_DoesNotThrow_PostalAddressNames()
        {
            var subject = new DemographicSubject()
            {
                Names = this.fakeNames,
                PostalAddress = this.fakeAddress
            };

            subject.Validate(SubjectUseContext.Any);
        }


        [TestMethod]
        public void DemographicSubject_Validate_Old_Rule_DoesNotThrow_PostalAddressPhones()
        {
            var subject = new DemographicSubject()
            {
                Phones = this.fakePhones,
                PostalAddress = this.fakeAddress
            };

            subject.Validate(SubjectUseContext.Any);
        }

        [TestMethod]
        [ExpectedException(typeof(PrivacySubjectIncompleteException))]
        public void DemographicSubject_ValidateForExport_Old_Rule_Throws_Emails()
        {
            var subject = new DemographicSubject()
            {
                Emails = this.fakeEmails
            };

            subject.Validate(SubjectUseContext.Export);
        }

        [TestMethod]
        [ExpectedException(typeof(PrivacySubjectIncompleteException))]
        public void DemographicSubject_ValidateForExport_Old_Rule_Throws_NamesEmails()
        {
            var subject = new DemographicSubject()
            {
                Names = this.fakeNames,
                Emails = this.fakeEmails
            };

            subject.Validate(SubjectUseContext.Export);
        }

        [TestMethod]
        [ExpectedException(typeof(PrivacySubjectIncompleteException))]
        public void DemographicSubject_ValidateForExport_Old_Rule_Throws_Phones()
        {
            var subject = new DemographicSubject()
            {
                Phones = this.fakePhones
            };

            subject.Validate(SubjectUseContext.Export);
        }

        [TestMethod]
        [ExpectedException(typeof(PrivacySubjectIncompleteException))]
        public void DemographicSubject_ValidateForExport_Old_Rule_Throws_NamesPhones()
        {
            var subject = new DemographicSubject()
            {
                Names = this.fakeNames,
                Phones = this.fakePhones
            };

            subject.Validate(SubjectUseContext.Export);
        }

        [TestMethod]
        [ExpectedException(typeof(PrivacySubjectIncompleteException))]
        public void DemographicSubject_ValidateForExport_Old_Rule_Throws_NamesPostalAddress()
        {
            var subject = new DemographicSubject()
            {
                Names = this.fakeNames,
                PostalAddress = this.fakeAddress
            };

            subject.Validate(SubjectUseContext.Export);
        }

        [TestMethod]
        [ExpectedException(typeof(PrivacySubjectIncompleteException))]
        public void DemographicSubject_ValidateForExport_Old_Rule_Throws_PhonesPostalAddress()
        {
            var subject = new DemographicSubject()
            {
                Phones = this.fakePhones,
                PostalAddress = this.fakeAddress
            };

            subject.Validate(SubjectUseContext.Export);
        }

    }
}
