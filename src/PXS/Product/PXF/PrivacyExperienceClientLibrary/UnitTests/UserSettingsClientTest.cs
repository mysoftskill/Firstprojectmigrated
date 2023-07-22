// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.UnitTests
{
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Models;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    /// <summary>
    /// SettingsClient Test
    /// </summary>
    [TestClass]
    public class UserSettingsClientTest : ClientTestBase
    {
        [TestMethod]
        public async Task GetSettingsDefaultArgsShouldTargetCorrectApiPath()
        {
            var expectedApiPath = "v1/settings";
            var client = this.CreateBasicClient();

            await client.GetUserSettingsAsync(new GetUserSettingsArgs(this.TestUserProxyTicket));

            this.MockHttpClient.Verify(
                c => c.SendAsync(It.Is<HttpRequestMessage>(m => m.RequestUri.OriginalString.Equals(expectedApiPath)), HttpCompletionOption.ResponseContentRead));
        }

        [TestMethod]
        public async Task UpdateSettingsDefaultArgsShouldTargetCorrectApiPath()
        {
            var expectedApiPath = "v1/settings";
            var client = this.CreateBasicClient();

            await client.UpdateUserSettingsAsync(new UpdateUserSettingsArgs(this.TestUserProxyTicket) { ResourceSettings = new ResourceSettingV1 { ETag = "123456789" }});

            this.MockHttpClient.Verify(
                c => c.SendAsync(It.Is<HttpRequestMessage>(m => m.RequestUri.OriginalString.Equals(expectedApiPath) && m.Headers.GetValues("If-Match").First() == "123456789"), HttpCompletionOption.ResponseContentRead));
        }

        [ExpectedException(typeof(ArgumentNullException))]
        [TestMethod]
        public async Task UpdateSettingsThrowException_NullResourceSettingsArgs()
        {
            var client = this.CreateBasicClient();

            try
            {
                await client.UpdateUserSettingsAsync(new UpdateUserSettingsArgs(this.TestUserProxyTicket) { ResourceSettings = null });
            }
            catch (Exception ex)
            {
                Assert.AreEqual("Value cannot be null.\r\nParameter name: ResourceSettings", ex.Message);
                throw;
            }

            Assert.Fail("Exception should have been thrown.");
        }

        [ExpectedException(typeof(ArgumentNullException))]
        [TestMethod]
        public async Task UpdateSettingsThrowException_NullETagArgs()
        {
            var client = this.CreateBasicClient();

            try
            {
                await client.UpdateUserSettingsAsync(new UpdateUserSettingsArgs(this.TestUserProxyTicket) { ResourceSettings = new ResourceSettingV1 {  ETag = null }});
            }
            catch (Exception ex)
            {
                Assert.AreEqual("Value cannot be null.\r\nParameter name: ETag", ex.Message);
                throw;
            }

            Assert.Fail("Exception should have been thrown.");
        }
    }
}