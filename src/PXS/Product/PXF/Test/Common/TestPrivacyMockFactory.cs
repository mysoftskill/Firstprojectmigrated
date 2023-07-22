// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common
{
    using Microsoft.Membership.MemberServices.Configuration;
    using Moq;

    /// <summary>
    /// TestMockFactory
    /// </summary>
    public static class TestPrivacyMockFactory
    {
        public static Mock<IPrivacyExperienceServiceConfiguration> CreatePrivacyExperienceServiceConfiguration()
        {
            var mockServiceConfiguration = new Mock<IPrivacyExperienceServiceConfiguration>();
            mockServiceConfiguration
                .SetupGet(c => c.S2SAppSiteName)
                .Returns("s2sapp.pxs.api.account.microsoft.com");
            mockServiceConfiguration
                .SetupGet(c => c.S2SUserSiteName)
                .Returns("s2suser.pxs.api.account.microsoft.com");
            mockServiceConfiguration
                .SetupGet(c => c.S2SUserLongSiteName)
                .Returns("s2suser-long.pxs.api.account.microsoft.com");

            return mockServiceConfiguration;
        }
    }
}