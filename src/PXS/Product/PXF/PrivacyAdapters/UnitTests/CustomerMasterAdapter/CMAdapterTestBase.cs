// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyAdapters.UnitTests.CustomerMasterAdapter
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.CustomerMasterAdapter;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.CustomerMasterAdapter.Models;
    using Microsoft.Membership.MemberServices.Test.Common;
    using Microsoft.OSGS.HttpClientCommon;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Windows.Services.AuthN.Client.S2S;
    using Moq;
    using Newtonsoft.Json.Linq;

    using Microsoft.PrivacyServices.Common.Azure;


    public abstract class CMAdapterTestBase : TestBase
    {
        protected const string PartnerBaseUrl = "https://servicexyz.com/";
        protected const string PartnerS2STargetSite = "microsoft.com";

        /// <summary>
        /// Configures mocks needed to simulate outbound calls to PXF providers
        /// </summary>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="onCall">Callback method when the outbound GET call is made</param>
        /// <param name="createResponse">Callback function that generates the response returned</param>
        /// <param name="httpClientMock">out: the IHttpClient mock</param>
        /// <param name="requestContextMock">out: The request context mock</param>
        /// <returns></returns>
        protected ICustomerMasterAdapter ConfigureMockAdapterToSend<TResponse>(
            Action<HttpRequestMessage, HttpCompletionOption> onCall,
            Func<TResponse> createResponse,
            out Mock<IHttpClient> httpClientMock,
            out Mock<IPxfRequestContext> requestContextMock)
        {
            return this.ConfigureMockAdapterToSend(
                onCall,
                new HttpResponseMessage(HttpStatusCode.OK) { Content = this.CreateHttpContent(createResponse()) },
                out httpClientMock,
                out requestContextMock);
        }

        /// <summary>
        /// Configures mocks needed to simulate outbound calls to PXF providers
        /// </summary>
        /// <param name="onCall">Callback method when the outbound GET call is made</param>
        /// <param name="responseMessage">The response returned</param>
        /// <param name="httpClientMock">out: the IHttpClient mock</param>
        /// <param name="requestContextMock">out: The request context mock</param>
        protected ICustomerMasterAdapter ConfigureMockAdapterToSend(
            Action<HttpRequestMessage, HttpCompletionOption> onCall,
            HttpResponseMessage responseMessage,
            out Mock<IHttpClient> httpClientMock,
            out Mock<IPxfRequestContext> requestContextMock)
        {
            var logger = new ConsoleLogger();

            // Set up mock http
            httpClientMock = new Mock<IHttpClient>(MockBehavior.Strict);
            httpClientMock
                .Setup(h => h.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<HttpCompletionOption>()))
                .Callback<HttpRequestMessage, HttpCompletionOption>(onCall)
                .ReturnsAsync(responseMessage);

            // Set up partner config
            var partnerConfig = new Mock<IPrivacyPartnerAdapterConfiguration>(MockBehavior.Strict);
            partnerConfig.SetupGet(c => c.BaseUrl).Returns(PartnerBaseUrl);
            partnerConfig.SetupGet(c => c.MsaS2STargetSite).Returns(PartnerS2STargetSite);
            partnerConfig.SetupGet(c => c.CounterCategoryName).Returns("MockPartnerCounterCategoryName");

            // Set up request Context
            requestContextMock = new Mock<IPxfRequestContext>(MockBehavior.Strict);
            requestContextMock.SetupGet(r => r.UserProxyTicket).Returns("{myuserproxyticketgoeshere}");
            requestContextMock.SetupGet(r => r.CV).Returns(new CorrelationVector());
            requestContextMock.SetupGet(r => r.AuthorizingPuid).Returns(12345);
            requestContextMock.SetupGet(r => r.TargetPuid).Returns(12345);
            requestContextMock.SetupGet(r => r.TargetCid).Returns(12345);
            requestContextMock.SetupGet(r => r.FamilyJsonWebToken).Returns((string)null);
            requestContextMock.SetupGet(r => r.Country).Returns("US");

            var mockAuthClient = new Mock<IS2SAuthClient>(MockBehavior.Strict);
            mockAuthClient
                .Setup(m => m.GetAccessTokenAsync(It.IsAny<string>(), CancellationToken.None, null))
                .ReturnsAsync("{myaccesstoken");

            return new CustomerMasterAdapter(httpClientMock.Object, partnerConfig.Object, mockAuthClient.Object, logger);
        }

        protected Profiles CreateGetPrivacyProfilesResponse(int numberItems = 1)
        {
            var profiles = new Profiles { Items = new List<JObject>() };

            for (int i = 0; i < numberItems; i++)
            {
                profiles.Items.Add(JObject.Parse(CreateResponseContent())["items"].First as JObject);
            }

            return profiles;
        }

        protected static string CreateResponseContent(string additionalContent = @"""advertising"": true, ""tailored_experiences_offers"": true, ""sharing_state"": true,")
        {
            return
@"{
  ""total_count"": 1,
  ""items"": [
    {
      ""object_type"": ""Profile"",
      ""id"": ""89f56b74-3ada-5676-7095-f55d2ec64abf"",
      ""type"": ""msa_privacy"",
      ""customer_id"": ""05f08f8bc88848628943d2b8aa2b436901""," +

additionalContent +

      @"""etag"": ""-3454209939327893012"",
      ""snapshot_id"": ""89f56b74-3ada-5676-7095-f55d2ec64abf/1"",
      ""resource_status"": ""Active"",
      ""links"": {
        ""self"": {
          ""href"": ""05f08f8bc88848628943d2b8aa2b436901/profiles/89f56b74-3ada-5676-7095-f55d2ec64abf"",
          ""method"": ""GET""
        },
        ""snapshot"": {
          ""href"": ""05f08f8bc88848628943d2b8aa2b436901/profiles/89f56b74-3ada-5676-7095-f55d2ec64abf/1"",
          ""method"": ""GET""
        },
        ""update"": {
          ""href"": ""05f08f8bc88848628943d2b8aa2b436901/profiles/89f56b74-3ada-5676-7095-f55d2ec64abf"",
          ""method"": ""PUT""
        },
        ""delete"": {
          ""href"": ""05f08f8bc88848628943d2b8aa2b436901/profiles/89f56b74-3ada-5676-7095-f55d2ec64abf"",
          ""method"": ""DELETE""
        }
      }
    }
  ],
  ""links"": {
    ""add"": {
      ""href"": ""05f08f8bc88848628943d2b8aa2b436901/profiles"",
      ""method"": ""POST""
    }
  },
  ""object_type"": ""Profiles"",
  ""resource_status"": ""Active""
}";
        }

        protected static void AreEqual(PrivacyProfile expected, PrivacyProfile actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual);
                return;
            }

            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.Advertising, actual.Advertising);
            Assert.AreEqual(expected.TailoredExperiencesOffers, actual.TailoredExperiencesOffers);
            Assert.AreEqual(expected.Type, actual.Type);
            Assert.AreEqual(expected.SharingState, actual.SharingState);
        }

        protected static string CreateConsumerProfileContent()
        {
            return
@"{
    ""total_count"": 1,
    ""items"": [{
        ""object_type"": ""Profile"",
        ""id"": ""76d7e1424e7044088b248546a2de451001"",
        ""type"": ""consumer"",
        ""culture"": ""en-US"",
        ""first_name"": ""New-FirstName's test"",
        ""last_name"": ""New-LastName"",
        ""email_address"": ""dzlfl@LcQkE.com"",
        ""customer_id"": ""05f08f8bc88848628943d2b8aa2b436901"",
        ""default_address_id"": ""8ae886c6d9f14cd2b96e7a510d2347bf01"",
        ""etag"": ""-3589169604483601223"",
        ""snapshot_id"": ""76d7e1424e7044088b248546a2de451001/5"",
        ""resource_status"": ""Active"",
        ""default_address"": {
            ""customer_id"": ""05f08f8bc88848628943d2b8aa2b436901"",
            ""id"": ""8ae886c6d9f14cd2b96e7a510d2347bf01"",
            ""country"": ""US"",
            ""region"": ""WA"",
            ""district"": """",
            ""city"": ""Redmond"",
            ""address_line1"": ""One Microsoft Way"",
            ""address_line2"": """",
            ""address_line3"": """",
            ""postal_code"": ""98052"",
            ""first_name"": null,
            ""first_name_pronunciation"": null,
            ""last_name"": null,
            ""last_name_pronunciation"": null,
            ""correspondence_name"": null,
            ""phone_number"": null,
            ""mobile"": null,
            ""fax"": null,
            ""telex"": null,
            ""email_address"": null,
            ""web_site_url"": null,
            ""street_supplement"": null,
            ""is_within_city_limits"": null,
            ""form_of_address"": null,
            ""address_notes"": null,
            ""time_zone"": null,
            ""latitude"": null,
            ""longitude"": null,
            ""is_avs_validated"": null,
            ""links"": {
            }
        }
    }]
}";
        }
    }
}