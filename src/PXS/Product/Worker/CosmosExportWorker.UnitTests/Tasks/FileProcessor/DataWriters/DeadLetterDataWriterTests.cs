// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExportWorker.UnitTests.Tasks.FileProcessor.DataWriters
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem.Cosmos;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Tasks;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExportWorker.UnitTests.TestUtility;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    [Ignore]
    public class DeadLetterDataWriterTests
    {
        private const string RootDir = "ROOT";
        private const string AgentId = "AgentId";
        private const string CmdId = "CommandId";
        private const string File = "File";

        private static readonly TimeSpan MaxAge = new TimeSpan(1, 2, 3, 4);

        public class TestPaths : ICosmosRelativePathsAndExpiryTimes
        {
            public string BasePath { get; } = "BASE/";
            public string AgentOutput { get; } = "OUTPUT/";
            public string PostProcessHolding { get; } = "HOLDING/";
            public string ActivityLog { get; } = "ACTIVITY/";
            public string DeadLetter { get; } = "DEADLETTER/";
            public int ActivityLogExpiryHours { get; set; } = 1;
            public int DeadLetterExpiryHours { get; } = 1;
            public int HoldingExpiryHours { get; } = 1;
            public string StatsLog { get; } = "STATS/";
            public int StatsLogExpiryHours { get; } = 1;
            public int ManifestHoldingExpiryHours { get; } = 1;
        }

        private readonly Mock<ICosmosFileSystem> mockFileSystem = new Mock<ICosmosFileSystem>();
        private readonly MockLogger mockLogger = new MockLogger();
        private readonly Mock<IFile> mockFile = new Mock<IFile>();

        private DeadLetterDataWriter testObj;

        [TestInitialize]
        public void Init()
        {
            this.mockFileSystem
                .Setup(o => o.CreateFileAsync(It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<FileCreateMode>()))
                .ReturnsAsync(this.mockFile.Object);

            this.mockFileSystem.SetupGet(o => o.DefaultLifetime).Returns(DeadLetterDataWriterTests.MaxAge);
            this.mockFileSystem.SetupGet(o => o.RootDirectory).Returns(DeadLetterDataWriterTests.RootDir + "/");

            this.testObj = new DeadLetterDataWriter(
                DeadLetterDataWriterTests.CmdId,
                DeadLetterDataWriterTests.AgentId,
                DeadLetterDataWriterTests.File,
                this.mockFileSystem.Object,
                this.mockLogger.Object);
        }

        [TestMethod]
        public async Task WriteCreatesFileInFileSystemOnFirstCall()
        {
            const string PidText = "PID";
            const string Json = "JSONDATA";

            string expectedFile = string.Format(
                "{0}/{1}/{2}/{3}.txt",
                DeadLetterDataWriterTests.RootDir,
                DeadLetterDataWriterTests.AgentId,
                DeadLetterDataWriterTests.CmdId,
                DeadLetterDataWriterTests.File);

            // test
            await this.testObj.WriteAsync(PidText, Json, 0);

            // verify
            this.mockFileSystem.Verify(
                o => o.CreateFileAsync(It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<FileCreateMode>()), 
                Times.Once);

            this.mockFileSystem.Verify(
                o => o.CreateFileAsync(expectedFile, DeadLetterDataWriterTests.MaxAge, FileCreateMode.CreateAlways),
                Times.Once);
        }

        [TestMethod]
        public async Task WriteCreatesFileInFileSystemOnlyOnFirstCall()
        {
            const string PidText = "PID";
            const string Json = "JSONDATA";

            string expectedFile = string.Format(
                "{0}/{1}/{2}/{3}.txt",
                DeadLetterDataWriterTests.RootDir,
                DeadLetterDataWriterTests.AgentId,
                DeadLetterDataWriterTests.CmdId,
                DeadLetterDataWriterTests.File);

            // test
            await this.testObj.WriteAsync(PidText, Json, 0);
            await this.testObj.WriteAsync(PidText, Json, 0);

            // verify
            this.mockFileSystem.Verify(
                o => o.CreateFileAsync(It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<FileCreateMode>()),
                Times.Once);

            this.mockFileSystem.Verify(
                o => o.CreateFileAsync(expectedFile, DeadLetterDataWriterTests.MaxAge, FileCreateMode.CreateAlways),
                Times.Once);
        }

        [TestMethod]
        public async Task WriteSendsToFileWhenPendingSizeMoreThanThreshold()
        {
            const string PidText = "PID";
            const string Json = "JSONDATA";

            long result;

            // test
            result = await this.testObj.WriteAsync(PidText, Json, 0);

            // verify
            Assert.AreEqual(0, result);
            Assert.AreEqual(Json.Length, this.testObj.Size);
            Assert.AreEqual(0, this.testObj.PendingSize);
            Assert.AreEqual(1, this.testObj.RowCount);

            this.mockFile.Verify(o => o.AppendAsync(It.IsAny<string>()), Times.Once);
            this.mockFile.Verify(o => o.AppendAsync($"{PidText}\t{Json}\n"), Times.Once);
        }

        [TestMethod]
        public async Task WriteDoesNotSendToFileWhenPendingSizeLessThanThreshold()
        {
            const string PidText = "PID";
            const string Json = "JSONDATA";

            int expectedLength = Json.Length + PidText.Length + 2;

            long result;

            // test
            result = await this.testObj.WriteAsync(PidText, Json, expectedLength + 1);

            // verify
            Assert.AreEqual(expectedLength, result);
            Assert.AreEqual(Json.Length, this.testObj.Size);
            Assert.AreEqual(expectedLength, this.testObj.PendingSize);
            Assert.AreEqual(1, this.testObj.RowCount);

            this.mockFile.Verify(o => o.AppendAsync(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task WriteSendsToFileWhenAccumulatedPendingSizeMoreThanThreshold()
        {
            const string PidText = "PID";
            const string Json1 = "JSONDATA1";
            const string Json2 = "JSONDATA2";

            string expectedData = $"{PidText}\t{Json1}\n{PidText}\t{Json2}\n";
            int expectedLength1 = Json1.Length + PidText.Length + 2;

            long result1;
            long result2;

            // test
            result1 = await this.testObj.WriteAsync(PidText, Json1, expectedLength1 + 1);
            result2 = await this.testObj.WriteAsync(PidText, Json2, expectedLength1 + 1);

            // verify
            Assert.AreEqual(expectedLength1, result1);
            Assert.AreEqual(expectedLength1 * -1, result2);
            Assert.AreEqual(Json1.Length + Json2.Length, this.testObj.Size);
            Assert.AreEqual(0, this.testObj.PendingSize);
            Assert.AreEqual(2, this.testObj.RowCount);

            this.mockFile.Verify(o => o.AppendAsync(It.IsAny<string>()), Times.Once);
            this.mockFile.Verify(o => o.AppendAsync(expectedData), Times.Once);
        }


        [TestMethod]
        public async Task WriteSendsToFileWhenAccumulatedPendingSizeMoreThanThresholdOverMultipleCalls()
        {
            const string PidText = "PID";
            const string Json1 = "JSONDATA1";
            const string Json2 = "JSONDATA2";
            const string Json3 = "JSONDATA3";
            const string Json4 = "JSONDATA4";

            string expectedData12 = $"{PidText}\t{Json1}\n{PidText}\t{Json2}\n";
            string expectedData34 = $"{PidText}\t{Json3}\n{PidText}\t{Json4}\n";
            int expectedLength1 = Json1.Length + PidText.Length + 2;
            int expectedLength3 = Json3.Length + PidText.Length + 2;

            long result1;
            long result2;
            long result3;
            long result4;

            // test
            result1 = await this.testObj.WriteAsync(PidText, Json1, expectedLength1 + 1);
            result2 = await this.testObj.WriteAsync(PidText, Json2, expectedLength1 + 1);
            result3 = await this.testObj.WriteAsync(PidText, Json3, expectedLength3 + 1);
            result4 = await this.testObj.WriteAsync(PidText, Json4, expectedLength3 + 1);

            // verify
            Assert.AreEqual(expectedLength1, result1);
            Assert.AreEqual(expectedLength1 * -1, result2);
            Assert.AreEqual(expectedLength3, result3);
            Assert.AreEqual(expectedLength3 * -1, result4);
            Assert.AreEqual(Json1.Length + Json2.Length + Json3.Length + Json4.Length, this.testObj.Size);
            Assert.AreEqual(0, this.testObj.PendingSize);
            Assert.AreEqual(4, this.testObj.RowCount);

            this.mockFile.Verify(o => o.AppendAsync(It.IsAny<string>()), Times.Exactly(2));
            this.mockFile.Verify(o => o.AppendAsync(expectedData12), Times.Once);
            this.mockFile.Verify(o => o.AppendAsync(expectedData34), Times.Once);
        }

        [TestMethod]
        public async Task CloseAsyncFlushesPendingDataToFile()
        {
            const string PidText = "PID";
            const string Json = "JSONDATA";

            int expectedLength = Json.Length + PidText.Length + 2;

            await this.testObj.WriteAsync(PidText, Json, expectedLength + 1);

            this.mockFile.Verify(o => o.AppendAsync(It.IsAny<string>()), Times.Never);

            // test
            await this.testObj.CloseAsync();

            // verify
            Assert.AreEqual(Json.Length, this.testObj.Size);
            Assert.AreEqual(0, this.testObj.PendingSize);
            Assert.AreEqual(1, this.testObj.RowCount);

            this.mockFile.Verify(o => o.AppendAsync(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task FlushAsyncFlushesPendingDataToFile()
        {
            const string PidText = "PID";
            const string Json = "JSONDATA";

            int expectedLength = Json.Length + PidText.Length + 2;
            long result;

            await this.testObj.WriteAsync(PidText, Json, expectedLength + 1);

            this.mockFile.Verify(o => o.AppendAsync(It.IsAny<string>()), Times.Never);

            // test
            result = await this.testObj.FlushAsync();

            // verify
            Assert.AreEqual(expectedLength, result);
            Assert.AreEqual(Json.Length, this.testObj.Size);
            Assert.AreEqual(0, this.testObj.PendingSize);
            Assert.AreEqual(1, this.testObj.RowCount);

            this.mockFile.Verify(o => o.AppendAsync(It.IsAny<string>()), Times.Once);
        }
    }
}
