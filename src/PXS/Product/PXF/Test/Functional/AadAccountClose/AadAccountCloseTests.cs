// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.FunctionalTests.AadAccountClose
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.EventHubProcessor;
    using Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.TableStorage;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.Utilities;
    using Microsoft.Membership.MemberServices.Test.Common;
    using Microsoft.Membership.MemberServices.Test.EventHubTestCommon;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Newtonsoft.Json;

    using TestConfiguration = Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.Config.TestConfiguration;

    /// <summary>
    ///     AadAccountClose Functional Tests
    /// </summary>
    [TestClass]
    public class AadAccountCloseTests
    {
        private static readonly Random random = new Random();

        private readonly EventHubWriter eventHubWriter;

        /// <summary>
        ///     AadAccountCloseTests
        /// </summary>
        public AadAccountCloseTests()
        {
            this.eventHubWriter = new EventHubWriter(TestConfiguration.AadAccountCloseEventHubConnectionString.Value);
        }

        [TestMethod, TestCategory("FCT"), TestCategory("PROXY")]
        [Description("This test inserts into Azure EventHub, waits for the worker to process the event, and validates it was sent to an Azure storage table.")]
        public async Task ShouldProcessAadAccountCloseSuccessfully()
        {
            // Arrange: Create a test aad account close notification.
            IList<Notification> events = new List<Notification>();
            Guid objectId = Guid.NewGuid();
            Guid tenantId = Guid.Parse(TestData.HomeTenantId);
            long orgPuid = random.Next(1, int.MaxValue);
            events.Add(CreateNotification(objectId, tenantId, Guid.Empty, orgPuid));

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

            Assert.IsNotNull(result, $"Command was not found in test storage. Object ID: {objectId}, Tenant ID: {tenantId}");
            Assert.AreEqual(RequestType.AccountClose, result.RequestType);
            Assert.IsInstanceOfType(result.Subject, typeof(AadSubject), $"Subject should be {nameof(AadSubject)}");
            Assert.AreEqual(objectId, ((AadSubject)result.Subject).ObjectId);
            Assert.AreEqual(tenantId, ((AadSubject)result.Subject).TenantId);
            Assert.AreEqual($"a:{objectId}", result.AuthorizationId);
            Console.WriteLine($"Validation succeeded for command id: {result.RequestId}.");
        }

        [TestMethod, TestCategory("FCT"), TestCategory("PROXY")]
        [Description("This test inserts an AcountClose request into Azure EventHub with tenant id causing AAD RVS failure, validates it was sent to deadletter table.")]
        [DataRow(TestData.TenantId400, "InvalidInput")]
        [DataRow(TestData.TenantId403, "Forbidden")]
        [DataRow(TestData.TenantId409, "ConcurrencyConflict")]
        public async Task ShouldAddToDeadLetterAfterMaxRetryOfAadRvsFailure(string tenantIdString, string errorCode)
        {
            // Arrange: Create a test aad account close notification.
            IList<Notification> events = new List<Notification>();
            Guid objectId = Guid.NewGuid();
            Guid tenantId = Guid.Parse(tenantIdString);
            long orgPuid = random.Next(1, int.MaxValue);
            events.Add(CreateNotification(objectId, tenantId, Guid.Empty, orgPuid));

            // Act: Insert into EventHub. Worker will pick it up and process.
            await this.eventHubWriter.SendAsync(JsonConvert.SerializeObject(events)).ConfigureAwait(false);

            // Assert: Validate that the command was added to DeadLetter table
            // querying DeadLetter table is done via a testhook in PartnerMock service
            AccountCloseDeadLetterStorage result = null;
            bool found = await PollingUtility.PollForCondition(
                iterationWait: TimeSpan.FromSeconds(1),
                maximumPollRetry: 30,
                conditionCheck: async () =>
                {
                    result = await MockTestHooks.GetDeadLetterItemAsync(tenantId: tenantId, objectId: objectId).ConfigureAwait(false);
                    if (result == null)
                    {
                        Console.WriteLine("Data not found in storage yet.");
                    }

                    return result != null;
                }
            ).ConfigureAwait(false);

            Assert.IsTrue(found, "Dead letter entry was not found.");
            VerifyAccountCloseRequest(result, objectId, tenantId, errorCode);
            Console.WriteLine($"Found command in DeadLetter table for command id: {result.DataActual.RequestId}.");
        }

        [TestMethod, TestCategory("FCT"), TestCategory("PROXY")]
        [Description("This test inserts an AcountClose request into Azure EventHub with tenant id causing AAD RVS failure, validates it was sent to deadletter table even there is a conflict.")]
        public async Task ShouldAddToDeadLetterWithConflict()
        {
            // Arrange: Create a test aad account close notification.
            IList<Notification> events = new List<Notification>();
            Guid objectId = Guid.NewGuid();
            Guid tenantId = Guid.Parse(TestData.TenantId400);
            long orgPuid = random.Next(1, int.MaxValue);
            events.Add(CreateNotification(objectId, tenantId, Guid.Empty, orgPuid));

            // Act: Insert into EventHub. Worker will pick it up and process.
            await this.eventHubWriter.SendAsync(JsonConvert.SerializeObject(events)).ConfigureAwait(false);

            // Assert: Validate that the command was added to DeadLetter table
            // querying DeadLetter table is done via a testhook in PartnerMock service
            AccountCloseDeadLetterStorage result = null;
            bool found = await PollingUtility.PollForCondition(
                iterationWait: TimeSpan.FromSeconds(1),
                maximumPollRetry: 30,
                conditionCheck: async () =>
                {
                    result = await MockTestHooks.GetDeadLetterItemAsync(tenantId: tenantId, objectId: objectId).ConfigureAwait(false);
                    if (result == null)
                    {
                        Console.WriteLine("Data not found in storage yet.");
                    }

                    return result != null;
                }
            ).ConfigureAwait(false);

            Assert.IsTrue(found, "Dead letter entry was not found.");
            VerifyAccountCloseRequest(result, objectId, tenantId, "InvalidInput");
            Console.WriteLine($"Found command in DeadLetter table for command id: {result.DataActual.RequestId}.");

            // Now insert the same command again
            await this.eventHubWriter.SendAsync(JsonConvert.SerializeObject(events)).ConfigureAwait(false);

            AccountCloseDeadLetterStorage newResult = null;
            found = await PollingUtility.PollForCondition(
                iterationWait: TimeSpan.FromSeconds(2),
                maximumPollRetry: 30,
                conditionCheck: async () =>
                {
                    newResult = await MockTestHooks.GetDeadLetterItemAsync(tenantId: tenantId, objectId: objectId).ConfigureAwait(false);
                    // Expect a new item apprears in the table
                    if (newResult == null || DateTimeOffset.Compare(newResult.Timestamp, result.Timestamp) == 0)
                    {
                        Console.WriteLine("Data not found in storage yet.");
                        return false;
                    }

                    return true;
                }
            ).ConfigureAwait(false);

            Assert.IsTrue(found, "New dead letter entry was not found.");
            VerifyAccountCloseRequest(newResult, objectId, tenantId, "InvalidInput");
            Console.WriteLine($"Found new command in DeadLetter table for command id: {newResult.DataActual.RequestId}.");
        }

        private static void VerifyAccountCloseRequest(AccountCloseDeadLetterStorage result, Guid objectId, Guid tenantId, string errorCode)
        {
            Assert.IsNotNull(result, $"Command was not found in deadletter table. Object ID: {objectId}, Tenant ID: {tenantId}");
            Assert.AreEqual(RequestType.AccountClose, result.DataActual?.RequestType);
            Assert.IsInstanceOfType(result.DataActual?.Subject, typeof(AadSubject), $"Subject should be {nameof(AadSubject)}");
            Assert.AreEqual(objectId, ((AadSubject)result.DataActual.Subject).ObjectId);
            Assert.AreEqual(tenantId, ((AadSubject)result.DataActual.Subject).TenantId);
            Assert.AreEqual($"a:{objectId}", result.DataActual.AuthorizationId);
            Assert.AreEqual(errorCode, result.ErrorCode);
        }

        private static Notification CreateNotification(Guid objectId, Guid tenantId, Guid homeTenantId, long orgPuid)
        {
            return new Notification
            {
                ResourceData = new ResourceData
                {
                    Id = objectId,
                    TenantId = tenantId,
                    EventTime = DateTimeOffset.UtcNow,
                    OrgPuid = orgPuid,
                    HomeTenantId = homeTenantId,
                },
                Token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N")
            };
        }
    }
}
