namespace Microsoft.PrivacyServices.CommandFeed.CustomActivity
{
    using global::Azure.Identity;
    using global::Azure.Security.KeyVault.Certificates;
    using global::Azure.Storage.Sas;
    using global::Azure.Storage;
    using global::Azure.Storage.Blobs;
    using global::Azure.Storage.Blobs.Models;
    using global::Azure.Storage.Blobs.Specialized;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using CsvHelper;
    using CsvHelper.Configuration;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts;
    using Microsoft.PrivacyServices.CommandFeed.Client.Helpers;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using PrivacyCommandCustomActivity;
    using System.Net.Http;

    /// <summary>
    /// A console app called by ADF CustomActivity
    /// https://docs.microsoft.com/en-us/azure/data-factory/transform-data-using-custom-activity
    /// </summary>
    internal class Program
    {
        private static readonly int blobUploadSizeLimit = 50 * 1024 * 1024; //Blob upload size limit
        static int Main(string[] args)
        {
            ConsoleLogger.Log("PcfCustomActivity Started.");
            try
            {
                var cts = new CancellationTokenSource();
                RunActivity(cts.Token);
                ConsoleLogger.Log("PcfCustomActivity Run Succeeded.");
            }
            catch (Exception ex)
            {
                ConsoleLogger.Log("PcfCustomActivity Run Failed.");
                ConsoleLogger.LogException(ex);
                return -1;
            }

            ConsoleLogger.Log("PcfCustomActivity Finished.");
            return 0;
        }

        private static (Action, OperationType) ValidateParameters(RunParameters parameters)
        {
            Action action = (Action)Enum.Parse(typeof(Action), parameters.CustomActivityType, ignoreCase: true);
            OperationType operationType = (OperationType)Enum.Parse(typeof(OperationType), parameters.Operation, ignoreCase: true);

            if (action == Action.CompleteCommands &&
                 operationType == OperationType.Export && (string.IsNullOrEmpty(parameters.StagingContainerUri) || string.IsNullOrEmpty(parameters.StagingRootFolder)))
            {
                throw new ArgumentException($"Missing or invalid parameters:\nFor complete export commands, staging storage container url and the name of the root export folder are required");
            }

            return (action, operationType);
        }

        private static void RunActivity(CancellationToken cancellationToken)
        {
            // Try activity.json in Test folder first, this is our test hook.
            var activityFilePath = @"Test\activity.json"; 
            if (!File.Exists(activityFilePath))
            {
                activityFilePath = "activity.json";
            }

            // Extract run parameters from activity's extendedProperties
            dynamic activity = JsonConvert.DeserializeObject(File.ReadAllText(activityFilePath));
            var runParameters = new RunParameters(activity.typeProperties.extendedProperties);
            ConsoleLogger.Log($"RunActivity with Action = {runParameters.CustomActivityType}, OperationType = {runParameters.Operation}.");
            
            (var action, var operationType) = ValidateParameters(runParameters);
            ConsoleLogger.Log("Loading activity.json and linkedServices.json.");
            //dynamic linkedServices = JsonConvert.DeserializeObject(File.ReadAllText("linkedServices.json"));

            ConsoleLogger.Log($"Loading client certificate {runParameters.ClientCertName} from {runParameters.ClientCertAKV}.");

            // Azure Batch Pool running this activity should have an UAMI assigned and the UAMI should have get cert permission for the KeyVault
            var certClient = new CertificateClient(new Uri(runParameters.ClientCertAKV), new DefaultAzureCredential());
            var certificateResponse = certClient.DownloadCertificate(runParameters.ClientCertName);

            ConsoleLogger.Log($"Client certificate {certificateResponse.Value.Thumbprint} loaded.");

            ConsoleLogger.Log($"Constructing PcfClient with config {runParameters.Endpoint} - {runParameters.Tenant}.");

            CommandFeedEndpointConfiguration pcfConfiguration;
            switch (runParameters.Tenant.ToLowerInvariant())
            {
                case "microsoft":
                    pcfConfiguration = runParameters.Endpoint.ToLowerInvariant() == "ppe" ? CommandFeedEndpointConfiguration.PreproductionV2 : CommandFeedEndpointConfiguration.ProductionV2;
                    break;
                case "ame":
                    pcfConfiguration = runParameters.Endpoint.ToLowerInvariant() == "ppe" ? CommandFeedEndpointConfiguration.PreproductionAMEV2 : CommandFeedEndpointConfiguration.ProductionAMEV2;
                    break;
                default:
                    throw new ArgumentException("extendedProperties.tenant has invalid value. It can only be either Microsoft or AME");
            }

            var pcfClient = new CommandFeedClient(
                runParameters.AgentId,
                runParameters.ClientAadAppId.ToString(),
                certificateResponse.Value,
                new ConsoleLogger(),
                new DefaultHttpClientFactory(),
                pcfConfiguration,
                sendX5c: true);

            ConsoleLogger.Log("PcfClient Constructed.");

            List<Task> tasks = new List<Task>();
            switch (action)
            {
                case Action.GetCommandConfiguration:
                    tasks.Add(GetCommandConfiguration(pcfClient, runParameters, cancellationToken));
                    break;

                case Action.GetAssetGroupDetails:
                    tasks.Add(GetAssetGroupDetailsAsync(pcfClient, runParameters, cancellationToken));
                    break;

                case Action.GetCommands:
                case Action.GetAllCommands:
                    // GetCommands will produce the following files in the target storage blob container:
                    // AssetGroupId/
                    //   |- CommandConfiguration.json
                    //   |- AssetGroupDetails_1.txt
                    //   |- AssetGroupDetails_2.txt
                    //   |- AssetGroupDetails_n.txt
                    //   |- StartTime_EndTime/
                    //      |- Delete/
                    //         |- DeleteCommands_1.txt
                    //         |- DeleteCommands_2.txt
                    //         |- DeleteCommands_n.txt
                    //         |- DeleteCompletionToken.txt
                    // ( If operationType is Export, replace Delete with Export)
                    if (!runParameters.GetCommandOptions.Contains("NoCommandConfiguration", StringComparison.OrdinalIgnoreCase))
                    {
                        tasks.Add(GetCommandConfiguration(pcfClient, runParameters, cancellationToken));
                    }
                    if (!runParameters.GetCommandOptions.Contains("NoAssetGroupDetails", StringComparison.OrdinalIgnoreCase))
                    {
                        tasks.Add(GetAssetGroupDetailsAsync(pcfClient, runParameters, cancellationToken));
                    }
                    tasks.Add(GetCommandsAsync(pcfClient, runParameters, operationType, getAllCommands: (action == Action.GetAllCommands), cancellationToken));
                    break;

                case Action.CompleteCommands:
                    tasks.Add(CompleteCommandsAsync(pcfClient, runParameters, operationType, cancellationToken));
                    break;

                default:
                    ConsoleLogger.Log($"Unsupported action type {action}.");
                    return;
            }

            Task.WaitAll(tasks.ToArray(), cancellationToken);
        }

        private static async Task GetAssetGroupDetailsAsync(ICommandFeedClient pcfClient, RunParameters runParameters, CancellationToken cancellationToken)
        {
            ConsoleLogger.Log($"Calling PcfClient.GetAssetGroupDetails with agentId={runParameters.AgentId}, assetGroupId={runParameters.AssetGroupId}, api-version={runParameters.ApiVersion}.");
            int index = 1;
            var response = await pcfClient.GetAssetGroupDetailsAsync(runParameters.AssetGroupId, runParameters.ApiVersion, cancellationToken);
            Dictionary<string, List<string>> resourceUriMap = null;
            
            List<CustomActivityAsset> customAssets = new List<CustomActivityAsset>();
            while (response != null && !string.IsNullOrEmpty(response.AssetPage))
            {
                if (runParameters.ApiVersion == PrivacyCommandProccesorVersions.v2)
                {
                    // only grab the resource URI map once
                    if (resourceUriMap == null)
                    {
                        resourceUriMap = await Program.GetResourceUriMapAsync(pcfClient, runParameters, cancellationToken);
                    }
                    customAssets.AddRange(GetCustomAssetDetailsV2(runParameters.AssetGroupId, response.AssetPage, resourceUriMap));
                }
                else
                {
                    customAssets.AddRange(GetCustomAssetDetailsV1(response.AssetPage));
                }

                if (!string.IsNullOrEmpty(response.NextLink))
                {
                    using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(Serialize(customAssets))))
                    {
                        if (stream.Length >= blobUploadSizeLimit)
                        {
                            var blobFilename = $"{runParameters.AssetGroupId}/AssetGroupDetails_{index}.txt";
                            await UploadStreamToBlockBlob(stream, new Uri(runParameters.OutputBlobUrl), runParameters.OutputBlobContainerName, blobFilename);
                            ++index;
                            customAssets.Clear();
                        }
                    }
                    ConsoleLogger.Log($"Calling PcfClient.GetNextAssetGroupDetails with nextLink={response.NextLink}");
                    response = await pcfClient.GetNextAssetGroupDetailsAsync(response.NextLink, cancellationToken);
                }
                else
                {
                    using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(Serialize(customAssets))))
                    {
                        var blobFilename = $"{runParameters.AssetGroupId}/AssetGroupDetails_{index}.txt";
                        ConsoleLogger.Log($"Writing output to blob {runParameters.OutputBlobUrl}, container {runParameters.OutputBlobContainerName} as {blobFilename}.");
                        await UploadStreamToBlockBlob(stream, new Uri(runParameters.OutputBlobUrl), runParameters.OutputBlobContainerName, blobFilename);
                        ConsoleLogger.Log($"Output file {blobFilename} has been written to blob.");
                    }

                    break;
                }
            }

            ConsoleLogger.Log("Exiting method to get assetgroup details");
        }

        private static async Task<Dictionary<string,List<string>>> GetResourceUriMapAsync(ICommandFeedClient pcfClient, RunParameters runParameters, CancellationToken cancellationToken)
        {
            ConsoleLogger.Log($"Calling PcfClient.GetResourceUriMap with agentId={runParameters.AgentId}, assetGroupId={runParameters.AssetGroupId}");
            var response = await pcfClient.GetResourceUriMapAsync(runParameters.AssetGroupId, cancellationToken);

            if (response == null)
            {
                // stop processing and return an empty dictionary since no
                // partitions exist for the resource URI
                return new Dictionary<string, List<string>>();
            }

            List<string> csvResourceUriMapPages = new List<string>();

            while (response != null && !string.IsNullOrEmpty(response.ResourceURIMapPage))
            {
                csvResourceUriMapPages.Add(response.ResourceURIMapPage.ToString());

                if (!string.IsNullOrEmpty(response.NextLink))
                {
                    ConsoleLogger.Log($"Calling PcfClient.GetNextResourceUriMap with nextLink={response.NextLink}");
                    response = await pcfClient.GetNextResourceUriMapAsync(response.NextLink, cancellationToken);
                }
                else
                {
                    break;
                }
            }

            // stack the individual csv strings to form one large csv string
            string csvResourceUriMap = string.Join("\n",csvResourceUriMapPages);

            ConsoleLogger.Log("Exiting method to get resource uri map");

            return GetResourceUriMapDictionary(csvResourceUriMap);
        }

        private static async Task GetCommandsAsync(ICommandFeedClient pcfClient, RunParameters runParameters, OperationType operationType, bool getAllCommands, CancellationToken cancellationToken)
        {
            ConsoleLogger.Log($"Calling PcfClient.GetBatchCommandAsync with agentId={runParameters.AgentId}, assetGroupId={runParameters.AssetGroupId}, " + 
                $"startTime={runParameters.StartTime}, endTime={runParameters.EndTime}." +
                $"getAllCommands={getAllCommands}, maxResult={runParameters.MaxResult}." +
                $"returnOnlyTest={runParameters.ReturnOnlyTest}");

            GetBatchCommandResponse response;
            if (getAllCommands)
            {
                response = operationType == OperationType.Delete ?
                    await pcfClient.GetBatchDeleteCommandAsync(runParameters.StartTime, runParameters.EndTime, runParameters.MaxResult, cancellationToken) :
                    await pcfClient.GetBatchExportCommandAsync(runParameters.StartTime, runParameters.EndTime, runParameters.MaxResult, cancellationToken);
            }
            else
            {
                response = operationType == OperationType.Delete ?
                    await pcfClient.GetBatchDeleteCommandAsync(runParameters.AssetGroupId, runParameters.StartTime, runParameters.EndTime, runParameters.ReturnOnlyTest, cancellationToken) :
                    await pcfClient.GetBatchExportCommandAsync(runParameters.AssetGroupId, runParameters.StartTime, runParameters.EndTime, runParameters.ReturnOnlyTest, cancellationToken);
            }

            int index = 1;
            List<CustomActivityPrivacyCommand> customCommands = new List<CustomActivityPrivacyCommand>();
            while (response != null && !string.IsNullOrEmpty(response.CommandPage))
            {
                var commandPage = JsonConvert.DeserializeObject<CommandPage>(response.CommandPage);
                ConsoleLogger.Log($"{commandPage?.Commands?.Count} commands received.");

                customCommands.AddRange(GetCustomPrivacyCommands(commandPage, operationType.ToString()));
                if (!string.IsNullOrEmpty(response.NextLink))
                {
                    using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(Serialize(customCommands))))
                    {
                        if (stream.Length >= blobUploadSizeLimit)
                        {
                            var blobFilename = $"{runParameters.AssetGroupId}/{runParameters.StartTime:s}_{runParameters.EndTime:s}/{operationType}/{operationType}Commands_{index}.txt";
                            ConsoleLogger.Log($"Writing output to blob {runParameters.OutputBlobUrl}, container {runParameters.OutputBlobContainerName} as {blobFilename}, with {customCommands.Count} commands.");
                            await UploadStreamToBlockBlob(stream, new Uri(runParameters.OutputBlobUrl), runParameters.OutputBlobContainerName, blobFilename);
                            ConsoleLogger.Log($"Output file {blobFilename} has been written to blob.");
                            ++index;
                            customCommands.Clear();
                        }
                    }
                    ConsoleLogger.Log($"Calling PcfClient.GetNextBatchCommandAsync with nextLink={response.NextLink}");

                    response = operationType == OperationType.Delete ?
                        await pcfClient.GetNextBatchDeleteCommandAsync(response.NextLink, cancellationToken) :
                        await pcfClient.GetNextBatchExportCommandAsync(response.NextLink, cancellationToken);
                }
                else
                {
                    using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(Serialize(customCommands))))
                    {
                        var blobFilename = $"{runParameters.AssetGroupId}/{runParameters.StartTime:s}_{runParameters.EndTime:s}/{operationType}/{operationType}Commands_{index}.txt";
                        ConsoleLogger.Log($"Writing output to blob {runParameters.OutputBlobUrl}, container {runParameters.OutputBlobContainerName} as {blobFilename}, with {customCommands.Count} commands.");
                        await UploadStreamToBlockBlob(stream, new Uri(runParameters.OutputBlobUrl), runParameters.OutputBlobContainerName, blobFilename);
                        ConsoleLogger.Log($"Output file {blobFilename} has been written to blob.");
                    }

                    if (!string.IsNullOrEmpty(response.CompletionToken))
                    {
                        var completionTokenLocation = $"{runParameters.AssetGroupId}/{runParameters.StartTime:s}_{runParameters.EndTime:s}/{operationType}/{operationType}CompletionToken.txt";
                        ConsoleLogger.Log($"Writing completionToken.txt to {completionTokenLocation}");
                        await UploadToBlockBlob(response.CompletionToken, new Uri(runParameters.OutputBlobUrl), runParameters.OutputBlobContainerName, completionTokenLocation);
                    }
                    else
                    {
                        ConsoleLogger.Log($"CompletionToken is missing.", ConsoleLogger.LogLevel.Error);
                        throw new HttpRequestException("Unexpected return value - missing CompletionToken.");
                    }    

                    break;
                }
            }

            ConsoleLogger.Log("Exiting method to get commands");
        }

        private static async Task GetCommandConfiguration(ICommandFeedClient pcfClient, RunParameters runParameters, CancellationToken cancellationToken)
        {
            ConsoleLogger.Log($"Calling PcfClient.CommandConfiguration");

            var response = await pcfClient.GetCommandConfigurationAsync(cancellationToken);

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(response)))
            {
                var blobFilename = $"{runParameters.AssetGroupId}/CommandConfiguration.json";
                ConsoleLogger.Log($"Writing output to blob {runParameters.OutputBlobUrl}, container {runParameters.OutputBlobContainerName} as {blobFilename}.");
                await UploadStreamToBlockBlob(stream, new Uri(runParameters.OutputBlobUrl), runParameters.OutputBlobContainerName, blobFilename);
                ConsoleLogger.Log($"Output file {blobFilename} has been written to blob.");
            }

            ConsoleLogger.Log("Exiting method to get command configuration");
        }

        private static async Task CompleteCommandsAsync(ICommandFeedClient pcfClient, RunParameters runParameters, OperationType operationType, CancellationToken cancellationToken)
        {
            ConsoleLogger.Log($"Downloading completionToken");
            string completionToken = await DownloadFromBlockBlob(new Uri(runParameters.OutputBlobUrl), runParameters.OutputBlobContainerName, $"{runParameters.AssetGroupId}/{runParameters.StartTime.ToString("s")}_{runParameters.EndTime.ToString("s")}/{operationType}/{operationType}CompletionToken.txt");
            ConsoleLogger.Log($"pcfClient.CompleteBatchCommandAsync operationType: {operationType} assetgroupid {runParameters.AssetGroupId} startTime {runParameters.StartTime} endTime{runParameters.EndTime}");

            var assetInfo = new
            {
                SucceededAssetUris = new string[] { },
                FailedAssetUris = new string[] { },
            };

            string assetLevelCompletionInfoString = null;
            if (!string.IsNullOrEmpty(runParameters.AssetLevelCompletionBlob))
            {
                ConsoleLogger.Log($"Downloading AssetLevelCompletionBlob {runParameters.AssetLevelCompletionBlob}");
                assetLevelCompletionInfoString = await DownloadFromBlockBlob(new Uri(runParameters.OutputBlobUrl), runParameters.OutputBlobContainerName, runParameters.AssetLevelCompletionBlob);
            }
            
            if (operationType == OperationType.Delete)
            {
                if (!string.IsNullOrEmpty(assetLevelCompletionInfoString))
                {
                    var assetLevelCompletionInfo = JsonConvert.DeserializeAnonymousType(assetLevelCompletionInfoString, assetInfo);

                    await pcfClient.CompleteBatchDeleteCommandWithAssetUrisAsync(runParameters.AssetGroupId, runParameters.StartTime, runParameters.EndTime, completionToken,
                        assetLevelCompletionInfo.SucceededAssetUris, assetLevelCompletionInfo.FailedAssetUris, cancellationToken);
                }
                else
                {
                    await pcfClient.CompleteBatchDeleteCommandAsync(runParameters.AssetGroupId, runParameters.StartTime, runParameters.EndTime, completionToken, cancellationToken);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(assetLevelCompletionInfoString))
                {
                    var assetLevelCompletionInfo = JsonConvert.DeserializeAnonymousType(assetLevelCompletionInfoString, assetInfo);

                    await pcfClient.CompleteBatchExportCommandWithAssetUrisAsync(
                        runParameters.AssetGroupId,
                        runParameters.StartTime, runParameters.EndTime,
                        completionToken: completionToken,
                        stagingContainer: await GetStagingContainerUrlWithSasAsync(new Uri(runParameters.StagingContainerUri)).ConfigureAwait(false),
                        stagingRootFolder: runParameters.StagingRootFolder,
                        assetLevelCompletionInfo.SucceededAssetUris, assetLevelCompletionInfo.FailedAssetUris,
                        cancellationToken);
                }
                else
                {
                    await pcfClient.CompleteBatchExportCommandAsync(
                        runParameters.AssetGroupId,
                        runParameters.StartTime, runParameters.EndTime,
                        completionToken: completionToken,
                        stagingContainer: await GetStagingContainerUrlWithSasAsync(new Uri(runParameters.StagingContainerUri)).ConfigureAwait(false),
                        stagingRootFolder: runParameters.StagingRootFolder,
                        cancellationToken);
                }
            }
        }

        private static IEnumerable<CustomActivityPrivacyCommand> GetCustomPrivacyCommands(CommandPage commandPage, string operationType)
        {
            IList<CustomActivityPrivacyCommand> privacyCustomCommands = new List<CustomActivityPrivacyCommand>();
            foreach (var command in commandPage.Commands)
            {
                var privacyCustomCommand = new CustomActivityPrivacyCommand(command);
                privacyCustomCommands.Add(privacyCustomCommand);
            }

            return privacyCustomCommands;
        }

        private static IEnumerable<CustomActivityAsset> GetCustomAssetDetailsV1(string assetPage)
        {
            IList<CustomActivityAsset> privacyCustomAssets = new List<CustomActivityAsset>();

            JObject assetPageObj = JObject.Parse(assetPage);
            var assetGroupId = assetPageObj["AssetGroupId"].ToString();
            var assetDetails = assetPageObj["ResourceSets"];

            foreach (var assetDetail in assetDetails)
            {
                foreach (var partitionUri in assetDetail["PartitionUris"].ToObject<List<string>>())
                {
                    CustomActivityAsset asset = new CustomActivityAsset();
                    asset.AssetGroupId = assetGroupId;
                    asset.ResourceSetUri = assetDetail["ResourceURI"].ToString();
                    asset.ApplicableCommandTypes = JsonConvert.SerializeObject(assetDetail["ApplicableCommandTypes"], Formatting.None);
                    asset.Tags = assetDetail["Tags"].ToString(Formatting.None);
                    asset.PartitionUri = partitionUri;
                    privacyCustomAssets.Add(asset);
                }
            }

            return privacyCustomAssets;
        }

        private static IEnumerable<CustomActivityAsset> GetCustomAssetDetailsV2(Guid assetGroupIdGuid, string assetPage, Dictionary<string,List<string>> resourceUriMap)
        {
            IList<CustomActivityAsset> privacyCustomAssets = new List<CustomActivityAsset>();

            var assetDetails = JArray.Parse(assetPage);
            var assetGroupId = assetGroupIdGuid.ToString();

            foreach (var assetDetail in assetDetails)
            {
                var applicableCommandTypes = GetFormattedApplicableCommandTypes(assetDetail);
                var resourceUri = assetDetail["ResourceUri"].ToString();
                foreach (var applicableCommandtype in applicableCommandTypes)
                {
                    if (resourceUriMap.Count > 0)
                    {
                        foreach (var partitionUri in resourceUriMap[resourceUri])
                        {
                            CustomActivityAsset asset = new CustomActivityAsset();
                            asset.AssetGroupId = assetGroupId;
                            asset.ResourceSetUri = resourceUri;
                            asset.ApplicableCommandTypes = applicableCommandtype;
                            asset.Tags = assetDetail["Tags"].ToString(Formatting.None);
                            asset.PartitionUri = partitionUri;
                            privacyCustomAssets.Add(asset);
                        }
                    }
                    else
                    {
                        CustomActivityAsset asset = new CustomActivityAsset();
                        asset.AssetGroupId = assetGroupId;
                        asset.ResourceSetUri = resourceUri;
                        asset.ApplicableCommandTypes = applicableCommandtype;
                        asset.Tags = assetDetail["Tags"].ToString(Formatting.None);
                        // no partition URI to specify since no map was found
                        asset.PartitionUri = "";
                        privacyCustomAssets.Add(asset);
                    }
                }
            }

            return privacyCustomAssets;
        }

        /// <summary>
        ///  This is a convenience method that reformats the applicable commandtype section in the coming from the
        ///  v2 API to one that more closely matches the one coming from v1 API.
        /// </summary>
        private static List<string> GetFormattedApplicableCommandTypes(JToken assetDetail)
        {
            var applicableCommandTypeVersions = assetDetail["ApplicableCommandTypeVersions"].ToList();
            var result = new List<string>();
        
            var operationDictionary = new Dictionary<string, List<JObject>>();
            foreach( var versionSet in applicableCommandTypeVersions)
            {
                foreach(var commandSet in versionSet["ApplicableCommandTypes"])
                {
                    var operationType = commandSet["OperationType"].ToString();
                    var entry = new JObject()
                    {
                        ["CommandTypeGroupName"] = versionSet["CommandTypeGroupName"],
                        ["CommandTypeId"] = int.Parse(commandSet["CommandTypeId"].ToString()),
                    };

                    if (operationDictionary.ContainsKey(operationType))
                    {
                        operationDictionary[operationType].Add(entry);
                    }
                    else
                    {
                        operationDictionary[operationType] = new List<JObject> { entry };
                    }

                }
            }

            foreach (var item in operationDictionary)
            {
                result.Add(JsonConvert.SerializeObject(item.Value,Formatting.None));
            }

            return result;
        }

        /// <summary>
        ///  The resourceUriMap returns a many-to-many map of Resource Uri to Partition Uri. This method creates a dictionary
        ///  representing a one-to-many map from Resource Uri to Partition Uri
        /// </summary>
        /// <param name="resourceUriMap"> A csv string of the Resource Uri Map.</param>
        /// <returns> A dictionary mapping resource uri to a list of partition uris.</returns>
        private static Dictionary<string, List<string>> GetResourceUriMapDictionary(string resourceUriMap)
        {
            IEnumerable<dynamic> records;
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
            };

            using (var reader = new StringReader(resourceUriMap))
            {
                using (var csvr = new CsvReader(reader, config))
                {
                    records = csvr.GetRecords<dynamic>().ToList();
                }
            }

            var result = new Dictionary<string, List<string>>();
            foreach (var record in records)
            {
                if (result.ContainsKey(record.Field1))
                {
                    result[record.Field1].Add(record.Field2);
                }
                else
                {
                    result[record.Field1] = new List<string> { record.Field2 };
                }
            }

            return result;
        }

        private static async Task UploadToBlockBlob(string content, Uri targetBlob, string blobContainerName, string blobFileName)
        {
            var blobServiceClient = new BlobServiceClient(targetBlob, new DefaultAzureCredential());
            var containerClient = blobServiceClient.GetBlobContainerClient(blobContainerName);
            var blobClient = containerClient.GetBlobClient(blobFileName);

            await blobClient.UploadAsync(new BinaryData(content), overwrite: true);
        }

        private static async Task<string> DownloadFromBlockBlob(Uri targetBlob, string blobContainerName, string blobFileName)
        {
            var blobServiceClient = new BlobServiceClient(targetBlob, new DefaultAzureCredential());
            var containerClient = blobServiceClient.GetBlobContainerClient(blobContainerName);
            var blobClient = containerClient.GetBlobClient(blobFileName);

            BlobDownloadResult result = await blobClient.DownloadContentAsync();
            return result.Content.ToString();
        }

        private static async Task UploadStreamToBlockBlob(Stream content, Uri targetBlob, string blobContainerName, string blobFileName)
        {
            var blobServiceClient = new BlobServiceClient(targetBlob, new DefaultAzureCredential());
            var containerClient = blobServiceClient.GetBlobContainerClient(blobContainerName);
            var blobClient = containerClient.GetBlobClient(blobFileName);

            await blobClient.UploadAsync(content, overwrite: true);
        }

        private static string Serialize<T>(IEnumerable<T> commands)
        {
            string result = string.Empty;
            CsvConfiguration csvConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture);
            csvConfiguration.HasHeaderRecord = true;
            csvConfiguration.Delimiter = "\t";
            csvConfiguration.ShouldQuote = (args) =>
            {
                return false;
            };

            using (var destinationStream = new MemoryStream())
            using (var destination = new StreamWriter(destinationStream, Encoding.UTF8, 4096, true))
            {
                using (var writer = new CsvWriter(destination, csvConfiguration))
                {
                    writer.WriteRecords(commands);
                }
                using (var reader = new StreamReader(destinationStream))
                {
                    destinationStream.Position = 0;
                    // Read results
                    result = reader.ReadToEnd().TrimEnd(Environment.NewLine.ToCharArray());
                }
            }
            return result;
        }

        private static async Task<Uri> GetStagingContainerUrlWithSasAsync(Uri blobContainerUri)
        {
            var blobContainerClient = new BlobContainerClient(blobContainerUri, new DefaultAzureCredential());
            var blobServiceClient = blobContainerClient.GetParentBlobServiceClient();

            // Start time for the key's validity, with null indicating an immediate start.
            // Setting it to null to avoid clock skew between the requesting node and azure servers.
            UserDelegationKey userDelegationKey = await blobServiceClient.GetUserDelegationKeyAsync(null,
                DateTimeOffset.UtcNow.AddDays(7)).ConfigureAwait(false);

            var blobSasBuilder = new BlobSasBuilder
            {
                StartsOn = userDelegationKey.SignedStartsOn,
                ExpiresOn = userDelegationKey.SignedExpiresOn,
                BlobContainerName = blobContainerClient.Name,
                Resource = "c", // "c" means the shared resource is a blob container
            };

            blobSasBuilder.SetPermissions(BlobSasPermissions.List | BlobSasPermissions.Read);

            var blobUriBuilder = new BlobUriBuilder(blobContainerClient.Uri)
            {
                Sas = blobSasBuilder.ToSasQueryParameters(userDelegationKey, blobServiceClient.AccountName)
            };

            return blobUriBuilder.ToUri();
        }

        /// <summary>
        /// Custom activity parameters passed by activity.json
        /// </summary>
        internal class RunParameters
        {
            /// <summary>
            /// The endpoint of pcf configuration, can be either ppe or production
            /// </summary>
            public string Endpoint { get; set; }

            /// <summary>
            /// The AAD authority of pcf configuration, can be either Microsoft or Tenant
            /// </summary>
            public string Tenant { get; set; }
            
            /// <summary>
            /// The Azure KeyVault url where the AadApp's certificate is stored
            /// </summary>
            public string ClientCertAKV { get; set; }

            /// <summary>
            /// The name the AadApp's certificate
            /// </summary>
            public string ClientCertName { get; set; }
            
            /// <summary>
            /// The AppId of the AadApp that is used for agent authentication
            /// </summary>
            public Guid ClientAadAppId { get; set; }
            
            /// <summary>
            /// The Azure Storage Blob url where the output is written to
            /// </summary>
            public string OutputBlobUrl { get; set; }

            /// <summary>
            /// The container name with the Azure Storage Blob where the output is written to
            /// </summary>
            public string OutputBlobContainerName { get; set; }
            
            /// <summary>
            /// The agent id
            /// </summary>
            public Guid AgentId { get; set; }
            
            /// <summary>
            /// The asset group id
            /// </summary>
            public Guid AssetGroupId { get; set; }
            
            /// <summary>
            /// The start time for GetCommands/CompleteCommands call
            /// </summary>
            public DateTime StartTime { get; set; }

            /// <summary>
            /// The end time for GetCommands/CompleteCommands call
            /// </summary>
            public DateTime EndTime { get; set; }

            /// <summary>
            /// The type of activity this run will perform. Valid values are: GetCommands, GetAllCommands, CompleteCommands
            /// </summary>
            public string CustomActivityType { get; set; }
            
            /// <summary>
            /// The OperationType of the target commands, can be either Delete or Export.
            /// </summary>
            public string Operation { get; set; }

            /// <summary>
            /// The Azure Blob container url where the export data is stored (only used when CustomActivityType == "CompleteCommands" && Operation == "Export")
            /// </summary>
            public string StagingContainerUri { get; set; }

            /// <summary>
            /// The root folder of Azure Blob container url where the export data is stored (only used when CustomActivityType == "CompleteCommands" && Operation == "Export")
            /// </summary>
            public string StagingRootFolder { get; set; }

            /// <summary>
            /// The maximum number of pages GetAllCommands should return (only used when CustomActivityType == "GetAllCommands")
            /// </summary>
            public int MaxResult { get; set; }

            /// <summary>
            /// Specify the options for Get(All)Commands activity type. 
            /// By default, Get(All)Commands will get CommandConfiguration, AssetGroupDetails and Commands all together.
            /// With this option set to NoCommandConfiguration and/or NoAssetGroupDetails, it can skip getting the date for the specified data type.
            /// </summary>
            public string GetCommandOptions{ get; set; }

            /// <summary>
            /// Specify the api version used by Custom Activity to run.
            /// </summary>
            public Version ApiVersion { get; set; }

            /// <summary>
            /// Specify whether only data associated with test commands are returned.
            /// </summary>
            public bool ReturnOnlyTest { get; set; }


            /// <summary>
            /// The blob contains asset level completion information. Expect to be found at OutputBlobContainerName in OutputBlobUrl
            /// The blob is json file formatted like is:
            /// {
            ///   "SucceededAssetUris": [
            ///     "asset1",
            ///     "asset2",
            ///   ],
            ///   "FailedAssetUris": [
            ///     "asset3",
            ///     "asset4",
            ///   ]
            /// }
            /// </summary>
            public string AssetLevelCompletionBlob { get; set; }

            public RunParameters(dynamic extendedProperties)
            {
                CustomActivityType = extendedProperties.customActivityType.Value;
                Operation = extendedProperties.operation.Value;
                Endpoint = extendedProperties.endpoint.Value;
                Tenant = extendedProperties.tenant ?? "Microsoft";
                ClientCertAKV = extendedProperties.clientCertAKV.Value;
                ClientCertName = extendedProperties.clientCertName.Value;
                ClientAadAppId = Guid.Parse(extendedProperties.clientAadAppId.Value);
                OutputBlobUrl = extendedProperties.outputBlobUrl.Value;
                OutputBlobContainerName = extendedProperties.outputBlobContainerName.Value;
                AgentId = Guid.Parse(extendedProperties.agentId.Value);
                AssetGroupId = extendedProperties.assetGroupId != null ? Guid.Parse(extendedProperties.assetGroupId.Value) : Guid.NewGuid();
                StartTime = extendedProperties.startTime != null ? DateTime.Parse(extendedProperties.startTime.Value) : DateTime.Now;
                EndTime = extendedProperties.endTime != null ? DateTime.Parse(extendedProperties.endTime.Value) : DateTime.Now;
                StagingContainerUri = extendedProperties.exportStagingContainerUri ?? string.Empty;
                StagingRootFolder = extendedProperties.exportStagingRootFolder ?? string.Empty;
                MaxResult = extendedProperties.maxResult ?? 100;
                GetCommandOptions = extendedProperties.getCommandOptions ?? string.Empty;
                AssetLevelCompletionBlob = extendedProperties.assetLevelCompletionBlob ?? string.Empty;
                ApiVersion = extendedProperties.apiVersion != null ? new Version((string)extendedProperties.apiVersion) : PrivacyCommandProccesorVersions.defaultVersion;
                ReturnOnlyTest = extendedProperties.returnOnlyTest ?? false;
            }
        }

        internal enum Action
        {
            GetCommandConfiguration,
            GetAssetGroupDetails,
            GetCommands,
            GetAllCommands,
            CompleteCommands,
        };

        internal enum OperationType
        {
            Delete,
            Export,
        };
    }
}
