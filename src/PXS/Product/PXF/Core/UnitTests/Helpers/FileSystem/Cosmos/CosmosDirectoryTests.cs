// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.Helpers.FileSystem.Cosmos
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.Common.Cosmos;
    using Microsoft.Membership.MemberServices.CosmosHelpers;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem.Cosmos;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;


    [TestClass]
    public class CosmosDirectoryTests
    {
        private const string Name = "1ED24ADC-0D8C-4ADD-AE21-5359EE39F8F1";
        private const string Path = "ROOT/PATH";

        private readonly Mock<ICosmosFileSystem> mockFileSystem = new Mock<ICosmosFileSystem>();
        private readonly Mock<ICosmosClient> mockClient = new Mock<ICosmosClient>();

        private readonly CosmosStreamInfo info = new CosmosStreamInfo
        {
            IsDirectory = true,
            StreamName = CosmosDirectoryTests.Path + "/" + CosmosDirectoryTests.Name,
            CreateTime = new DateTime(2018, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            Length = 20060405,
        };

        private CosmosDirectory testObj;

        [TestInitialize]
        public void Init()
        {
            this.mockFileSystem.SetupGet(o => o.Client).Returns(this.mockClient.Object);

            this.testObj = new CosmosDirectory(this.info, this.mockFileSystem.Object, null);
        }

        [TestMethod]
        public void ConstructorSetsPropertiesCorrectlyWhenCalledWithStreamInfo()
        {
            this.testObj = new CosmosDirectory(this.info, this.mockFileSystem.Object,null);

            Assert.AreEqual(FileSystemObjectType.Directory, this.testObj.Type);
            Assert.AreEqual(this.info.StreamName, this.testObj.Path);
            Assert.AreEqual(CosmosDirectoryTests.Name, this.testObj.Name);
            Assert.AreEqual(CosmosDirectoryTests.Path, this.testObj.ParentDirectory);
        }


        [TestMethod]
        public void ConstructorSetsPropertiesCorrectlyWhenCalledWithPath()
        {
            this.testObj = new CosmosDirectory(this.info, this.mockFileSystem.Object, null);

            Assert.AreEqual(FileSystemObjectType.Directory, this.testObj.Type);
            Assert.AreEqual(this.info.StreamName, this.testObj.Path);
            Assert.AreEqual(CosmosDirectoryTests.Name, this.testObj.Name);
            Assert.AreEqual(CosmosDirectoryTests.Path, this.testObj.ParentDirectory);
        }

        [TestMethod]
        [ExpectedException(typeof(DirectoryNotFoundException))]
        public async Task EnumerateThrowsDirNotFoundWhenClientReturnsNull()
        {
            this.mockClient
                .Setup(o => o.GetDirectoryInfoAsync(It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync((ICollection<CosmosStreamInfo>)null);

            await this.testObj.EnumerateAsync();
        }

        [TestMethod]
        public async Task EnumerateReturnsAllDirectoriesInResult()
        {
            List<CosmosStreamInfo> info = new List<CosmosStreamInfo>
            {
                new CosmosStreamInfo { StreamName = "ROOT/PATH/DIR1", IsDirectory = true },
            };

            ICollection<IFileSystemObject> result;

            this.mockClient
                .Setup(o => o.GetDirectoryInfoAsync(It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(info);

            // test
            result = await this.testObj.EnumerateAsync();

            // verify

            Assert.AreEqual(1, result.Count);
            Assert.IsInstanceOfType(result.First(), typeof(IDirectory));
            Assert.AreEqual(info[0].StreamName, result.First().Path);

            this.mockClient.Verify(o => o.GetDirectoryInfoAsync(this.info.StreamName, true), Times.Once);
        }

        [TestMethod]
        public async Task EnumerateReturnsCompletedFilesInResult()
        {
            List<CosmosStreamInfo> info = new List<CosmosStreamInfo>
            {
                new CosmosStreamInfo { StreamName = "ROOT/PATH/FNot", IsDirectory = false, IsComplete = false },
                new CosmosStreamInfo { StreamName = "ROOT/PATH/FComplete", IsDirectory = false, IsComplete = true },
                new CosmosStreamInfo { StreamName = "ROOT/PATH/FNot2", IsDirectory = false, IsComplete = false },
            };

            ICollection<IFileSystemObject> result;

            this.mockClient
                .Setup(o => o.GetDirectoryInfoAsync(It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(info);

            // test
            result = await this.testObj.EnumerateAsync();

            // verify

            Assert.AreEqual(1, result.Count);
            Assert.IsInstanceOfType(result.First(), typeof(IFile));
            Assert.AreEqual(info[1].StreamName, result.First().Path);

            this.mockClient.Verify(o => o.GetDirectoryInfoAsync(this.info.StreamName, true), Times.Once);
        }
    }
}
