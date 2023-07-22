namespace PCF.FunctionalTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Predicates;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.Policy;

    using Newtonsoft.Json;

    using Xunit;
    using Xunit.Abstractions;

#if INCLUDE_TEST_HOOKS

    [Trait("Category", "FCT")]
    public class InsertCommandTests
    {
        private readonly ITestOutputHelper outputHelper;

        public InsertCommandTests(ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;
        }

        [Theory]
        [AutoMoqDeleteCommand(AssetGroupId = TestData.UniversalAssetGroupId, DataTypeId = "BrowsingHistory", SubjectType = typeof(MsaSubject))]
        public Task Delete_BrowsingHistory(DeleteCommand deleteCommand)
        {
            deleteCommand.Subject = TestData.SampleMsaSubject;
            deleteCommand.AssetGroupQualifier = TestData.UniversalAssetGroupQualifier;
            return this.RunDeleteEndToEnd(deleteCommand);
        }

        [Theory]
        [AutoMoqDeleteCommand(AssetGroupId = TestData.UniversalAssetGroupId, DataTypeId = "ContentConsumption", SubjectType = typeof(AadSubject))]
        public Task Delete_ContentConsumption(DeleteCommand deleteCommand)
        {
            deleteCommand.AssetGroupQualifier = TestData.UniversalAssetGroupQualifier;
            deleteCommand.CloudInstance = Policies.Current.CloudInstances.Ids.Public.Value;

            return this.RunDeleteEndToEnd(deleteCommand);
        }

        [Theory]
        [AutoMoqDeleteCommand(AssetGroupId = TestData.UniversalAssetGroupId, DataTypeId = "InkingTypingAndSpeechUtterance", SubjectType = typeof(NonWindowsDeviceSubject))]
        public Task NonWindowsDevice_Delete_ITSU(DeleteCommand deleteCommand)
        {
            deleteCommand.AssetGroupQualifier = TestData.UniversalAssetGroupQualifier;
            return this.RunDeleteEndToEnd(deleteCommand);
        }

        [Theory]
        [AutoMoqDeleteCommand(AssetGroupId = TestData.UniversalAssetGroupId, DataTypeId = "InkingTypingAndSpeechUtterance", SubjectType = typeof(EdgeBrowserSubject))]
        public Task EdgeBrowser_Delete_ITSU(DeleteCommand deleteCommand)
        {
            deleteCommand.AssetGroupQualifier = TestData.UniversalAssetGroupQualifier;
            return this.RunDeleteEndToEnd(deleteCommand);
        }

        [Theory]
        [AutoMoqDeleteCommand(AssetGroupId = TestData.UniversalAssetGroupId, DataTypeId = "InkingTypingAndSpeechUtterance", SubjectType = typeof(DeviceSubject))]
        public Task Win10Device_Delete_ITSU(DeleteCommand deleteCommand)
        {
            deleteCommand.AssetGroupQualifier = TestData.UniversalAssetGroupQualifier;
            return this.RunDeleteEndToEnd(deleteCommand);
        }

        [Theory]
        [AutoMoqDeleteCommand(AssetGroupId = TestData.UniversalAssetGroupId, DataTypeId = "SearchRequestsAndQuery", SubjectType = typeof(DemographicSubject))]
        public Task Delete_SRQ(DeleteCommand deleteCommand)
        {
            deleteCommand.AssetGroupQualifier = TestData.UniversalAssetGroupQualifier;
            return this.RunDeleteEndToEnd(deleteCommand);
        }

        [Theory]
        [AutoMoqDeleteCommand(AssetGroupId = TestData.UniversalAssetGroupId, DataTypeId = "ProductAndServiceUsage", SubjectType = typeof(MsaSubject))]
        public Task Delete_PSU(DeleteCommand deleteCommand)
        {
            var dtpredicate = new ProductAndServiceUsagePredicate
            {
                PropertyBag = new Dictionary<string, List<string>> { { "key1", new[] { "value1", "value2" }.ToList() } }
            };

            deleteCommand.Subject = TestData.SampleMsaSubject;
            deleteCommand.AssetGroupQualifier = TestData.UniversalAssetGroupQualifier;
            deleteCommand.DataTypePredicate = dtpredicate;

            return this.RunDeleteEndToEnd(deleteCommand);
        }

        [Theory]
        [AutoMoqDeleteCommand(AssetGroupId = TestData.UniversalAssetGroupId, DataTypeId = "CustomerContent", SubjectType = typeof(MsaSubject))]
        public Task Delete_NotApplicableToAssetGroup(DeleteCommand deleteCommand)
        {
            // The "universal" asset group doesn't support customer content.
            return this.ExpectInsertFailure(deleteCommand, HttpStatusCode.BadRequest);
        }

        [Theory]
        [AutoMoqDeleteCommand(AssetGroupId = "not an asset group", DataTypeId = "CustomerContent", SubjectType = typeof(MsaSubject))]
        public Task Delete_InvalidAssetGroupId(DeleteCommand deleteCommand)
        {
            // Pick a bogus asset group.
            return this.ExpectInsertFailure(deleteCommand, HttpStatusCode.BadRequest);
        }

        [Theory]
        [AutoMoqDeleteCommand(AssetGroupId = "C74A129B-8EA8-4FBD-A4C5-879F7E4FAAF6", DataTypeId = "CustomerContent", SubjectType = typeof(MsaSubject))]
        public Task Delete_AssetGroupNotInAgent(DeleteCommand deleteCommand)
        {
            // Asset group doesn't belong to agent.
            return this.ExpectInsertFailure(deleteCommand, HttpStatusCode.BadRequest);
        }

        [Theory]
        [AutoMoqExportCommand("ContentConsumption", "SearchRequestsAndQuery", "ProductAndServiceUsage", AssetGroupId = TestData.UniversalAssetGroupId)]
        public async Task EndToEndExport_Universal_AllowedDataTypes(ExportCommand exportCommand)
        {
            exportCommand.AssetGroupQualifier = TestData.UniversalAssetGroupQualifier;

            CommandFeedClient client = await this.CreateCommandFeedClientAsync();
            await client.InsertCommandsAsync(new PrivacyCommand[] { exportCommand }, CancellationToken.None);

            var command = await client.ReceiveCommandAsync<ExportCommand>(Guid.Parse(exportCommand.CommandId));

            Assert.Equal(exportCommand.PrivacyDataTypes, command.PrivacyDataTypes);
            Assert.Equal(exportCommand.AzureBlobContainerTargetUri, command.AzureBlobContainerTargetUri);
            Assert.True(string.IsNullOrEmpty(exportCommand.AgentState));

            DateTimeOffset originalLeaseExpiry = command.ApproximateLeaseExpiration;
            string originalLeaseReceipt = command.LeaseReceipt;

            // Extend lease by 30 seconds, update agent state.
            command.AgentState = "foobar";
            await command.CheckpointAsync(CommandStatus.Pending, 10, TimeSpan.FromSeconds(30));

            // Make sure that the lease expiration changed and that we got a different lease receipt.
            Assert.NotEqual(originalLeaseReceipt, command.LeaseReceipt);
            Assert.True(command.ApproximateLeaseExpiration > originalLeaseExpiry.AddSeconds(15));
            
            // Mark as failed to immediately release the lease.
            await command.CheckpointAsync(CommandStatus.Failed, 0);

            await Task.Delay(TimeSpan.FromSeconds(5));

            // Make sure the agent state got saved.
            command = await client.ReceiveCommandAsync<ExportCommand>(Guid.Parse(exportCommand.CommandId));

            Assert.Equal("foobar", command.AgentState);

            await command.CheckpointAsync(CommandStatus.Complete, 0);
        }

        [Theory]
        [AutoMoqAccountCloseCommand(AssetGroupId = TestData.UniversalAssetGroupId)]
        public Task EndToEndAccountClose(AccountCloseCommand accountCloseCommand)
        {
            accountCloseCommand.AssetGroupQualifier = TestData.UniversalAssetGroupQualifier;
            return this.RunEndToEnd(accountCloseCommand);
        }

        [Theory]
        [AutoMoqAccountCloseCommand(AssetGroupId = TestData.UniversalAssetGroupId, SubjectType = typeof(AadSubject2))]
        public Task EndToEndAccountCloseAad2(AccountCloseCommand accountCloseCommand)
        {
            accountCloseCommand.AssetGroupQualifier = TestData.UniversalAssetGroupQualifier;
            return this.RunEndToEnd(accountCloseCommand);
        }

        [Theory]
        [AutoMoqAgeOutCommand(AssetGroupId = TestData.UniversalAssetGroupId, SubjectType = typeof(MsaSubject))]
        public Task EndToEndAgeOut(AgeOutCommand ageOutCommand)
        {
            ageOutCommand.AssetGroupQualifier = TestData.UniversalAssetGroupQualifier;
            ageOutCommand.LastActive = DateTimeOffset.Parse("1900-01-01T00:00:00Z"); // Hard coding a time value to make test behavior deterministic
            ageOutCommand.IsSuspended = false;

            return this.RunEndToEnd(
                ageOutCommand,
                command =>
                {
                    Assert.Equal(JsonConvert.SerializeObject(ageOutCommand.Subject), JsonConvert.SerializeObject(command.Subject));
                    Assert.False(command.IsSuspended);
                },
                supportsQueryCommand: false);
        }

        [Theory]
        [AutoMoqAgeOutCommand(AssetGroupId = TestData.UniversalAssetGroupId, SubjectType = typeof(MsaSubject))]
        public Task EndToEndAgeOutSuspended(AgeOutCommand ageOutCommand)
        {
            ageOutCommand.AssetGroupQualifier = TestData.UniversalAssetGroupQualifier;
            ageOutCommand.LastActive = DateTimeOffset.Parse("1900-01-01T00:00:00Z"); // Hard coding a time value to make test behavior deterministic
            ageOutCommand.IsSuspended = true;
            ageOutCommand.CommandId = Guid.NewGuid().ToString("n");

            return this.RunEndToEnd(
                ageOutCommand,
                command =>
                {
                    Assert.Equal(JsonConvert.SerializeObject(ageOutCommand.Subject), JsonConvert.SerializeObject(command.Subject));
                    Assert.True(command.IsSuspended, "Command should be suspended");
                },
                supportsQueryCommand: false);
        }

        private Task RunDeleteEndToEnd(DeleteCommand deleteCommand)
        {
            return this.RunEndToEnd(
                deleteCommand,
                command =>
                {
                    Assert.Equal(JsonConvert.SerializeObject(deleteCommand.Subject), JsonConvert.SerializeObject(command.Subject));
                    Assert.Equal(JsonConvert.SerializeObject(deleteCommand.DataTypePredicate), JsonConvert.SerializeObject(command.DataTypePredicate));
                });
        }

        private async Task RunEndToEnd<TCommand>(TCommand initialCommand, Action<TCommand> responseValidator = null, bool supportsQueryCommand = true)
            where TCommand : PrivacyCommand
        {
            DateTimeOffset startTime = DateTimeOffset.UtcNow;
            
            CommandFeedClient client = await this.CreateCommandFeedClientAsync();
            await client.InsertCommandsAsync(new PrivacyCommand[] { initialCommand }, CancellationToken.None);

            TCommand command = await client.ReceiveCommandAsync<TCommand>(Guid.Parse(initialCommand.CommandId));

            Assert.NotNull(command);
            
            Assert.IsType<TCommand>(command);
            responseValidator?.Invoke(command);

            // Immediately re-query the command that was obtained, and make sure the IDs match
            try
            {
                if (supportsQueryCommand)
                {
                    var command2 = await client.QueryCommandAsync(command.LeaseReceipt, CancellationToken.None) as TCommand;

                    Assert.NotNull(command2);
                    Assert.NotNull(command2.LeaseReceipt);
                    Assert.Equal(command.CommandId, command2.CommandId);
                }
            }
            finally
            {
                await command.CheckpointAsync(CommandStatus.Complete, 0);

                // Let worker complete command
                await Task.Delay(TimeSpan.FromSeconds(60));
            }
        }

        private async Task ExpectInsertFailure(PrivacyCommand command, HttpStatusCode expectedStatusCode)
        {
            CommandFeedClient client = await this.CreateCommandFeedClientAsync();

            try
            {
                await client.InsertCommandsAsync(new[] { command }, CancellationToken.None);
                Assert.False(true);
            }
            catch (HttpRequestException ex)
            {
                var statusCode = (HttpStatusCode)ex.Data["StatusCode"];
                Assert.Equal(expectedStatusCode, statusCode);
            }
        }

        private async Task<CommandFeedClient> CreateCommandFeedClientAsync()
        {
            X509Certificate2 clientCertificate = await TestSettings.GetStsCertificateAsync();

            return new CommandFeedClient(
                Guid.Parse(TestData.TestAgentId),
                TestSettings.TestSiteId,
                clientCertificate,
                new TestCommandFeedLogger(this.outputHelper),
                new InsecureHttpClientFactory(),
                TestSettings.TestEndpointConfig);
        }
    }
#endif
}
