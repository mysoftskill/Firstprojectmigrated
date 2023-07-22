// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.FunctionalTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Adapters.Common;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Extensions;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Helpers;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Models;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common;
    using Microsoft.Membership.MemberServices.ScheduleDbClient.Model;
    using Microsoft.OSGS.HttpClientCommon;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;
    using HttpClient = Microsoft.OSGS.HttpClientCommon.HttpClient;
    using TestConfiguration = Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.Config.TestConfiguration;

    /// <summary>
    ///     Timeline aggregate count tests
    /// </summary>
    [TestClass]
    public class RecurringDeletesTests : TestBase
    {
        private static IHttpClient TestHttpClient => httpClient.Value;
        private static Uri TestBaseUrl => TestConfiguration.MockBaseUrl.Value;
        private static readonly Lazy<IHttpClient> httpClient = new Lazy<IHttpClient>(
            () =>
            {
                var certHandler = new WebRequestHandler();
                var client = new HttpClient(certHandler) { BaseAddress = TestConfiguration.ServiceEndpoint.Value };
                client.MessageHandler.AttachClientCertificate(TestConfiguration.S2SCert.Value);
                ServicePointManager.ServerCertificateValidationCallback += (o, c, ch, er) => true;
                return client;
            });

        [TestMethod, TestCategory("FCT")]
        public async Task DeleteRecurringDeletesSuccessAsync()
        {
            var testUser = TestUsers.DeleteBrowse0;
            string userProxyTicket = await MockTestHooks.GetProxyTicketAsync(testUser).ConfigureAwait(false);
            string dataType = Policies.Current.DataTypes.Ids.BrowsingHistory.Value;
            string requestId = Guid.NewGuid().ToString();
            string cv = new CorrelationVector().ToString();

            await this.CreateRecurringDeleteScheduleAsync(testUser, dataType, RecurringIntervalDays.Days30).ConfigureAwait(false);

            var requestArgs = new DeleteRecurringDeletesArgs(userProxyTicket, dataType)
            {
                CorrelationVector = new CorrelationVector().ToString(),
                RequestId = requestId
            };

            await S2SClient.DeleteRecurringDeletesAsync(requestArgs).ConfigureAwait(false);

            var recurringDeletes = await S2SClient.GetRecurringDeletesAsync(
                new PrivacyExperienceClientBaseArgs(userProxyTicket)
                {
                    RequestId = requestId,
                    CorrelationVector = cv,
                })
                .ConfigureAwait(false);

            Assert.IsTrue(!recurringDeletes.Any(x => x.DataType == dataType));
        }

        [TestMethod, TestCategory("FCT")]
        [ExpectedException(typeof(PrivacyExperienceTransportException))]
        public async Task DeleteNonExistingRecurringDeleteDocumentShouldThrowException()
        {
            var testUser = TestUsers.DeleteBrowse0;
            string userProxyTicket = await MockTestHooks.GetProxyTicketAsync(testUser).ConfigureAwait(false);
            string dataType = Policies.Current.DataTypes.Ids.BrowsingHistory.Value;
            string requestId = Guid.NewGuid().ToString();

            var requestArgs = new DeleteRecurringDeletesArgs(userProxyTicket, dataType)
            {
                CorrelationVector = new CorrelationVector().ToString(),
                RequestId = requestId
            };

            try 
            {
                await S2SClient.DeleteRecurringDeletesAsync(requestArgs).ConfigureAwait(false);
                Assert.Fail("Trying to delete a document that doesn't exist should throw exception");
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex.ToString());
                Assert.IsTrue(ex.Message.Contains("No record found in scheduledb for delete operation"));
                throw;
            }
        }

        [TestMethod, TestCategory("FCT")]
        public async Task CreateOrUpdateRecurringDeletesSuccessAsync()
        {
            TestUser testUser = TestUsers.TimelineDeleteByType;
            string userProxyTicket = await MockTestHooks.GetProxyTicketAsync(testUser).ConfigureAwait(false);
            string dataType = Policies.Current.DataTypes.Ids.BrowsingHistory.Value;
            string cv = new CorrelationVector().ToString();
            string requestId = Guid.NewGuid().ToString();
            RecurringIntervalDays recurringIntervalDays = RecurringIntervalDays.Days30;
            DateTimeOffset utcNow = DateTimeOffset.UtcNow;
            DateTimeOffset nextDeleteDate = utcNow.AddDays((int)recurringIntervalDays);
            DeleteRecurringDeletesArgs deleteRecurringDeletesArgs = new DeleteRecurringDeletesArgs(
                        userProxyTicket: userProxyTicket,
                        dataType: dataType)
            {
                CorrelationVector = cv,
                FamilyTicket = null,
                RequestId = requestId
            };

            // make sure records do not exists
            try
            {
                await S2SClient.DeleteRecurringDeletesAsync(deleteRecurringDeletesArgs).ConfigureAwait(false);
            }
            catch (PrivacyExperienceTransportException ex)
            {
                // Check the string from custom exception
                Assert.IsTrue(ex.Message.Contains("No record found in scheduledb for delete operation"));
            }

            // Create
            CreateOrUpdateRecurringDeletesArgs args = new CreateOrUpdateRecurringDeletesArgs(
                userProxyTicket: userProxyTicket,
                dataType: dataType,
                nextDeleteDate: nextDeleteDate,
                recurringIntervalDays: recurringIntervalDays)
            {
                CorrelationVector = cv,
                RequestId = requestId
            };

            var record = await S2SClient.CreateOrUpdateRecurringDeletesAsync(args).ConfigureAwait(false);

            Assert.AreEqual(testUser.Puid, record.PuidValue);
            Assert.AreEqual(recurringIntervalDays, record.RecurringIntervalDays);
            // date time diff should be < 1 sec, because we lose milliseconds in serialization
            Assert.IsTrue(nextDeleteDate.Subtract(record.NextDeleteOccurrence.Value) < TimeSpan.FromSeconds(1));
            Assert.AreEqual(RecurrentDeleteStatus.Active, record.Status);

            // get the record back
            var recurringDeletes = await S2SClient.GetRecurringDeletesAsync(
                new PrivacyExperienceClientBaseArgs(userProxyTicket)
                { 
                    RequestId = requestId,
                    CorrelationVector = cv,
                })
                .ConfigureAwait(false); 

            Assert.IsNotNull(recurringDeletes);
            Assert.IsTrue(recurringDeletes.Count == 1);
            record = recurringDeletes.First();
            Assert.AreEqual(testUser.Puid, record.PuidValue);
            Assert.AreEqual(recurringIntervalDays, record.RecurringIntervalDays);
            Assert.IsTrue(nextDeleteDate.Subtract(record.NextDeleteOccurrence.Value) < TimeSpan.FromSeconds(1));
            Assert.AreEqual(RecurrentDeleteStatus.Active, record.Status);

            // cleanup
            await S2SClient.DeleteRecurringDeletesAsync(deleteRecurringDeletesArgs).ConfigureAwait(false);
        }

        [TestMethod, TestCategory("FCT")]
        public async Task UpdateRecurringDeletesSuccessAsync()
        {
            var testUser = TestUsers.DeleteBrowse0;
            string userProxyTicket = await MockTestHooks.GetProxyTicketAsync(testUser).ConfigureAwait(false);
            string dataType = Policies.Current.DataTypes.Ids.BrowsingHistory.Value;
            string requestId = Guid.NewGuid().ToString();
            string cv = new CorrelationVector().ToString();

            await this.CreateRecurringDeleteScheduleAsync(testUser, dataType, RecurringIntervalDays.Days30).ConfigureAwait(false);

            // get the document
            var recurringDeletes = await S2SClient.GetRecurringDeletesAsync(
                new PrivacyExperienceClientBaseArgs(userProxyTicket)
                {
                    RequestId = requestId,
                    CorrelationVector = cv,
                })
                .ConfigureAwait(false);
            GetRecurringDeleteResponse response = recurringDeletes.Where(x => x.DataType == dataType).Single();

            // update
            var updatedNextDeleteDate = response.NextDeleteOccurrence.Value.AddDays(10);
            // Create
            CreateOrUpdateRecurringDeletesArgs args = new CreateOrUpdateRecurringDeletesArgs(
                userProxyTicket: userProxyTicket,
                dataType: dataType,
                nextDeleteDate: updatedNextDeleteDate,
                recurringIntervalDays: RecurringIntervalDays.Days180,
                RecurrentDeleteStatus.Paused)
            {
                CorrelationVector = cv,
                RequestId = requestId
            };

            await S2SClient.CreateOrUpdateRecurringDeletesAsync(args).ConfigureAwait(false);

            // get again and compare
            recurringDeletes = await S2SClient.GetRecurringDeletesAsync(
                            new PrivacyExperienceClientBaseArgs(userProxyTicket)
                            {
                                RequestId = requestId,
                                CorrelationVector = cv,
                            })
                            .ConfigureAwait(false);
            response = recurringDeletes.Where(x => x.DataType == dataType).Single();

            Assert.IsNotNull(response);
            Assert.AreEqual(testUser.Puid, response.PuidValue);
            Assert.AreEqual(RecurringIntervalDays.Days180, response.RecurringIntervalDays);
            Assert.IsTrue(updatedNextDeleteDate.Subtract(response.NextDeleteOccurrence.Value) < TimeSpan.FromSeconds(1));
            Assert.AreEqual(RecurrentDeleteStatus.Paused, response.Status);

            // cleanup
            DeleteRecurringDeletesArgs deleteRecurringDeletesArgs = new DeleteRecurringDeletesArgs(
                        userProxyTicket: userProxyTicket,
                        dataType: dataType)
            {
                CorrelationVector = cv,
                FamilyTicket = null,
                RequestId = requestId
            };
            await S2SClient.DeleteRecurringDeletesAsync(deleteRecurringDeletesArgs).ConfigureAwait(false);
        }

        [TestMethod, TestCategory("FCT")]
        public async Task RecurringDeletesScannerWorkerE2ESuccess()
        {
            var originalScheduleDbDoc = this.CreateRecurrentDeleteScheduleDbDocument(new Random().Next(1, 10000), "PreciseUserLocation", "preVerifier", 0);
            var jsonDoc = JsonConvert.SerializeObject(originalScheduleDbDoc);
            var stringContent = new StringContent(jsonDoc, UnicodeEncoding.UTF8, "application/json");

            HttpResponseMessage response = await TestHttpClient.PostAsync(new Uri(TestBaseUrl, "recurringdelete/scannerworkere2e"), stringContent).ConfigureAwait(false);
            
            // Assert
            Assert.IsNotNull(response);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var queueDepths = JsonConvert.DeserializeObject<int[]>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
            Assert.AreEqual(0, queueDepths[0]);
            Assert.AreEqual(1, queueDepths[1], "The applicable record should be enqueued after the scanner's job.");
            Assert.AreEqual(0, queueDepths[2], "The applicable record should be dequeued after the worker's job.");
        }

        [TestMethod, TestCategory("FCT")]
        public async Task RecurringDeletesProcessScheduleDbDocSuccess()
        {
            TestUser user = TestUsers.TimelineDeleteById;
            var preVerifier = await this.GetPreVerifier(user).ConfigureAwait(false);
            
            // Construct schedule db doc
            var originalScheduleDbDoc = this.CreateRecurrentDeleteScheduleDbDocument(user.Puid, "BrowsingHistory", preVerifier, 0);
            var jsonDoc = JsonConvert.SerializeObject(originalScheduleDbDoc);
            var stringContent = new StringContent(jsonDoc, UnicodeEncoding.UTF8, "application/json");
            HttpResponseMessage response = await TestHttpClient.PostAsync(new Uri(TestBaseUrl, "recurringdelete/processscheduledbdoc"), stringContent);

            // Assert
            Assert.IsNotNull(response);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var newScheduleDbDoc = await HttpHelper.HandleHttpResponseAsync<RecurrentDeleteScheduleDbDocument>(response).ConfigureAwait(false);
            Assert.AreEqual(originalScheduleDbDoc.DataType, newScheduleDbDoc.DataType);
            Assert.AreEqual(originalScheduleDbDoc.Puid, newScheduleDbDoc.Puid);
            Assert.AreEqual(originalScheduleDbDoc.NumberOfRetries, newScheduleDbDoc.NumberOfRetries, "Should not change since no retry should be triggered");
        }

        [TestMethod, TestCategory("FCT")]
        public async Task RecurringDeletesProcessScheduleDbDocErrorAndRetry()
        {
            TestUser user = TestUsers.TimelineDeleteById;
            string preverifier = await this.GetPreVerifier(user).ConfigureAwait(false);

            // Construct schedule db doc
            var originalScheduleDbDoc = this.CreateRecurrentDeleteScheduleDbDocument(user.Puid, "ContentConsumption", preverifier, 0);
            var jsonDoc = JsonConvert.SerializeObject(originalScheduleDbDoc);
            var stringContent = new StringContent(jsonDoc, UnicodeEncoding.UTF8, "application/json");
            HttpResponseMessage response = await TestHttpClient.PostAsync(new Uri(TestBaseUrl, "recurringdelete/processdocpcferror"), stringContent);

            // Assert
            Assert.IsNotNull(response);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var newScheduleDbDoc = await HttpHelper.HandleHttpResponseAsync<RecurrentDeleteScheduleDbDocument>(response).ConfigureAwait(false);
            Assert.AreEqual(originalScheduleDbDoc.DataType, newScheduleDbDoc.DataType);
            Assert.AreEqual(originalScheduleDbDoc.Puid, newScheduleDbDoc.Puid);
            Assert.AreEqual(originalScheduleDbDoc.RecurrentDeleteStatus, newScheduleDbDoc.RecurrentDeleteStatus, "Should still be active");
            Assert.IsTrue(newScheduleDbDoc.NumberOfRetries == originalScheduleDbDoc.NumberOfRetries + 1, "Should be increased by 1 since the retry is triggered.");
        }

        [TestMethod, TestCategory("FCT")]
        public async Task RecurringDeletesProcessScheduleDbDocRetryReachesLimit()
        {
            const int MaxNumberOfRetries = 10;
            TestUser user = TestUsers.TimelineDeleteById;
            string preverifier = await this.GetPreVerifier(user).ConfigureAwait(false);

            // Construct schedule db doc
            var originalScheduleDbDoc = this.CreateRecurrentDeleteScheduleDbDocument(user.Puid, "InkingTypingAndSpeechUtterance", preverifier, MaxNumberOfRetries);
            var jsonDoc = JsonConvert.SerializeObject(originalScheduleDbDoc);
            var stringContent = new StringContent(jsonDoc, UnicodeEncoding.UTF8, "application/json");
            HttpResponseMessage response = await TestHttpClient.PostAsync(new Uri(TestBaseUrl, "recurringdelete/processdocpcferror"), stringContent);

            // Assert
            Assert.IsNotNull(response);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var newScheduleDbDoc = await HttpHelper.HandleHttpResponseAsync<RecurrentDeleteScheduleDbDocument>(response).ConfigureAwait(false);
            Assert.AreEqual(originalScheduleDbDoc.DataType, newScheduleDbDoc.DataType);
            Assert.AreEqual(originalScheduleDbDoc.Puid, newScheduleDbDoc.Puid);
            Assert.AreEqual(originalScheduleDbDoc.NumberOfRetries, newScheduleDbDoc.NumberOfRetries, "Should not increase since already reached MaxNumberOfRetries");
            Assert.IsTrue(newScheduleDbDoc.RecurrentDeleteStatus == RecurrentDeleteStatus.Paused, "Should be set to Paused since already reached MaxNumberOfRetries");
        }

        private RecurrentDeleteScheduleDbDocument CreateRecurrentDeleteScheduleDbDocument(long puid, string dataType, string preVerifier, int numberOfRetries)
        {
            return new RecurrentDeleteScheduleDbDocument(
                    puidValue: puid,
                    dataType: dataType,
                    documentId: Guid.NewGuid().ToString(),
                    preVerifier: preVerifier,
                    preVerifierExpirationDateUtc: new DateTimeOffset(),
                    nextDeleteOccurrence: DateTime.UtcNow.AddDays(-1),
                    numberOfRetries: numberOfRetries,
                    status: RecurrentDeleteStatus.Active
                );
        }

        private async Task<string> GetPreVerifier(TestUser user)
        {
            string userProxyTicket = await MockTestHooks.GetProxyTicketAsync(user).ConfigureAwait(false);

            // Call MSA RVS with refresh claim to get a pre-verifier
            var requestContent = new FormUrlEncodedContent(new Dictionary<string, string>
                              {
                                { "userProxyTicket", userProxyTicket },
                                { "userPuid",  user.Puid.ToString()}
                              });

            var getPreVerifierResponse = await TestHttpClient.PostAsync(new Uri(TestBaseUrl, "msaRvs/GetGdprUserDeleteVerifierWithRefreshClaim"), requestContent).ConfigureAwait(false);
            var result = await HttpHelper.HandleHttpResponseAsync<AdapterResponse<string>>(getPreVerifierResponse).ConfigureAwait(false);
            return result.Result;
        }

        private async Task CreateRecurringDeleteScheduleAsync(TestUser testUser, string dataType, RecurringIntervalDays recurringIntervalDays)
        {
            string userProxyTicket = await MockTestHooks.GetProxyTicketAsync(testUser).ConfigureAwait(false);
            string cv = new CorrelationVector().ToString();
            string requestId = Guid.NewGuid().ToString();

            DateTimeOffset utcNow = DateTimeOffset.UtcNow;
            DateTimeOffset nextDeleteDate = utcNow.AddDays((int)recurringIntervalDays);

            DeleteRecurringDeletesArgs deleteRecurringDeletesArgs = new DeleteRecurringDeletesArgs(
                        userProxyTicket: userProxyTicket,
                        dataType: dataType)
            {
                CorrelationVector = cv,
                FamilyTicket = null,
                RequestId = requestId
            };

            // make sure records do not exists
            try
            {
                await S2SClient.DeleteRecurringDeletesAsync(deleteRecurringDeletesArgs).ConfigureAwait(false);
            }
            catch (PrivacyExperienceTransportException ex)
            {
                // Check the string from custom exception
                Assert.IsTrue(ex.Message.Contains("No record found in scheduledb for delete operation"));
            }

            // Create
            CreateOrUpdateRecurringDeletesArgs args = new CreateOrUpdateRecurringDeletesArgs(
                userProxyTicket: userProxyTicket,
                dataType: dataType,
                nextDeleteDate: nextDeleteDate,
                recurringIntervalDays: recurringIntervalDays)
            {
                CorrelationVector = cv,
                RequestId = requestId
            };

            await S2SClient.CreateOrUpdateRecurringDeletesAsync(args).ConfigureAwait(false);
        }
    }
}
