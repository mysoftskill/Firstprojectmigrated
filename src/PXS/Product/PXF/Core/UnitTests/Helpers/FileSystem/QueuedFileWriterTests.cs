// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.Helpers.FileSystem.Cosmos
{
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class QueuedFileWriterTests
    {
        private const int ChunkSize = 5;

        private readonly Mock<IFile> mockFile = new Mock<IFile>();

        private QueuedFileWriter testObj;

        [TestInitialize]
        public void Init()
        {
            this.testObj = new QueuedFileWriter(this.mockFile.Object, QueuedFileWriterTests.ChunkSize);
        }

        [TestMethod]
        public async Task FlushWritesQueuedStreamsToFileWhenInputIsLessThanChunkSize()
        {
            const string Input = "t1";

            // test
            await this.testObj.QueueWriteAsync(Input);
            await this.testObj.FlushQueueAsync(CancellationToken.None);

            // verify
            this.mockFile.Verify(o => o.AppendAsync(Input), Times.Once);
            this.mockFile.Verify(o => o.AppendAsync(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task FlushWritesQueuedStreamsToFileWhenInputIsGreaterThanChunkSize()
        {
            const string Input = "t1t2t3";

            // test
            await this.testObj.QueueWriteAsync(Input);
            await this.testObj.FlushQueueAsync(CancellationToken.None);

            // verify
            this.mockFile.Verify(o => o.AppendAsync(Input), Times.Once);
            this.mockFile.Verify(o => o.AppendAsync(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task FlushCombinesChunksUntilChunkSizeIsReached()
        {
            const string Input0 = "t1t2t3";
            const string Input1 = "c1";
            const string Input2 = "t4t5t6";
            const string Input3 = "c2";
            const string Input4 = "c3";
            const string Input5 = "c4";
            const string Input6 = "c";

            // test
            await this.testObj.QueueWriteAsync(Input0);
            await this.testObj.QueueWriteAsync(Input1);
            await this.testObj.QueueWriteAsync(Input2);
            await this.testObj.QueueWriteAsync(Input3);
            await this.testObj.QueueWriteAsync(Input4);
            await this.testObj.QueueWriteAsync(Input5);
            await this.testObj.QueueWriteAsync(Input6);
            await this.testObj.FlushQueueAsync(CancellationToken.None);

            // verify
            this.mockFile.Verify(o => o.AppendAsync(Input0), Times.Once);
            this.mockFile.Verify(o => o.AppendAsync(Input1 + Input2), Times.Once);
            this.mockFile.Verify(o => o.AppendAsync(Input3 + Input4 + Input5), Times.Once);
            this.mockFile.Verify(o => o.AppendAsync(Input6), Times.Once);
            this.mockFile.Verify(o => o.AppendAsync(It.IsAny<string>()), Times.Exactly(4));
        }
    }
}
