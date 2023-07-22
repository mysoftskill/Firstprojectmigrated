// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.FunctionalTests
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Privacy.DataContracts.ExportTypes;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.ExportStorageHelper;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Azure.Storage.Blob;

    using Moq;

    [TestClass]
    public class ExportStagingHelperUnitTest : StorageEmulatorBase
    {
        [TestInitialize]
        public void Init()
        {
            this.mockAzureStorageConfiguration.SetupGet(c => c.UseEmulator).Returns(true);
            this.StartEmulator();
        }

        [TestMethod]
        [TestCategory("FCT"), Ignore]
        public async Task ExportStaging_GetFile()
        {
            const string TestFileName = "TestContainer1/TestFile1.txt";
            var stagingHelper = new ExportStagingStorageHelper(this.BlobClient, this.Logger);
            await stagingHelper.InitializeStagingAsync(RecordCreator.Puid, ExportStorageProvider.GetNewRequestId());
            var stagingFile = stagingHelper.GetStagingFile(TestFileName);
            Assert.IsNotNull(stagingFile);
            Assert.AreEqual(TestFileName, stagingFile.FileName);
            // file isn't written until committed.
        }

        [TestMethod]
        [TestCategory("FCT"), Ignore]
        public async Task ExportStaging_ZipIt()
        {
            var mockCounterFactory = new Mock<ICounterFactory>(MockBehavior.Loose);
            mockCounterFactory.Setup(f => f.GetCounter(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CounterType>()))
                .Returns(new Mock<ICounter>(MockBehavior.Loose).Object);

            const string TestFileName = "TestContainer1/TestFile1.txt";
            string requestId = ExportStorageProvider.GetNewRequestId();
            var stagingHelper = new ExportStagingStorageHelper(this.BlobClient, this.Logger);
            await stagingHelper.InitializeStagingAsync(RecordCreator.Puid, requestId);
            var stagingFile = stagingHelper.GetStagingFile(TestFileName);
            await stagingFile.AddBlockAsync("abc");
            await stagingFile.CommitAsync();
            stagingFile.Dispose();
            await stagingHelper.ZipStagingAsync(mockCounterFactory.Object, "unit_test");
            Assert.IsTrue(await this.FileExistsAsync(ExportZipStorageHelper.GetZipContainerName(RecordCreator.Id), ExportZipStorageHelper.GetZipBlobName(requestId)));
        }

        [TestMethod]
        [TestCategory("FCT"), Ignore]
        public async Task ExportStaging_DeleteContainer()
        {
            const string TestFileName = "TestContainer1/TestFile1.txt";
            string requestId = ExportStorageProvider.GetNewRequestId();
            var stagingHelper = new ExportStagingStorageHelper(this.BlobClient, this.Logger);
            await stagingHelper.InitializeStagingAsync(RecordCreator.Puid, requestId);
            var stagingFile = stagingHelper.GetStagingFile(TestFileName);
            await stagingFile.AddBlockAsync("abc");
            await stagingFile.CommitAsync();
            stagingFile.Dispose();
            string stagingContainerName = ExportStagingStorageHelper.GetStagingContainerName(RecordCreator.Id, requestId);
            Assert.IsTrue(await this.FileExistsAsync(stagingContainerName, TestFileName));
            await stagingHelper.DeleteStagingContainerAsync();
            Assert.IsFalse(await this.ContainerExistsAsync(stagingContainerName));
        }
    }
}
