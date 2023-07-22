// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.TestClient
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Security.Cryptography.X509Certificates;

    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.Utilities;
    using Microsoft.Membership.MemberServices.PrivacyExperience.TestClient.V1;
    using Microsoft.Membership.MemberServices.Test.Common;

    using HttpClient = Microsoft.OSGS.HttpClientCommon.HttpClient;

    /// <summary>
    ///     Client
    /// </summary>
    public class Client
    {
        private readonly Options options;

        /// <summary>
        ///     Runs this instance.
        /// </summary>
        public void Run()
        {
            Console.WriteLine("Starting privacy-experience-test-client. Environment: {0}, Service Endpoint: {1}", this.options.Environment, Config.ServiceEndpoint);

            char input = char.MinValue;
            Tuple<string, long?> proxyTicketAndPuid;
            string userProxyTicket = string.Empty;
            long? puid = null;

            do
            {
                if (input.Equals('S') || input.Equals('s'))
                {
                    Console.WriteLine("using same puid " + puid);
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(this.options.UserName) && !string.IsNullOrWhiteSpace(this.options.Password))
                    {
                        proxyTicketAndPuid = GetUserProxyTicketAndPuid(this.options.UserName, this.options.Password);
                    }
                    else
                    {
                        proxyTicketAndPuid = RequestCredentials();
                    }

                    userProxyTicket = proxyTicketAndPuid.Item1;
                    puid = proxyTicketAndPuid.Item2;

                    if (puid.HasValue)
                    {
                        Console.WriteLine("User Puid is: " + puid);
                    }
                }

                if (!string.IsNullOrWhiteSpace(this.options.Operation))
                {
                    switch(this.options.Operation)
                    {
                        case Options.PostExportRequest:
                            PrivacyExperienceClient.PostExportRequestCallback(userProxyTicket, this.options.Argument1, this.options.Argument2, this.options.Argument3);
                            break;
                        case Options.ListExportHistory:
                            PrivacyExperienceClient.ListExportHistoryCallback(userProxyTicket);
                            break;
                        case Options.ExportSync:
                            PrivacyExperienceClient.SyncrhonousExportRequestCallback(userProxyTicket, this.options.Argument1, this.options.Argument2, this.options.Argument3, this.options.Argument4);
                            break;
                        default:
                            Console.WriteLine("not implemented operation");
                            break;
                    }
                    return;
                }

                var menu = new Menu("Select an API to execute:");
                menu.AddItem(
                    "GetTimelineV2",
                    () => PrivacyExperienceClient.GetTimelineV2Callback(userProxyTicket));
                menu.AddItem(
                    "GetTimelineV2 on-behalf of child",
                    () => PrivacyExperienceClient.GetTimelineV2OnBehalfOfChildCallback(userProxyTicket, puid, Config.FamilyClientConfiguration));
                menu.AddItem(
                    "DeleteTimelineByIdsV2",
                    () => PrivacyExperienceClient.DeleteTimelineByIdsV2Callback(userProxyTicket));
                menu.AddItem(
                    "DeleteTimelineByTypesV2",
                    () => PrivacyExperienceClient.DeleteTimelineByTypesV2Callback(userProxyTicket));
                menu.AddItem(
                    "GetVoiceCardAudioV2",
                    () => PrivacyExperienceClient.GetVoiceCardAudioV2Callback(userProxyTicket));
                menu.AddItem(
                    "GetUserSettingsV1",
                    () => PrivacyExperienceClient.GetUserSettingsV1Callback(userProxyTicket));
                menu.AddItem(
                    "PostExportRequest",
                    () => PrivacyExperienceClient.PostExportRequestCallback(userProxyTicket, null, null, null));
                menu.AddItem(
                    "ListExportHistory",
                    () => PrivacyExperienceClient.ListExportHistoryCallback(userProxyTicket));
                menu.AddItem(
                    "PostExportCancel",
                    () => PrivacyExperienceClient.PostExportCancelCallback(userProxyTicket, null));
                menu.AddItem(
                    "DeleteExportArchives",
                    () => PrivacyExperienceClient.DeleteExportArchivesCallback(userProxyTicket, null, Privacy.ExperienceContracts.ExportType.Quick));
                menu.AddItem(
                    "PostTestMsaClose",
                    () => PrivacyExperienceClient.PostTestMsaCloseCallback(userProxyTicket));
                menu.AddItem(
                    "TestGetCommandStatusById",
                    () => PrivacyExperienceClient.TestGetCommandStatusByIdCallback(userProxyTicket, Guid.Empty));
                menu.AddItem(
                    "TestGetAgentStatistics",
                    () => PrivacyExperienceClient.TestGetAgentStatisticsCallback(userProxyTicket, Guid.Empty));
                menu.AddItem(
                    "TestForceCommandCompletion",
                    () => PrivacyExperienceClient.TestForceCommandCompletionCallback(userProxyTicket, Guid.Empty));
                menu.AddItem(
                    "TestGetCommandStatuses",
                    () => PrivacyExperienceClient.TestGetCommandStatusesCallback(userProxyTicket));
                menu.AddItem(
                    "BatchExportByTypes",
                    () => PrivacyExperienceClient.BatchExportByTypesCallback(userProxyTicket, null));
                menu.Render();
                do
                {
                    input = IOHelpers.GetUserInputCharacter("Again? Y/N/S (S = again with same user)");
                } while (!(input.Equals('Y') || input.Equals('y') || input.Equals('S') || input.Equals('s') || input.Equals('N') || input.Equals('n')));
            } while (input.Equals('Y') || input.Equals('y') || input.Equals('S') || input.Equals('s'));
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Client" /> class.
        /// </summary>
        /// <param name="options">The options.</param>
        internal Client(Options options)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            this.options = options;
            Config = new Config(options);

            S2SCertificate = Config.S2SCertificateInfo.LoadFromStore();

            IPrivacyAuthClient authClient =
                new PrivacyAuthClient(
                    Config.SiteId,
                    Config.TargetSite,
                    S2SCertificate,
                    Config.MsaOathEndpoint);

            PrivacyExperienceClient =
                new PrivacyExperienceClient(
                    Config.ServiceEndpoint,
                    new HttpClient(new WebRequestHandler()),
                    authClient);

            if (options.SkipServerCertValidation.HasValue && options.SkipServerCertValidation.Value)
            {
                //using below query identifier for suppressing CodeQL Error for Certificate validation disabled.
                //Test scenarios do not need cert validation.
                ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true; // lgtm[cs/do-not-disable-cert-validation]
            }
            else
            {
                if (!options.SkipServerCertValidation.HasValue)
                {
                    Console.WriteLine("SkipServerCertValidation is null");
                }
                else if (!options.SkipServerCertValidation.Value)
                {
                    Console.WriteLine("SkipServerCertValidation is false");
                }
                else
                {
                    Console.WriteLine("SkipServerCertValidation is something else");
                }
            }
        }

        private static Config Config { get; set; }

        private static PrivacyExperienceClient PrivacyExperienceClient { get; set; }

        private static X509Certificate2 S2SCertificate { get; set; }

        private static Tuple<string, long?> GetUserProxyTicketAndPuid(string username, string password)
        {
            password = WebUtility.HtmlEncode(password);

            var userProxyTicketProvider = new UserProxyTicketProvider(Config.RpsConfiguration);
            UserProxyTicketAndPuidResult userTicketResponse = userProxyTicketProvider.GetTicketAndPuidAsync(username, password).Result;

            if (!userTicketResponse.IsSuccess)
            {
                throw new MissingFieldException("Retrieving user proxy ticket failed. ErrorMessage=" + userTicketResponse.ErrorMessage);
            }

            return Tuple.Create(userTicketResponse.Ticket, userTicketResponse.Puid);
        }

        private static Tuple<string, long?> RequestCredentials()
        {
            string userName = IOHelpers.GetUserInputString("Username:");
            string password = IOHelpers.GetUserInputStringPrivate("Password:");
            return GetUserProxyTicketAndPuid(userName, password);
        }
    }
}
