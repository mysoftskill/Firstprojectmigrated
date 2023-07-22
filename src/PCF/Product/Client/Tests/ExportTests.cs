namespace Microsoft.PrivacyServices.CommandFeed.Client.Test
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts;
    using Microsoft.PrivacyServices.CommandFeed.Client.Testing;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    [TestClass]
    public class ExportTests
    {
        [TestMethod]
        public async Task TestExportForAadSubjectWithFileSizeDetails()
        {
            int count = 100000;
            int partitions = 10;
            var memoryExportDestination = new MemoryExportDestination();
            var client = new InMemoryCommandFeedClient();
            IExportCommand exportCommand = new ExportCommand(
                "commandId",
                "assetGroupId",
                "assetGroupQualifier",
                "verifier",
                "correlationVector",
                "leaseReceipt",
                DateTimeOffset.MaxValue,
                DateTimeOffset.UtcNow,
                new AadSubject(),
                null,
                new[] { Policies.Current.DataTypes.Ids.BrowsingHistory },
                new Uri("https://microsoft.com"),
                client,
                Policies.Current.CloudInstances.Ids.Public.Value);

            using (ExportPipeline pipeline = ExportPipelineFactory.CreateMemoryPipeline(new ConsoleCommandFeedLogger(), memoryExportDestination, true))
            {
                IList<ExportedFileSizeDetails> exportFileSizeList = new List<ExportedFileSizeDetails>();
                for (int i = 0; i < count; i++)
                {
                    var exampleRandomRecord = new ExportExampleRecord();
                    var fileName = $"test_{exampleRandomRecord.IntData % partitions}.txt";

                    // Should return FileSizeDetails
                    var result = await pipeline.ExportAsync(
                        ExportProductId.Unknown,
                        fileName,
                        DateTimeOffset.UtcNow,
                        Guid.NewGuid().ToString(),
                        exampleRandomRecord).ConfigureAwait(false);

                    Assert.IsNotNull(result);
                    Assert.IsNotNull(result.FileName);
                    Assert.IsNotNull(result.OriginalSize);
                    Assert.AreEqual(true, result.IsCompressed);
                    StringAssert.Contains(result.FileName, fileName);

                    exportFileSizeList.Add(result);
                }

                Assert.AreEqual(count, exportFileSizeList.Count);
            }
        }

        [TestMethod]
        public async Task TestExport()
        {
            int count = 100000;
            int partitions = 10;
            var memoryExportDestination = new MemoryExportDestination();
            ExportedFileSizeDetails result = null;
            using (ExportPipeline pipeline = ExportPipelineFactory.CreateMemoryPipeline(new ConsoleCommandFeedLogger(), memoryExportDestination, false))
            {
                for (int i = 0; i < count; i++)
                {
                    var exampleRandomRecord = new ExportExampleRecord();
                    var fileName = $"test_{exampleRandomRecord.IntData % partitions}.txt";
                    result = await pipeline.ExportAsync(
                        ExportProductId.Unknown,
                        fileName,
                        DateTimeOffset.UtcNow,
                        Guid.NewGuid().ToString(),
                        exampleRandomRecord).ConfigureAwait(false);

                    Assert.IsNotNull(result);
                    Assert.IsNotNull(result.FileName);
                    Assert.IsNotNull(result.OriginalSize);
                    Assert.AreEqual(false, result.IsCompressed);
                    StringAssert.Contains(result.FileName, fileName);
                }
            }


            Assert.AreEqual(partitions, memoryExportDestination.Files.Count, "Did not get multiple partitions");

            List<ExportExampleRecord> data = memoryExportDestination.Files.Values.Select(
                    s =>
                    {
                        Assert.AreNotEqual(0, s.Length, "Stream is 0 length");
                        s.Seek(0, SeekOrigin.Begin);
                        using (var reader = new StreamReader(s))
                        {
                            return JsonConvert.DeserializeObject<List<ExportExampleRecord>>(reader.ReadToEnd());
                        }
                    })
                .SelectMany(d => d)
                .ToList();

            Assert.AreEqual(count, data.Count, "Did not write all data");
            Assert.IsTrue(data.TrueForAll(r => !string.IsNullOrWhiteSpace(r.StringData)), "String data did not serialize");
        }

        [TestMethod]
        public async Task TestExportCompression()
        {
            int count = 100000;
            int partitions = 10;
            var memoryExportDestination = new MemoryExportDestination();
            using (ExportPipeline pipeline = ExportPipelineFactory.CreateMemoryPipeline(new ConsoleCommandFeedLogger(), memoryExportDestination, true))
            {
                for (int i = 0; i < count; i++)
                {
                    var exampleRandomRecord = new ExportExampleRecord();
                    await pipeline.ExportAsync(
                        ExportProductId.Unknown,
                        $"test_{exampleRandomRecord.IntData % partitions}.txt",
                        DateTimeOffset.UtcNow,
                        Guid.NewGuid().ToString(),
                        exampleRandomRecord).ConfigureAwait(false);
                }
            }

            Assert.AreEqual(partitions, memoryExportDestination.Files.Count, "Did not get multiple partitions");

            List<ExportExampleRecord> data = memoryExportDestination.Files.Select(
                    kvp =>
                    {
                        Stream s = kvp.Value;
                        string name = kvp.Key;

                        Assert.IsTrue(name.EndsWith(".zip"));
                        Assert.AreNotEqual(0, s.Length, "Stream is 0 length");

                        s.Seek(0, SeekOrigin.Begin);

                        using (ZipArchive zipArchive = new ZipArchive(s, ZipArchiveMode.Read, false))
                        {
                            Assert.AreEqual(1, zipArchive.Entries.Count);

                            var entry = zipArchive.Entries.First();

                            // Make sure that the inner file has the right name.
                            Assert.AreEqual(entry.Name + ".zip", Path.GetFileName(name));

                            using (Stream entryStream = zipArchive.Entries.First().Open())
                            using (var reader = new StreamReader(entryStream))
                            {
                                return JsonConvert.DeserializeObject<List<ExportExampleRecord>>(reader.ReadToEnd());
                            }
                        }
                    })
                .SelectMany(d => d)
                .ToList();

            Assert.AreEqual(count, data.Count, "Did not write all data");
            Assert.IsTrue(data.TrueForAll(r => !string.IsNullOrWhiteSpace(r.StringData)), "String data did not serialize");
        }

        /// <summary>
        /// Tests that small chunks of data throw when .Dispose is called.
        /// </summary>
        [TestMethod]
        public async Task TestExportSmallUploadFailure()
        {
            string exceptionMessage = Guid.NewGuid().ToString();
            var mockDestination = new Mock<IExportDestination>(MockBehavior.Strict);
            mockDestination
                .Setup(m => m.GetOrCreateFileAsync(It.IsAny<string>()))
                .ReturnsAsync(
                    () =>
                    {
                        var mockFile = new Mock<IExportFile>(MockBehavior.Strict);
                        mockFile
                            .Setup(f => f.AppendAsync(It.IsAny<Stream>()))
                            .Throws(new Exception(exceptionMessage));

                        mockFile
                            .Setup(m => m.Dispose());

                        return mockFile.Object;
                    });

            var logger = new ConsoleCommandFeedLogger();
            try
            {
                using (var pipeline = new ExportPipeline(new JsonExportSerializer(logger), mockDestination.Object))
                {
                    // Enqueue a work item
                    await pipeline.ExportAsync(ExportProductId.Unknown, Policies.Current.DataTypes.Ids.Account, DateTimeOffset.UtcNow, null, new { Test = "test" });
                }

                Assert.Fail("Expected exception from Dispose");
            }
            catch (Exception ex)
            {
                Assert.AreEqual(exceptionMessage, ex.Message);
            }
        }

        /// <summary>
        /// Tests that small chunks of data throw when .Dispose is called.
        /// </summary>
        [TestMethod]
        public async Task TestExportLargeUploadFailure()
        {
            string exceptionMessage = Guid.NewGuid().ToString();
            var mockDestination = new Mock<IExportDestination>(MockBehavior.Strict);
            mockDestination
                .Setup(m => m.GetOrCreateFileAsync(It.IsAny<string>()))
                .ReturnsAsync(
                    () =>
                    {
                        var mockFile = new Mock<IExportFile>(MockBehavior.Strict);
                        mockFile
                            .Setup(f => f.AppendAsync(It.IsAny<Stream>()))
                            .Throws(new Exception(exceptionMessage));

                        mockFile
                            .Setup(m => m.Dispose());

                        return mockFile.Object;
                    });

            var logger = new ConsoleCommandFeedLogger();
            try
            {
                bool thrown = false;

                using (var pipeline = new ExportPipeline(new JsonExportSerializer(logger), mockDestination.Object))
                {
                    // 10KB.
                    pipeline.MaxInternalBufferPerFile = 10 * 1024;

                    int count = 10000;
                    while (count > 0)
                    {
                        count--;
                        try
                        {
                            await pipeline.ExportAsync(ExportProductId.Unknown, Policies.Current.DataTypes.Ids.Account, DateTimeOffset.UtcNow, null, new { Test = "test" });
                        }
                        catch (TimeoutException ex)
                        {
                            Assert.AreEqual(exceptionMessage, ex.Message);
                            thrown = true;
                            break;
                        }
                    }

                    Assert.IsTrue(thrown);
                }

                Assert.Fail("Expected exception in dispose.");
            }
            catch (Exception ex)
            {
                Assert.AreEqual(exceptionMessage, ex.Message);
            }
        }

        [TestMethod]
        public async Task TestExportFullStack()
        {
            var logger = new ConsoleCommandFeedLogger();
            var client = new InMemoryCommandFeedClient();
            client.Enqueue(
                new ExportCommand(
                    "commandId",
                    "assetGroupId",
                    "assetGroupQualifier",
                    "verifier",
                    "correlationVector",
                    "leaseReceipt",
                    DateTimeOffset.MaxValue,
                    DateTimeOffset.UtcNow,
                    new MsaSubject(),
                    null,
                    new[] { Policies.Current.DataTypes.Ids.BrowsingHistory },
                    new Uri("https://microsoft.com"),
                    client,
                    Policies.Current.CloudInstances.Ids.Public.Value));

            var memoryDestination = new MemoryExportDestination();
            var data = new Dictionary<DataTypeId, List<Tuple<DateTimeOffset, string, object>>>();
            var browseData = new List<Tuple<DateTimeOffset, string, object>>();
            for (int i = 0; i < 1000; i++)
            {
                browseData.Add(
                    Tuple.Create(
                        DateTimeOffset.UtcNow,
                        Guid.NewGuid().ToString(),
                        (object)new { Uri = $"https://{Guid.NewGuid()}", Timestamp = DateTimeOffset.UtcNow, Id = Guid.NewGuid() }));
            }

            data.Add(Policies.Current.DataTypes.Ids.BrowsingHistory, browseData);

            var exportAgent = new ExportTestAgent(logger, memoryDestination, data);

            var receiver = new PrivacyCommandReceiver(
                exportAgent,
                client,
                new ConsoleCommandFeedLogger());

            using (var cts = new CancellationTokenSource())
            {
#pragma warning disable 4014
                receiver.BeginReceivingAsync(cts.Token);
#pragma warning restore 4014

                // ReSharper disable once MethodSupportsCancellation
                await Task.Delay(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);

                cts.Cancel();
            }

            Assert.AreEqual(1, memoryDestination.Files?.Count);

            // ReSharper disable once PossibleNullReferenceException
            string fileName = $"{ExportPipeline.UnknownProductId}/" + Policies.Current.DataTypes.Ids.BrowsingHistory.Value + ".json";
            Assert.IsTrue(memoryDestination.Files.ContainsKey(fileName));
            MemoryStream dataStream = memoryDestination.Files[fileName];
            dataStream.Seek(0, SeekOrigin.Begin);
            using (var reader = new StreamReader(dataStream))
            {
                var jsonArray = JsonConvert.DeserializeObject<JArray>(await reader.ReadToEndAsync().ConfigureAwait(false));

                Assert.AreEqual(1000, jsonArray.Count);

                foreach (JToken node in jsonArray.Children())
                {
                    Assert.IsNotNull(node.SelectToken("time"));
                    Assert.IsNotNull(node.SelectToken("correlationId"));
                    Assert.IsNotNull(node.SelectToken("properties.Uri"));
                    Assert.IsNotNull(node.SelectToken("properties.Timestamp"));
                    Assert.IsNotNull(node.SelectToken("properties.Id"));
                }
            }
        }

        [TestMethod]
        public async Task TestExportPipelineForRawJsonData()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            string nowText = now.ToString("o");
            int count = 5;
            var memoryExportDestination = new MemoryExportDestination();
            using (ExportPipeline pipeline = ExportPipelineFactory.CreateMemoryPipeline(new ConsoleCommandFeedLogger(), memoryExportDestination, false))
            {
                var obj = new RawJsonExampleRecord
                {
                    CorrelationId = "abc" + Guid.NewGuid(),
                    Timestamp = DateTimeOffset.UtcNow,
                    Data = new RawJsonDataRecord()
                };

                for (int i = 0; i < count; ++i)
                {
                    obj.Data.Data = i;
                    await pipeline
                        .ExportAsync(ExportProductId.Unknown, "testData.txt", JsonConvert.SerializeObject(obj))
                        .ConfigureAwait(false);
                }
            }

            Assert.AreEqual(1, memoryExportDestination.Files.Count, "Did not get single file");

            List<RawJsonExampleRecord> data = memoryExportDestination.Files.Values
                .Select(
                    s =>
                    {
                        Assert.AreNotEqual(0, s.Length, "Stream is 0 length");
                        s.Seek(0, SeekOrigin.Begin);
                        using (var reader = new StreamReader(s))
                        {
                            return JsonConvert.DeserializeObject<List<RawJsonExampleRecord>>(reader.ReadToEnd());
                        }
                    })
                .SelectMany(d => d)
                .OrderBy(d => d.Data.Data)
                .ToList();

            Assert.AreEqual(count, data.Count, "Did not write all data");
            Assert.IsTrue(data.All(o => o.Timestamp == now || o.CorrelationId.StartsWith("abc")), "unexpected timestamp or correlation id");

            for (int i = 0; i < data.Count; ++i)
            {
                Assert.AreEqual(i, data[i].Data.Data, "unexpected data value");
            }
        }

        [TestMethod]
        public async Task TestLocalDiskPath()
        {
            var destination = new LocalDiskExportDestination(Path.GetTempPath());
            string path = $"{Guid.NewGuid()}/{Guid.NewGuid()}/{Guid.NewGuid()}.txt";
            using (IExportFile file = await destination.GetOrCreateFileAsync(path).ConfigureAwait(false))
            {
            }

            Assert.IsTrue(File.Exists(Path.Combine(Path.GetTempPath(), path)));
        }
    }
}
