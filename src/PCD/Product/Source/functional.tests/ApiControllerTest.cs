using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace functional.tests
{
    /// <summary>
    /// Functional tests for APIs handled by the controller - "ApiController"
    /// </summary>
    [TestClass]
    public class ApiControllerTest
    {
        private static string dnsEndpoint = "";
        private static HttpClient client;

        
        [ClassInitialize]
        public static void InitializeClass(TestContext ct)
        {
            if (Environment.GetEnvironmentVariable("testEnvironment") != null &&
                Environment.GetEnvironmentVariable("testEnvironment").CompareTo("PCD-CI2") == 0)
            {
                //CI2 Endpoint
                dnsEndpoint = "https://sf-ci2.manage.privacy.microsoft-int.com/";
            }
            else
            {
                //CI1 Endpoint
                dnsEndpoint = "https://sf-ci1.manage.privacy.microsoft-int.com/";
            }

            client = new HttpClient();
            client.BaseAddress = new Uri(dnsEndpoint);
        }

        [TestMethod]
        public async Task CheckGetCountriesListAsyncIsFailngForHeadMethod()
        {
            HttpRequestMessage request = new HttpRequestMessage( HttpMethod.Head, "api/getcountrieslist");

            HttpResponseMessage response = await client.SendAsync(request);

            Assert.AreEqual(response.StatusCode, HttpStatusCode.Unauthorized);
        }

        [TestMethod]
        public async Task CheckGetPrivacyPolicyAsyncIsFailngForHeadMethod()
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Head, "api/getprivacypolicy");

            HttpResponseMessage response = await client.SendAsync(request);

            Assert.AreEqual(response.StatusCode, HttpStatusCode.Unauthorized);
        }

        [TestMethod]
        public async Task CheckGetServicesByNameAsyncIsFailngForHeadMethod()
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Head, "api/getservicesbyname");

            HttpResponseMessage response = await client.SendAsync(request);

            Assert.AreEqual(response.StatusCode, HttpStatusCode.Unauthorized);
        }

        [TestMethod]
        public async Task CheckGetServicesByIdAsyncIsFailngForHeadMethod()
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Head, "api/getservicesbyid");

            HttpResponseMessage response = await client.SendAsync(request);

            Assert.AreEqual(response.StatusCode, HttpStatusCode.Unauthorized);
        }

        [TestMethod]
        public async Task CheckGetOwnersByAuthenticatedUserAsyncIsFailngForHeadMethod()
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Head, "api/getownersbyauthenticateduser");

            HttpResponseMessage response = await client.SendAsync(request);

            Assert.AreEqual(response.StatusCode, HttpStatusCode.Unauthorized);
        }
    }
}
