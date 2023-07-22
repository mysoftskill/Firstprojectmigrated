// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.UnitTests
{
    using System;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    ///     Test cases for legal age group restrictions based on privacy spec:
    ///     https://microsoft-my.sharepoint.com/:w:/r/personal/mihern_microsoft_com/_layouts/15/doc2.aspx?sourcedoc=%7B4d694b7e-4286-4c31-a1e6-c7a0387fce6b%7D
    /// </summary>
    [TestClass]
    public class AgeAuthZLegalAgeGroupTests
    {
        private readonly IAgeAuthZRules rules = new AgeAuthZLegalAgeGroup();

        [DataTestMethod]

        // In family - Minors - obo
        [DataRow(AuthType.OnBehalfOf, "jwt", LegalAgeGroup.MinorNoParentalConsentRequired, true, true, true)]
        [DataRow(AuthType.OnBehalfOf, "jwt", LegalAgeGroup.MinorNoParentalConsentRequired, true, false, true)]
        [DataRow(AuthType.OnBehalfOf, "jwt", LegalAgeGroup.MinorWithParentalConsent, true, true, true)]
        [DataRow(AuthType.OnBehalfOf, "jwt", LegalAgeGroup.MinorWithParentalConsent, true, false, true)]
        [DataRow(AuthType.OnBehalfOf, "jwt", LegalAgeGroup.MinorWithoutParentalConsent, true, true, true)]
        [DataRow(AuthType.OnBehalfOf, "jwt", LegalAgeGroup.MinorWithoutParentalConsent, true, false, true)]
        [DataRow(AuthType.OnBehalfOf, "jwt", LegalAgeGroup.NotAdult, true, true, true)]
        [DataRow(AuthType.OnBehalfOf, "jwt", LegalAgeGroup.NotAdult, true, false, true)]

        // In family - Majority age met - obo
        [DataRow(AuthType.OnBehalfOf, "jwt", LegalAgeGroup.Undefined, true, true, true)]
        [DataRow(AuthType.OnBehalfOf, "jwt", LegalAgeGroup.Undefined, true, false, false)]
        [DataRow(AuthType.OnBehalfOf, "jwt", LegalAgeGroup.Adult, true, true, true)]
        [DataRow(AuthType.OnBehalfOf, "jwt", LegalAgeGroup.Adult, true, false, false)]
        [DataRow(AuthType.OnBehalfOf, "jwt", null, true, true, true)]
        [DataRow(AuthType.OnBehalfOf, "jwt", null, true, false, false)]

        // In family - minors - self
        [DataRow(AuthType.MsaSelf, null, LegalAgeGroup.MinorNoParentalConsentRequired, true, true, false)]
        [DataRow(AuthType.MsaSelf, null, LegalAgeGroup.MinorNoParentalConsentRequired, true, false, false)]
        [DataRow(AuthType.MsaSelf, null, LegalAgeGroup.MinorWithParentalConsent, true, true, false)]
        [DataRow(AuthType.MsaSelf, null, LegalAgeGroup.MinorWithParentalConsent, true, false, false)]
        [DataRow(AuthType.MsaSelf, null, LegalAgeGroup.MinorWithoutParentalConsent, true, true, false)]
        [DataRow(AuthType.MsaSelf, null, LegalAgeGroup.MinorWithoutParentalConsent, true, false, false)]
        [DataRow(AuthType.MsaSelf, null, LegalAgeGroup.NotAdult, true, true, false)]
        [DataRow(AuthType.MsaSelf, null, LegalAgeGroup.NotAdult, true, false, false)]

        // In family - Majority age met - self
        [DataRow(AuthType.MsaSelf, null, LegalAgeGroup.Undefined, true, true, true)]
        [DataRow(AuthType.MsaSelf, null, LegalAgeGroup.Undefined, true, false, true)]
        [DataRow(AuthType.MsaSelf, null, LegalAgeGroup.Adult, true, true, true)]
        [DataRow(AuthType.MsaSelf, null, LegalAgeGroup.Adult, true, false, true)]
        [DataRow(AuthType.MsaSelf, null, null, true, true, true)]
        [DataRow(AuthType.MsaSelf, null, null, true, false, true)]

        // Not In family - self
        [DataRow(AuthType.MsaSelf, null, LegalAgeGroup.MinorNoParentalConsentRequired, false, null, true)]
        [DataRow(AuthType.MsaSelf, null, LegalAgeGroup.MinorWithParentalConsent, false, null, true)]
        [DataRow(AuthType.MsaSelf, null, LegalAgeGroup.MinorWithoutParentalConsent, false, null, true)]
        [DataRow(AuthType.MsaSelf, null, LegalAgeGroup.Undefined, false, null, true)]
        [DataRow(AuthType.MsaSelf, null, LegalAgeGroup.Adult, false, null, true)]
        [DataRow(AuthType.MsaSelf, null, LegalAgeGroup.NotAdult, false, null, true)]
        public void ShouldEnforceDeleteRules(AuthType authType, string familyJwt, LegalAgeGroup? ageGroup, bool inFamily, bool? doesConsent, bool canView)
        {
            Assert.AreEqual(
                canView,
                this.rules.CanDelete(new MsaSelfIdentity(null, familyJwt, 0, 4, 1, null, 0, 5, "US", DateTimeOffset.UtcNow, inFamily, authType, ageGroup, doesConsent)));
        }

        [DataTestMethod]

        // In family - Minors - obo
        [DataRow(AuthType.OnBehalfOf, "jwt", LegalAgeGroup.MinorNoParentalConsentRequired, true, true, true)]
        [DataRow(AuthType.OnBehalfOf, "jwt", LegalAgeGroup.MinorNoParentalConsentRequired, true, false, true)]
        [DataRow(AuthType.OnBehalfOf, "jwt", LegalAgeGroup.MinorWithParentalConsent, true, true, true)]
        [DataRow(AuthType.OnBehalfOf, "jwt", LegalAgeGroup.MinorWithParentalConsent, true, false, true)]
        [DataRow(AuthType.OnBehalfOf, "jwt", LegalAgeGroup.MinorWithoutParentalConsent, true, true, true)]
        [DataRow(AuthType.OnBehalfOf, "jwt", LegalAgeGroup.MinorWithoutParentalConsent, true, false, true)]
        [DataRow(AuthType.OnBehalfOf, "jwt", LegalAgeGroup.NotAdult, true, true, true)]
        [DataRow(AuthType.OnBehalfOf, "jwt", LegalAgeGroup.NotAdult, true, false, true)]

        // In family - Majority age met - obo
        [DataRow(AuthType.OnBehalfOf, "jwt", LegalAgeGroup.Undefined, true, true, true)]
        [DataRow(AuthType.OnBehalfOf, "jwt", LegalAgeGroup.Undefined, true, false, false)]
        [DataRow(AuthType.OnBehalfOf, "jwt", LegalAgeGroup.Adult, true, true, true)]
        [DataRow(AuthType.OnBehalfOf, "jwt", LegalAgeGroup.Adult, true, false, false)]
        [DataRow(AuthType.OnBehalfOf, "jwt", null, true, true, true)]
        [DataRow(AuthType.OnBehalfOf, "jwt", null, true, false, false)]

        // In family - minors - self
        [DataRow(AuthType.MsaSelf, null, LegalAgeGroup.MinorNoParentalConsentRequired, true, true, true)]
        [DataRow(AuthType.MsaSelf, null, LegalAgeGroup.MinorNoParentalConsentRequired, true, false, true)]
        [DataRow(AuthType.MsaSelf, null, LegalAgeGroup.MinorWithParentalConsent, true, true, true)]
        [DataRow(AuthType.MsaSelf, null, LegalAgeGroup.MinorWithParentalConsent, true, false, true)]
        [DataRow(AuthType.MsaSelf, null, LegalAgeGroup.MinorWithoutParentalConsent, true, true, true)]
        [DataRow(AuthType.MsaSelf, null, LegalAgeGroup.MinorWithoutParentalConsent, true, false, true)]
        [DataRow(AuthType.MsaSelf, null, LegalAgeGroup.NotAdult, true, true, true)]
        [DataRow(AuthType.MsaSelf, null, LegalAgeGroup.NotAdult, true, false, true)]

        // In family - Majority age met - self
        [DataRow(AuthType.MsaSelf, null, LegalAgeGroup.Undefined, true, true, true)]
        [DataRow(AuthType.MsaSelf, null, LegalAgeGroup.Undefined, true, false, true)]
        [DataRow(AuthType.MsaSelf, null, LegalAgeGroup.Adult, true, true, true)]
        [DataRow(AuthType.MsaSelf, null, LegalAgeGroup.Adult, true, false, true)]
        [DataRow(AuthType.MsaSelf, null, null, true, true, true)]
        [DataRow(AuthType.MsaSelf, null, null, true, false, true)]

        // Not In family - self
        [DataRow(AuthType.MsaSelf, null, LegalAgeGroup.MinorNoParentalConsentRequired, false, null, true)]
        [DataRow(AuthType.MsaSelf, null, LegalAgeGroup.MinorWithParentalConsent, false, null, true)]
        [DataRow(AuthType.MsaSelf, null, LegalAgeGroup.MinorWithoutParentalConsent, false, null, true)]
        [DataRow(AuthType.MsaSelf, null, LegalAgeGroup.Undefined, false, null, true)]
        [DataRow(AuthType.MsaSelf, null, LegalAgeGroup.Adult, false, null, true)]
        [DataRow(AuthType.MsaSelf, null, LegalAgeGroup.NotAdult, false, null, true)]
        public void ShouldEnforceViewRules(AuthType authType, string familyJwt, LegalAgeGroup? ageGroup, bool childInFamily, bool? doesConsent, bool canView)
        {
            Assert.AreEqual(
                canView,
                this.rules.CanView(new MsaSelfIdentity(null, familyJwt, 0, 4, 1, null, 0, 5, "US", DateTimeOffset.UtcNow, childInFamily, authType, ageGroup, doesConsent)));
        }
    }
}
