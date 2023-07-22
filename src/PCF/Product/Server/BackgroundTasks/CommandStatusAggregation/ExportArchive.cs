namespace Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks.CommandStatusAggregation
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.Azure.Storage.Blob;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.Common.Azure;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Processes staged export files into a user consumable archive.
    /// </summary>
    public class ExportArchive
    {
        // The name of the agent map file dropped in the root of the zip file.
        private const string AgentMapFileName = "agentMap.json";
        private const string PcfKustoTableForMalwareFound = "PCFAgentsWithMalwareInMSAExports";

        // Reference to archive stream. Does not own the stream.
        private Stream destinationStream;
        private IExportMalwareScannerFactory scannerFactory;
        private IKustoClient kustoClient;
        private List<Task> ingestToKustoTasks = new List<Task>();

        private ExportPathTransformer transformer = new ExportPathTransformer(
                id => ExportProductId.ProductIds.TryGetValue(id, out ExportProductId exportProductId) ? exportProductId.Path : null);

        /// <summary>
        /// Constructs a new instance of <see cref="ExportArchive"/>.
        /// </summary>
        /// <param name="stream">Stream to write to the archive.</param>
        /// <param name="scannerFactory">Factory to create a malware scanner.</param>
        /// <param name="kustoClient">Kusto client.</param>
        public ExportArchive(
            Stream stream,
            IExportMalwareScannerFactory scannerFactory,
            IKustoClient kustoClient)
        {
            this.destinationStream = stream;
            this.scannerFactory = scannerFactory;
            this.kustoClient = kustoClient;
        }

        /// <summary>
        /// Gets the count of stage sources (containers).
        /// </summary>
        public int SourceCount
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the total count of staged source files.
        /// </summary>
        public int SourceFileCount
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the total count of bytes from all staged source files.
        /// </summary>
        public long TotalSourceBytes
        {
            get;
            private set;
        }

        /// <summary>
        /// Builds an export archive.
        /// </summary>
        /// <param name="storageManager">Storage manager.</param>
        /// <param name="record">Export command history record.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Void</returns>
        public async Task BuildAsync(
            IExportStorageManager storageManager,
            CommandHistoryRecord record,
            CancellationToken cancellationToken)
        {
            this.SourceCount = 0;
            this.SourceFileCount = 0;
            this.TotalSourceBytes = 0;
            this.ingestToKustoTasks.Clear();

            // Build temporary stage archive
            Directory.CreateDirectory(Config.Instance.Worker.DefenderFileDownloadTempDirectory);
            string tempZipFile = Path.Combine(Config.Instance.Worker.DefenderFileDownloadTempDirectory, Guid.NewGuid().ToString("n") + ".avscan");

            try
            {
                DualLogger.Instance.Information(nameof(ExportArchive), $"Processing export for subject type: {record.Core.Subject.GetSubjectType()}, Command Id: {record.CommandId}");

                using (var zipStream = File.Create(tempZipFile, 4096, FileOptions.Asynchronous))
                using (ZipArchive zip = new ZipArchive(zipStream, ZipArchiveMode.Update, true, Encoding.UTF8))
                {
                    // Go through each source stage directory
                    foreach (var source in record.ExportDestinations)
                    {
                        await ProcessStageDirectoryAsync(storageManager, record, source, zip, cancellationToken);
                    }

                    // When all files have been written, write out the mapping file in the root
                    var mapData = new { Paths = this.transformer.EnumeratePaths() };
                    string mapDataString = JsonConvert.SerializeObject(mapData, Formatting.Indented);
                    ZipArchiveEntry mapEntry = zip.CreateEntry(AgentMapFileName, CompressionLevel.Optimal);

                    using (Stream mapFileStream = new BufferedStream(mapEntry.Open(), 1024 * 1024))
                    using (StreamWriter writer = new StreamWriter(mapFileStream))
                    {
                        await writer.WriteAsync(mapDataString);
                    }
                }

                // Build final archive
                using (var finalStream = File.Open(tempZipFile, FileMode.Open))
                {
                    await finalStream.CopyToAsync(this.destinationStream);
                }
            }
            finally
            {
                if (File.Exists(tempZipFile))
                {
                    File.Delete(tempZipFile);
                }
            }
        }

        /// <summary>
        /// Processes a single stage directory.
        /// </summary>
        /// <param name="storageManager">Storage manager.</param>
        /// <param name="record">Export command history record.</param>
        /// <param name="source">Map of command history destination record.</param>
        /// <param name="zip">Destination archive.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Void</returns>
        internal async Task ProcessStageDirectoryAsync(
            IExportStorageManager storageManager,
            CommandHistoryRecord record,
            KeyValuePair<(AgentId agentId, AssetGroupId assetGroupId), CommandHistoryExportDestinationRecord> source,
            ZipArchive zip,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.SourceCount++;

            // Get the staging directory
            CloudBlobDirectory sourceDirectory = storageManager.GetFullAccessContainer(source.Value.ExportDestinationUri)
                .GetDirectoryReference(source.Value.ExportDestinationPath ?? string.Empty);

            BlobContinuationToken continuationToken = null;
            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                BlobResultSegment segment = await sourceDirectory.ListBlobsSegmentedAsync(true, BlobListingDetails.Metadata, null, continuationToken, null, null, cancellationToken);
                foreach (CloudBlob sourceBlob in segment.Results.Cast<CloudBlob>())
                {
                    // Go through each file in the staging directory
                    using (var sourceStream = new BufferedStream(await sourceBlob.OpenReadAsync(cancellationToken), 10 * 1024 * 1024))
                    {
                        string blobPath = sourceBlob.Uri.LocalPath;
                        string blobFilePath = sourceBlob.Uri.LocalPath.Substring(sourceDirectory.Uri.LocalPath.Length).Trim('/');

                        await ProcessStageFileAsync(
                            zip,
                            sourceStream,
                            sourceBlob.Properties.Length,
                            record,
                            source.Key.agentId,
                            source.Key.assetGroupId,
                            blobPath,
                            blobFilePath,
                            cancellationToken);

                        this.TotalSourceBytes += sourceBlob.Properties.Length;
                        this.SourceFileCount++;
                    }
                }

                await Task.WhenAll(this.ingestToKustoTasks);
                continuationToken = segment.ContinuationToken;
            }
            while (continuationToken != null);
        }

        /// <summary>
        /// Processes a single staged source file and adds it to the archive.
        /// </summary>
        /// <param name="zip">Destination archive.</param>
        /// <param name="sourceStream">Source stream to process.</param>
        /// <param name="sourceStreamLength">Source stream length.</param>
        /// <param name="record">Export command history record.</param>
        /// <param name="agentId">Agent id.</param>
        /// <param name="assetGroupId">Asset group id.</param>
        /// <param name="blobPath">Full path to the staged source file.</param>
        /// <param name="blobFilePath">Relative file path to the staged source file.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Void</returns>
        internal async Task ProcessStageFileAsync(
            ZipArchive zip,
            Stream sourceStream,
            long sourceStreamLength,
            CommandHistoryRecord record,
            AgentId agentId,
            AssetGroupId assetGroupId,
            string blobPath,
            string blobFilePath,
            CancellationToken cancellationToken)
        {
            long initialPosition = destinationStream.Position;

            ZipArchive sourceZipArchive = null;
            bool sourceCompressed = false;

            try
            {
                Stream activeStream = sourceStream;

                if (blobFilePath.EndsWith(".zip"))
                {
                    // We're going to try to read this as a Zip file. We might fail, which means we need to rewind our cursor back to the beginning.
                    bool resetOrigin = true;

                    // Might be a zip? We won't know until we open it.
                    try
                    {
                        sourceZipArchive = new ZipArchive(sourceStream, ZipArchiveMode.Read, true);
                        sourceCompressed = true;

                        // See if the zip is special. A special zip has the following properties:
                        //  - Has one file.
                        //  - The single file matches the name of the container
                        // For example, Foo.json.zip contains only Foo.json.
                        if (sourceZipArchive.Entries.Count == 1 &&
                            sourceZipArchive.Entries.Single().FullName + ".zip" == Path.GetFileName(blobFilePath))
                        {
                            var entry = sourceZipArchive.Entries.First();

                            var count = await this.GetPropertiesCountInZippedStreamAsync(entry);
                            DualLogger.Instance.Information(nameof(ExportArchive),
                                $"Information of the original zip file uploaded to staging blob AgentId : {agentId}" +
                                $" AssetGroupId : {assetGroupId} CommandId : {record.CommandId} Properties field count : {count}" +
                                $" Zip file path : {entry.FullName} Zip file length : {entry.CompressedLength}");

                            activeStream = entry.Open();

                            // Trim off the ".zip" from the end.
                            blobFilePath = blobFilePath.Substring(0, blobFilePath.Length - 4);
                            resetOrigin = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        // If we had an error opening the zip, then copy it line for line.
                        Logger.Instance.UnexpectedException(ex);
                    }

                    if (resetOrigin)
                    {
                        sourceStream.Seek(0, SeekOrigin.Begin);
                    }
                }
                else
                {
                    // If stream is not zipped, also log orignal stream information.
                    using (var tempStream = new MemoryStream())
                    {
                        activeStream.Position = 0;
                        await activeStream.CopyToAsync(tempStream);
                        tempStream.Position = 0;
                        var count = await this.GetPropertiesCountInStreamAsync(tempStream);
                        DualLogger.Instance.Information(nameof(ExportArchive),
                            $"Information of the original unzipped file uploaded to staging blob AgentId : {agentId}" +
                            $" AssetGroupId : {assetGroupId} CommandId : {record.CommandId} Properties field count : {count}" +
                            $" Unzipped file length : {activeStream.Length}");

                        // Reset stream position after logging.
                        activeStream.Position = 0;
                    }
                }

                string finalPath = this.transformer.TransformPath(blobFilePath, agentId, assetGroupId);
                var maxCompletedTime = record.StatusMap.Select(a => a.Value.CompletedTime.Value).Max();

                // Scan file for malware
                using (var scanner = this.scannerFactory.CreateScanner())
                {
                    var result = await scanner.ScanAsync(finalPath, activeStream, record.CommandId, maxCompletedTime, cancellationToken);

                    if (result.IsMalware)
                    {
                        using (var malwareStream = new MemoryStream(Defender.GetMalwareReplacementFileText()))
                        {
                            await WriteArchiveEntry(
                                zip,
                                finalPath,
                                malwareStream,
                                isMalware: true,
                                agentId,
                                record,
                                cancellationToken);
                        }

                        this.ingestToKustoTasks.Add(this.IngestToKustoTelemetry(agentId, record.CommandId, blobPath, DateTime.UtcNow));
                    }
                    else
                    {
                        await WriteArchiveEntry(
                            zip,
                            finalPath,
                            scanner.ScanStream,
                            isMalware: false,
                            agentId,
                            record,
                            cancellationToken);
                    }
                }

                long finalPosition = this.destinationStream.Position;

                // Approximate the length of the compressed output by measuring final position - initial position of the destination stream.
                Logger.Instance?.LogExportFileSizeEvent(
                    agentId,
                    assetGroupId,
                    record.CommandId,
                    finalPath,
                    sourceStreamLength,
                    finalPosition - initialPosition,
                    sourceCompressed,
                    SubjectType.Msa,
                    AgentType.NonCosmos,
                    "Public");
            }
            finally
            {
                sourceZipArchive?.Dispose();
            }
        }

        // Write an entry to the archive.
        private async Task WriteArchiveEntry(
            ZipArchive zip,
            string destinationPath,
            Stream stream,
            bool isMalware,
            AgentId agentId,
            CommandHistoryRecord record,
            CancellationToken cancellationToken)
        {
            await Logger.InstrumentAsync(
                new OperationEvent(SourceLocation.Here()),
                async ev =>
                {
                    ev["Step"] = $"WriteArchiveEntry";
                    ev["AgentId"] = $"{agentId}";
                    ev["CommandId"] = $"{record.CommandId}";
                    ev["File"] = $"{destinationPath}";

                    stream.Position = 0;

                    bool isJsonEntry = string.Equals(Path.GetExtension(destinationPath), ".json", StringComparison.OrdinalIgnoreCase);

                    // Only process exports to CSV for MSA subjects since PCF is a data controller for consumers' data.
                    // AAD subject data should not be modified.
                    if (Config.Instance.Worker.ExportToCSVEnabled &&
                        FlightingUtilities.IsSubjectEnabled(FlightingNames.ExportToCsvBySubjectEnabled, record?.Core.Subject) &&
                        record?.Core.Subject is MsaSubject &&
                        isJsonEntry)
                    {
                        var csvPath = Path.ChangeExtension(destinationPath, "csv");
                        DualLogger.Instance.Information(nameof(ExportArchive), $"Attempting to serialize to CSV: {csvPath}, Command Id: {record.CommandId}");

                        ZipArchiveEntry destinationEntry = zip.CreateEntry($"{csvPath}", CompressionLevel.Optimal);
                        using (Stream destination = new BufferedStream(destinationEntry.Open(), 1024 * 1024))
                        using (var writer = new JsonToCsvWriter(stream, destination, leaveOpen: true))
                        {
                            if (isMalware)
                            {
                                // If payload contained malware do not attempt to transform result to CSV.
                                await stream.CopyToAsync(destination);
                            }
                            else
                            {
                                try
                                {
                                    await writer.WriteAsync(cancellationToken);
                                }
                                catch (Exception ex)
                                {
                                    DualLogger.Instance.Error(
                                        nameof(ExportArchive),
                                        ex,
                                        $"Exception occurred writing CSV content for [{csvPath}], " +
                                        $"Agent Id: {agentId}, Command Id: {record.CommandId}");

                                    // Failed to transform to CSV.  Revert to JSON content.
                                    destination.Close();
                                    destinationEntry.Delete();

                                    ZipArchiveEntry jsonEntry = zip.CreateEntry($"{destinationPath}", CompressionLevel.Optimal);
                                    using (Stream json = new BufferedStream(jsonEntry.Open(), 1024 * 1024))
                                    {
                                        stream.Position = 0;
                                        await stream.CopyToAsync(json);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        DualLogger.Instance.Information(nameof(ExportArchive), $"Defaulting to JSON: {destinationPath}");

                        ZipArchiveEntry destinationEntry = zip.CreateEntry($"{destinationPath}", CompressionLevel.Optimal);
                        using (Stream destination = new BufferedStream(destinationEntry.Open(), 1024 * 1024))
                        {
                            await stream.CopyToAsync(destination);
                        }
                    }
                });
        }

        internal Task IngestToKustoTelemetry(AgentId AgentId, CommandId CommandId, string FilePath, DateTime IngestionTime)
        {
            var items = new[] { new { AgentId, CommandId, FilePath, IngestionTime } }.Select(x => new
            {
                AgentId,
                CommandId,
                FilePath,
                IngestionTime
            });

            IDataReader dataReader = this.kustoClient.CreateDataReader(
                items,
                nameof(AgentId),
                nameof(CommandId),
                nameof(FilePath),
                nameof(IngestionTime));

            return Logger.InstrumentAsync(
                new OperationEvent(SourceLocation.Here()),
                async ev =>
                {
                    ev["AgentId"] = AgentId.Value;
                    ev["CommandId"] = CommandId.Value;
                    ev["LocalPath"] = FilePath;
                    ev["KustoTable"] = PcfKustoTableForMalwareFound;
                    ev["IngestionTime"] = IngestionTime.ToString();

                    await this.kustoClient.IngestAsync(PcfKustoTableForMalwareFound, dataReader, true);
                });
        }

        private async Task<int> GetPropertiesCountInZippedStreamAsync(ZipArchiveEntry entry)
        {
            int count = 0;

            using (var stream = entry.Open())
            {
                count = await this.GetPropertiesCountInStreamAsync(stream);
            }

            return count;
        }

        private async Task<int> GetPropertiesCountInStreamAsync(Stream stream)
        {
            int count = 0;

            if (stream == null)
            {
                return count;
            }

            try
            {
                using (var streamReader = new StreamReader(stream))
                {
                    var json = JArray.Parse(await streamReader.ReadToEndAsync());
                    foreach (var item in json)
                    {
                        if (item["properties"] != null && item["properties"].HasValues) count++;
                    }
                }
            }
            catch (Exception ex)
            {
                // Swallow all errors
                DualLogger.Instance.Error(nameof(ExportArchive), $"Error when try to get payload count : {ex.Message}");
            }

            return count;
        }
    }
}
