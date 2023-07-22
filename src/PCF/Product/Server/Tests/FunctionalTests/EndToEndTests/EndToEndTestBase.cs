namespace PCF.FunctionalTests
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.Storage.Blob;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;    
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.PrivacyServices.PXS.Command.CommandStatus;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Xunit;
    using Xunit.Abstractions;
    using PXSV1 = Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    /// <summary>
    /// Base class for end to end tests. This class has the common helper methods.
    /// </summary>
    public class EndToEndTestBase
    {
        private const string MalwareTestString = @"X5O!P%@AP[4\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*";

        protected static readonly string TestCommandRequester = Guid.NewGuid().ToString();
        protected static readonly string TestCommandContext = Guid.NewGuid().ToString();
        
        private TestCommandFeedLogger commandFeedLogger;

        protected TestCommandFeedLogger CommandFeedLogger => this.commandFeedLogger;

        protected ITestOutputHelper OutputHelper => this.outputHelper;

        public readonly ITestOutputHelper outputHelper;

        public EndToEndTestBase(ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;
        }
        
        protected async Task<Uri> PublishAndReceiveAsync<TCommand>(
            Guid agentId,
            Guid assetGroupId,
            PXSV1.PrivacyRequest request,
            Func<TCommand, Task> commandAction = null,
            bool checkCompleteness = true,
            bool supportsQueryCommand = true) where TCommand : PrivacyCommand
        {
            this.OutputHelper.WriteLine($"CommandId = {request.RequestId:n}");
            request.Context = TestCommandContext;
            request.Requester = TestCommandRequester;
            
            this.OutputHelper.WriteLine("Publishing command...");
            await TestSettings.InsertPxsCommandAsync(new[] { request }, this.outputHelper);

            return await this.ReceiveAsync(agentId, assetGroupId, request, commandAction, checkCompleteness, supportsQueryCommand);
        }

        protected async Task<Uri> ReceiveAsync<TCommand>(
            Guid agentId,
            Guid assetGroupId,
            PXSV1.PrivacyRequest request,
            Func<TCommand, Task> commandAction = null,
            bool checkCompleteness = true,
            bool supportsQueryCommand = true) where TCommand : PrivacyCommand
        {
            if (supportsQueryCommand)
            {
                this.OutputHelper.WriteLine("Querying command by ID (no lease receipt)...");
                await this.QueryCommandByIdAsync(agentId, assetGroupId, request.RequestId);
            }

            this.OutputHelper.WriteLine("Receiving command...");
            await this.ReceiveAndCompleteCommandAsync(agentId, request.RequestId, commandAction);

            Uri exportUri = null;
            if (checkCompleteness)
            {
                this.OutputHelper.WriteLine("Waiting for asset group completion...");
                exportUri = await this.WaitForAssetGroupCompletedAsync(request.RequestId, assetGroupId);

                this.OutputHelper.WriteLine("Waiting for global completion...");
                await this.WaitForGloballyCompleteAsync(request.RequestId);
            }

            this.OutputHelper.WriteLine("Querying by requester...");
            await this.CheckCommandIsVisibleByRequesterAsync(request.RequestId, request.RequestType);

            this.OutputHelper.WriteLine("Querying by subject...");
            await this.CheckCommandIsVisibleBySubjectAsync(request.RequestId, request.Subject, request.RequestType);

            return exportUri;
        }

        protected static T PopulateRequest<T>(PrivacyCommand pcfCommand, PXSV1.RequestType requestType) where T : PXSV1.PrivacyRequest, new()
        {
            T item = new T
            {
                RequestType = requestType,
                CorrelationVector = pcfCommand.CorrelationVector,
                RequestGuid = Guid.NewGuid(),
                RequestId = Guid.NewGuid(),
                Subject = pcfCommand.Subject,
                Timestamp = pcfCommand.Timestamp,
                VerificationToken = string.Empty,
                VerificationTokenV3 = string.Empty,
                CloudInstance = pcfCommand.CloudInstance
            };

            return item;
        }

        /// <summary>
        /// When using real event grid, we may need to tell PCF to publish completed events for a given command
        /// so that the "after-command-is-complete" machinery kicks in.
        /// </summary>
        protected async Task ForceCompleteCommandAsync(string commandId)
        {
            // Invoke test hook API to send "complete" events for all agents/asset groups.
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://{TestSettings.ApiHostName}/debug/completecommand/{commandId}");
            var response = await TestSettings.SendWithS2SAync(request, this.OutputHelper);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        protected Task DummyExport(ExportCommand command)
        {
            return this.DummyExport(command, null);
        }

        protected async Task DummyExport(ExportCommand command, Func<ExportCommand, Task> commandAction)
        {
            if (commandAction != null)
            {
                await commandAction(command);
            }

            // Assert that the command's data types were filtered down to what this agent supports.
            Assert.Single(command.PrivacyDataTypes);
            Assert.Equal(Policies.Current.DataTypes.Ids.BrowsingHistory, command.PrivacyDataTypes.Single());

            using (ExportPipeline pipeline = ExportPipelineFactory.CreateAzureExportPipeline(
                new TestCommandFeedLogger(this.OutputHelper),
                command.AzureBlobContainerTargetUri,
                command.AzureBlobContainerPath,
                enableCompression: true))
            {
                for (int i = 0; i < 100000; ++i)
                {
                    await pipeline.ExportAsync(
                        ExportProductId.Office,
                        Policies.Current.DataTypes.Ids.ProductAndServiceUsage,
                        new DateTimeOffset(2020, 6, 15, 13, 45, 30, TimeSpan.FromMinutes(30)),
                        "correlationId",
                        new
                        {
                            foo = new
                            {
                                entry = "bar"
                            }
                        });
                }
            }
        }

        protected async Task DummyMalwareExport(ExportCommand command)
        {
            var container = new CloudBlobContainer(command.AzureBlobContainerTargetUri).GetDirectoryReference(command.AzureBlobContainerPath ?? string.Empty);
            using (ExportPipeline pipeline = new ExportPipeline(new CustomJsonSerializer(), new AzureContainerExportDestination(container), true))
            {
                // Export special Malware text treated as Malware by most scanning agents
                // https://en.wikipedia.org/wiki/EICAR_test_file
                await pipeline.ExportAsync(
                        ExportProductId.Office,
                        Policies.Current.DataTypes.Ids.ProductAndServiceUsage,
                        DateTimeOffset.UtcNow,
                        "correlationId",
                        MalwareTestString);
            }
        }

        protected Task VerifyAgentVariant(DeleteCommand command)
        {
            Assert.Single(command.ApplicableVariants);

            this.OutputHelper.WriteLine($"VariantId = {command.ApplicableVariants[0].VariantId}");

            Assert.Equal("1331F9556A1E4FFF9F38472B76BCDA1A", command.ApplicableVariants[0].VariantId, StringComparer.OrdinalIgnoreCase);

            return Task.FromResult(0);
        }

        protected static async Task VerifyDummyExport(string commandId, Uri storageUri, string agentId, string assetGroupId, bool isMsa = true)
        {
            CloudBlobContainer container = new CloudBlobContainer(storageUri);
            CloudBlob exportZipFileBlob = container.GetBlobReference($"Export-{commandId}.zip");
            using (var memStream = new MemoryStream())
            {
                await exportZipFileBlob.DownloadToStreamAsync(memStream);
                using (var zipArchive = new ZipArchive(memStream, ZipArchiveMode.Read, true))
                {
                    if (isMsa)
                    {
                        // Validate expected CSV format for ProductAndServiceUsage data.
                        string path = ExportProductId.Office.Path + "/001/" + Policies.Current.DataTypes.Ids.ProductAndServiceUsage.Value + ".csv";
                        ZipArchiveEntry entry = zipArchive.Entries.SingleOrDefault(e => e.FullName == path);
                        Assert.NotNull(entry);

                        using (var streamReader = new StreamReader(entry.Open()))
                        {
                            var result = streamReader.ReadLine();
                            Assert.Equal("time,correlationId,\"properties/foo/entry\"", result);

                            result = streamReader.ReadLine();
                            Assert.Equal("\"06/15/2020 13:45:30 +00:30\",correlationId,bar", result);
                        }
                    }
                    else
                    {
                        // Validate expected JSON format for ProductAndServiceUsage data.
                        string path = ExportProductId.Office.Path + "/001/" + Policies.Current.DataTypes.Ids.ProductAndServiceUsage.Value + ".json";
                        ZipArchiveEntry entry = zipArchive.Entries.SingleOrDefault(e => e.FullName == path);
                        Assert.NotNull(entry);
                        using (var streamReader = new StreamReader(entry.Open()))
                        {
                            var json = JArray.Parse(await streamReader.ReadToEndAsync());
                            Assert.True(json[0]["correlationId"].Value<string>() == "correlationId");
                            Assert.True(json[0]["properties"]["foo"]["entry"].Value<string>() == "bar");
                        }
                    }

                    ZipArchiveEntry agentMapEntry = zipArchive.GetEntry("agentMap.json");
                    Assert.NotNull(agentMapEntry);
                    using (var streamReader = new StreamReader(agentMapEntry.Open()))
                    {
                        var json = JObject.Parse(await streamReader.ReadToEndAsync());
                        Assert.Equal(ExportProductId.Office.Path + "/001", json["Paths"][0]["Path"].Value<string>());
                        Assert.Equal(agentId, json["Paths"][0]["AgentId"].Value<string>());
                        Assert.Equal(assetGroupId, json["Paths"][0]["AssetGroupId"].Value<string>());
                    }
                }
            }
        }

        protected static async Task VerifyMalwareDummyExport(string commandId, Uri storageUri)
        {
            CloudBlobContainer container = new CloudBlobContainer(storageUri);
            CloudBlob exportZipFileBlob = container.GetBlobReference($"Export-{commandId}.zip");
            using (var memStream = new MemoryStream())
            {
                await exportZipFileBlob.DownloadToStreamAsync(memStream);
                using (var zipArchive = new ZipArchive(memStream, ZipArchiveMode.Read, true))
                {
                    string path = ExportProductId.Office.Path + "/001/" + Policies.Current.DataTypes.Ids.ProductAndServiceUsage.Value + ".csv";
                    ZipArchiveEntry entry = zipArchive.Entries.SingleOrDefault(e => e.FullName == path);
                    Assert.NotNull(entry);
                    using (var streamReader = new StreamReader(entry.Open()))
                    {
                        string fileContent = await streamReader.ReadToEndAsync();
                        Assert.NotEqual(MalwareTestString, fileContent);                        
                    }
                }
            }
        }

        protected async Task ReceiveAndCompleteCommandAsync<TCommand>(Guid agentId, Guid commandId, Func<TCommand, Task> commandAction) where TCommand : PrivacyCommand
        {
            var client = await this.CreateCommandFeedClientAsync(agentId);

            TCommand command = await client.ReceiveCommandAsync<TCommand>(commandId);
            Assert.NotNull(command);

            if (commandAction != null)
            {
                await commandAction(command);
            }

            await command.CheckpointAsync(CommandStatus.Complete, 0);
        }

        protected async Task QueryCommandByIdAsync(Guid agentId, Guid assetGroupId, Guid commandId)
        {
            var startTime = DateTimeOffset.UtcNow;
            Uri getStatusUri = new Uri($"https://{TestSettings.ApiHostName}/coldstorage/v3/commandquery/{agentId}/{assetGroupId}/{commandId}");

            while (DateTimeOffset.UtcNow - startTime <= TimeSpan.FromSeconds(60))
            {
                var response = await TestSettings.SendWithS2SAync(new HttpRequestMessage(HttpMethod.Get, getStatusUri), this.outputHelper);
                if (response == null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    continue;
                }

                string responseBody = await response.Content.ReadAsStringAsync();
                JObject parsedResponse = JsonConvert.DeserializeObject<JObject>(responseBody);
                string responseCode = parsedResponse.Property("ResponseCode").Value.ToObject<string>();
                if (responseCode != "OK")
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    continue;
                }

                Assert.NotNull(parsedResponse.Property("Command").Value);

                break;
            }
        }

        /// <summary>
        /// Check cold storage that synthetic command got dropped for all non-synthetic agents
        /// </summary>
        protected async Task CheckSyntheticCommandDropped(Guid commandId)
        {
            var startTime = DateTimeOffset.UtcNow;

            while (DateTimeOffset.UtcNow - startTime <= TimeSpan.FromSeconds(60))
            {
                var responseItem = await TestSettings.GetCommandStatusAsync(commandId, this.outputHelper);

                if (responseItem == null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    continue;
                }

                Assert.True(responseItem.IsSyntheticCommand);
                Assert.True(responseItem.AssetGroupStatuses.All(
                    x => string.Equals(x.IngestionActionTaken, "DroppedDueToFiltering", StringComparison.CurrentCultureIgnoreCase)));

                break;
            }
        }

        /// <summary>
        /// Waits for the asset group to mark as completed. This is a result of the checkpoint operation. Returns the export URI for this asset group,
        /// if applicable.
        /// </summary>
        private async Task<Uri> WaitForAssetGroupCompletedAsync(Guid commandId, Guid assetGroupId)
        {
            // Now, poll and check to make sure the command gets marked as "globally complete". This is an indication that our cold storage pipe is working as intended.
            var startTime = DateTimeOffset.UtcNow;
            Uri getStatusUri = new Uri($"https://{TestSettings.ApiHostName}/coldstorage/v3/status/commandid/{commandId:n}");
            Uri exportUri = null;
            bool completed = false;
            int? affectedRows = null;

            while (DateTimeOffset.UtcNow - startTime <= TimeSpan.FromSeconds(60))
            {
                var responseItem = await TestSettings.GetCommandStatusAsync(commandId, this.outputHelper);

                if (responseItem == null)
                {
                    continue;
                }
                
                // Check that "our" asset group is complete.
                var item = responseItem.AssetGroupStatuses.FirstOrDefault(x => x.AssetGroupId == assetGroupId);
                if (item?.CompletedTime != null)
                {
                    // All done
                    completed = true;
                    affectedRows = item.AffectedRows;
                    exportUri = responseItem.FinalExportDestinationUri;

                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            Assert.True(completed);
            Assert.NotNull(affectedRows);
            return exportUri;
        }

        /// <summary>
        /// Force-complete the rest of the asset groups, and wait for the command to be "globally complete".
        /// </summary>
        private async Task WaitForGloballyCompleteAsync(Guid commandId)
        {
            // Now, poll and check to make sure the command gets marked as "globally complete". This is an indication that our cold storage pipe is working as intended.
            var startTime = DateTimeOffset.UtcNow;
            Uri getStatusUri = new Uri($"https://{TestSettings.ApiHostName}/coldstorage/v3/status/commandid/{commandId:n}");
            bool completed = false;

            string expectedCommandId = commandId.ToString("n");

            while (DateTimeOffset.UtcNow - startTime <= TimeSpan.FromSeconds(600))
            {
                // Force the service to mark everyone as completed.
                await this.ForceCompleteCommandAsync(expectedCommandId);

                var requestMessage = new HttpRequestMessage(HttpMethod.Get, getStatusUri);
                var response = await TestSettings.SendWithS2SAync(requestMessage, this.OutputHelper);

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                string json = await response.Content.ReadAsStringAsync();
                var responseItem = JsonConvert.DeserializeObject<CommandStatusResponse>(json);

                Assert.Equal(commandId, responseItem.CommandId);
                Assert.NotEmpty(responseItem.AssetGroupStatuses);

                completed = responseItem.IsGloballyComplete;
                if (completed)
                {
                    // Make sure everyone that we ingested the command for marked as complete.
                    Assert.True(responseItem.AssetGroupStatuses.Where(x => x.IngestionTime != null).All(x => x.CompletedTime != null));
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            Assert.True(completed);
        }

        private async Task CheckCommandIsVisibleByRequesterAsync(Guid commandId, PXSV1.RequestType commandType)
        {
            await Task.Delay(TimeSpan.FromSeconds(2));

            // Also verify the command shows up by the requester as well, with context
            Uri getStatusByRequesterUri = new Uri($"https://{TestSettings.ApiHostName}/coldstorage/v3/status/query?requester={TestCommandRequester}&commandTypes={commandType}");

            var requestByReqeusterMessage = new HttpRequestMessage(HttpMethod.Get, getStatusByRequesterUri);
            var responseByRequester = await TestSettings.SendWithS2SAync(requestByReqeusterMessage, this.OutputHelper);

            Assert.Equal(HttpStatusCode.OK, responseByRequester.StatusCode);

            string jsonByRequester = await responseByRequester.Content.ReadAsStringAsync();
            var responseItemsByRequester = JsonConvert.DeserializeObject<CommandStatusResponse[]>(jsonByRequester);
            var matchingResponseItem = responseItemsByRequester.SingleOrDefault(d => d.CommandId == commandId);
            Assert.NotNull(matchingResponseItem);
            Assert.Equal(matchingResponseItem.Context, TestCommandContext);
            Assert.All(responseItemsByRequester, x => Assert.Empty(x.AssetGroupStatuses));
        }

        private async Task CheckCommandIsVisibleBySubjectAsync(Guid commandId, IPrivacySubject subject, PXSV1.RequestType commandType)
        {
            string subjectQuery;
            switch (subject)
            {
                case MsaSubject msaSubject:
                    subjectQuery = $"msaPuid={msaSubject.Puid}";
                    break;
                case AadSubject aadSubject:
                    subjectQuery = $"aadObjectId={aadSubject.ObjectId}";
                    break;
                default:
                    return;
            }

            Uri lookupByPuidUri = new Uri($"https://{TestSettings.ApiHostName}/coldstorage/v3/status/query?{subjectQuery}&commandTypes={commandType}");
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, lookupByPuidUri);
            var response = await TestSettings.SendWithS2SAync(requestMessage, this.OutputHelper);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string responseBody = await response.Content.ReadAsStringAsync();
            var responseItems = JsonConvert.DeserializeObject<CommandStatusResponse[]>(responseBody);

            Assert.True((subject is MsaSubject && responseItems.All(x => x.Subject is MsaSubject)) ||
                (subject is AadSubject && responseItems.All(x => x.Subject is AadSubject)));

            Assert.Single(responseItems, x => x.CommandId == commandId);
            Assert.All(responseItems, x => Assert.Empty(x.AssetGroupStatuses));
        }

        protected async Task<CommandFeedClient> CreateCommandFeedClientAsync(Guid agentId)
        {
            var clientCertificate = await TestSettings.GetStsCertificateAsync();

            this.commandFeedLogger = new TestCommandFeedLogger(this.OutputHelper);

            return new CommandFeedClient(
                agentId,
                TestSettings.TestSiteId,
                clientCertificate,
                this.commandFeedLogger,
                new InsecureHttpClientFactory(),
                TestSettings.TestEndpointConfig);
        }
    }
}
