// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.FunctionalTests
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common;
    using Microsoft.Membership.MemberServices.Test.Common;
    using Microsoft.OSGS.HttpClientCommon;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using TestConfiguration = Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.Config.TestConfiguration;

    /// <summary>
    /// SmokeTest Base
    /// </summary>
    [TestClass]
    [DeploymentItem("Microsoft.WindowsLive.Test.WinLiveUser.AuthInterface.dll.config")]
    public abstract class TestBase
    {
        protected static readonly Uri MsaOathEndpoint = new Uri("https://login.live-int.com/pksecure/oauth20_clientcredentials.srf");
        protected static readonly object lockObj = new object();

        protected static PrivacyExperienceClient S2SClient { get; private set; }

        protected readonly Mock<ICounterFactory> mockCounterFactory = TestMockFactory.CreateCounterFactory();

        [TestInitialize]
        public void TestInitialize()
        {
            // Uncomment this if you are running tests localy without a hosts file override
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            
            Sll.ResetContext();
            
            InitializeS2SClient();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Sll.ResetContext();
        }

        /// <summary>
        /// Retrieves a user proxy ticket for a user and asserts that it was successfully retrieved.
        /// </summary>
        /// <param name="testUser">The test user.</param>
        /// <returns>The user proxy ticket</returns>
        protected static Task<string> GetUserProxyTicketAsync(TestUser testUser)
        {
            return GetUserProxyTicketAsync(testUser.UserName, testUser.Password);
        }

        /// <summary>
        /// Retrieves a user proxy ticket for a user and asserts that it was successfully retrieved.
        /// </summary>
        /// <param name="userName">The user name</param>
        /// <param name="password">The password</param>
        /// <returns>The user proxy ticket</returns>
        protected static async Task<string> GetUserProxyTicketAsync(string userName, string password)
        {
            UserProxyTicketProvider userProxyTicketProvider = new UserProxyTicketProvider(TestData.IntUserTicketConfiguration());
            UserProxyTicketResult userProxyTicketResult = await userProxyTicketProvider.GetTicket(userName, password);

            if (!string.IsNullOrWhiteSpace(userProxyTicketResult.ErrorMessage))
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Ticket is invalid: {0}",
                        userProxyTicketResult.ErrorMessage));
            }

            return userProxyTicketResult.Ticket;
        }

        protected static void ValidateOrderByDateTimeDescending<T>(List<T> itemsList) where T : ResourceV1
        {
            for (int i = 0; i < itemsList.Count - 1; i++)
            {
                Assert.IsTrue(
                    itemsList[i].DateTime.UtcDateTime >= itemsList[i + 1].DateTime.UtcDateTime, 
                    "Item at index #{0}:{1} should be greater than item at index #:{2}:{3}",
                    i,
                    itemsList[i].DateTime.UtcDateTime,
                    i + 1,
                    itemsList[i + 1].DateTime.UtcDateTime);
            }
        }

        private static void InitializeS2SClient()
        {
            if (S2SClient == null)
            {
                lock (lockObj)
                {
                    if (S2SClient == null)
                    {
                        IPrivacyAuthClient authClient = new PrivacyAuthClient(
                            TestData.TestSiteIdIntProd,
                            TestData.IntS2STargetScope,
                            TestConfiguration.S2SCert.Value,
                            MsaOathEndpoint);

                        S2SClient = new PrivacyExperienceClient(
                            TestConfiguration.ServiceEndpoint.Value,
                            CreateClientLibraryHttpClient(),
                            authClient);
                    }
                }
            }
        }

        private static IHttpClient CreateClientLibraryHttpClient()
        {
            // The client library requires a web request handler to attach certificates
            WebRequestHandler certHandler = new WebRequestHandler();
            IHttpClient httpClient = new OSGS.HttpClientCommon.HttpClient((HttpMessageHandler)certHandler);

            // Increasing the timeout from 100 seconds to 5 minutes to avoid partner timeout during the FCT run.
            httpClient.Timeout = TimeSpan.FromMinutes(5.0);
            return httpClient;
        }
    }
}