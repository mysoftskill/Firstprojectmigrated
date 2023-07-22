namespace PCF.FunctionalTests
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.Policy;

    using Newtonsoft.Json;
    using Xunit;
    using Xunit.Abstractions;
    using PXSV1 = Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    /// <summary>
    /// InsertCommandTests
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    [Trait("Category", "FCT")]
    public class PxsInsertCommandTests : EndToEndTestBase
    {
        // These values correspond to values in the INT configuration that are hardcoded. Test AgentId 4
        private static readonly Guid AgentId = Guid.Parse("F3D89DC9-428E-4823-A64C-A243B459DE53");

        // Supports:
        // Subjects: AAD, MSA
        // Capabilities: Export, Delete, Account Close, AgeOut
        // Data types: ["BrowsingHistory", "ProductAndServiceUsage", "InkingTypingAndSpeechUtterance"] with variants
        private static readonly Guid AssetGroupId = Guid.Parse("7BAA875B-2D94-4008-9D0F-CCBFFCFAD856");

        // These values correspond to values in the INT configuration that are hardcoded. Test AgentId 10 is created for Xbox scenario.
        private static readonly Guid XboxAgentId = Guid.Parse("08F8D795-0AAD-41A9-9D00-3EE6D41DCDB3");

        // This is the asset group associated with Xbox agent.
        private static readonly Guid XboxAssetGroupId = Guid.Parse("F0182920-A465-4AD2-AA86-E5649E3238FE");

        // These values correspond to values in the INT configuration that are hardcoded. Test AgentId 11 is created for Aad2 scenario.
        private static readonly Guid Aad2AgentId = Guid.Parse("7E1148E5-073B-4AB7-94AC-E550093F1A26");

        // This is the asset group associated with Aad2 agent.
        private static readonly Guid Aad2AssetGroupId = Guid.Parse("5695F1A6-C434-4D48-8BE2-0397B3DED56D");

        public PxsInsertCommandTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [SkippableTheory]
        [AutoMoqDeleteCommand(DataTypeId = "BrowsingHistory", SubjectType = typeof(MsaSubject))]
        public async Task Delete_BrowsingHistory(DeleteCommand deleteCommand)
        {
            deleteCommand.AssetGroupId = AssetGroupId.ToString("n");

            var pxsDeleteCommand = PopulateRequest<PXSV1.DeleteRequest>(deleteCommand, PXSV1.RequestType.Delete);

            pxsDeleteCommand.TimeRangePredicate = deleteCommand.TimeRangePredicate;
            pxsDeleteCommand.PrivacyDataType = deleteCommand.PrivacyDataType.Value;
            pxsDeleteCommand.Predicate = deleteCommand.DataTypePredicate;

            await this.PublishAndReceiveAsync<DeleteCommand>(AgentId, AssetGroupId, pxsDeleteCommand);
        }

        [SkippableTheory]
        [AutoMoqDeleteCommand(DataTypeId = "ProductAndServiceUsage", SubjectType = typeof(MsaSubject))]
        public async Task Delete_ProductAndServiceUsage(DeleteCommand deleteCommand)
        {
            deleteCommand.AssetGroupId = AssetGroupId.ToString("n");

            var pxsDeleteCommand = PopulateRequest<PXSV1.DeleteRequest>(deleteCommand, PXSV1.RequestType.Delete);

            pxsDeleteCommand.TimeRangePredicate = deleteCommand.TimeRangePredicate;
            pxsDeleteCommand.PrivacyDataType = deleteCommand.PrivacyDataType.Value;
            pxsDeleteCommand.Predicate = deleteCommand.DataTypePredicate;

            await this.PublishAndReceiveAsync<DeleteCommand>(AgentId, AssetGroupId, pxsDeleteCommand);
        }

        [SkippableTheory]
        [AutoMoqExportCommand(DataTypeIds = new[] { "BrowsingHistory", "ContentConsumption" }, SubjectType = typeof(AadSubject))]
        public async Task Export_BrowsingHistory_Aad(ExportCommand exportCommand)
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri($"https://{TestSettings.ApiHostName}/exportstorage/v1/accounts"));
            var response = await TestSettings.SendWithS2SAync(requestMessage, this.OutputHelper);
            Uri[] storageAccounts = JsonConvert.DeserializeObject<Uri[]>(await response.Content.ReadAsStringAsync());

            exportCommand.AzureBlobContainerTargetUri = storageAccounts.FirstOrDefault();

            exportCommand.AssetGroupId = AssetGroupId.ToString("n");
            exportCommand.CloudInstance = Policies.Current.CloudInstances.Ids.Public.Value;

            PXSV1.ExportRequest exportRequest = PopulateRequest<PXSV1.ExportRequest>(exportCommand, PXSV1.RequestType.Export);
            exportRequest.StorageUri = exportCommand.AzureBlobContainerTargetUri;
            exportRequest.PrivacyDataTypes = exportCommand.PrivacyDataTypes.Select(x => x.Value).ToArray();

            Uri exportUri = await this.PublishAndReceiveAsync<ExportCommand>(AgentId, AssetGroupId, exportRequest, this.DummyExport);
            await VerifyDummyExport(
                exportRequest.RequestId.ToString("n"),
                exportUri,
                AgentId.ToString("n"),
                AssetGroupId.ToString("n"),
                isMsa: false);
        }

        [SkippableTheory]
        [AutoMoqExportCommand(DataTypeIds = new[] { "BrowsingHistory" }, SubjectType = typeof(AadSubject2))]
        public async Task Export_BrowsingHistory_Aad2(ExportCommand exportCommand)
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri($"https://{TestSettings.ApiHostName}/exportstorage/v1/accounts"));
            var response = await TestSettings.SendWithS2SAync(requestMessage, this.OutputHelper);
            Uri[] storageAccounts = JsonConvert.DeserializeObject<Uri[]>(await response.Content.ReadAsStringAsync());

            exportCommand.AzureBlobContainerTargetUri = storageAccounts.FirstOrDefault();

            exportCommand.AssetGroupId = Aad2AssetGroupId.ToString("n");
            exportCommand.CloudInstance = Policies.Current.CloudInstances.Ids.Public.Value;

            PXSV1.ExportRequest exportRequest = PopulateRequest<PXSV1.ExportRequest>(exportCommand, PXSV1.RequestType.Export);
            exportRequest.StorageUri = exportCommand.AzureBlobContainerTargetUri;
            exportRequest.PrivacyDataTypes = exportCommand.PrivacyDataTypes.Select(x => x.Value).ToArray();

            Uri exportUri = await this.PublishAndReceiveAsync<ExportCommand>(Aad2AgentId, Aad2AssetGroupId, exportRequest, this.DummyExport);
            await VerifyDummyExport(
                exportRequest.RequestId.ToString("n"),
                exportUri,
                Aad2AgentId.ToString("n"),
                Aad2AssetGroupId.ToString("n"),
                isMsa: false);
        }

        [SkippableTheory]
        [AutoMoqExportCommand(DataTypeIds = new[] { "BrowsingHistory", "ContentConsumption" }, SubjectType = typeof(MsaSubject))]
        public async Task Export_BrowsingHistory_Msa(ExportCommand exportCommand)
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri($"https://{TestSettings.ApiHostName}/exportstorage/v1/accounts"));
            var response = await TestSettings.SendWithS2SAync(requestMessage, this.OutputHelper);
            Uri[] storageAccounts = JsonConvert.DeserializeObject<Uri[]>(await response.Content.ReadAsStringAsync());

            exportCommand.AzureBlobContainerTargetUri = storageAccounts.FirstOrDefault();

            exportCommand.AssetGroupId = AssetGroupId.ToString("n");

            PXSV1.ExportRequest exportRequest = PopulateRequest<PXSV1.ExportRequest>(exportCommand, PXSV1.RequestType.Export);
            exportRequest.StorageUri = exportCommand.AzureBlobContainerTargetUri;
            exportRequest.PrivacyDataTypes = exportCommand.PrivacyDataTypes.Select(x => x.Value).ToArray();

            Uri exportUri = await this.PublishAndReceiveAsync<ExportCommand>(AgentId, AssetGroupId, exportRequest, this.DummyExport);
            await VerifyDummyExport(exportRequest.RequestId.ToString("n"), exportUri, AgentId.ToString("n"), AssetGroupId.ToString("n"));
        }

        [SkippableTheory]
        [AutoMoqExportCommand(DataTypeIds = new[] { "BrowsingHistory", "ContentConsumption" }, SubjectType = typeof(MsaSubject))]
        public async Task Export_BrowsingHistory_Msa_WithCheckpoint(ExportCommand exportCommand)
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri($"https://{TestSettings.ApiHostName}/exportstorage/v1/accounts"));
            var response = await TestSettings.SendWithS2SAync(requestMessage, this.OutputHelper);
            Uri[] storageAccounts = JsonConvert.DeserializeObject<Uri[]>(await response.Content.ReadAsStringAsync());

            exportCommand.AzureBlobContainerTargetUri = storageAccounts.FirstOrDefault();

            exportCommand.AssetGroupId = AssetGroupId.ToString("n");

            PXSV1.ExportRequest exportRequest = PopulateRequest<PXSV1.ExportRequest>(exportCommand, PXSV1.RequestType.Export);
            exportRequest.StorageUri = exportCommand.AzureBlobContainerTargetUri;
            exportRequest.PrivacyDataTypes = exportCommand.PrivacyDataTypes.Select(x => x.Value).ToArray();

            Uri exportUri = await this.PublishAndReceiveAsync<ExportCommand>(
                AgentId,
                AssetGroupId,
                exportRequest,
                command =>
                    this.DummyExport(
                        command,
                        async c => { await c.CheckpointAsync(CommandStatus.Pending, 0, TimeSpan.FromHours(24)); }));
            await VerifyDummyExport(exportRequest.RequestId.ToString("n"), exportUri, AgentId.ToString("n"), AssetGroupId.ToString("n"));
        }

        [SkippableTheory]
        [AutoMoqExportCommand(DataTypeIds = new[] { "BrowsingHistory", "ContentConsumption" }, SubjectType = typeof(MsaSubject))]
        public async Task Export_Malware_Msa(ExportCommand exportCommand)
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri($"https://{TestSettings.ApiHostName}/exportstorage/v1/accounts"));
            var response = await TestSettings.SendWithS2SAync(requestMessage, this.OutputHelper);
            Uri[] storageAccounts = JsonConvert.DeserializeObject<Uri[]>(await response.Content.ReadAsStringAsync());

            exportCommand.AzureBlobContainerTargetUri = storageAccounts.FirstOrDefault();

            exportCommand.AssetGroupId = AssetGroupId.ToString("n");

            PXSV1.ExportRequest exportRequest = PopulateRequest<PXSV1.ExportRequest>(exportCommand, PXSV1.RequestType.Export);
            exportRequest.StorageUri = exportCommand.AzureBlobContainerTargetUri;
            exportRequest.PrivacyDataTypes = exportCommand.PrivacyDataTypes.Select(x => x.Value).ToArray();

            Uri exportUri = await this.PublishAndReceiveAsync<ExportCommand>(AgentId, AssetGroupId, exportRequest, this.DummyMalwareExport);

            await VerifyMalwareDummyExport(exportRequest.RequestId.ToString("n"), exportUri);
        }

        [SkippableTheory]
        [AutoMoqAccountCloseCommand]
        public async Task EndToEndAccountClose(AccountCloseCommand accountCloseCommand)
        {
            accountCloseCommand.AssetGroupId = AssetGroupId.ToString("n");

            PXSV1.AccountCloseRequest accountCloseRequest = PopulateRequest<PXSV1.AccountCloseRequest>(accountCloseCommand, PXSV1.RequestType.AccountClose);
            await this.PublishAndReceiveAsync<AccountCloseCommand>(AgentId, AssetGroupId, accountCloseRequest);
        }

        [SkippableTheory]
        [AutoMoqAccountCloseCommand(SubjectType = typeof(AadSubject2))]
        public async Task EndToEndAccountCloseAad2(AccountCloseCommand accountCloseCommand)
        {
            accountCloseCommand.AssetGroupId = Aad2AssetGroupId.ToString("n");

            PXSV1.AccountCloseRequest accountCloseRequest = PopulateRequest<PXSV1.AccountCloseRequest>(accountCloseCommand, PXSV1.RequestType.AccountClose);
            await this.PublishAndReceiveAsync<AccountCloseCommand>(Aad2AgentId, Aad2AssetGroupId, accountCloseRequest);
        }

        [SkippableTheory]
        [AutoMoqAccountCloseCommand(SubjectType = typeof(AadSubject2), TenantIdType = TenantIdType.Resource)]
        public async Task EndToEndAccountCleanupAad2(AccountCloseCommand accountCleanupCommand)
        {
            accountCleanupCommand.AssetGroupId = Aad2AssetGroupId.ToString("n");

            PXSV1.AccountCloseRequest accountCloseRequest = PopulateRequest<PXSV1.AccountCloseRequest>(accountCleanupCommand, PXSV1.RequestType.AccountClose);
            await this.PublishAndReceiveAsync<AccountCloseCommand>(Aad2AgentId, Aad2AssetGroupId, accountCloseRequest);
        }

        [SkippableTheory]
        [AutoMoqAgeOutCommand(SubjectType = typeof(MsaSubject))]
        public async Task EndToEndAgeOutComplete(AgeOutCommand ageOutCommand)
        {
            PXSV1.AgeOutRequest request = CreateAgeOutRequest(ageOutCommand);
            request.IsSuspended = false;

            await this.PublishAndReceiveAsync<AgeOutCommand>(
                AgentId,
                AssetGroupId,
                request,
                checkCompleteness: false,
                supportsQueryCommand: false,
                commandAction: c => { Assert.False(c.IsSuspended); return Task.CompletedTask; });
        }

        [SkippableTheory]
        [AutoMoqAgeOutCommand(SubjectType = typeof(MsaSubject))]
        public async Task EndToEndAgeOutSuspendedComplete(AgeOutCommand ageOutCommand)
        {
            PXSV1.AgeOutRequest request = CreateAgeOutRequest(ageOutCommand);
            request.IsSuspended = true;

            await this.PublishAndReceiveAsync<AgeOutCommand>(
                AgentId,
                AssetGroupId,
                request,
                checkCompleteness: false,
                supportsQueryCommand: false,
                commandAction: c => { Assert.True(c.IsSuspended); return Task.CompletedTask; });
        }

        [SkippableTheory]
        [AutoMoqAgeOutCommand(SubjectType = typeof(MsaSubject))]
        public async Task EndToEndAgeOutCheckpointPendingWithAgentStateAndLeaseExtension(AgeOutCommand ageOutCommand)
        {
            PXSV1.AgeOutRequest request = CreateAgeOutRequest(ageOutCommand);

            await this.PublishAndReceiveAsync<AgeOutCommand>(
                AgentId, 
                AssetGroupId, 
                request, 
                async command => 
                {
                    // request long lease extension before completing, and include agent state.
                    command.AgentState = "agent state here!";
                    await command.CheckpointAsync(CommandStatus.Pending, 0, TimeSpan.FromHours(24));
                },
                checkCompleteness: false,
                supportsQueryCommand: false);
        }

        [SkippableTheory]
        [AutoMoqAgeOutCommand(SubjectType = typeof(MsaSubject))]
        public async Task EndToEndAgeOutCheckpointPendingWithAgentState(AgeOutCommand ageOutCommand)
        {
            PXSV1.AgeOutRequest request = CreateAgeOutRequest(ageOutCommand);

            await this.PublishAndReceiveAsync<AgeOutCommand>(
                AgentId, 
                AssetGroupId, 
                request, 
                async command =>
                {
                    // only checkpoint pending with agent state (no lease extension)
                    command.AgentState = "agent state here!";
                    await command.CheckpointAsync(CommandStatus.Pending, 0);
                }, 
                checkCompleteness: false, 
                supportsQueryCommand: false);
        }

        [SkippableTheory]
        [AutoMoqAgeOutCommand(SubjectType = typeof(MsaSubject))]
        public async Task EndToEndAgeOutCheckpointFailed(AgeOutCommand ageOutCommand)
        {
            PXSV1.AgeOutRequest request = CreateAgeOutRequest(ageOutCommand);

            await this.PublishAndReceiveAsync<AgeOutCommand>(
                AgentId, 
                AssetGroupId, 
                request, 
                async command =>
                {
                    await command.CheckpointAsync(CommandStatus.Failed, 0);
                },
                checkCompleteness: false,
                supportsQueryCommand: false);
        }

        [SkippableTheory]
        [AutoMoqAgeOutCommand(SubjectType = typeof(MsaSubject))]
        public async Task EndToEndAgeOutCheckpointVerificationFailed(AgeOutCommand ageOutCommand)
        {
            PXSV1.AgeOutRequest request = CreateAgeOutRequest(ageOutCommand);

            await this.PublishAndReceiveAsync<AgeOutCommand>(
                AgentId, 
                AssetGroupId, 
                request, 
                async command =>
                {
                    await command.CheckpointAsync(CommandStatus.VerificationFailed, 0);
                },
                checkCompleteness: false,
                supportsQueryCommand: false);
        }

        [SkippableTheory]
        [AutoMoqAgeOutCommand(SubjectType = typeof(MsaSubject))]
        public async Task EndToEndAgeOutCheckpointUnexpectedCommand(AgeOutCommand ageOutCommand)
        {
            PXSV1.AgeOutRequest request = CreateAgeOutRequest(ageOutCommand);

            await this.PublishAndReceiveAsync<AgeOutCommand>(
                AgentId, 
                AssetGroupId, 
                request, 
                async command =>
                {
                    await command.CheckpointAsync(CommandStatus.UnexpectedCommand, 0);
                },
                checkCompleteness: false,
                supportsQueryCommand: false);
        }

        /// <summary>
        /// Special test to ensure that deferred deletions happen as expected when a long enough lease receipt permits it.
        /// </summary>
        [SkippableTheory]
        [AutoMoqDeleteCommand(DataTypeId = "BrowsingHistory", SubjectType = typeof(MsaSubject))]
        public async Task Delete_BrowsingHistory_DeferredDeletion(DeleteCommand deleteCommand)
        {
            deleteCommand.AssetGroupId = AssetGroupId.ToString("n");

            var pxsDeleteCommand = PopulateRequest<PXSV1.DeleteRequest>(deleteCommand, PXSV1.RequestType.Delete);

            pxsDeleteCommand.TimeRangePredicate = deleteCommand.TimeRangePredicate;
            pxsDeleteCommand.PrivacyDataType = deleteCommand.PrivacyDataType.Value;
            pxsDeleteCommand.Predicate = deleteCommand.DataTypePredicate;

            await TestSettings.InsertPxsCommandAsync(new[] { pxsDeleteCommand }, this.OutputHelper);

            await this.ReceiveAndCompleteCommandAsync<DeleteCommand>(AgentId, pxsDeleteCommand.RequestId, async command =>
            {
                // request long lease extension before completing.
                await command.CheckpointAsync(CommandStatus.Pending, 0, TimeSpan.FromHours(24));
            });

            // Now, inspect the headers to make sure that PCF did the deferred deletion pipeline.
            Assert.True(this.CommandFeedLogger.LastResponse.Headers.TryGetValues("X-NonTransactional-Checkpoint-Delay", out var values));
            Assert.Single(values);
        }

        [SkippableTheory]
        [AutoMoqDeleteCommand(DataTypeId = "InkingTypingAndSpeechUtterance", SubjectType = typeof(MsaSubject))]
        public async Task PcfDoesNotSuppressCommandForAgentVariant(DeleteCommand deleteCommand)
        {
            deleteCommand.AssetGroupId = AssetGroupId.ToString("n");

            var pxsDeleteCommand = PopulateRequest<PXSV1.DeleteRequest>(deleteCommand, PXSV1.RequestType.Delete);

            pxsDeleteCommand.TimeRangePredicate = deleteCommand.TimeRangePredicate;
            pxsDeleteCommand.PrivacyDataType = deleteCommand.PrivacyDataType.Value;
            pxsDeleteCommand.Predicate = deleteCommand.DataTypePredicate;

            await this.PublishAndReceiveAsync<DeleteCommand>(AgentId, AssetGroupId, pxsDeleteCommand, this.VerifyAgentVariant);
        }

        [SkippableTheory]
        [AutoMoqDeleteCommand(SubjectType = typeof(MsaSubject))]
        public async Task SyntheticTestCommand_Dropped(DeleteCommand deleteCommand)
        {
            deleteCommand.AssetGroupId = AssetGroupId.ToString("n");

            var pxsDeleteCommand = PopulateRequest<PXSV1.DeleteRequest>(deleteCommand, PXSV1.RequestType.Delete);

            pxsDeleteCommand.TimeRangePredicate = deleteCommand.TimeRangePredicate;
            pxsDeleteCommand.PrivacyDataType = deleteCommand.PrivacyDataType.Value;
            pxsDeleteCommand.Predicate = deleteCommand.DataTypePredicate;
            pxsDeleteCommand.IsSyntheticRequest = true;

            this.OutputHelper.WriteLine($"CommandId = {pxsDeleteCommand.RequestId:n}");
            pxsDeleteCommand.Context = TestCommandContext;
            pxsDeleteCommand.Requester = TestCommandRequester;

            this.OutputHelper.WriteLine("Publishing command...");
            await TestSettings.InsertPxsCommandAsync(new[] { pxsDeleteCommand }, this.OutputHelper);

            await this.CheckSyntheticCommandDropped(pxsDeleteCommand.RequestId);
        }

        [SkippableTheory]
        [AutoMoqDeleteCommand(DataTypeId = "ProductAndServiceUsage", SubjectType = typeof(AadSubject))]
        public async Task PcfSuppressesCommandForPcfVariant(DeleteCommand deleteCommand)
        {
            deleteCommand.AssetGroupId = AssetGroupId.ToString("n");
            deleteCommand.CloudInstance = Policies.Current.CloudInstances.Ids.Public.Value;

            var pxsDeleteCommand = PopulateRequest<PXSV1.DeleteRequest>(deleteCommand, PXSV1.RequestType.Delete);

            pxsDeleteCommand.TimeRangePredicate = deleteCommand.TimeRangePredicate;
            pxsDeleteCommand.PrivacyDataType = deleteCommand.PrivacyDataType.Value;
            pxsDeleteCommand.Predicate = deleteCommand.DataTypePredicate;

            this.OutputHelper.WriteLine($"CommandId = {pxsDeleteCommand.RequestId:n}");
            pxsDeleteCommand.Context = TestCommandContext;
            pxsDeleteCommand.Requester = TestCommandRequester;

            this.OutputHelper.WriteLine("Publishing command...");
            await TestSettings.InsertPxsCommandAsync(new[] { pxsDeleteCommand }, this.OutputHelper);

            var filteredCommandId = pxsDeleteCommand.RequestId;

            deleteCommand.Subject = new MsaSubject
            {
                Anid = "anId",
                Cid = 12345,
                Opid = "opid",
                Puid = 12345,
                Xuid = "xuid"
            };

            deleteCommand.CommandId = Guid.NewGuid().ToString();
            pxsDeleteCommand = PopulateRequest<PXSV1.DeleteRequest>(deleteCommand, PXSV1.RequestType.Delete);

            pxsDeleteCommand.TimeRangePredicate = deleteCommand.TimeRangePredicate;
            pxsDeleteCommand.PrivacyDataType = deleteCommand.PrivacyDataType.Value;
            pxsDeleteCommand.Predicate = deleteCommand.DataTypePredicate;

            await TestSettings.InsertPxsCommandAsync(new[] { pxsDeleteCommand }, this.OutputHelper);

            var client = await this.CreateCommandFeedClientAsync(AgentId);

            DeleteCommand command = await client.ReceiveCommandWithFilteringAsync<DeleteCommand>(pxsDeleteCommand.RequestId, filteredCommandId);
            Assert.NotNull(command);

            await command.CheckpointAsync(CommandStatus.Complete, 0);
        }

        [SkippableTheory]
        [AutoMoqDeleteCommand(DataTypeId = "BrowsingHistory", SubjectType = typeof(MsaSubject))]
        public async Task Delete_BrowsingHistory_ForXboxAgent(DeleteCommand deleteCommand)
        {
            deleteCommand.AssetGroupId = XboxAssetGroupId.ToString("n");

            var pxsDeleteCommand = PopulateRequest<PXSV1.DeleteRequest>(deleteCommand, PXSV1.RequestType.Delete);

            pxsDeleteCommand.TimeRangePredicate = deleteCommand.TimeRangePredicate;
            pxsDeleteCommand.PrivacyDataType = deleteCommand.PrivacyDataType.Value;
            pxsDeleteCommand.Predicate = deleteCommand.DataTypePredicate;

            await this.PublishAndReceiveAsync<DeleteCommand>(XboxAgentId, XboxAssetGroupId, pxsDeleteCommand);
        }

        [SkippableTheory]
        [AutoMoqExportCommand(DataTypeIds = new[] { "BrowsingHistory" }, SubjectType = typeof(MsaSubject))]
        public async Task Export_BrowsingHistory_Msa_ForXboxAgent(ExportCommand exportCommand)
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri($"https://{TestSettings.ApiHostName}/exportstorage/v1/accounts"));
            var response = await TestSettings.SendWithS2SAync(requestMessage, this.OutputHelper);
            Uri[] storageAccounts = JsonConvert.DeserializeObject<Uri[]>(await response.Content.ReadAsStringAsync());

            exportCommand.AzureBlobContainerTargetUri = storageAccounts.FirstOrDefault();

            exportCommand.AssetGroupId = XboxAssetGroupId.ToString("n");

            PXSV1.ExportRequest exportRequest = PopulateRequest<PXSV1.ExportRequest>(exportCommand, PXSV1.RequestType.Export);
            exportRequest.StorageUri = exportCommand.AzureBlobContainerTargetUri;
            exportRequest.PrivacyDataTypes = exportCommand.PrivacyDataTypes.Select(x => x.Value).ToArray();

            Uri exportUri = await this.PublishAndReceiveAsync<ExportCommand>(XboxAgentId, XboxAssetGroupId, exportRequest, this.DummyExport);
            await VerifyDummyExport(exportRequest.RequestId.ToString("n"), exportUri, XboxAgentId.ToString("n"), XboxAssetGroupId.ToString("n"));
        }

        private static PXSV1.AgeOutRequest CreateAgeOutRequest(AgeOutCommand ageOutCommand)
        {
            ageOutCommand.AssetGroupId = AssetGroupId.ToString("n");
            PXSV1.AgeOutRequest request = PopulateRequest<PXSV1.AgeOutRequest>(ageOutCommand, PXSV1.RequestType.AgeOut);
            request.IsSuspended = ageOutCommand.IsSuspended;
            request.LastActive = DateTimeOffset.Parse("1900-01-01T00:00:00Z"); // Hard coding a time value to make test behavior deterministic
            return request;
        }
    }
}
