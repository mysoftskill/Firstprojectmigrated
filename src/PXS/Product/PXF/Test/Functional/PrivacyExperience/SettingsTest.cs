// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.FunctionalTests
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Azure.Documents.SystemFunctions;
    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Models;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Config = Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.Config;

    /// <summary>
    ///     SettingsTest
    /// </summary>
    [TestClass]
    public class SettingsTest : TestBase
    {
        [TestMethod]
        [TestCategory("FCT")]
        public async Task GetChildSettings()
        {
            // Make call to family roster to get JWT for a child account
            TestUser testUser = TestUsers.Parent1;
            string userProxyTicket = await MockTestHooks.GetProxyTicketAsync(testUser).ConfigureAwait(false);
            FamilyModel familyModel = await FamilyModel.GetFamilyAsync(testUser.Puid, userProxyTicket, Test.Common.Config.TestConfiguration.S2SCert.Value).ConfigureAwait(false);
            FamilyModel.FamilyMemberModel child = familyModel.Members.First(m => m.Id == TestUsers.Child1.Puid.ToString());

            var requestArgs = new GetUserSettingsArgs(userProxyTicket)
            {
                FamilyTicket = child.JsonWebToken,
                CorrelationVector = new CorrelationVector().ToString(),
                RequestId = Guid.NewGuid().ToString()
            };

            ResourceSettingV1 childSettings = await S2SClient.GetUserSettingsAsync(requestArgs).ConfigureAwait(false);

            requestArgs.FamilyTicket = null;
            ResourceSettingV1 parentSettings = await S2SClient.GetUserSettingsAsync(requestArgs).ConfigureAwait(false);

            // Verify that we're not getting back the same profile for the parent and child
            Assert.AreNotEqual(parentSettings.ETag, childSettings.ETag);
        }

        [TestMethod]
        [TestCategory("FCT")]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task GetChildSettings_ForChildTypeUserShouldThrowInvalidOperationException()
        {
            TestUser testUser = TestUsers.Child1;
            string userProxyTicket = await MockTestHooks.GetProxyTicketAsync(testUser).ConfigureAwait(false);

            try 
            {
                FamilyModel familyModel = await FamilyModel.GetFamilyAsync(testUser.Puid, userProxyTicket, Test.Common.Config.TestConfiguration.S2SCert.Value).ConfigureAwait(false);
                FamilyModel.FamilyMemberModel child = familyModel.Members.First(m => m.Id == TestUsers.Child2.Puid.ToString());
                Assert.Fail("Get child should have thrown an exception");
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine(e.ToString());
                Assert.AreEqual("Sequence contains no matching element", e.Message);
                throw;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                throw;
            }
        }

        [TestMethod]
        [TestCategory("FCT")]
        [ExpectedException(typeof(PrivacyExperienceTransportException))]
        public async Task GetChildSettingsUsingHttpMethodHead()
        {
            // Make call to family roster to get JWT for a child account
            TestUser testUser = TestUsers.Parent1;
            string userProxyTicket = await MockTestHooks.GetProxyTicketAsync(testUser).ConfigureAwait(false);
            FamilyModel familyModel = await FamilyModel.GetFamilyAsync(testUser.Puid, userProxyTicket, Test.Common.Config.TestConfiguration.S2SCert.Value).ConfigureAwait(false);
            FamilyModel.FamilyMemberModel child = familyModel.Members.First(m => m.Id == TestUsers.Child1.Puid.ToString());

            var requestArgs = new GetUserSettingsArgs(userProxyTicket)
            {
                FamilyTicket = child.JsonWebToken,
                CorrelationVector = new CorrelationVector().ToString(),
                RequestId = Guid.NewGuid().ToString()
            };

            await S2SClient.GetUserSettingsAsync(requestArgs, HttpMethod.Head).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("FCT")]
        public async Task GetSettings()
        {
            string userProxyTicket = await MockTestHooks.GetProxyTicketAsync(TestUsers.ViewSettings0).ConfigureAwait(false);
            var requestArgs = new GetUserSettingsArgs(userProxyTicket)
            {
                CorrelationVector = new CorrelationVector().ToString(),
                RequestId = Guid.NewGuid().ToString()
            };

            ResourceSettingV1 response = await S2SClient.GetUserSettingsAsync(requestArgs).ConfigureAwait(false);
            Assert.IsFalse(response.OnBehalfOfPrivacy ?? false);
            Assert.IsNotNull(response);
        }


        [TestMethod]
        [TestCategory("FCT")]
        [ExpectedException(typeof(PrivacyExperienceTransportException))]
        public async Task GetSettingsUsingHttpMethodHead()
        {
            string userProxyTicket = await MockTestHooks.GetProxyTicketAsync(TestUsers.ViewSettings0).ConfigureAwait(false);
            var requestArgs = new GetUserSettingsArgs(userProxyTicket)
            {
                CorrelationVector = new CorrelationVector().ToString(),
                RequestId = Guid.NewGuid().ToString()
            };

            await S2SClient.GetUserSettingsAsync(requestArgs, HttpMethod.Head).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("FCT")]
        public async Task UpdateChildSettings()
        {
            ResourceSettingV1 childSettings = await this.UpdateChildSettingAsync(
                TestUsers.Parent1,
                TestUsers.Child1,
                setting =>
                {
                    setting.OnBehalfOfPrivacy = true;
                    return setting;
                }).ConfigureAwait(false);

            Assert.IsTrue(childSettings.OnBehalfOfPrivacy ?? false);

            string userProxyTicket = await MockTestHooks.GetProxyTicketAsync(TestUsers.Parent1).ConfigureAwait(false);
            var requestArgs = new GetUserSettingsArgs(userProxyTicket)
            {
                CorrelationVector = new CorrelationVector().ToString(),
                RequestId = Guid.NewGuid().ToString()
            };

            ResourceSettingV1 parentSettings = await S2SClient.GetUserSettingsAsync(requestArgs).ConfigureAwait(false);
            Assert.IsFalse(parentSettings.OnBehalfOfPrivacy ?? false);
            Assert.IsNotNull(parentSettings);

            Assert.AreNotEqual(childSettings.ETag, parentSettings.ETag);
        }

        [TestMethod]
        [TestCategory("FCT")]
        public async Task UpdateSettings_AdvertisingFalse()
        {
            await TestUpdateSettings(TestUsers.UpdateSettings0, nameof(ResourceSettingV1.Advertising), false).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("FCT")]
        public async Task UpdateSettings_AdvertisingTrue()
        {
            await TestUpdateSettings(TestUsers.UpdateSettings0, nameof(ResourceSettingV1.Advertising), true).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("FCT")]
        [ExpectedException(typeof(PrivacyExperienceTransportException))]
        public async Task UpdateSettings_UserProfileNotExistsShouldThrowPreconditionFailedException()
        {
            string userProxyTicket = await MockTestHooks.GetProxyTicketAsync(TestUsers.TimelineDeleteById).ConfigureAwait(false);
         
            var updateRequestArgs = new UpdateUserSettingsArgs(userProxyTicket)
            {
                CorrelationVector = new CorrelationVector().ToString(),
                RequestId = Guid.NewGuid().ToString(),
                ResourceSettings = new ResourceSettingV1
                {
                    Advertising = true,

                    ETag = Guid.NewGuid().ToString(),
                }
            };

            try
            {
                await S2SClient.UpdateUserSettingsAsync(updateRequestArgs).ConfigureAwait(false);
                Assert.Fail("Update should have thrown an exception");
            }
            catch (PrivacyExperienceTransportException e)
            {
                Console.WriteLine(e.ToString());
                Assert.AreEqual(HttpStatusCode.PreconditionFailed, e.HttpStatusCode);
                Assert.AreEqual("PreconditionFailed", e.Error.Code);
                Assert.AreEqual(
                    "(Code=Unknown, Message=ETag should exist on the existing profile. It was Null or WhiteSpace., StatusCode=412)",
                    e.Error.Message);
                throw;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                throw;
            }
        }

        [TestMethod]
        [TestCategory("FCT")]
        [ExpectedException(typeof(PrivacyExperienceTransportException))]
        public async Task UpdateSettings_NoChangeRequestedShouldThrowNotModifiedException()
        {
            string userProxyTicket = await MockTestHooks.GetProxyTicketAsync(TestUsers.UpdateSettings1).ConfigureAwait(false);
            var viewRequestArgs = new GetUserSettingsArgs(userProxyTicket)
            {
                CorrelationVector = new CorrelationVector().ToString(),
                RequestId = Guid.NewGuid().ToString()
            };
            ResourceSettingV1 response = await S2SClient.GetUserSettingsAsync(viewRequestArgs).ConfigureAwait(false);

            var updateRequestArgs = new UpdateUserSettingsArgs(userProxyTicket)
            {
                CorrelationVector = new CorrelationVector().ToString(),
                RequestId = Guid.NewGuid().ToString(),
                ResourceSettings = new ResourceSettingV1
                {
                    Advertising = null,

                    // Must pass in ETag from 'GET' call to update a resource.
                    ETag = response.ETag
                }
            };

            try
            {
                await S2SClient.UpdateUserSettingsAsync(updateRequestArgs).ConfigureAwait(false);
                Assert.Fail("Update should have thrown an exception");
            }
            catch (PrivacyExperienceTransportException e)
            {
                Console.WriteLine(e.ToString());
                Assert.AreEqual(HttpStatusCode.NotModified, e.HttpStatusCode);
                Assert.AreEqual("ResourceNotModified", e.Error.Code);
                Assert.AreEqual("Resource not modified.", e.Error.Message);
                throw;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                throw;
            }
        }

        [TestMethod]
        [TestCategory("FCT")]
        public async Task UpdateSettings_SharingStateFalse()
        {
            await TestUpdateSettings(TestUsers.UpdateSettings1, nameof(ResourceSettingV1.SharingState), false).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("FCT")]
        public async Task UpdateSettings_SharingStateTrue()
        {
            await TestUpdateSettings(TestUsers.UpdateSettings1, nameof(ResourceSettingV1.SharingState), true).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("FCT")]
        [ExpectedException(typeof(PrivacyExperienceTransportException))]
        public async Task UpdateSettings_WithoutMatchingETagShouldThrowConflictError()
        {
            string userProxyTicket = await MockTestHooks.GetProxyTicketAsync(TestUsers.UpdateSettings0).ConfigureAwait(false);

            // Do a GET first so a profile gets created.
            var requestArgs = new GetUserSettingsArgs(userProxyTicket)
            {
                CorrelationVector = new CorrelationVector().ToString(),
                RequestId = Guid.NewGuid().ToString()
            };

            await S2SClient.GetUserSettingsAsync(requestArgs).ConfigureAwait(false);

            var updateRequestArgs = new UpdateUserSettingsArgs(userProxyTicket)
            {
                CorrelationVector = new CorrelationVector().ToString(),
                RequestId = Guid.NewGuid().ToString(),
                ResourceSettings = new ResourceSettingV1
                {
                    Advertising = true,

                    // Generate a new guid so the ETag is expected to NOT match on this request for an update.
                    ETag = Guid.NewGuid().ToString()
                }
            };

            try
            {
                await S2SClient.UpdateUserSettingsAsync(updateRequestArgs).ConfigureAwait(false);
                Assert.Fail("Update should have thrown an exception");
            }
            catch (PrivacyExperienceTransportException e)
            {
                Console.WriteLine(e.ToString());
                Assert.AreEqual(HttpStatusCode.Conflict, e.HttpStatusCode);
                Assert.AreEqual("UpdateConflict", e.Error.Code);
                Assert.AreEqual(
                    "(Code=ConcurrencyConflict, Message=ETag did not match. The resource has been changed. Refresh the existing profile setting and re-submit the request., StatusCode=409)",
                    e.Error.Message);
                throw;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                throw;
            }
        }

        [TestMethod]
        [TestCategory("FCT")]
        public async Task UpdateSettings_TailoredExperiencesOffersFalse()
        {
            await TestUpdateSettings(TestUsers.UpdateSettings2, nameof(ResourceSettingV1.TailoredExperiencesOffers), false).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("FCT")]
        public async Task UpdateSettings_TailoredExperiencesOffersTrue()
        {
            await TestUpdateSettings(TestUsers.UpdateSettings2, nameof(ResourceSettingV1.TailoredExperiencesOffers), true).ConfigureAwait(false);
        }

        private static async Task TestUpdateSettings(TestUser user, string resourceSettingsType, bool usage)
        {
            string userProxyTicket = await MockTestHooks.GetProxyTicketAsync(user).ConfigureAwait(false);
            var viewRequestArgs = new GetUserSettingsArgs(userProxyTicket)
            {
                CorrelationVector = new CorrelationVector().ToString(),
                RequestId = Guid.NewGuid().ToString()
            };
            ResourceSettingV1 response = await S2SClient.GetUserSettingsAsync(viewRequestArgs).ConfigureAwait(false);

            var updateRequestArgs = new UpdateUserSettingsArgs(userProxyTicket)
            {
                CorrelationVector = new CorrelationVector().ToString(),
                RequestId = Guid.NewGuid().ToString(),
                ResourceSettings = new ResourceSettingV1
                {
                    // Must pass in ETag from 'GET' call to update a resource.
                    ETag = response.ETag
                }
            };

            switch (resourceSettingsType)
            {
                case nameof(ResourceSettingV1.Advertising):
                    updateRequestArgs.ResourceSettings.Advertising = usage;
                    break;
                case nameof(ResourceSettingV1.SharingState):
                    updateRequestArgs.ResourceSettings.SharingState = usage;
                    break;
                case nameof(ResourceSettingV1.TailoredExperiencesOffers):
                    updateRequestArgs.ResourceSettings.TailoredExperiencesOffers = usage;
                    break;
                default:
                    break;
            }
                
            response = await S2SClient.UpdateUserSettingsAsync(updateRequestArgs).ConfigureAwait(false);
            bool result = false;
            foreach (var property in response.GetType().GetProperties())
            { 
                if (property.Name.Equals(resourceSettingsType))
                    result = (bool)property.GetValue(response, null);
            }
            
            Assert.IsNotNull(result);
            Assert.AreEqual(usage, result);
        }

        private async Task<ResourceSettingV1> UpdateChildSettingAsync(TestUser parentUser, TestUser childUser, Func<ResourceSettingV1, ResourceSettingV1> modification)
        {
            // Make call to family roster to get JWT for a child account
            string userProxyTicket = await MockTestHooks.GetProxyTicketAsync(parentUser).ConfigureAwait(false);
            FamilyModel familyModel = await FamilyModel.GetFamilyAsync(parentUser.Puid, userProxyTicket, Config.TestConfiguration.S2SCert.Value).ConfigureAwait(false);
            FamilyModel.FamilyMemberModel child = familyModel.Members.First(m => m.Id == childUser.Puid.ToString());
            var updateRequestArgs = new GetUserSettingsArgs(userProxyTicket)
            {
                FamilyTicket = child.JsonWebToken,
                CorrelationVector = new CorrelationVector().ToString(),
                RequestId = Guid.NewGuid().ToString()
            };

            var requestArgs = new UpdateUserSettingsArgs(userProxyTicket)
            {
                FamilyTicket = child.JsonWebToken,
                CorrelationVector = new CorrelationVector().ToString(),
                RequestId = Guid.NewGuid().ToString(),
                ResourceSettings = modification(await S2SClient.GetUserSettingsAsync(updateRequestArgs).ConfigureAwait(false))
            };

            return await S2SClient.UpdateUserSettingsAsync(requestArgs).ConfigureAwait(false);
        }
    }
}
