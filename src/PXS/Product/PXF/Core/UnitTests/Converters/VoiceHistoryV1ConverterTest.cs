// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.VoiceHistory
{
    using System;
    using System.Globalization;
    using Microsoft.Membership.MemberServices.Privacy.Core.Converters;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// VoiceHistoryV1 Converter Test
    /// </summary>
    [TestClass]
    public class VoiceHistoryV1ConverterTest
    {
        #region PrivacyAdapters.Models to ExperienceContracts.V1

        [TestMethod]
        public void ToVoiceHistoryV1Test()
        {
            var expected = new VoiceHistoryV1
            {
                DateTime = DateTimeOffset.Parse("2/29/2016 12:07:10 AM +00:00", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal),
                IsAggregate = false,
                DeviceId = "global[343434]",
                Source = "Bing",
                Ids = new[] { "abc123,635923012300000000" },
                Application = "Application",
                DeviceType = "DeviceType",
                DisplayText = "Display text",
                PartnerId = "Mock Partner id",
            };

            VoiceResource adapterVoiceResource = new VoiceResource
            {
                DateTime = DateTimeOffset.Parse("2/29/2016 12:07:10 AM +00:00", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal),
                DeviceId = "global[343434]",
                Sources = new[] { "Bing" },
                Application = "Application",
                DeviceType = "DeviceType",
                DisplayText = "Display text",
                Id = "abc123",
                Status = ResourceStatus.Active,
                PartnerId = "Mock Partner id"
            };

            var actual = adapterVoiceResource.ToVoiceHistoryV1();

            EqualityHelper.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ToVoiceHistoryAudioV1Test()
        {
            var expected = new VoiceHistoryAudioV1
            {
                DateTime = DateTimeOffset.Parse("2/29/2016 12:07:10 AM +00:00", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal),
                IsAggregate = false,
                DeviceId = "global[343434]",
                Source = "Bing",
                Ids = new[] { "abc123" },
                Audio = new byte[] { 0x00, 0x01, 0x02 },
                PartnerId = "Mock Partner id",
            };

            VoiceAudioResource adapterVoiceResource = new VoiceAudioResource
            {
                DateTime = DateTimeOffset.Parse("2/29/2016 12:07:10 AM +00:00", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal),
                DeviceId = "global[343434]",
                Sources = new[] { "Bing" },
                Audio = new byte[] { 0x00, 0x01, 0x02 },
                Id = "abc123",
                Status = ResourceStatus.Active,
                PartnerId = "Mock Partner id"
            };

            var actual = adapterVoiceResource.ToVoiceHistoryAudioV1();

            EqualityHelper.AreEqual(expected, actual);
        }

        #endregion
    }
}