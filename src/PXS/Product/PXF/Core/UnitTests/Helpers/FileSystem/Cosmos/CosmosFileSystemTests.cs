// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.Helpers.FileSystem.Cosmos
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.Common.Cosmos;
    using Microsoft.Membership.MemberServices.CosmosHelpers;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;


    [TestClass]
    public class CosmosFileSystemTests
    {
        private class CosmosFileSystemTestException : Exception
        {
        }

        private readonly Mock<ICosmosClient> mockClient = new Mock<ICosmosClient>();

        private CosmosFileSystem testObj;

        [TestInitialize]
        public void Init()
        {
            this.testObj = new CosmosFileSystem(this.mockClient.Object, "ROOT", "TAG", null, null);
        }

        [TestMethod]
        public async Task OpenDirReturnsNullIfDirectoryDoesntExist()
        {
            const string Path = "ROOT/PATH/DIR";

            IDirectory result;

            this.mockClient.Setup(o => o.DirectoryExistsAsync(It.IsAny<string>())).ReturnsAsync(false);

            // test
            result = await this.testObj.OpenExistingDirectoryAsync(Path).ConfigureAwait(false);

            // verify
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task OpenDirReturnsDirectoryObjIfDirectoryExists()
        {
            const string Path = "ROOT/PATH/DIR";

            IDirectory result;

            this.mockClient.Setup(o => o.DirectoryExistsAsync(It.IsAny<string>())).ReturnsAsync(true);

            // test
            result = await this.testObj.OpenExistingDirectoryAsync(Path).ConfigureAwait(false);

            // verify
            Assert.IsNotNull(result);
            Assert.AreEqual(Path, result.Path);
        }

        [TestMethod]
        [ExpectedException(typeof(CosmosFileSystemTestException))]
        public async Task OpenDirRethrowsIfClientThrowsOtherException()
        {
            const string Path = "ROOT/PATH/FILE";

            this.mockClient.Setup(o => o.DirectoryExistsAsync(It.IsAny<string>())).Throws(new CosmosFileSystemTestException());

            await this.testObj.OpenExistingDirectoryAsync(Path).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task OpenFileReturnsNullIfDirectoryDoesntExist()
        {
            const string Path = "ROOT/PATH/FILE";

            IFile result;

            this.mockClient
                .Setup(o => o.GetStreamInfoAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync((CosmosStreamInfo)null);

            // test
            result = await this.testObj.OpenExistingFileAsync(Path).ConfigureAwait(false);

            // verify
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task OpenFileReturnsDirectoryObjIfDirectoryExists()
        {
            const string Path = "ROOT/PATH/DIR";

            CosmosStreamInfo info = new CosmosStreamInfo
            {
                StreamName = Path,
            };

            IFile result;

            this.mockClient
                .Setup(o => o.GetStreamInfoAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(info);

            // test
            result = await this.testObj.OpenExistingFileAsync(Path).ConfigureAwait(false);

            // verify
            Assert.IsNotNull(result);
            Assert.AreEqual(Path, result.Path);
        }


        [TestMethod]
        [ExpectedException(typeof(CosmosFileSystemTestException))]
        public async Task OpenFileRethrowsIfClientThrowsOtherException()
        {
            const string Path = "ROOT/PATH/FILE";

            this.mockClient
                .Setup(o => o.GetStreamInfoAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .Throws(new CosmosFileSystemTestException());

            await this.testObj.OpenExistingFileAsync(Path).ConfigureAwait(false);
        }
        
        [TestMethod]
        public async Task CreateCallsCreateAndFetchOnClient()
        {
            const string Path = "ROOT/PATH/FILE";

            IFile result;

            CosmosStreamInfo info = new CosmosStreamInfo
            {
                StreamName = Path,
            };

            this.mockClient
                .Setup(o => o.GetStreamInfoAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(info);

            // test
            result = await this.testObj.CreateFileAsync(Path, TimeSpan.Zero, FileCreateMode.OpenExisting).ConfigureAwait(false);

            // verify
            Assert.IsNotNull(result);
            this.mockClient.Verify(o => o.CreateAsync(Path, TimeSpan.Zero, CosmosCreateStreamMode.OpenExisting), Times.Once);
            this.mockClient.Verify(o => o.GetStreamInfoAsync(Path, true, false), Times.Once);
        }


        [TestMethod]
        [ExpectedException(typeof(CosmosFileSystemTestException))]
        public async Task CreateRethrowsIfClientThrowsOtherException()
        {
            const string Path = "ROOT/PATH/FILE";

            this.mockClient
                .Setup(o => o.CreateAsync(It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<CosmosCreateStreamMode>()))
                .Throws(new CosmosFileSystemTestException());

            await this.testObj.CreateFileAsync(Path, TimeSpan.Zero, FileCreateMode.OpenExisting).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task CreateQueuedWriterCreatesFileAndReturnsQueuedWriter()
        {
            const string Path = "ROOT/PATH/FILE";

            IQueuedFileWriter result;

            CosmosStreamInfo info = new CosmosStreamInfo
            {
                StreamName = Path,
            };

            this.mockClient
                .Setup(o => o.GetStreamInfoAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(info);

            // test
            result = await this.testObj
                .CreateQueuedFileWriterAsync(Path, TimeSpan.Zero, FileCreateMode.OpenExisting)
                .ConfigureAwait(false);

            // verify
            Assert.IsNotNull(result);
            this.mockClient.Verify(o => o.CreateAsync(Path, TimeSpan.Zero, CosmosCreateStreamMode.OpenExisting), Times.Once);
            this.mockClient.Verify(o => o.GetStreamInfoAsync(Path, true, false), Times.Once);
        }

        [TestMethod]
        public async Task DeleteCallsDeleteOnClient()
        {
            const string Path = "ROOT/PATH/FILE";

            // test
            await this.testObj.DeleteAsync(Path, true).ConfigureAwait(false);

            // verify
            this.mockClient.Verify(o => o.DeleteAsync(Path, true), Times.Once);
        }


        [TestMethod]
        [ExpectedException(typeof(CosmosFileSystemTestException))]
        public async Task DeleteRethrowsIfClientThrowsOtherException()
        {
            const string Path = "ROOT/PATH/FILE";

            this.mockClient
                .Setup(o => o.DeleteAsync(It.IsAny<string>(), It.IsAny<bool>()))
                .Throws(new CosmosFileSystemTestException());

            await this.testObj.DeleteAsync(Path, true).ConfigureAwait(false);
        }
    }
}
