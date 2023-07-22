namespace PCF.UnitTests.BackgroundTasks
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks.CommandStatusAggregation;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Moq;
    using Newtonsoft.Json;
    using Xunit;

    [Trait("Category", "UnitTest")]
    public class ExportArchiveTests
    {
        private static string SourceFileName = $"{Guid.NewGuid()}.json";
        private static string SourceArchivePath = $"{SourceFileName}.zip";

        private static string DestinationFileName = Path.ChangeExtension(SourceFileName, "csv");
        private static string DestinationArchive = $"{Guid.NewGuid()}.zip";
        private static string DestinationEntryPath = "Miscellaneous/001/";
        private static string DestinationEntry = $"{DestinationEntryPath}{DestinationFileName}";

        private static List<dynamic> ExportContent = new List<dynamic>()
        {
            new {
                time = new DateTimeOffset(2020, 6, 15, 13, 45, 30, TimeSpan.FromMinutes(30)),
                correlationId = new Guid("d85f47f0-9621-4fc2-803d-818b89388e77"),
                properties = new {
                    prop1 = "foo1",
                    prop2 = "bar1"
                }
            },
            new {
                time = new DateTimeOffset(2020, 12, 15, 00, 00, 30, TimeSpan.FromMinutes(10)),
                correlationId = new Guid("5eb0165f-6ede-477e-b92d-7e26f693eaf6"),
                properties = new {
                    prop1 = "foo2",
                    prop2 = "bar2"
                }
            }
        };

        private static List<string> ExpectedContent = new List<string>()
        {
            "time,correlationId,\"properties/prop1\",\"properties/prop2\"",
            "\"06/15/2020 13:45:30 +00:30\",d85f47f0-9621-4fc2-803d-818b89388e77,foo1,bar1",
            "\"12/15/2020 00:00:30 +00:10\",5eb0165f-6ede-477e-b92d-7e26f693eaf6,foo2,bar2"
        };

        /// <summary>
        /// Verify export archive can process staged zip files.
        /// Staged files that are already compressed are specially handled.
        /// </summary>
        [Fact]
        public async Task ProcessCompressedStageFile()
        {
            try
            {
                Action<string, byte[]> verify = (fileEntry, data) =>
                {
                    Assert.Equal(DestinationEntry, fileEntry);

                    using (var reader = new StreamReader(new MemoryStream(data)))
                    {
                        foreach (var expectedResult in ExpectedContent)
                        {
                            var result = reader.ReadLine();
                            Assert.Equal(expectedResult, result);
                        }
                    }
                };

                CreateTestArchive(SourceArchivePath, SourceFileName);

                using (var sourceStream = new FileStream(SourceArchivePath, FileMode.Open))
                {
                    await RunTest(
                        SourceArchivePath,
                        sourceStream,
                        hasMalware: false,
                        new List<string>() { DestinationEntry },
                        verifier: verify);
                }
            }
            finally
            {
                if (File.Exists(SourceArchivePath))
                {
                    File.Delete(SourceArchivePath);
                }
            }
        }

        /// <summary>
        ///  Verify export archive can process a standard staged file.
        /// </summary>
        [Fact]
        public async Task ProcessStageFile()
        {
            Action<string, byte[]> verify = (fileEntry, data) =>
            {
                Assert.Equal(DestinationEntry, fileEntry);

                using (var reader = new StreamReader(new MemoryStream(data)))
                {
                    foreach (var expectedResult in ExpectedContent)
                    {
                        var result = reader.ReadLine();
                        Assert.Equal(expectedResult, result);
                    }
                }
            };

            using (var sourceStream = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(ExportContent))))
            {
                await RunTest(
                    SourceFileName,
                    sourceStream,
                    hasMalware: false,
                    new List<string>() { DestinationEntry },
                    verifier: verify);
            }
        }

        /// <summary>
        ///  Verify export archive processes stage files with malware.
        /// </summary>
        [Fact]
        public async Task ProcessMalwareStageFile()
        {
            Action<string, byte[]> verify = (fileEntry, data) =>
            {
                Assert.Equal(DestinationEntry, fileEntry);

                Assert.Equal(
                    Encoding.UTF8.GetString(Defender.GetMalwareReplacementFileText()),
                    Encoding.UTF8.GetString(data));
            };

            using (var sourceStream = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(ExportContent))))
            {
                await RunTest(
                    SourceFileName,
                    sourceStream,
                    hasMalware: true,
                    new List<string>() { DestinationEntry },
                    verifier: verify);
            }
        }

        /// <summary>
        ///  Verify invalid JSON content is written as is.
        /// </summary>
        [Fact]
        public async Task ProcessCsvWriterError()
        {
            var item = "[{bad json";

            Action<string, byte[]> verify = (fileEntry, data) =>
            {
                Assert.Equal($"{DestinationEntryPath}{SourceFileName}", fileEntry);
                
                // Verify result is the original JSON
                using (var reader = new StreamReader(new MemoryStream(data)))
                {
                    Assert.Equal(item, Encoding.UTF8.GetString(data));
                }
            };

            using (var sourceStream = new MemoryStream(Encoding.UTF8.GetBytes(item)))
            {
                await RunTest(
                    SourceFileName,
                    sourceStream,
                    hasMalware: false,
                    new List<string>() { $"{DestinationEntryPath}{SourceFileName}" },
                    verifier: verify);
            }
        }

        /// <summary>
        ///  Verify export archive can process a non-JSON staged file.
        /// </summary>
        [Fact]
        public async Task ProcessNonJsonFile()
        {
            var testData = "Just some random text";

            Action<string, byte[]> verify = (fileEntry, data) =>
            {
                Assert.Equal($"{DestinationEntryPath}randomFile.txt", fileEntry);
                var result = Encoding.UTF8.GetString(data);
                Assert.Equal(testData, Encoding.UTF8.GetString(data));
            };

            using (var sourceStream = new MemoryStream(Encoding.UTF8.GetBytes(testData)))
            {
                await RunTest(
                    "randomFile.txt",
                    sourceStream,
                    hasMalware: false,
                    new List<string>() { $"{DestinationEntryPath}randomFile.txt" },
                    verifier: verify);
            }
        }

        private async Task RunTest(
            string sourcePath,
            Stream sourceStream,
            bool hasMalware,
            List<string> expectedDestinationEntries,
            Action<string, byte[]> verifier)
        {
            try
            {
                var defender = new Mock<IDefender>();
                defender.Setup(d => d.GetScanResultAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(new DefenderScanResult() { IsMalware = hasMalware }));

                var factory = new Mock<IExportMalwareScannerFactory>();
                factory.Setup(f => f.CreateScanner())
                    .Returns(new ExportMalwareScanner(defender.Object));

                // Create export archive
                using (new FlightEnabled(new string[] { FlightingNames.AvScanEnabled, FlightingNames.ExportToCsvBySubjectEnabled }))
                using (var fileStream = File.Create(DestinationArchive, 4096, FileOptions.None))
                using (var destinationZip = new ZipArchive(fileStream, ZipArchiveMode.Update, true, Encoding.UTF8))
                {
                    var archive = new ExportArchive(fileStream, factory.Object, new Mock<IKustoClient>().Object);
                    var commandId = new CommandId(Guid.NewGuid().ToString());
                    var commandHistoryCore = new CommandHistoryCoreRecord(commandId);
                    commandHistoryCore.Subject = new MsaSubject();
                    var commandHistory = new CommandHistoryRecord(commandId, commandHistoryCore, null, MakeDefaultStatusMap(), null, null); ;

                    await archive.ProcessStageFileAsync(
                        destinationZip,
                        sourceStream,
                        sourceStream.Length,
                        commandHistory,
                        new AgentId(Guid.NewGuid()),
                        new AssetGroupId(Guid.NewGuid()),
                        sourcePath,
                        sourcePath,
                        CancellationToken.None);
                }

                // Verify export archive written successfully
                using (var fileStream = new FileStream(DestinationArchive, FileMode.Open))
                using (var destinationZip = new ZipArchive(fileStream, ZipArchiveMode.Read, true))
                {
                    Assert.True(destinationZip.Entries.Count == expectedDestinationEntries.Count);

                    foreach (var expectedDestinationEntry in expectedDestinationEntries)
                    {
                        var entry = destinationZip.GetEntry(expectedDestinationEntry);
                        using (var entryStream = entry.Open())
                        using (MemoryStream memStream = new MemoryStream())
                        {
                            entryStream.CopyTo(memStream);
                            verifier?.Invoke(expectedDestinationEntry, memStream.ToArray());
                        }
                    }
                }
            }
            finally
            {
                if (File.Exists(DestinationArchive))
                {
                    File.Delete(DestinationArchive);
                }
            }
        }

        private void CreateTestArchive(string path, string entryName)
        {
            using (var fileStream = new FileStream(path, FileMode.Create))
            using (var zip = new ZipArchive(fileStream, ZipArchiveMode.Create, true))
            {
                ZipArchiveEntry destinationEntry = zip.CreateEntry(entryName, CompressionLevel.Optimal);

                using (var content = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(ExportContent))))
                using (var destination = new BufferedStream(destinationEntry.Open(), 1024 * 1024))
                {
                    content.CopyTo(destination);
                }
            }
        }

        private IDictionary<ValueTuple<AgentId, AssetGroupId>, CommandHistoryAssetGroupStatusRecord> MakeDefaultStatusMap()
        {
            var agentId = new AgentId(Guid.NewGuid());
            var assetGroupId = new AssetGroupId(Guid.NewGuid());

            var commandStatusRecord = new CommandHistoryAssetGroupStatusRecord(agentId, assetGroupId);
            commandStatusRecord.IngestionTime = DateTimeOffset.UtcNow;
            commandStatusRecord.CompletedTime = DateTimeOffset.UtcNow;

            IDictionary<ValueTuple<AgentId, AssetGroupId>, CommandHistoryAssetGroupStatusRecord> statusMap =
                new Dictionary<ValueTuple<AgentId, AssetGroupId>, CommandHistoryAssetGroupStatusRecord>()
                {
                    { new ValueTuple<AgentId, AssetGroupId>(agentId, assetGroupId), commandStatusRecord }
                };

            return statusMap;
        }
    }
}
