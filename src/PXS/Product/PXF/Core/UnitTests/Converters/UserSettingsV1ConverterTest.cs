// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.Converters
{
    using Microsoft.Membership.MemberServices.Privacy.Core.Converters;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.CustomerMasterAdapter;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class UserSettingsV1ConverterTest
    {
        [TestMethod]
        public void ToResourceSettingsV1_Success()
        {
            PrivacyProfile privacyProfile = null;
            Assert.IsNull(privacyProfile.ToResourceSettingsV1());

            privacyProfile = new PrivacyProfile();
            var result = privacyProfile.ToResourceSettingsV1();
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);

            // just Advertising
            privacyProfile = new PrivacyProfile();
            privacyProfile.Advertising = true;
            result = privacyProfile.ToResourceSettingsV1();
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result[0].Advertising.Value);

            privacyProfile = new PrivacyProfile();
            privacyProfile.Advertising = false;
            result = privacyProfile.ToResourceSettingsV1();
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.IsFalse(result[0].Advertising.Value);

            // just TailoredExperiencesOffers
            privacyProfile = new PrivacyProfile();
            privacyProfile.TailoredExperiencesOffers = true;
            result = privacyProfile.ToResourceSettingsV1();
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result[0].TailoredExperiencesOffers.Value);

            privacyProfile = new PrivacyProfile();
            privacyProfile.TailoredExperiencesOffers = false;
            result = privacyProfile.ToResourceSettingsV1();
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.IsFalse(result[0].TailoredExperiencesOffers.Value);

            // just SharingState
            privacyProfile = new PrivacyProfile();
            privacyProfile.SharingState = true;
            result = privacyProfile.ToResourceSettingsV1();
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result[0].SharingState.Value);

            privacyProfile = new PrivacyProfile();
            privacyProfile.SharingState = false;
            result = privacyProfile.ToResourceSettingsV1();
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.IsFalse(result[0].SharingState.Value);

            // all
            privacyProfile = new PrivacyProfile();
            privacyProfile.Advertising = true;
            privacyProfile.SharingState = true;
            privacyProfile.TailoredExperiencesOffers = false;
            result = privacyProfile.ToResourceSettingsV1();
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result[0].Advertising.Value);
            Assert.IsFalse(result[1].TailoredExperiencesOffers.Value);
            Assert.IsTrue(result[2].SharingState.Value);
        }
    }
}