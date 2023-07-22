// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.Helpers.FileSystem.Cosmos
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.Common.Cosmos;
    using Microsoft.Membership.MemberServices.CosmosHelpers;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem.Cosmos;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class CosmosFileTests
    {
        private class CosmosFileTestException : Exception
        {
        }

        private const string Name = "Name_2018_1_2_0.txt";
        private const string Path = "ROOT/PATH";

        private readonly Mock<ICosmosFileSystem> mockFileSystem = new Mock<ICosmosFileSystem>();
        private readonly Mock<ICosmosClient> mockClient = new Mock<ICosmosClient>();

        private readonly CosmosStreamInfo info = new CosmosStreamInfo
        {
            StreamName = CosmosFileTests.Path + "/" + CosmosFileTests.Name,
            CreateTime = new DateTime(2018, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            Length = 20060405,
        };

        private CosmosFile testObj;

        [TestInitialize]
        public void Init()
        {
            this.mockFileSystem.SetupGet(o => o.Client).Returns(this.mockClient.Object);

            this.testObj = new CosmosFile(this.info, this.mockFileSystem.Object, null);
        }

        [TestMethod]
        public void ConstructorSetsPropertiesCorrectly()
        {
            Assert.AreEqual(FileSystemObjectType.File, this.testObj.Type);
            Assert.AreEqual(this.info.StreamName, this.testObj.Path);
            Assert.AreEqual(CosmosFileTests.Name, this.testObj.Name);
            Assert.AreEqual(CosmosFileTests.Path, this.testObj.ParentDirectory);
            Assert.AreEqual(this.info.CreateTime, this.testObj.Created.DateTime);
            Assert.AreEqual(this.info.Length, this.testObj.Size);
        }

        [TestMethod]
        public async Task ReadCallsReadStreamOnClientAndReturnsResultIfResultNonNull()
        {
            const string Contents = "TobyDog";
            const long Offset = 17;
            const int Size = 19;

            Stream result;
            string resultText;

            this.mockClient
                .Setup(
                    o => o.ReadStreamAsync(
                        It.IsAny<string>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<bool>()))
                .ReturnsAsync(new DataInfo(Encoding.UTF8.GetBytes(Contents), Encoding.UTF8.GetBytes(Contents).Length));

            // test
            result = await this.testObj.ReadFileChunkAsync(Offset, Size);
            using (StreamReader r = new StreamReader(result)) { resultText = r.ReadToEnd(); }

            // verify
            this.mockClient.Verify(o => o.ReadStreamAsync(this.testObj.Path, Offset, Size, true), Times.Once);
            Assert.AreEqual(Contents, resultText);
        }

        [TestMethod]
        public async Task ReadCallsReadStreamOnClientAndReturnsResultEmptyStreamIfResultNull()
        {
            const long Offset = 17;
            const int Size = 19;

            Stream result;

            this.mockClient
                .Setup(
                    o => o.ReadStreamAsync(
                        It.IsAny<string>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<bool>()))
                .ReturnsAsync((DataInfo)null);

            // test
            result = await this.testObj.ReadFileChunkAsync(Offset, Size);

            // verify
            this.mockClient.Verify(o => o.ReadStreamAsync(this.testObj.Path, Offset, Size, true), Times.Once);
            Assert.AreEqual(0, result.Length);
        }

        [TestMethod]
        [ExpectedException(typeof(CosmosFileTestException))]
        public async Task ReadEmitsExceptionForAnyOtherExceptionType()
        {
            this.mockClient
                .Setup(
                    o => o.ReadStreamAsync(
                        It.IsAny<string>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<bool>()))
                .Returns(Task.FromException<DataInfo>(new CosmosFileTestException()));

            await this.testObj.ReadFileChunkAsync(0, 1);
        }

        [TestMethod]
        public async Task DeleteCallsDeleteOnClient()
        {
            await this.testObj.DeleteAsync();

            this.mockClient.Verify(o => o.DeleteAsync(this.info.StreamName, true), Times.Once);
        }


        [TestMethod]
        [ExpectedException(typeof(CosmosFileTestException))]
        public async Task DeleteEmitsExceptionForAnyOtherExceptionType()
        {
            this.mockClient
                .Setup(o => o.DeleteAsync(It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(Task.FromException(new CosmosFileTestException()));

            await this.testObj.DeleteAsync();
        }

        [TestMethod]
        public async Task AppendCallsAppendOnClient()
        {
            string data = "Data";
            byte[] expected = Encoding.UTF8.GetBytes(data);

            // test
            await this.testObj.AppendAsync(data);

            // verify
            this.mockClient.Verify(o => o.AppendAsync(this.info.StreamName, expected), Times.Once);
        }


        [TestMethod]
        [ExpectedException(typeof(CosmosFileTestException))]
        public async Task AppendEmitsExceptionForAnyOtherExceptionType()
        {
            this.mockClient
                .Setup(o => o.AppendAsync(It.IsAny<string>(), It.IsAny<byte[]>()))
                .Returns(Task.FromException(new CosmosFileTestException()));

            await this.testObj.AppendAsync("test");
        }

        [TestMethod]
        [DataRow("/newPath/dir/")]
        [DataRow("newPath/dir/")]
        [DataRow("/newPath/dir")]
        [DataRow("newPath/dir")]
        public async Task MoveRelativeBehavesCorrectlyForData(string targetPath)
        {
            const string ExpectedTargetPath = "newPath/dir/";
            const string Root = "ROOT/";

            string originalPath = this.testObj.Path;
            string expected = Root + ExpectedTargetPath + this.testObj.Name;

            this.mockFileSystem.SetupGet(o => o.RootDirectory).Returns(Root);

            // test
            await this.testObj.MoveRelativeAsync(targetPath, true, true);

            // verify
            this.mockClient.Verify(o => o.RenameAsync(originalPath, expected, true, true), Times.Once);
        }


        [TestMethod]
        [ExpectedException(typeof(CosmosFileTestException))]
        public async Task MoveRelativeEmitsExceptionForAnyOtherExceptionType()
        {
            this.mockClient
                .Setup(o => o.RenameAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .Returns(Task.FromException(new CosmosFileTestException()));

            await this.testObj.MoveRelativeAsync("test", true, true);
        }

        [TestMethod]
        public async Task SetLifetimeCallsClient()
        {
            TimeSpan length = TimeSpan.FromMinutes(2016);

            // test
            await this.testObj.SetLifetimeAsync(length, true);

            // verify
            this.mockClient.Verify(o => o.SetLifetimeAsync(this.testObj.Path, length, true), Times.Once);
        }


        [TestMethod]
        [ExpectedException(typeof(CosmosFileTestException))]
        public async Task SetLifetimeEmitsExceptionForAnyOtherExceptionType()
        {
            this.mockClient
                .Setup(o => o.SetLifetimeAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<bool>()))
                .Returns(Task.FromException(new CosmosFileTestException()));

            await this.testObj.SetLifetimeAsync(TimeSpan.FromMinutes(1), true);
        }

    }
}
