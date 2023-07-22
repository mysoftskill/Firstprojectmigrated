// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>


namespace Microsoft.Membership.MemberServices.Test.PartnerTestClient
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.PCF;
    using Microsoft.Membership.MemberServices.Privacy.Core.PrivacyCommand;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Helpers;
    using Microsoft.Membership.MemberServices.Test.Common;
    using Microsoft.Practices.Unity;
    using Microsoft.PrivacyServices.Policy;
    using Moq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    ///     The intent of CoreHost is to be able to initialize our Core and Adapter layers so that we can invoke the
    ///     methods directly without having to follow the same patterns as our services normally do. The analogy is that
    ///     instead of hosting our code in a WebAPI we host it here.
    /// </summary>
    internal static class CoreHost
    {
        private const AdapterConfigurationSource ConfigSource = AdapterConfigurationSource.MockConfiguration;

        private const string Password = "";

        private const RpsEnvironment RpsEnvironment = Configuration.RpsEnvironment.Int;

        private const string UserName = "";

        // Flight names are in config file. For purposes of testing, you can replace this here.
        private static string[] flights = { "PxsPdApi" };

        public static void Main(string[] args)
        {
            ServerCertificateValidation.GlobalSkipServerCertificateValidation = true;
            Task mainTask = MainAsync();
            mainTask.GetAwaiter().GetResult();
        }

        private static RequestContext CreatePxsRequestContext()
        {
            Tuple<string, long?, long?, long> auth = GetUserProxyTicketAndPuid();
            var context = RequestContext.CreateOldStyle(new Uri("https://PartnerTestClient"),
                auth.Item1,
                null,
                auth.Item2.Value,
                auth.Item2.Value,
                auth.Item3,
                auth.Item3,
                "us",
                "PartnerTestClient-CoreHost",
                auth.Item4,
                flights);
            return context;
        }

        private static IRpsConfiguration CreateRpsConfiguration(RpsEnvironment rpsEnvironment)
        {
            var mockConfig = new Mock<IRpsConfiguration>(MockBehavior.Strict);
            mockConfig.SetupGet(m => m.Environment).Returns(rpsEnvironment);
            mockConfig.SetupGet(m => m.SiteId).Returns(TestData.TestSiteIdIntProd.ToString);
            mockConfig.SetupGet(m => m.SiteName).Returns(TestData.TestSiteName);
            mockConfig.SetupGet(m => m.SiteUri).Returns(TestData.TestSiteUri);
            mockConfig.SetupGet(m => m.AuthPolicy).Returns("MBI_SSL_SA");
            return mockConfig.Object;
        }

        private static Tuple<string, long?, long?, long> GetUserProxyTicketAndPuid()
        {
            IRpsConfiguration rpsConfiguration = CreateRpsConfiguration(RpsEnvironment);
            var userProxyTicketProvider = new UserProxyTicketProvider(rpsConfiguration);
            UserProxyTicketAndPuidResult userTicketResponse = userProxyTicketProvider.GetTicketAndPuidAsync(UserName, WebUtility.HtmlEncode(Password)).Result;

            if (!userTicketResponse.IsSuccess)
            {
                throw new MissingFieldException("Retrieving user proxy ticket failed. ErrorMessage=" + userTicketResponse.ErrorMessage);
            }

            return Tuple.Create(userTicketResponse.Ticket, userTicketResponse.Puid, userTicketResponse.Cid, long.Parse(rpsConfiguration.SiteId));
        }

        private static void InitializeVerboseTraceLogger()
        {
            TraceLogger.TraceSwitch = new TraceSwitch("partnerTestClient", "tracer for partner test client", "Verbose");
            using (var console = new ConsoleTraceListener())
            {
                Trace.Listeners.Add("ConsoleListener", console);
            }
        }

        private static async Task MainAsync()
        {
            InitializeVerboseTraceLogger();

            // Modify the AdapterConfigurationSource depending on where you want to load the config from.
            var config = new DependencyConfiguration(ConfigSource);
            Sll.Context.Vector = new CorrelationVector();

            SqlServerTypes.Utilities.LoadNativeAssemblies(AppDomain.CurrentDomain.BaseDirectory);

            await TestTokenValidation(config).ConfigureAwait(false);
        }

        private static async Task TestAndLog<T>(Func<Task<T>> testCase, string testName)
        {
            var serializerSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                Converters = new JsonConverter[] { new StringEnumConverter() },
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            try
            {
                T result = await testCase().ConfigureAwait(false);
                Console.WriteLine(testName + " success:");
                Console.WriteLine(JsonConvert.SerializeObject(result, serializerSettings));
            }
            catch (Exception ex)
            {
                Console.WriteLine(testName + " exception:");
                Console.WriteLine(JsonConvert.SerializeObject(ex, serializerSettings));
            }
        }

        private static async Task TestTokenValidation(DependencyConfiguration config)
        {
            await TestAndLog(
                async () =>
                {
                    var service = config.Container.Resolve<IPcfProxyService>();
                    var context = CreatePxsRequestContext();
                    return await service.PostDeleteRequestsAsync(
                        context,
                        PrivacyRequestConverter.CreatePcfDeleteRequests(
                            PrivacyRequestConverter.CreateMsaSubjectFromContext(context),
                            context,
                            Guid.NewGuid(),
                            "keithjac-cv-" + Guid.NewGuid(),
                            null,
                            DateTimeOffset.UtcNow,
                            new[] { Policies.Current.DataTypes.Ids.BrowsingHistory.Value },
                            DateTimeOffset.UtcNow.AddYears(-1),
                            DateTimeOffset.UtcNow,
                            null,
                            "keithjac",
                            isTest: true).ToList()).ConfigureAwait(false);
                },
                nameof(TestTokenValidation)).ConfigureAwait(false);
        }
    }
}
