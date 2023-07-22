namespace PCF.UnitTests.Applicability
{
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Predicates;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.Applicability;
    using Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.PrivacyServices.SignalApplicability;
    using Newtonsoft.Json;
    using PCF.UnitTests;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Xunit;
    using Xunit.Abstractions;
    using ServicePrivacyCommand = Microsoft.PrivacyServices.CommandFeed.Service.Common;

    /// <summary>
    /// Applicability library unittests.
    /// </summary>
    [Trait("Category", "UnitTest")]
    public class ApplicabilityLibraryUnitTests
    {
        private readonly ITestOutputHelper outputHelper;
        private const string TestCommandsFileName = "TestCommands.json";
        private static readonly ApplicabilityResultMatrix ApplicabilityMatrix;
        private static readonly IDataAgentMap TestDataAgentMap;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        static ApplicabilityLibraryUnitTests()
        {
            Assembly current = typeof(ApplicabilityLibraryUnitTests).Assembly;
            string configFile = current.GetManifestResourceNames().Single(x => x.IndexOf(TestCommandsFileName, StringComparison.OrdinalIgnoreCase) >= 0);

            using (var streamReader = new StreamReader(current.GetManifestResourceStream(configFile)))
            using (var jsonReader = new JsonTextReader(streamReader))
            {
                JsonSerializer serializer = new JsonSerializer();
                ApplicabilityMatrix = serializer.Deserialize<ApplicabilityResultMatrix>(jsonReader);
            }

            TestDataAgentMap = new TestDataAgentMap();
        }

        public ApplicabilityLibraryUnitTests(ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;
        }

        public static IAssetGroupInfo GetAssetGroupInfoById(AssetGroupId assetGroupId)
        {
            return TestDataAgentMap.AssetGroupInfos.Where(x => x.AssetGroupId == assetGroupId).First();
        }

        public static IEnumerable<object[]> GetCommandApplicabilityInfo()
        {
            var applicabilityInfo = new List<object[]>();
            foreach (var testCommand in ApplicabilityMatrix.Commands)
            {
                foreach (var assetGroup in ApplicabilityMatrix.AssetGroups)
                {
                    applicabilityInfo.Add(new object[] 
                    {
                        testCommand.CommandName,
                        testCommand.BuildPrivacyCommand(assetGroup),
                        assetGroup,
                        testCommand.ApplicableAssetGroups.Contains(assetGroup)
                    });
                }
            }

            return applicabilityInfo;
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

            public ServicePrivacyCommand.PrivacyCommand BuildPrivacyCommand(string assetGroupId)
            {
                ServicePrivacyCommand.PrivacyCommand privacyCommand;

                switch (this.CommandType)
                {
                    case "Delete":
                        privacyCommand = new ServicePrivacyCommand.DeleteCommand(
                            agentId: new AgentId(Guid.NewGuid()),
                            assetGroupQualifier: "assetGroupQualifier",
                            verifier: "verifier",
                            verifierV3: "verifierv3",
                            commandId: new CommandId(Guid.NewGuid()),
                            batchId: new RequestBatchId(Guid.NewGuid()),
                            nextVisibleTime: DateTimeOffset.UtcNow,
                            subject: this.GetPrivacySubject(),
                            clientCommandState: this.CommandName,
                            assetGroupId: new AssetGroupId(assetGroupId),
                            correlationVector: "correlationVector",
                            timestamp: DateTimeOffset.UtcNow,
                            cloudInstance: this.CloudInstance,
                            commandSource: "commandSource",
                            processorApplicable: null,
                            controllerApplicable: null,
                            dataTypePredicate: null,
                            timePredicate: new AutoFixtureTestDataBuilder<TimeRangePredicate>().Build(),
                            dataType: Policies.Current.DataTypes.CreateId(this.DataTypes[0]),
                            absoluteExpirationTime: DateTimeOffset.UtcNow,
                            queueStorageType: QueueStorageType.AzureCosmosDb);
                        break;

                    case "AccountClose":
                        privacyCommand = new ServicePrivacyCommand.AccountCloseCommand(
                            agentId: new AgentId(Guid.NewGuid()),
                            assetGroupQualifier: "assetGroupQualifier",
                            verifier: "verifier",
                            verifierV3: "verifierv3",
                            commandId: new CommandId(Guid.NewGuid()),
                            batchId: new RequestBatchId(Guid.NewGuid()),
                            nextVisibleTime: DateTimeOffset.UtcNow,
                            subject: this.GetPrivacySubject(),
                            clientCommandState: this.CommandName,
                            assetGroupId: new AssetGroupId(assetGroupId),
                            correlationVector: "correlationVector",
                            timestamp: DateTimeOffset.UtcNow,
                            cloudInstance: this.CloudInstance,
                            commandSource: "commandSource",
                            processorApplicable: null,
                            controllerApplicable: null,
                            absoluteExpirationTime: DateTimeOffset.UtcNow,
                            queueStorageType: QueueStorageType.AzureCosmosDb);
                        break;

                    case "Export":
                        privacyCommand = new ServicePrivacyCommand.ExportCommand(
                            agentId: new AgentId(Guid.NewGuid()),
                            assetGroupQualifier: "assetGroupQualifier",
                            verifier: "verifier",
                            verifierV3: "verifierv3",
                            commandId: new CommandId(Guid.NewGuid()),
                            batchId: new RequestBatchId(Guid.NewGuid()),
                            nextVisibleTime: DateTimeOffset.UtcNow,
                            subject: this.GetPrivacySubject(),
                            clientCommandState: this.CommandName,
                            assetGroupId: new AssetGroupId(assetGroupId),
                            correlationVector: "correlationVector",
                            timestamp: DateTimeOffset.UtcNow,
                            cloudInstance: this.CloudInstance,
                            commandSource: "commandSource",
                            processorApplicable: null,
                            controllerApplicable: null,
                            dataTypes: this.DataTypes.Select(d => Policies.Current.DataTypes.CreateId(d)),
                            absoluteExpirationTime: DateTimeOffset.UtcNow,
                            queueStorageType: QueueStorageType.AzureCosmosDb);
                        break;

                    case "AgeOut":
                        var requestTime = DateTimeOffset.UtcNow;
                        privacyCommand = new ServicePrivacyCommand.AgeOutCommand(
                            agentId: new AgentId(Guid.NewGuid()),
                            assetGroupQualifier: "assetGroupQualifier",
                            verifier: "verifier",
                            verifierV3: "verifierv3",
                            commandId: new CommandId(Guid.NewGuid()),
                            batchId: new RequestBatchId(Guid.NewGuid()),
                            nextVisibleTime: DateTimeOffset.UtcNow,
                            subject: this.GetPrivacySubject(),
                            clientCommandState: this.CommandName,
                            assetGroupId: new AssetGroupId(assetGroupId),
                            correlationVector: "correlationVector",
                            timestamp: requestTime,
                            cloudInstance: this.CloudInstance,
                            commandSource: "commandSource",
                            processorApplicable: null,
                            controllerApplicable: null,
                            absoluteExpirationTime: DateTimeOffset.UtcNow,
                            lastActive: requestTime.AddMonths(-1 * 10 * 12),
                            isSuspended: false,
                            queueStorageType: QueueStorageType.AzureCosmosDb);
                        break;

                    default:
                        throw new InvalidOperationException();
                }

                return privacyCommand;
            }

            private IPrivacySubject GetPrivacySubject()
            {
                IPrivacySubject privacySubject = null;

                switch (this.SubjectType)
                {
                    case "Aad":
                        privacySubject = new AadSubject()
                        {
                            ObjectId = Guid.NewGuid(),
                            OrgIdPUID = 12345L,
                            TenantId = Guid.NewGuid()
                        };
                        break;
                    case "Aad2":
                        // Randomly select the same TenantId and HomeTenantId
                        Guid guid = Guid.NewGuid();
                        privacySubject = new AadSubject2()
                        {
                            ObjectId = Guid.NewGuid(),
                            OrgIdPUID = 12345L,
                            TenantId = guid,
                            HomeTenantId = guid,
                            TenantIdType = TenantIdType.Resource,
                        };
                        break;
                    case "Msa":
                        privacySubject = new MsaSubject()
                        {
                            Puid = 12345L,
                            Anid = "anid",
                            Cid = 123L,
                            Opid = "opid",
                            Xuid = "xuid"
                        };
                        break;
                    case "Device":
                        privacySubject = new DeviceSubject()
                        {
                            GlobalDeviceId = 12345L
                        };
                        break;
                    case "Demographic":
                        privacySubject = new DemographicSubject()
                        {
                            EmailAddresses = new[] { "mytest@test.com" }
                        };
                        break;

                    case "MicrosoftEmployee":
                        privacySubject = new MicrosoftEmployee()
                        {
                            Emails = new[] { "mytest@test.com" }
                        };
                        break;
                    case "NonWindowsDevice":
                        privacySubject = new NonWindowsDeviceSubject()
                        {
                            MacOsPlatformDeviceId = Guid.NewGuid()
                        };
                        break;
                    case "EdgeBrowser":
                        privacySubject = new EdgeBrowserSubject()
                        {
                            EdgeBrowserId = 12345L
                        };
                        break;
                    default:
                        throw new InvalidOperationException($"Cannot convert test command subject {this.SubjectType} to CommandFeed.Contracts.Subjects.");
                }

                return privacySubject;
            }
        }

        [Theory]
        [MemberData(nameof(GetCommandApplicabilityInfo))]
        public void DoApplicabilityTest(string commandName, ServicePrivacyCommand.PrivacyCommand privacyCommand, string assetGroup, bool isApplicable)
        {
            IAssetGroupInfo assetGroupInfo = GetAssetGroupInfoById(new AssetGroupId(assetGroup));

            DataAsset dataAsset = assetGroupInfo.ToDataAsset();
            SignalInfo signal = privacyCommand.ToSignalInfo();

            var applicabilityResult = dataAsset.CheckSignalApplicability(signal);

            Assert.True(
                applicabilityResult.IsApplicable() == isApplicable,
                $"Unexpected SAL result. Expected IsApplicable={isApplicable}, TestCommand={commandName}, AssetGroupId={assetGroup}, SAL={applicabilityResult.ReasonDescription}");
        }
    }
}