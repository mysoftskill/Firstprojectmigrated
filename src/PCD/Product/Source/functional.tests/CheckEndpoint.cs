using System;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace functional.tests
{
    //These tests are used to determine that the endpoint is functioning properly
    [TestClass]
    public class CheckEndpoint
    {
        private static string dnsEndpoint = "";
        
        //Tests Endpoint
        [ClassInitialize]
        public static void InitializeClass(TestContext ct){
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
        }

        [TestMethod]
        public void CheckKeepAlive()
        {
            HttpWebRequest request = HttpWebRequest.CreateHttp(dnsEndpoint+ "keepalive");
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Assert.AreEqual(response.StatusCode, HttpStatusCode.OK);
        }

        [TestMethod]
        public void CheckHealthCheck()
        {
            HttpWebRequest request = HttpWebRequest.CreateHttp(dnsEndpoint + "healthcheck");
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Assert.AreEqual(response.StatusCode, HttpStatusCode.OK);
        }

        [TestMethod]
        public void CheckResponse(){
            string errorMsg = "Auth Page html file did not contain \"Privacy Compliance Dashboard\"";
            string html;
            using(WebClient client = new WebClient()){
                html = client.DownloadString(dnsEndpoint);
            }
            Assert.IsTrue(html.Contains("Privacy Compliance Dashboard"),errorMsg);
        }
    }
}
