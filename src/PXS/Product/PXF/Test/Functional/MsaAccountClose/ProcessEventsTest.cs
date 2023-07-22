// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.FunctionalTests.MsaAccountClose
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Formatting;
    using System.Threading.Tasks;
    using System.Web;
    using Microsoft.Membership.MemberServices.Privacy.Core.AQS;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.Utilities;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;

    public class UserItem
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("isClose")]
        public bool IsClose { get; set; }

        [JsonProperty("eventData")]
        public EventData EventData { get; set; }
    }

    [TestClass]
    public class ProcessEventsTest
    {
        [TestCategory("FCT")]
        [DataTestMethod]
        [DataRow(2, RequestType.AgeOut, true)]
        [DataRow(2, RequestType.AgeOut, false)]
        [DataRow(1, RequestType.AccountClose, false)]
        public async Task ShouldProcessMsaUserDelete(int closeReason, RequestType expectedRequestType, bool isSuspended)
        {
            long id = GenerateFakePuid();
            Uri uri = GenerateAddWorkUri(id);

            var evtData = new UserDelete
            {
                Property = new[]
                {
                    new EventDataBaseProperty
                    {
                        Name = "CredentialName",
                        // Last signin and suspended is ignored for non age out
                        ExtendedData = $"UserDeleteReason:{closeReason},cid:{id:X16},GdprPreVerifier:gdpr,LastSuccessSignIn:2000-01-01T14:05:00Z,Suspended:{isSuspended}"
                    }
                }
            };

            using (var response = await SendRequestAsync(HttpMethod.Post, uri, evtData))
            {
                Assert.IsTrue(response.IsSuccessStatusCode, response.ReasonPhrase);
            }

            PrivacyRequest result = await GetExpectedPrivacyRequestAsync(id);
            Assert.IsNotNull(result, "Request failed reaching PCF in time limit");
            Assert.AreEqual(expectedRequestType, result.RequestType);
        }

        [TestCategory("FCT")]
        [DataTestMethod]
        [DataRow(10)]
        public async Task ShouldProcessMultipleUserDeleteItems(int numItems)
        {
            List<UserItem> data = Enumerable.Repeat(0L, numItems).Select(_ => GenerateFakePuid()).Select(id => new UserItem
            {
                Id = id,
                IsClose = true,
                EventData = new UserDelete
                {
                    Property = new[]
                    {
                        new EventDataBaseProperty
                        {
                            Name = "CredentialName",
                            // Last signin and suspended is ignored for non age out
                            ExtendedData = $"UserDeleteReason:2,cid:{id:X16},GdprPreVerifier:gdpr,LastSuccessSignIn:2000-01-01T14:05:00Z,Suspended:true"
                        }
                    }
                }
            }).ToList();

            Uri uri = GetAddMultipleWorkUri();
            using (var response = await SendRequestAsync(HttpMethod.Post, uri, data))
            {
                Assert.IsTrue(response.IsSuccessStatusCode);
            }

            List<long> expectedIds = data.Select(item => item.Id).ToList();
            IEnumerable<Task<PrivacyRequest>> tasks = expectedIds.Select(id => GetExpectedPrivacyRequestAsync(id));
            PrivacyRequest[] requests = await Task.WhenAll(tasks);
            Assert.IsTrue(requests.All(r => r != null), "An expected result may be missing");
        }

        private static Lazy<Random> rand = new Lazy<Random>(() => new Random());

        private static Random Random => rand.Value;

        private static async Task<PrivacyRequest> GetExpectedPrivacyRequestAsync(long puid)
        {
            // Assert: Validate that the event was picked up and was sent to PCF
            // Because we don't know a way to query PCF for this in a test environment, we are using the mock service to store the events for test validation.
            // Poll and wait because the worker is async. This time needs to be reliable for the test to properly function.
            PrivacyRequest result = null;
            await PollingUtility.PollForCondition(
                iterationWait: TimeSpan.FromSeconds(2),
                maximumPollRetry: 30,
                conditionCheck: async () =>
                {
                    result = await MockTestHooks.GetPrivacyRequestAsync(puid);
                    if (result == null)
                    {
                        Console.WriteLine("Data not found in storage yet.");
                    }

                    return result != null;
                }
            );

            return result;
        }

        private static Uri GetAddMultipleWorkUri(string queueName = "MeePXS-LiveIDNotifications")
        {
            var query = HttpUtility.ParseQueryString("");
            query.Add("queueName", queueName);

            return new Uri($"aqs/AddMultipleWork?{query.ToString()}", UriKind.Relative);
        }

        private static Uri GenerateAddWorkUri(long puid, string queueName = "MeePXS-LiveIDNotifications")
        {
            var query = HttpUtility.ParseQueryString("");
            query.Add("queueName", queueName);
            query.Add("isClose", true.ToString());
            query.Add("puid", puid.ToString());

            return new Uri($"aqs/AddWork?{query.ToString()}", UriKind.Relative);
        }

        private static long GenerateFakePuid()
        {
            // Make a fake puid
            long hi = Random.Next(int.MaxValue);
            long low = Random.Next(int.MaxValue);
            long id = (hi << 32) | low;

            return id;
        }

        private static async Task<HttpResponseMessage> SendRequestAsync<TBody>(HttpMethod method, Uri uri, TBody body)
        {
            Uri baseUri = Test.Common.Config.TestConfiguration.MockBaseUrl.Value;

            using (var httpMessageHandler = new WebRequestHandler())
            using (var httpClient = new OSGS.HttpClientCommon.HttpClient(httpMessageHandler))
            using (var requestMessage = new HttpRequestMessage(method, new Uri(baseUri, uri)))
            {
                if (body != null)
                {
                    requestMessage.Content = new ObjectContent<TBody>(body, new JsonMediaTypeFormatter());
                }

                return await httpClient.SendAsync(requestMessage);
            }
        }
    }
}
