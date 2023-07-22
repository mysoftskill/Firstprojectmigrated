// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExportWorker.UnitTests.Utility.PeriodicFileWriter
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem.Cosmos;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class PeriodicFileWriterTests
    {
        public interface INameGen
        {
            string GetName(DateTimeOffset time);
        }

        private const string LogPath = "LOGPATH/";
        private const string Root = "ROOT/";

        private readonly Mock<ICosmosFileSystem> mockFileSystem = new Mock<ICosmosFileSystem>();
        private readonly Mock<IQueuedFileWriter> mockWriter = new Mock<IQueuedFileWriter>();
        private readonly Mock<INameGen> mockNameGen = new Mock<INameGen>();
        private readonly Mock<IClock> mockClock = new Mock<IClock>();

        private PeriodicFileWriter testObj;
        private TimeSpan period = TimeSpan.FromHours(1);

        [TestInitialize]
        public void Init()
        {
            this.mockFileSystem.SetupGet(o => o.RootDirectory).Returns(PeriodicFileWriterTests.Root);
            this.mockFileSystem.SetupGet(o => o.DefaultLifetime).Returns(TimeSpan.FromHours(20060415));

            this.mockFileSystem
                .Setup(o => o.CreateQueuedFileWriterAsync(It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<FileCreateMode>()))
                .ReturnsAsync(this.mockWriter.Object);

            this.testObj = new PeriodicFileWriter(
                this.mockFileSystem.Object,
                PeriodicFileWriterTests.LogPath,
                t => this.mockNameGen.Object.GetName(t),
                this.period,
                this.mockClock.Object);
        }

        [TestMethod]
        public async Task WriterCreatesFileWhenMessageQueued()
        {
            const string Name = "NAME.tsv";
            const string Msg = "MSG";

            const string ExpectedName = PeriodicFileWriterTests.Root + PeriodicFileWriterTests.LogPath + Name;

            this.mockNameGen.Setup(o => o.GetName(It.IsAny<DateTimeOffset>())).Returns(Name);
            
            // test
            await this.testObj.QueueWriteAsync(Msg);

            // verify
            this.mockFileSystem.Verify(
                o => o.CreateQueuedFileWriterAsync(
                    ExpectedName, 
                    this.mockFileSystem.Object.DefaultLifetime, 
                    FileCreateMode.OpenExisting),
                Times.Once);
        }
        
        [TestMethod]
        public async Task WriterCreatesAdditionalFileWhenSufficientTimePased()
        {
            const string Name1 = "NAME1.tsv";
            const string Name2 = "NAME2.tsv";
            const string Name3 = "NAME3.tsv";
            const string Msg1 = "MSG1";
            const string Msg2 = "MSG2";

            const string ExpectedName1 = PeriodicFileWriterTests.Root + PeriodicFileWriterTests.LogPath + Name1;
            const string ExpectedName2 = PeriodicFileWriterTests.Root + PeriodicFileWriterTests.LogPath + Name2;
            const string ExpectedName3 = PeriodicFileWriterTests.Root + PeriodicFileWriterTests.LogPath + Name3;

            this.mockNameGen
                .SetupSequence(o => o.GetName(It.IsAny<DateTimeOffset>()))
                .Returns(Name1)
                .Returns(Name2)
                .Returns(Name3);

            // test
            await this.testObj.QueueWriteAsync(Msg1);

            // advance time long enough to force a new file
            this.mockClock.SetupGet(o => o.UtcNow).Returns(this.mockClock.Object.UtcNow + TimeSpan.FromHours(this.period.Hours * 2));

            await this.testObj.QueueWriteAsync(Msg2);

            // advance time long enough to force a new file
            this.mockClock.SetupGet(o => o.UtcNow).Returns(this.mockClock.Object.UtcNow + TimeSpan.FromHours(this.period.Hours * 4));

            await this.testObj.QueueWriteAsync(Msg1);

            // verify
            Assert.AreEqual(2, this.testObj.PreviousCount);

            this.mockFileSystem.Verify(
                o => o.CreateQueuedFileWriterAsync(
                    ExpectedName1, this.mockFileSystem.Object.DefaultLifetime, FileCreateMode.OpenExisting),
                Times.Once);

            this.mockFileSystem.Verify(
                o => o.CreateQueuedFileWriterAsync(
                    ExpectedName2, this.mockFileSystem.Object.DefaultLifetime, FileCreateMode.OpenExisting),
                Times.Once);

            this.mockFileSystem.Verify(
                o => o.CreateQueuedFileWriterAsync(
                    ExpectedName3, this.mockFileSystem.Object.DefaultLifetime, FileCreateMode.OpenExisting),
                Times.Once);
        }

        [TestMethod]
        public async Task FlushInvokesFlushOnCurrentFileWhenCalled()
        {
            const string Name = "NAME.tsv";
            const string Msg = "MSG";

            this.mockNameGen.Setup(o => o.GetName(It.IsAny<DateTimeOffset>())).Returns(Name);

            await this.testObj.QueueWriteAsync(Msg);

            // test
            await this.testObj.FlushQueueAsync(CancellationToken.None);

            // verify
            this.mockWriter.Verify(o => o.FlushQueueAsync(CancellationToken.None), Times.Once);
        }

        [TestMethod]
        public async Task FlushInvokesFlushOnAllFilesWhenSufficientTimePased()
        {
            const string Name1 = "NAME1.tsv";
            const string Name2 = "NAME2.tsv";
            const string Name3 = "NAME3.tsv";
            const string Msg = "MSG1";

            Mock<IQueuedFileWriter> mock1 = new Mock<IQueuedFileWriter>();
            Mock<IQueuedFileWriter> mock2 = new Mock<IQueuedFileWriter>();
            Mock<IQueuedFileWriter> mock3 = new Mock<IQueuedFileWriter>();

            this.mockNameGen
                .SetupSequence(o => o.GetName(It.IsAny<DateTimeOffset>()))
                .Returns(Name1)
                .Returns(Name2)
                .Returns(Name3);

            this.mockFileSystem
                .Setup(
                    o => o.CreateQueuedFileWriterAsync(
                        It.Is<string>(p => p.Contains(Name1)), It.IsAny<TimeSpan?>(), It.IsAny<FileCreateMode>()))
                .ReturnsAsync(mock1.Object);

            this.mockFileSystem
                .Setup(
                    o => o.CreateQueuedFileWriterAsync(
                        It.Is<string>(p => p.Contains(Name2)), It.IsAny<TimeSpan?>(), It.IsAny<FileCreateMode>()))
                .ReturnsAsync(mock2.Object);

            this.mockFileSystem
                .Setup(
                    o => o.CreateQueuedFileWriterAsync(
                        It.Is<string>(p => p.Contains(Name3)), It.IsAny<TimeSpan?>(), It.IsAny<FileCreateMode>()))
                .ReturnsAsync(mock3.Object);


            await this.testObj.QueueWriteAsync(Msg);

            // advance time long enough to force a new file
            this.mockClock.SetupGet(o => o.UtcNow).Returns(this.mockClock.Object.UtcNow + TimeSpan.FromHours(this.period.Hours * 2));

            await this.testObj.QueueWriteAsync(Msg);

            // advance time long enough to force a new file
            this.mockClock.SetupGet(o => o.UtcNow).Returns(this.mockClock.Object.UtcNow + TimeSpan.FromHours(this.period.Hours * 4));

            await this.testObj.QueueWriteAsync(Msg);

            // test
            await this.testObj.FlushQueueAsync(CancellationToken.None);

            // verify
            mock1.Verify(o => o.FlushQueueAsync(CancellationToken.None), Times.Once);
            mock2.Verify(o => o.FlushQueueAsync(CancellationToken.None), Times.Once);
            mock3.Verify(o => o.FlushQueueAsync(CancellationToken.None), Times.Once);
        }
    }
}
