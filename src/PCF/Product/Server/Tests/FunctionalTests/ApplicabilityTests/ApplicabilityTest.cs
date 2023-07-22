namespace PCF.FunctionalTests.ApplicabilityTests
{
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.PXS.Command.CommandStatus;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.Threading.Tasks;
    using Xunit;

    [Trait("Category", "FCT")]
    public class ApplicabilityTest
    {
        private const string TestCommandsFileName = "TestCommands.json";
        private static readonly ApplicabilityResultMatrix ApplicabilityMatrix;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        static ApplicabilityTest()
        {
            string testCommandContext = Guid.NewGuid().ToString();
            string testCommandRequester = Guid.NewGuid().ToString();

            Assembly current = typeof(ApplicabilityTest).Assembly;
            string configFile = current.GetManifestResourceNames().Single(x => x.IndexOf(TestCommandsFileName, StringComparison.OrdinalIgnoreCase) >= 0);

            using (var streamReader = new StreamReader(current.GetManifestResourceStream(configFile)))
            using (var jsonReader = new JsonTextReader(streamReader))
            {
                JsonSerializer serializer = new JsonSerializer();
                ApplicabilityMatrix = serializer.Deserialize<ApplicabilityResultMatrix>(jsonReader);
            }

            List<PrivacyRequest> privacyRequests = new List<PrivacyRequest>();
            foreach (var testCommand in ApplicabilityMatrix.Commands)
            {
                var command = testCommand.BuildPrivacyRequest();
                command.Context = testCommandContext;
                command.Requester = testCommandRequester;
                testCommand.CommandId = command.RequestId;

                privacyRequests.Add(command);
            }

            TestSettings.InsertPxsCommandAsync(privacyRequests, null).GetAwaiter().GetResult();
        }

        public static IEnumerable<object[]> GetCommandStatus()
        {
            Guid agentId = new Guid(ApplicabilityMatrix.AgentId);
            var commandStatuses = new ConcurrentBag<object[]>();

            foreach (var testCommand in ApplicabilityMatrix.Commands)
            {
                var task = QueryCommandByIdAsync(testCommand.CommandId);
                task.Wait();
                var assetGroupStatuses = task.Result;

                foreach (var assetGroup in ApplicabilityMatrix.AssetGroups)
                {
                    var assetGroupId = new Guid(assetGroup);
                    var commandStatus = assetGroupStatuses.FirstOrDefault(x => (x.AgentId == agentId && x.AssetGroupId == assetGroupId));
                    commandStatuses.Add(new object[] { commandStatus, testCommand.CommandName, testCommand.CommandId, assetGroupId, testCommand.ApplicableAssetGroups.Contains(assetGroup) });
                }
            }

            return commandStatuses;
        }


        [Theory]
        [MemberData(nameof(GetCommandStatus))]
        public void DoApplicabilityTest(AssetGroupCommandStatus commandStatus, string commandName, Guid commandId, Guid assetGroupId, bool isApplicable)
        {
            // using Assert.True instead of Assert.NotNull because this allows for a user message with failure
            Assert.True(commandStatus != null, $"No status retrieved for Command:{commandName} and AssetGroupId: {assetGroupId}");

            // PCF: Not applicable
            bool doesNotApply = true;

            // PCF: applicable
            if (commandStatus.IngestionDebugText == "OK")
            {
                doesNotApply = false;
            } // SAL: Not applicable
            else if (commandStatus.IngestionDebugText.Contains("DoesNotApply"))
            {
                doesNotApply = true;
            } // // SAL: applicable
            else if (
                commandStatus.IngestionDebugText.Contains("FullyApplicable")
                || commandStatus.IngestionDebugText.Contains("PartiallyApplicable"))
            {
                doesNotApply = false;
            }

            if (isApplicable)
            {
                Assert.False(
                    doesNotApply,
                    $"Command:{commandName}, CommandId: {commandId} applies to AssetGroupId: {assetGroupId} but is {commandStatus.IngestionDebugText}");
            }
            else
            {
                Assert.True(
                    doesNotApply,
                    $"Command:{commandName}, CommandId: {commandId} does not apply to AssetGroupId: {assetGroupId} but is {commandStatus.IngestionDebugText}");
            }
        }
        
        protected static async Task<List<AssetGroupCommandStatus>> QueryCommandByIdAsync(Guid commandId)
        {
            var startTime = DateTimeOffset.UtcNow;
            Uri getStatusUri = new Uri($"https://{TestSettings.ApiHostName}/coldstorage/v3/status/commandid/{commandId}");

            while (DateTimeOffset.UtcNow - startTime <= TimeSpan.FromSeconds(60))
            {
                var response = await TestSettings.SendWithS2SAync(new HttpRequestMessage(HttpMethod.Get, getStatusUri), null);
                if (response == null || response.StatusCode == HttpStatusCode.NoContent)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    continue;
                }

                string responseBody = await response.Content.ReadAsStringAsync();
                CommandStatusResponse commandStatusResponse = JsonConvert.DeserializeObject<CommandStatusResponse>(responseBody);
                
                return commandStatusResponse.AssetGroupStatuses;
            }

            return new List<AssetGroupCommandStatus>();
        }
        
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
        private class ApplicabilityResultMatrix
        {
            [JsonProperty]
            public string AgentId { get; set; }

            [JsonProperty]
            public string[] AssetGroups { get; set; }

            [JsonProperty]
            public TestCommand[] Commands { get; set; }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
        private class TestCommand
        {
            /// <summary>
            /// This will be set after the pxs request is built
            /// </summary>
            public Guid CommandId { get; set; }

            [JsonProperty]
            public string CommandName { get; set; }

            [JsonProperty]
            public string CloudInstance { get; set; }

            [JsonProperty]
            public string CommandType { get; set; }

            [JsonProperty]
            public string SubjectType { get; set; }

            [JsonProperty]
            public string[] DataTypes { get; set; }

            [JsonProperty]
            public string AgentId { get; set; }

            [JsonProperty]
            public string[] ApplicableAssetGroups { get; set; }

            public PrivacyRequest BuildPrivacyRequest()
            {
                PrivacyRequest privacyRequest;
                DateTimeOffset requestTimeStamp = DateTimeOffset.UtcNow;
                
                switch (this.CommandType)
                {
                    case "AgeOut":
                        privacyRequest = this.CreateAgeOutRequest(requestTimeStamp);
                        break;

                    case "Delete":
                        privacyRequest = this.CreateDeleteRequest();
                        break;

                    case "AccountClose":
                        privacyRequest = this.CreateAccountCloseRequest();
                        break;

                    case "Export":
                        privacyRequest = this.CreateExportRequest();
                        break;

                    default:
                        throw new InvalidOperationException();
                }

                switch (this.SubjectType)
                {
                    case "Aad":
                        privacyRequest.Subject = new AadSubject()
                        {
                            ObjectId = Guid.NewGuid(),
                            OrgIdPUID = 12345L,
                            TenantId = Guid.NewGuid()
                        };
                        break;
                    case "Aad2":
                        privacyRequest.Subject = new AadSubject2()
                        {
                            ObjectId = Guid.NewGuid(),
                            OrgIdPUID = 12345L,
                            TenantId = Guid.NewGuid(),
                            HomeTenantId = Guid.NewGuid(),
                        };
                        break;
                    case "Msa":
                        privacyRequest.Subject = new MsaSubject()
                        {
                            Puid = 12345L,
                            Anid = "anid",
                            Cid = 123L,
                            Opid = "opid",
                            Xuid = "xuid"
                        };
                        break;
                    case "Device":
                        privacyRequest.Subject = new DeviceSubject()
                        {
                            GlobalDeviceId = 12345L
                        };
                        break;
                    case "Demographic":
                        privacyRequest.Subject = new DemographicSubject()
                        {
                            EmailAddresses = new[] { "mytest@test.com" }
                        };
                        break;
                    case "MicrosoftEmployee":
                        privacyRequest.Subject = new MicrosoftEmployee()
                        {
                            Emails = new[] { "mytest@test.com" }
                        };
                        break;
                    case "NonWindowsDevice":
                        privacyRequest.Subject = new NonWindowsDeviceSubject();
                        break;
                    case "EdgeBrowser":
                        privacyRequest.Subject = new EdgeBrowserSubject();
                        break;
                    default:
                        throw new InvalidOperationException();
                }

                privacyRequest.CorrelationVector = "cv1";
                privacyRequest.RequestGuid = Guid.NewGuid();
                privacyRequest.RequestId = Guid.NewGuid();
                privacyRequest.Timestamp = requestTimeStamp;
                privacyRequest.VerificationToken = string.Empty;
                privacyRequest.VerificationTokenV3 = string.Empty;
                privacyRequest.CloudInstance = this.CloudInstance;
                return privacyRequest;
            }

            private ExportRequest CreateExportRequest()
            {
                return new ExportRequest()
                {
                    RequestType = RequestType.Export,
                    PrivacyDataTypes = this.DataTypes,
                    StorageUri = new Uri("https://www.microsoft.com")
                };
            }

            private AccountCloseRequest CreateAccountCloseRequest()
            {
                return new AccountCloseRequest()
                {
                    RequestType = RequestType.AccountClose
                };
            }

            private AgeOutRequest CreateAgeOutRequest(DateTimeOffset requestTimeStamp, ushort ageInMonths = 10 * 12)
            {
                return new AgeOutRequest
                {
                    RequestType = RequestType.AgeOut,
                    IsSuspended = false,
                    LastActive = requestTimeStamp.AddMonths(-1 * ageInMonths)
                };
            }

            private DeleteRequest CreateDeleteRequest()
            {
                return new DeleteRequest()
                {
                    RequestType = RequestType.Delete,
                    PrivacyDataType = this.DataTypes[0]
                };
            }
        }
    }
}
