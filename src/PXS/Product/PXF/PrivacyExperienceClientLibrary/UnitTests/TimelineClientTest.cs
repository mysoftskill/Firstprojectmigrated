// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Formatting;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;

    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Models;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;
    using static Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2.TimelineCard;

    /// <summary>
    ///     TimelineClient Test
    /// </summary>
    [TestClass]
    public class TimelineClientTest : ClientTestBase
    {
        [TestMethod]
        public async Task DeleteTimelineAsyncTestHeaderValues()
        {
            PrivacyExperienceClient client = this.CreateBasicClient();
            var requestArgs = new DeleteTimelineByTypesArgs(this.TestUserProxyTicket, TimeSpan.FromDays(1), new[] { "foo" })
            {
                CorrelationVector = "test_correlationVector_value",
                RequestId = new Guid().ToString()
            };

            await client.DeleteTimelineAsync(requestArgs).ConfigureAwait(false);

            this.ValidateHttpClientMockHeader(HeaderNames.AccessToken, this.TestAccessToken);
            this.ValidateHttpClientMockHeader(HeaderNames.ProxyTicket, this.TestUserProxyTicket);
            this.ValidateHttpClientMockHeader(HeaderNames.ClientRequestId, requestArgs.RequestId);
            this.ValidateHttpClientMockHeader(HeaderNames.CorrelationVector, requestArgs.CorrelationVector);

            this.MockAuthClient.Verify(
                c => c.GetAccessTokenAsync(CancellationToken.None),
                Times.Once);
        }

        [TestMethod]
        public async Task DeleteTimelineByIdSuccess()
        {
            Func<HttpRequestMessage, bool> isValid = m =>
            {
                string data = m.Content.ReadAsStringAsync().Result;
                if (data != "[\"foo\",\"bar\"]")
                    return false;
                return true;
            };

            var expectedResponse = new DeleteResponseV1 { Status = DeleteStatusV1.Deleted };
            var mockHttpResponse = new HttpResponseMessage { Content = new ObjectContent<DeleteResponseV1>(expectedResponse, new JsonMediaTypeFormatter()) };
            this.MockHttpClient
                .Setup(c => c.SendAsync(It.Is<HttpRequestMessage>(r => isValid(r)), HttpCompletionOption.ResponseContentRead))
                .ReturnsAsync(mockHttpResponse)
                .Verifiable("Content was incorrect");
            PrivacyExperienceClient client = this.CreateBasicClient();

            await client.DeleteTimelineAsync(new DeleteTimelineByIdsArgs(this.TestUserProxyTicket, new[] { "foo", "bar" })).ConfigureAwait(false);

            this.MockHttpClient.Verify();
        }

        [TestMethod]
        public async Task DeleteTimelineDefaultArgsShouldTargetCorrectApiPath()
        {
            string expectedApiPath = "v2/timeline?types=foo&period=1.00%3a00%3a00";

            PrivacyExperienceClient client = this.CreateBasicClient();

            await client.DeleteTimelineAsync(new DeleteTimelineByTypesArgs(this.TestUserProxyTicket, TimeSpan.FromDays(1), new[] { "foo" })).ConfigureAwait(false);

            this.MockHttpClient.Verify(
                c => c.SendAsync(It.Is<HttpRequestMessage>(m => m.RequestUri.OriginalString.Equals(expectedApiPath)), HttpCompletionOption.ResponseContentRead));
        }

        [TestMethod]
        [ExpectedException(typeof(PrivacyExperienceTransportException))]
        public async Task DeleteTimelineError()
        {
            var expectedResponse = new Error(ErrorCode.InvalidInput, "Invalid input test");
            var mockHttpResponse = new HttpResponseMessage
            {
                Content = new ObjectContent<Error>(expectedResponse, new JsonMediaTypeFormatter()),
                StatusCode = HttpStatusCode.BadRequest
            };
            this.MockHttpClient
                .Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>(), HttpCompletionOption.ResponseContentRead)).ReturnsAsync(mockHttpResponse);
            PrivacyExperienceClient client = this.CreateBasicClient();

            await this.ExecuteAndAssertExceptionMatchesAsync(
                () => client.DeleteTimelineAsync(new DeleteTimelineByTypesArgs(this.TestUserProxyTicket, TimeSpan.FromDays(1), new[] { "foo" })),
                expectedResponse,
                HttpStatusCode.BadRequest).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeleteTimelineSuccess()
        {
            var expectedResponse = new DeleteResponseV1 { Status = DeleteStatusV1.Deleted };
            var mockHttpResponse = new HttpResponseMessage { Content = new ObjectContent<DeleteResponseV1>(expectedResponse, new JsonMediaTypeFormatter()) };
            this.MockHttpClient
                .Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>(), HttpCompletionOption.ResponseContentRead))
                .ReturnsAsync(mockHttpResponse);
            PrivacyExperienceClient client = this.CreateBasicClient();

            await client.DeleteTimelineAsync(new DeleteTimelineByTypesArgs(this.TestUserProxyTicket, TimeSpan.FromDays(1), new[] { "foo" })).ConfigureAwait(false);
        }

        [ExpectedException(typeof(ArgumentNullException))]
        [TestMethod]
        public async Task DeleteTimelineThrowExceptionNullArgs()
        {
            PrivacyExperienceClient client = this.CreateBasicClient();

            try
            {
                await client.DeleteTimelineAsync((DeleteTimelineByTypesArgs)null).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Assert.AreEqual("Value cannot be null.\r\nParameter name: args", ex.Message);
                throw;
            }

            Assert.Fail("Exception should have been thrown.");
        }

        [ExpectedException(typeof(ArgumentNullException))]
        [TestMethod]
        public async Task DeleteTimelineThrowExceptionNullProxyTicket()
        {
            PrivacyExperienceClient client = this.CreateBasicClient();

            try
            {
                await client.DeleteTimelineAsync(new DeleteTimelineByTypesArgs(null, TimeSpan.FromDays(1), new[] { "foo" })).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Assert.AreEqual("Value cannot be null.\r\nParameter name: userProxyTicket", ex.Message);
                throw;
            }

            Assert.Fail("Exception should have been thrown.");
        }

        [TestMethod]
        public async Task GetTimelineAsyncTestHeaderValues()
        {
            PrivacyExperienceClient client = this.CreateBasicClient();
            var requestArgs = new GetTimelineArgs(
                this.TestUserProxyTicket,
                new[] { TimelineCard.CardTypes.AppUsageCard },
                null,
                null,
                null,
                null,
                TimeSpan.Zero,
                DateTimeOffset.UtcNow)
            {
                CorrelationVector = "test_correlationVector_value",
                RequestId = new Guid().ToString()
            };

            await client.GetTimelineAsync(requestArgs).ConfigureAwait(false);

            this.MockHttpClient.Verify(
                c => c.SendAsync(
                    It.Is<HttpRequestMessage>(
                        m =>
                            m.Headers.Contains(HeaderNames.AccessToken) &&
                            m.Headers.GetValues(HeaderNames.AccessToken).FirstOrDefault().Equals(this.TestAccessToken)),
                    HttpCompletionOption.ResponseContentRead));

            this.ValidateHttpClientMockHeader(HeaderNames.AccessToken, this.TestAccessToken);
            this.ValidateHttpClientMockHeader(HeaderNames.ProxyTicket, this.TestUserProxyTicket);
            this.ValidateHttpClientMockHeader(HeaderNames.ClientRequestId, requestArgs.RequestId);
            this.ValidateHttpClientMockHeader(HeaderNames.CorrelationVector, requestArgs.CorrelationVector);

            this.MockAuthClient.Verify(
                c => c.GetAccessTokenAsync(CancellationToken.None),
                Times.Once);
        }

        [TestMethod]
        public async Task GetTimelineDefaultArgsShouldTargetCorrectApiPath()
        {
            DateTimeOffset startingAt = DateTimeOffset.UtcNow;
            string expectedApiPath = $"v3/timeline?cardTypes=AppUsageCard&timeZoneOffset=00%3a00%3a00&startingAt={HttpUtility.UrlEncode(startingAt.ToString("o"))}";
            PrivacyExperienceClient client = this.CreateBasicClient();

            await
                client.GetTimelineAsync(
                        new GetTimelineArgs(this.TestUserProxyTicket, new[] { TimelineCard.CardTypes.AppUsageCard }, null, null, null, null, TimeSpan.Zero, startingAt))
                    .ConfigureAwait(false);

            this.MockHttpClient.Verify(
                c => c.SendAsync(It.Is<HttpRequestMessage>(m => m.RequestUri.OriginalString.Equals(expectedApiPath)), HttpCompletionOption.ResponseContentRead));
        }

        [TestMethod]
        [ExpectedException(typeof(PrivacyExperienceTransportException))]
        public async Task GetTimelineError()
        {
            var expectedResponse = new Error(ErrorCode.InvalidInput, "Invalid input test");
            var mockHttpResponse = new HttpResponseMessage
            {
                Content = new ObjectContent<Error>(expectedResponse, new JsonMediaTypeFormatter()),
                StatusCode = HttpStatusCode.BadRequest
            };
            this.MockHttpClient
                .Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>(), HttpCompletionOption.ResponseContentRead)).ReturnsAsync(mockHttpResponse);
            PrivacyExperienceClient client = this.CreateBasicClient();

            await this.ExecuteAndAssertExceptionMatchesAsync(
                () => client.GetTimelineAsync(
                    new GetTimelineArgs(this.TestUserProxyTicket, new[] { TimelineCard.CardTypes.AppUsageCard }, null, null, null, null, TimeSpan.Zero, DateTimeOffset.UtcNow)),
                expectedResponse,
                HttpStatusCode.BadRequest).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task GetTimelineMultipleArgsShouldTargetCorrectApiPath()
        {
            DateTimeOffset startingAt = DateTimeOffset.UtcNow;
            string expectedApiPath =
                $"v3/timeline?cardTypes=AppUsageCard%2cVoiceCard&count=200&search=foobar&timeZoneOffset=00%3a00%3a00&startingAt={HttpUtility.UrlEncode(startingAt.ToString("o"))}";

            PrivacyExperienceClient client = this.CreateBasicClient();
            var args = new GetTimelineArgs(
                this.TestUserProxyTicket,
                new[] { TimelineCard.CardTypes.AppUsageCard, TimelineCard.CardTypes.VoiceCard },
                200,
                null,
                null,
                "foobar",
                TimeSpan.Zero,
                startingAt);

            await client.GetTimelineAsync(args).ConfigureAwait(false);

            this.MockHttpClient.Verify(
                c => c.SendAsync(
                    It.Is<HttpRequestMessage>(m => m.RequestUri.OriginalString.Equals(expectedApiPath)),
                    HttpCompletionOption.ResponseContentRead));
        }

        [TestMethod]
        public async Task GetTimelineNextPageShouldTargetCorrectApiPath()
        {
            var expectedApiPath = new Uri("https://testendpoint.com/v3/timeline/randomContinuationTokenValue");
            PrivacyExperienceClient client = this.CreateBasicClient();
            var args = new PrivacyExperienceClientBaseArgs(this.TestUserProxyTicket);

            await client.GetTimelineNextPageAsync(args, expectedApiPath).ConfigureAwait(false);

            this.MockHttpClient.Verify(
                c => c.SendAsync(
                    It.Is<HttpRequestMessage>(m => m.RequestUri.Equals(expectedApiPath)),
                    HttpCompletionOption.ResponseContentRead));
        }

        [TestMethod]
        public async Task GetTimelineNextPageTestHeaderValues()
        {
            PrivacyExperienceClient client = this.CreateBasicClient();
            var args = new PrivacyExperienceClientBaseArgs(this.TestUserProxyTicket);
            var expectedApiPath = new Uri("https://testendpoint.com/v3/timeline/randomContinuationTokenValue");
            args.CorrelationVector = "test_correlationVector_value";
            args.RequestId = new Guid().ToString();

            await client.GetTimelineNextPageAsync(args, expectedApiPath).ConfigureAwait(false);

            this.ValidateHttpClientMockHeader(HeaderNames.AccessToken, this.TestAccessToken);
            this.ValidateHttpClientMockHeader(HeaderNames.ProxyTicket, this.TestUserProxyTicket);
            this.ValidateHttpClientMockHeader(HeaderNames.ClientRequestId, args.RequestId);
            this.ValidateHttpClientMockHeader(HeaderNames.CorrelationVector, args.CorrelationVector);

            this.MockAuthClient.Verify(c => c.GetAccessTokenAsync(CancellationToken.None), Times.Once);
        }

        [TestMethod]
        public async Task GetTimelineSuccess()
        {
            var expectedResponse = new Privacy.ExperienceContracts.V2.PagedResponse<TimelineCard>
            {
                Items =
                    new List<TimelineCard>
                    {
                        new AppUsageCard("appId", "aggregation", "#ffffff", new Uri("https://unittest"), "appName", "appPublisher", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1), null, null, null)
                    }
            };
            var jsonMediaTypeFormatter = new JsonMediaTypeFormatter();
            jsonMediaTypeFormatter.SerializerSettings.SerializationBinder = new TimelineCardBinder();
            var mockHttpResponse = new HttpResponseMessage
            {
                Content = new ObjectContent<Privacy.ExperienceContracts.V2.PagedResponse<TimelineCard>>(expectedResponse, jsonMediaTypeFormatter)
            };
            this.MockHttpClient
                .Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>(), HttpCompletionOption.ResponseContentRead)).ReturnsAsync(mockHttpResponse);
            PrivacyExperienceClient client = this.CreateBasicClient();

            Privacy.ExperienceContracts.V2.PagedResponse<TimelineCard> actualResponse =
                await
                    client.GetTimelineAsync(
                            new GetTimelineArgs(
                                this.TestUserProxyTicket,
                                new[] { TimelineCard.CardTypes.AppUsageCard },
                                null,
                                null,
                                null,
                                null,
                                TimeSpan.Zero,
                                DateTimeOffset.UtcNow))
                        .ConfigureAwait(false);

            Assert.IsNotNull(actualResponse);
            EqualityHelper.AreEqual(expectedResponse, actualResponse);
        }

        [ExpectedException(typeof(ArgumentNullException))]
        [TestMethod]
        public async Task GetTimelineThrowExceptionNullArgs()
        {
            PrivacyExperienceClient client = this.CreateBasicClient();

            try
            {
                await client.GetTimelineAsync(null).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Assert.AreEqual("Value cannot be null.\r\nParameter name: args", ex.Message);
                throw;
            }

            Assert.Fail("Exception should have been thrown.");
        }

        [ExpectedException(typeof(ArgumentNullException))]
        [TestMethod]
        public async Task GetTimelineThrowExceptionNullNextLink()
        {
            PrivacyExperienceClient client = this.CreateBasicClient();
            var args = new GetTimelineArgs(this.TestUserProxyTicket, new[] { TimelineCard.CardTypes.AppUsageCard }, null, null, null, null, TimeSpan.Zero, DateTimeOffset.UtcNow);

            try
            {
                await client.GetTimelineNextPageAsync(args, null).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Assert.AreEqual("Value cannot be null.\r\nParameter name: nextLink", ex.Message);
                throw;
            }

            Assert.Fail("Exception should have been thrown.");
        }

        [ExpectedException(typeof(ArgumentNullException))]
        [TestMethod]
        public async Task GetTimelineThrowExceptionNullProxyTicket()
        {
            PrivacyExperienceClient client = this.CreateBasicClient();

            try
            {
                await client.GetTimelineAsync(
                    new GetTimelineArgs(null, new[] { TimelineCard.CardTypes.AppUsageCard }, null, null, null, null, TimeSpan.Zero, DateTimeOffset.UtcNow)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Assert.AreEqual("Value cannot be null.\r\nParameter name: userProxyTicket", ex.Message);
                throw;
            }

            Assert.Fail("Exception should have been thrown.");
        }

        [TestMethod]
        public async Task GetTimelineAggregateCountSuccess()
        {
            var counts = new Dictionary<string, int>();

            counts.Add(CardTypes.AppUsageCard, 15);
            counts.Add(CardTypes.BrowseCard, 12);

            
            AggregateCountResponse expectedResponse = new AggregateCountResponse()
            {
                AggregateCounts = counts
            };

            var jsonMediaTypeFormatter = new JsonMediaTypeFormatter();
            jsonMediaTypeFormatter.SerializerSettings.SerializationBinder = new TimelineCardBinder();
            var mockHttpResponse = new HttpResponseMessage
            {
                Content = new ObjectContent<AggregateCountResponse>(expectedResponse, jsonMediaTypeFormatter)
            };
            this.MockHttpClient
                .Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>(), HttpCompletionOption.ResponseContentRead)).ReturnsAsync(mockHttpResponse);
            PrivacyExperienceClient client = this.CreateBasicClient();
            AggregateCountResponse actualResponse =
                await
                    client.GetAggregateCountAsync(
                        new GetTimelineAggregateCountArgs(
                            this.TestUserProxyTicket,
                            new[] { TimelineCard.CardTypes.AppUsageCard }
                        ))
                    .ConfigureAwait(false);

            Assert.IsNotNull(actualResponse);
            EqualityHelper.AreEqual(expectedResponse, actualResponse);
        }

        [TestMethod]
        public async Task GetTimelineAggregateCountResponseMismatch()
        {
            var expectedCount = new Dictionary<string, int>();

            expectedCount.Add(CardTypes.AppUsageCard, 15);
            expectedCount.Add(CardTypes.BrowseCard, 12);

            var countMismatch = new Dictionary<string, int>();

            countMismatch.Add(CardTypes.AppUsageCard, 40);
            countMismatch.Add(CardTypes.BrowseCard, 30);

            AggregateCountResponse expectedResponse = new AggregateCountResponse()
            {
                AggregateCounts = expectedCount
            };
            AggregateCountResponse countResponse = new AggregateCountResponse()
            {
                AggregateCounts = countMismatch
            };

            var jsonMediaTypeFormatter = new JsonMediaTypeFormatter();
            jsonMediaTypeFormatter.SerializerSettings.SerializationBinder = new TimelineCardBinder();
            var mockHttpResponse = new HttpResponseMessage
            {
                Content = new ObjectContent<AggregateCountResponse>(countResponse, jsonMediaTypeFormatter)
            };
            this.MockHttpClient
                .Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>(), HttpCompletionOption.ResponseContentRead)).ReturnsAsync(mockHttpResponse);
            PrivacyExperienceClient client = this.CreateBasicClient();
            AggregateCountResponse actualResponse =
                await
                    client.GetAggregateCountAsync(
                        new GetTimelineAggregateCountArgs(
                            this.TestUserProxyTicket,
                            new[] { TimelineCard.CardTypes.AppUsageCard }
                        ))
                    .ConfigureAwait(false);

            Assert.IsNotNull(actualResponse);
            try
            {
                EqualityHelper.AreEqual(expectedResponse, actualResponse);
                Assert.Fail("A mismatch should be detected");
            }
            catch (Exception ex)
            {
                Assert.AreEqual("Assert.AreEqual failed. Expected:<15>. Actual:<40>. ", ex.Message);
                
            }
        }

        [ExpectedException(typeof(ArgumentNullException))]
        [TestMethod]
        public async Task GetTimelineAggregateCountThrowExceptionNullProxyTicket()
        {
            PrivacyExperienceClient client = this.CreateBasicClient();

            try
            {
                await client.GetAggregateCountAsync( new GetTimelineAggregateCountArgs(this.TestUserProxyTicket,null)).ConfigureAwait(false); ;
            }
            catch (Exception ex)
            {
                Assert.AreEqual("Value cannot be null.\r\nParameter name: cardTypes", ex.Message);
                throw;
            }

            Assert.Fail("Exception should have been thrown.");
        }

        [TestMethod]
        public async Task GetAggregateCountArgsShouldTargetCorrectApiPath()
        {
            string expectedApiPath = $"v1/timelineaggregatecount?cardTypes=AppUsageCard%2cBrowseCard";
            PrivacyExperienceClient client = this.CreateBasicClient();

            await client.GetAggregateCountAsync(new GetTimelineAggregateCountArgs(this.TestUserProxyTicket, new[] { TimelineCard.CardTypes.AppUsageCard , TimelineCard.CardTypes.BrowseCard}));

            this.MockHttpClient.Verify(
                c => c.SendAsync(It.Is<HttpRequestMessage>(m => m.RequestUri.OriginalString.Equals(expectedApiPath)), HttpCompletionOption.ResponseContentRead));
        }

        [TestMethod]
        [ExpectedException(typeof(PrivacyExperienceTransportException))]
        public async Task GetAggregateCountError()
        {
            var expectedResponse = new Error(ErrorCode.InvalidInput, "Invalid input test");
            var mockHttpResponse = new HttpResponseMessage
            {
                Content = new ObjectContent<Error>(expectedResponse, new JsonMediaTypeFormatter()),
                StatusCode = HttpStatusCode.BadRequest
            };
            this.MockHttpClient
                .Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>(), HttpCompletionOption.ResponseContentRead)).ReturnsAsync(mockHttpResponse);
            PrivacyExperienceClient client = this.CreateBasicClient();

            await this.ExecuteAndAssertExceptionMatchesAsync(
                () => client.GetAggregateCountAsync(new GetTimelineAggregateCountArgs(this.TestUserProxyTicket, new[] { TimelineCard.CardTypes.AppUsageCard, TimelineCard.CardTypes.BrowseCard })),
                expectedResponse,
                HttpStatusCode.BadRequest).ConfigureAwait(false);
        }

        [ExpectedException(typeof(ArgumentNullException))]
        [TestMethod]
        public async Task PrivacyExperienceClientThrowExceptionIfCreateOrUpdateRecurringDeletesArgsIsNull()
        {
            PrivacyExperienceClient client = this.CreateBasicClient();

            try
            {
                await client.CreateOrUpdateRecurringDeletesAsync(null).ConfigureAwait(false); ;
            }
            catch (Exception ex)
            {
                Assert.AreEqual("Value cannot be null.\r\nParameter name: args", ex.Message);
                throw;
            }

            Assert.Fail("Exception should have been thrown.");
        }

        [ExpectedException(typeof(ArgumentNullException))]
        [TestMethod]
        public async Task DeleteRecurringDeletesArgIsNull()
        {
            PrivacyExperienceClient client = this.CreateBasicClient();

            try
            {
                await client.DeleteRecurringDeletesAsync(null).ConfigureAwait(false); ;
            }
            catch (Exception ex)
            {
                Assert.AreEqual("Value cannot be null.\r\nParameter name: args", ex.Message);
                throw;
            }

            Assert.Fail("Exception should have been thrown.");
        }

        [ExpectedException(typeof(ArgumentNullException))]
        [TestMethod]
        public async Task GetRecurringDeletesArgIsNull()
        {
            PrivacyExperienceClient client = this.CreateBasicClient();

            try
            {
                await client.GetRecurringDeletesAsync(null).ConfigureAwait(false); ;
            }
            catch (Exception ex)
            {
                Assert.AreEqual("Value cannot be null.\r\nParameter name: args", ex.Message);
                throw;
            }

            Assert.Fail("Exception should have been thrown.");
        }

        [TestMethod]
        public async Task GetRecurringDeletesSuccess()
        {
            var list = new List<GetRecurringDeleteResponse>();
            var now = DateTimeOffset.Now;
            var puidValue = 123;
            var numberOfRetries = 0;
            var maxNumberOfRetries = 10;
            var status = RecurrentDeleteStatus.Active;
            var recurringIntervalDays = RecurringIntervalDays.Days2;

            list.Add(new GetRecurringDeleteResponse(
                puidValue: puidValue,
                dataType: TimelineCard.CardTypes.BrowseCard,
                createDate: now,
                updateDate: now,
                lastDeleteOccurrence: now,
                nextDeleteOccurrence: now,
                lastSucceededDeleteOccurrence: now,
                numberOfRetries: numberOfRetries,
                maxNumberOfRetries: maxNumberOfRetries,
                status: status,
                recurringIntervalDays: recurringIntervalDays));

            list.Add(new GetRecurringDeleteResponse(
                puidValue: puidValue,
                dataType: TimelineCard.CardTypes.BrowseCard,
                createDate: now,
                updateDate: now,
                lastDeleteOccurrence: now,
                nextDeleteOccurrence: now,
                lastSucceededDeleteOccurrence: now,
                numberOfRetries: numberOfRetries,
                maxNumberOfRetries: maxNumberOfRetries,
                status: RecurrentDeleteStatus.Failed,
                recurringIntervalDays: RecurringIntervalDays.Days180));

            IList<GetRecurringDeleteResponse> expectedResponse = list;

            var jsonMediaTypeFormatter = new JsonMediaTypeFormatter();
            jsonMediaTypeFormatter.SerializerSettings.SerializationBinder = new TimelineCardBinder();
            var mockHttpResponse = new HttpResponseMessage
            {
                Content = new ObjectContent<IList<GetRecurringDeleteResponse>>(expectedResponse, jsonMediaTypeFormatter)
            };

            this.MockHttpClient
                .Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>(), HttpCompletionOption.ResponseContentRead)).ReturnsAsync(mockHttpResponse);

            PrivacyExperienceClient client = this.CreateBasicClient();
            IList<GetRecurringDeleteResponse> actualResponse =
                await
                    client.GetRecurringDeletesAsync(
                        new PrivacyExperienceClientBaseArgs(this.TestUserProxyTicket)
                        { 
                            CorrelationVector = "cv",
                            RequestId = Guid.NewGuid().ToString()
                        })
                    .ConfigureAwait(false);

            Assert.IsNotNull(actualResponse);
            EqualityHelper.AreEqual<GetRecurringDeleteResponse>(expectedResponse, actualResponse, EqualityHelper.AreEqual);
        }

        [TestMethod]
        public async Task CreateOrUpdateRecurringDeletesSuccess()
        {
            var now = DateTimeOffset.Now;
            var puidValue = 123;
            var numberOfRetries = 0;
            var maxNumberOfRetries = 10;
            var dataType = TimelineCard.CardTypes.BrowseCard;
            var interval = RecurringIntervalDays.Days180;
            var status = RecurrentDeleteStatus.Failed;

            GetRecurringDeleteResponse expectedResponse = new GetRecurringDeleteResponse(
                puidValue: puidValue,
                dataType: dataType,
                createDate: now,
                updateDate: now,
                lastDeleteOccurrence: now,
                nextDeleteOccurrence: now,
                lastSucceededDeleteOccurrence: now,
                numberOfRetries: numberOfRetries,
                maxNumberOfRetries: maxNumberOfRetries,
                status: status,
                recurringIntervalDays: interval);


            var jsonMediaTypeFormatter = new JsonMediaTypeFormatter();
            jsonMediaTypeFormatter.SerializerSettings.SerializationBinder = new TimelineCardBinder();
            var mockHttpResponse = new HttpResponseMessage
            {
                Content = new ObjectContent<GetRecurringDeleteResponse>(expectedResponse, jsonMediaTypeFormatter)
            };

            this.MockHttpClient
                .Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>(), HttpCompletionOption.ResponseContentRead)).ReturnsAsync(mockHttpResponse);

            PrivacyExperienceClient client = this.CreateBasicClient();
            GetRecurringDeleteResponse actualResponse =
                await
                    client.CreateOrUpdateRecurringDeletesAsync( new CreateOrUpdateRecurringDeletesArgs(
                            userProxyTicket: this.TestUserProxyTicket,
                            dataType: dataType,
                            nextDeleteDate: now,
                            recurringIntervalDays: interval,
                            status: status)
                        {
                            CorrelationVector = "cv",
                            RequestId = Guid.NewGuid().ToString()
                        })
                    .ConfigureAwait(false);

            Assert.IsNotNull(actualResponse);
            EqualityHelper.AreEqual(expectedResponse, actualResponse);
        }

        private static CreateOrUpdateRecurringDeletesArgs CreateBasicCreateOrUpdateRecurringDeletesArgs()
        {
            CreateOrUpdateRecurringDeletesArgs args = new CreateOrUpdateRecurringDeletesArgs(
                userProxyTicket: "userProxyTicket",
                dataType: TimelineCard.CardTypes.AppUsageCard,
                nextDeleteDate: DateTimeOffset.Now,
                recurringIntervalDays: RecurringIntervalDays.Days30,
                status: RecurrentDeleteStatus.Active)
            {
                RequestId = Guid.NewGuid().ToString()
            };

            return args;
        }
    }
}
