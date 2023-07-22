// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.FunctionalTests.MultiTenantCollaboration
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.EventHubProcessor;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.Utilities;
    using Microsoft.Membership.MemberServices.Test.Common;
    using Microsoft.Membership.MemberServices.Test.EventHubTestCommon;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Newtonsoft.Json;

    using TestConfiguration = Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.Config.TestConfiguration;

    /// <summary>
    ///     AadAccountClose Functional Tests for multi tenant collaboration scenarios from EventHub
    /// </summary>
    [TestClass]
    public class AccountCloseViaNotificationTests
    {
        private static readonly Random random = new Random();

        private readonly EventHubWriter eventHubWriter;

        /// <summary>
        ///     AadAccountCloseTests from Event Hub
        /// </summary>
        public AccountCloseViaNotificationTests()
        {
            this.eventHubWriter = new EventHubWriter(TestConfiguration.AadAccountCloseEventHubConnectionString.Value);
        }

        [TestMethod, TestCategory("FCT"), TestCategory("NI")]
        public async Task HomeTenantAccountClose()
        {
            // Arrange: Create a test aad account close notification in home tenant.
            IList<Notification> events = new List<Notification>();
            Guid objectId = Guid.NewGuid();
            Guid tenantId = Guid.Parse(TestData.HomeTenantId);
            long orgPuid = random.Next(1, int.MaxValue);
            events.Add(CreateNotification(objectId, tenantId, tenantId, orgPuid));

            // Act: Insert into EventHub. Worker will pick it up and process.
            await this.eventHubWriter.SendAsync(JsonConvert.SerializeObject(events)).ConfigureAwait(false);

            // Assert: Validate that the event was picked up and was sent to PCF
            // Because we don't know a way to query PCF for this in a test environment, we are using the mock service to store the events for test validation.
            // Poll and wait because the worker is async. This time needs to be reliable for the test to properly function.
            PrivacyRequest result = null;
            await PollingUtility.PollForCondition(
                iterationWait: TimeSpan.FromSeconds(1),
                maximumPollRetry: 30,
                conditionCheck: async () =>
                {
                    result = await MockTestHooks.GetPrivacyRequestAsync(objectId, tenantId).ConfigureAwait(false);
                    if (result == null)
                    {
                        Console.WriteLine("Data not found in storage yet.");
                    }

                    return result != null;
                }
            ).ConfigureAwait(false);

            VerifyAccountCloseRequest(result, objectId, tenantId, tenantId);
        }

        [TestMethod, TestCategory("FCT"), TestCategory("NI")]
        public async Task ResourceTenantAccountCleanupViaEventHub()
        {
            // Arrange: Create a test aad account close notification in resource tenant.
            IList<Notification> events = new List<Notification>();
            Guid objectId = Guid.NewGuid();
            Guid tenantId = Guid.Parse(TestData.ResourceTenantId);
            long orgPuid = random.Next(1, int.MaxValue);
            events.Add(CreateNotification(objectId, tenantId, Guid.Parse(TestData.HomeTenantId), orgPuid));

            // Act: Insert into EventHub. Worker will pick it up and process.
            await this.eventHubWriter.SendAsync(JsonConvert.SerializeObject(events)).ConfigureAwait(false);

            // Assert: Validate that the event was picked up and was sent to PCF
            // Because we don't know a way to query PCF for this in a test environment, we are using the mock service to store the events for test validation.
            // Poll and wait because the worker is async. This time needs to be reliable for the test to properly function.
            PrivacyRequest result = null;
            await PollingUtility.PollForCondition(
                iterationWait: TimeSpan.FromSeconds(1),
                maximumPollRetry: 30,
                conditionCheck: async () =>
                {
                    result = await MockTestHooks.GetPrivacyRequestAsync(objectId, tenantId).ConfigureAwait(false);
                    if (result == null)
                    {
                        Console.WriteLine("Data not found in storage yet.");
                    }

                    return result != null;
                }
            ).ConfigureAwait(false);

            VerifyAccountCloseRequest(result, objectId, tenantId, Guid.Parse(TestData.HomeTenantId));
        }

        private static void VerifyAccountCloseRequest(PrivacyRequest result, Guid objectId, Guid tenantId, Guid homeTenantId)
        {
            Assert.IsNotNull(result, $"Command was not found in test storage. Object ID: {objectId}, Tenant ID: {tenantId}");
            Assert.AreEqual(RequestType.AccountClose, result.RequestType);

            if (tenantId == homeTenantId)
            {
                Assert.IsInstanceOfType(result.Subject, typeof(AadSubject), $"Subject should be {nameof(AadSubject)}");
                Assert.IsNotInstanceOfType(result.Subject, typeof(AadSubject2), $"Subject should not be {nameof(AadSubject2)}");
                Assert.AreEqual(objectId, ((AadSubject)result.Subject).ObjectId);
                Assert.AreEqual(tenantId, ((AadSubject)result.Subject).TenantId);
            }
            else
            {
                Assert.IsInstanceOfType(result.Subject, typeof(AadSubject2), $"Subject should be {nameof(AadSubject2)}");
                Assert.AreEqual(objectId, ((AadSubject2)result.Subject).ObjectId);
                Assert.AreEqual(tenantId, ((AadSubject2)result.Subject).TenantId);
                Assert.AreEqual(homeTenantId, ((AadSubject2)result.Subject).HomeTenantId);
            }

            Assert.AreEqual($"a:{objectId}", result.AuthorizationId);

            if (tenantId == homeTenantId)
            {
                // Should have V2 verifier if the request was from home tenant
                Assert.IsFalse(string.IsNullOrEmpty(result.VerificationToken));
                Assert.IsTrue(string.IsNullOrEmpty(result.VerificationTokenV3));
                Assert.IsFalse(string.Compare(result.VerificationTokenV3, result.VerificationToken, StringComparison.Ordinal) == 0);
            }
            else
            {
                // Should not have V2 verifier if the request was from resource tenant
                Assert.IsTrue(string.IsNullOrEmpty(result.VerificationToken));
                Assert.IsFalse(string.IsNullOrEmpty(result.VerificationTokenV3));
            }

            Console.WriteLine($"Validation succeeded for command id: {result.RequestId}.");
        }

        private static Notification CreateNotification(Guid objectId, Guid tenantId, Guid homeTenantId, long orgPuid)
        {
            // TODO: Get new Notification contract from AADNS for home vs resource tenant
            return new Notification
            {
                ResourceData = new ResourceData
                {
                    Id = objectId,
                    TenantId = tenantId,
                    EventTime = DateTimeOffset.UtcNow,
                    OrgPuid = orgPuid,
                    HomeTenantId = homeTenantId
                },
                Token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N")
            };
        }
    }
}
