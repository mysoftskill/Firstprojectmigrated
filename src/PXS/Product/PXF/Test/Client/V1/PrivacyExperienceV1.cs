// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.TestClient.V1
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading;

    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.PrivacySubject;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Models;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Models.PrivacySubject;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.Utilities;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.PrivacyServices.PXS.Command.CommandStatus;

    using Newtonsoft.Json;

    /// <summary>
    ///     Privacy-Experience-V1
    /// </summary>
    public static class PrivacyExperienceV1
    {
        #region User Settings

        public static void GetUserSettingsV1Callback(this PrivacyExperienceClient client, string userProxyTicket, string familyTicket = null)
        {
            var args = new GetUserSettingsArgs(userProxyTicket)
            {
                CorrelationVector = new CorrelationVector().ToString(),
                RequestId = Guid.NewGuid().ToString()
            };

            if (!string.IsNullOrWhiteSpace(familyTicket))
            {
                args.FamilyTicket = familyTicket;
            }

            Console.WriteLine("ClientId: {0}", args.RequestId);
            ResourceSettingV1 result = client.GetUserSettingsAsync(args).Result;

            IOHelpers.DisplaySuccessResult(result);
            IOHelpers.SaveSuccessResult(result, "GetUserSettings-V1.json");
        }

        #endregion

        private static string GetChildJsonWebToken(
            string userProxyTicket,
            long? userPuid,
            FamilyModel.FamilyClientConfiguration familyClientConfiguration)
        {
            if (string.IsNullOrWhiteSpace(userProxyTicket) || userPuid == null)
            {
                return null;
            }

            FamilyModel familyModel = FamilyModel.GetFamilyAsync(userPuid.Value, userProxyTicket, familyClientConfiguration).Result;

            if (familyModel?.Members == null)
            {
                Console.WriteLine("Family is null. Proceeding without JWT for the on-behalf of scenario.");
                return null;
            }

            if (familyModel.Members.Count == 0)
            {
                Console.WriteLine("No members exist in the family. Proceeding without JWT for the on-behalf of scenario.");
                return null;
            }

            Console.WriteLine("The members of the family are:");

            for (int i = 0; i < familyModel.Members.Count; i++)
            {
                FamilyModel.FamilyMemberModel member = familyModel.Members[i];

                Console.WriteLine(i + ":member puid " + member.Id);
            }

            int childIndex;

            do
            {
                childIndex = IOHelpers.GetUserInputInt("Please select a number corresponding to the child's puid to view.");
            } while (childIndex < 0 || childIndex >= familyModel.Members.Count);

            FamilyModel.FamilyMemberModel child = familyModel.Members[childIndex];
            return child.JsonWebToken;
        }

        #region Timeline

        public static void GetTimelineV2Callback(this PrivacyExperienceClient client, string userProxyTicket, string familyTicket = null)
        {
            var args = new GetTimelineArgs(
                userProxyTicket,
                IOHelpers.GetUserInputString("Comma-separated types").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
                IOHelpers.GetUserInputInt("Max count?"),
                null,
                IOHelpers.GetUserInputString("Comma-separated sources", true)?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
                IOHelpers.GetUserInputString("Search:", true),
                DateTimeOffset.UtcNow.Offset,
                DateTimeOffset.UtcNow)
            {
                CorrelationVector = new CorrelationVector().ToString(),
                RequestId = Guid.NewGuid().ToString()
            };

            if (!string.IsNullOrWhiteSpace(familyTicket))
            {
                args.FamilyTicket = familyTicket;
            }

            Console.WriteLine("ClientId: {0}", args.RequestId);
            Privacy.ExperienceContracts.V2.PagedResponse<TimelineCard> result = client.GetTimelineAsync(args).Result;
            int count = 0;
            IOHelpers.DisplaySuccessResult(result);
            foreach (TimelineCard timelineCard in result.Items)
            {
                Console.WriteLine("Card type: " + timelineCard.GetType().FullName);
            }

            IOHelpers.SaveSuccessResult(result, string.Format(CultureInfo.InvariantCulture, "GetTimeline-{0}-V2.json", count));

            client.GetTimelineNextPageV2Callback(args.UserProxyTicket, result.NextLink, ++count);
        }

        public static void GetVoiceCardAudioV2Callback(this PrivacyExperienceClient client, string userProxyTicket, string familyTicket = null)
        {
            var args = new GetVoiceCardAudioArgs(
                userProxyTicket,
                IOHelpers.GetUserInputString("Id"))
            {
                CorrelationVector = new CorrelationVector().ToString(),
                RequestId = Guid.NewGuid().ToString()
            };

            if (!string.IsNullOrWhiteSpace(familyTicket))
            {
                args.FamilyTicket = familyTicket;
            }

            Console.WriteLine("ClientId: {0}", args.RequestId);
            VoiceCardAudio result = client.GetVoiceCardAudioAsync(args).Result;
            int count = 0;
            IOHelpers.DisplaySuccessResult(result);
            IOHelpers.SaveSuccessResult(result, string.Format(CultureInfo.InvariantCulture, "GetVoiceCardAudio-{0}-V2.json", count));
        }

        public static void GetTimelineV2OnBehalfOfChildCallback(
            this PrivacyExperienceClient client,
            string userProxyTicket,
            long? userPuid,
            FamilyModel.FamilyClientConfiguration familyClientConfiguration)
        {
            string childJsonWebToken = GetChildJsonWebToken(userProxyTicket, userPuid, familyClientConfiguration);
            client.GetTimelineV2Callback(userProxyTicket, childJsonWebToken);
        }

        private static void GetTimelineNextPageV2Callback(this PrivacyExperienceClient client, string userProxyTicket, Uri nextPageUri, int count)
        {
            if (nextPageUri != null)
            {
                char nextPage = IOHelpers.GetUserInputCharacter("Get next page? Y/N");

                if (nextPage.Equals('Y') || nextPage.Equals('y'))
                {
                    var args = new PrivacyExperienceClientBaseArgs(userProxyTicket)
                    {
                        CorrelationVector = new CorrelationVector().ToString(),
                        RequestId = Guid.NewGuid().ToString()
                    };

                    Console.WriteLine("ClientId: {0}", args.RequestId);
                    Privacy.ExperienceContracts.V2.PagedResponse<TimelineCard> result = client.GetTimelineNextPageAsync(args, nextPageUri).Result;
                    IOHelpers.DisplaySuccessResult(result);
                    IOHelpers.SaveSuccessResult(result, string.Format(CultureInfo.InvariantCulture, "GetTimeline-{0}-V2.json", count));

                    client.GetTimelineNextPageV2Callback(userProxyTicket, result.NextLink, ++count);
                }
            }
        }

        public static void DeleteTimelineByIdsV2Callback(this PrivacyExperienceClient client, string userProxyTicket)
        {
            var args = new DeleteTimelineByIdsArgs(userProxyTicket, IOHelpers.GetUserInputString("Comma-separated ids").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                CorrelationVector = new CorrelationVector().ToString(),
                RequestId = Guid.NewGuid().ToString()
            };

            Console.WriteLine("ClientId: {0}", args.RequestId);
            client.DeleteTimelineAsync(args).Wait();
            string result = "Success";
            IOHelpers.DisplaySuccessResult(result);
            IOHelpers.SaveSuccessResult(result, "DeleteTimelineByIdsV2.json");
        }

        public static void DeleteTimelineByTypesV2Callback(this PrivacyExperienceClient client, string userProxyTicket)
        {
            var args = new DeleteTimelineByTypesArgs(
                userProxyTicket,
                TimeSpan.FromDays(IOHelpers.GetUserInputLong("Last N days?")),
                IOHelpers.GetUserInputString("Comma-separated types").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                CorrelationVector = new CorrelationVector().ToString(),
                RequestId = Guid.NewGuid().ToString()
            };

            Console.WriteLine("ClientId: {0}", args.RequestId);
            client.DeleteTimelineAsync(args).Wait();
            string result = "Success";
            IOHelpers.DisplaySuccessResult(result);
            IOHelpers.SaveSuccessResult(result, "DeleteTimelineByTypesV2.json");
        }

        #endregion

        #region Export

        public static void PostExportRequestCallback(this PrivacyExperienceClient client, string userProxyTicket, string exportDataTypes, string startTimeStr, string endTimeStr)
        {
            if (string.IsNullOrWhiteSpace(exportDataTypes))
            {
                exportDataTypes = IOHelpers.GetUserInputString(
                    "Enter DataTypes to Export in comma delimited list such as: " +
                    $"{Policies.Current.DataTypes.Ids.PreciseUserLocation.Value},{Policies.Current.DataTypes.Ids.SearchRequestsAndQuery.Value},{Policies.Current.DataTypes.Ids.BrowsingHistory.Value}",
                    true);
            }

            if (string.IsNullOrWhiteSpace(exportDataTypes))
            {
                exportDataTypes =
                    $"{Policies.Current.DataTypes.Ids.PreciseUserLocation.Value},{Policies.Current.DataTypes.Ids.SearchRequestsAndQuery.Value},{Policies.Current.DataTypes.Ids.BrowsingHistory.Value}";
            }

            if (!DateTimeOffset.TryParse(startTimeStr, out DateTimeOffset startTime))
            {
                startTime = DateTimeOffset.MinValue;
            }

            if (!DateTimeOffset.TryParse(endTimeStr, out DateTimeOffset endTime))
            {
                endTime = DateTimeOffset.MaxValue;
            }

            string[] dataTypes = exportDataTypes.Split(new[] { ",", " ", ";" }, StringSplitOptions.RemoveEmptyEntries);
            var args = new PostExportRequestArgs(dataTypes, startTime, endTime, userProxyTicket)
            {
                CorrelationVector = new CorrelationVector().ToString()
            };
            Console.WriteLine("PostExportRequestArgs:" + JsonConvert.SerializeObject(args));
            PostExportResponse result = client.PostExportRequestAsync(args).Result;
            IOHelpers.DisplaySuccessResult(result);
        }

        public static void PostExportCancelCallback(this PrivacyExperienceClient client, string userProxyTicket, string requestId)
        {
            if (string.IsNullOrWhiteSpace(requestId))
            {
                requestId = IOHelpers.GetUserInputString("Enter Export RequestId:");
            }

            var args = new PostExportCancelArgs(requestId, userProxyTicket)
            {
                CorrelationVector = new CorrelationVector().ToString(),
                RequestId = Guid.NewGuid().ToString()
            };

            ExportStatus result = client.PostExportCancelAsync(args).Result;
            IOHelpers.DisplaySuccessResult(result);
        }


        public static void DeleteExportArchivesCallback(this PrivacyExperienceClient client, string userProxyTicket, string requestId, ExportType exportType)
        {
            if (string.IsNullOrWhiteSpace(requestId))
            {
                requestId = IOHelpers.GetUserInputString("Enter Export RequestId:");
            }

            var args = new DeleteExportArchivesArgs(requestId, exportType, userProxyTicket)
            {
                CorrelationVector = new CorrelationVector().ToString(),
                RequestId = Guid.NewGuid().ToString()
            };

            var result = client.DeleteExportArchivesAsync(args).Result;
            IOHelpers.DisplaySuccessResult(result);
        }

        public static void PostTestMsaCloseCallback(this PrivacyExperienceClient client, string userProxyTicket)
        {
            var args = new TestMsaCloseArgs(userProxyTicket)
            {
                CorrelationVector = new CorrelationVector().ToString(),
                RequestId = Guid.NewGuid().ToString()
            };

            OperationResponse result = client.TestMsaCloseAsync(args).Result;
            IOHelpers.DisplaySuccessResult(result);
        }

        public static void TestGetCommandStatusByIdCallback(this PrivacyExperienceClient client, string userProxyTicket, Guid commandId)
        {
            while (commandId == Guid.Empty)
            {
                string guidStr = IOHelpers.GetUserInputString("Enter command id:");
                if (!Guid.TryParse(guidStr, out commandId))
                    Console.WriteLine("Must be a guid");
            }

            var args = new TestGetCommandStatusByIdArgs(userProxyTicket, commandId)
            {
                CorrelationVector = new CorrelationVector().ToString(),
                RequestId = Guid.NewGuid().ToString()
            };

            CommandStatusResponse result = client.TestGetCommandStatusByIdAsync(args).Result;
            IOHelpers.DisplaySuccessResult(result);
        }

        public static void TestGetAgentStatisticsCallback(this PrivacyExperienceClient client, string userProxyTicket, Guid agentId)
        {
            while (agentId == Guid.Empty)
            {
                string guidStr = IOHelpers.GetUserInputString("Enter agent id:");
                if (!Guid.TryParse(guidStr, out agentId))
                    Console.WriteLine("Must be a guid");
            }

            var args = new TestGetAgentStatisticsArgs(userProxyTicket, agentId)
            {
                CorrelationVector = new CorrelationVector().ToString(),
                RequestId = Guid.NewGuid().ToString()
            };

            IList<AssetGroupQueueStatistics> result = client.TestGetAgentStatisticsAsync(args).Result;
            IOHelpers.DisplaySuccessResult(result);
        }

        public static void TestForceCommandCompletionCallback(this PrivacyExperienceClient client, string userProxyTicket, Guid commandId)
        {
            while (commandId == Guid.Empty)
            {
                string guidStr = IOHelpers.GetUserInputString("Enter command id:");
                if (!Guid.TryParse(guidStr, out commandId))
                    Console.WriteLine("Must be a guid");
            }

            var args = new TestForceCompletionArgs(userProxyTicket, commandId)
            {
                CorrelationVector = new CorrelationVector().ToString(),
                RequestId = Guid.NewGuid().ToString()
            };

            client.TestForceCommandCompletionAsync(args).GetAwaiter().GetResult();
            IOHelpers.DisplaySuccessResult("Completed");
        }

        public static void TestGetCommandStatusesCallback(this PrivacyExperienceClient client, string userProxyTicket)
        {
            var args = new PrivacyExperienceClientBaseArgs(userProxyTicket)
            {
                CorrelationVector = new CorrelationVector().ToString(),
                RequestId = Guid.NewGuid().ToString()
            };

            IList<CommandStatusResponse> result = client.TestGetCommandStatusesAsync(args).Result;
            IOHelpers.DisplaySuccessResult(result);
        }

        public static void BatchExportByTypesCallback(this PrivacyExperienceClient client, string userProxyTicket, string exportDataTypes, bool altSubject = false)
        {
            IPrivacySubject subject = new MsaSelfAuthSubject(userProxyTicket);
            if (altSubject)
            {
                subject = new DemographicSubject() { Emails = new List<string> { "MEEPXSENG@microsoft.com" }, Phones = new List<string> { "1-800-642-7676" }};
            }
            if (string.IsNullOrWhiteSpace(exportDataTypes))
            {
                exportDataTypes = IOHelpers.GetUserInputString(
                    "Enter DataTypes to Export in comma delimited list such as: " +
                    $"{Policies.Current.DataTypes.Ids.PreciseUserLocation.Value},{Policies.Current.DataTypes.Ids.SearchRequestsAndQuery.Value},{Policies.Current.DataTypes.Ids.BrowsingHistory.Value}",
                    true);
            }

            if (string.IsNullOrWhiteSpace(exportDataTypes))
            {
                exportDataTypes =
                    $"{Policies.Current.DataTypes.Ids.PreciseUserLocation.Value},{Policies.Current.DataTypes.Ids.SearchRequestsAndQuery.Value},{Policies.Current.DataTypes.Ids.BrowsingHistory.Value},{Policies.Current.DataTypes.Ids.ProductAndServiceUsage.Value}";
            }

            string[] dataTypes = exportDataTypes.Split(new[] { ",", " ", ";" }, StringSplitOptions.RemoveEmptyEntries);

            var args = new ExportByTypesArgs(subject, dataTypes, DateTimeOffset.MinValue, DateTimeOffset.MaxValue, null, true)
            {
                CorrelationVector = new CorrelationVector().ToString(),
                RequestId = Guid.NewGuid().ToString()
            };

            OperationResponse result = client.ExportByTypesAsync(args).Result;
            IOHelpers.DisplaySuccessResult(result);
        }

        public static void ListExportHistoryCallback(this PrivacyExperienceClient client, string userProxyTicket)
        {
            var args = new PrivacyExperienceClientBaseArgs(userProxyTicket)
            {
                CorrelationVector = new CorrelationVector().ToString(),
                RequestId = Guid.NewGuid().ToString()
            };

            ListExportHistoryResponse result = client.ListExportHistoryAsync(args).Result;
            IOHelpers.DisplaySuccessResult(result);
        }

        public static void SyncrhonousExportRequestCallback(
            this PrivacyExperienceClient client,
            string userProxyTicket,
            string exportDataTypes,
            string startTimeStr,
            string endTimeStr,
            string outfile)
        {
            if (string.IsNullOrWhiteSpace(exportDataTypes))
            {
                exportDataTypes = IOHelpers.GetUserInputString(
                    "Enter DataTypes to Export in comma delimited list such as: " +
                    $"{Policies.Current.DataTypes.Ids.PreciseUserLocation.Value},{Policies.Current.DataTypes.Ids.SearchRequestsAndQuery.Value},{Policies.Current.DataTypes.Ids.BrowsingHistory.Value}");
            }

            if (string.IsNullOrWhiteSpace(exportDataTypes))
            {
                exportDataTypes =
                    $"{Policies.Current.DataTypes.Ids.PreciseUserLocation.Value},{Policies.Current.DataTypes.Ids.SearchRequestsAndQuery.Value},{Policies.Current.DataTypes.Ids.BrowsingHistory.Value},{Policies.Current.DataTypes.Ids.ProductAndServiceUsage.Value}";
            }

            if (!DateTimeOffset.TryParse(startTimeStr, out DateTimeOffset startTime))
            {
                startTime = DateTimeOffset.MinValue;
            }

            if (!DateTimeOffset.TryParse(endTimeStr, out DateTimeOffset endTime))
            {
                endTime = DateTimeOffset.MaxValue;
            }

            string[] dataTypes = exportDataTypes.Split(new[] { ",", " ", ";" }, StringSplitOptions.RemoveEmptyEntries);
            var args = new PostExportRequestArgs(dataTypes, startTime, endTime, userProxyTicket)
            {
                CorrelationVector = new CorrelationVector().ToString()
            };

            Console.WriteLine("PostExportRequestArgs:" + JsonConvert.SerializeObject(args));
            PostExportResponse postExportResult = client.PostExportRequestAsync(args).Result;
            Console.WriteLine("PostExportRequest Completed");
            IOHelpers.DisplaySuccessResult(postExportResult);

            var statusArgs = new PrivacyExperienceClientBaseArgs(userProxyTicket)
            {
                CorrelationVector = new CorrelationVector().ToString(),
                RequestId = Guid.NewGuid().ToString()
            };

            bool completed = false;
            ExportStatus statusResult = null;
            for (int i = 0; i < 60 && !completed; i++)
            {
                statusResult = client.ListExportHistoryAsync(statusArgs).Result.Exports.Single(e => e.ExportId == postExportResult.ExportId);
                Console.WriteLine("Export status");
                if (statusResult.IsComplete)
                {
                    completed = true;
                }
                else
                {
                    Console.Write(".");
                    Thread.Sleep(3000);
                }
            }

            Console.WriteLine(" ");
            if (statusResult != null)
            {
                IOHelpers.DisplaySuccessResult(statusResult);
            }

            if (!completed)
            {
                Console.WriteLine("timed out");
            }
        }
        
        #endregion
    }
}
