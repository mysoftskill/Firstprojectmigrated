// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.PerfClient.Views
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Security.Cryptography.X509Certificates;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Extensions;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.Utilities;
    using Microsoft.OSGS.HttpClientCommon;

    [Flags]
    public enum RequestType
    {
        None,
        View,
        Delete
    }

    public class PerfTestSetupView : IView
    {
        private int requestsPerSecond;
        private readonly TimeSpan testDuration = TimeSpan.FromHours(8);
        private readonly IList<TestUser> testUsers;
        private readonly IPrivacyAuthClient authClient;
        private readonly IHttpClient httpClient;

        private static Config Config { get; set; }
        private static X509Certificate2 S2SCertificate { get; set; }

        internal PerfTestSetupView(Options options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            Config = new Config(options);

            S2SCertificate = Config.S2SCertificateInfo.LoadFromStore();

            this.authClient = 
                new PrivacyAuthClient(
                    Config.SiteId, 
                    Config.TargetSite,
                    S2SCertificate, 
                    Config.MsaOathEndpoint);

            this.httpClient = new OSGS.HttpClientCommon.HttpClient(new WebRequestHandler());
            this.httpClient.BaseAddress = Config.ServiceEndpoint;
            this.httpClient.MessageHandler.AttachClientCertificate(this.authClient.ClientCertificate);

            if (options.SkipServerCertValidation.HasValue && options.SkipServerCertValidation.Value)
            {
                //using below query identifier for suppressing CodeQL Error for Certificate validation disabled.
                //Test scenarios do not need cert validation.
                ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true; // lgtm[cs/do-not-disable-cert-validation]
            }

            ServicePointManager.DefaultConnectionLimit = int.MaxValue;
            ServicePointManager.MaxServicePointIdleTime = int.MaxValue;

            Console.WriteLine("Loading test user credentials.");
            this.testUsers = LoadTestUsersFromFile(Config.TestUserFileName);
        }

        private static IList<TestUser> LoadTestUsersFromFile(string testUserFileName)
        {
            if (!File.Exists(testUserFileName))
            {
                throw new FileNotFoundException("File was not found.", testUserFileName);
            }

            var testUers = new List<TestUser>();

            using (var reader = new StreamReader(testUserFileName))
            {

                string line;
                while((line = reader.ReadLine()) != null)
                {
                    if (line.Length == 0)
                    {
                        continue;
                    }

                    string[] parts = line.Split('\t');

                    if (parts.Length != 3)
                    {
                        throw new InvalidOperationException($"Invalid line entry: '{line}'");
                    }

                    testUers.Add(new TestUser(parts[0], parts[1], (UserType)Enum.Parse(typeof(UserType), parts[2])));
                }
            }

            return testUers;
        }


        public void Render()
        {
            this.requestsPerSecond = IOHelpers.GetUserInputInt("Enter # of rps:");

            Menu menu = new Menu("Select the test.");
            menu.AddItem("View/Delete", () => this.ExecutePerfTest(RequestType.View | RequestType.Delete));
            menu.AddItem("View", () => this.ExecutePerfTest(RequestType.View));
            menu.AddItem("Delete", () => this.ExecutePerfTest(RequestType.Delete));
            menu.Render();
        }

        private void ExecutePerfTest(RequestType requestType)
        {
            ViewManager.NavigateForwards(
                new PerfTestView(
                    requestType, 
                    this.requestsPerSecond, 
                    this.testDuration, 
                    this.testUsers,
                    Config.RpsConfiguration,
                    this.httpClient,
                    this.authClient));
        }
    }
}