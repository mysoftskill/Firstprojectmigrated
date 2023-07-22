// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.FunctionalTests
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.ExportStorageHelper;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Azure.Storage.Blob;

    using Microsoft.PrivacyServices.Common.Azure;

    [TestClass]
    public class ExportZipStorageHelperUnitTest : StorageEmulatorBase
    {
        [TestInitialize]
        public void Init()
        {
            this.mockAzureStorageConfiguration.SetupGet(c => c.UseEmulator).Returns(true);
            this.StartEmulator();
        }

        [TestMethod]
        [TestCategory("FCT"), Ignore]
        public async Task ExportZip_WriteZipFile()
        {
            var content = "test content 123";
            var id = RecordCreator.RandomId;
            var requestId = ExportStorageProvider.GetNewRequestId();
            var zipHelper = new ExportZipStorageHelper(this.BlobClient, new ConsoleLogger());
            await zipHelper.InitializeAsync(id);
            using (var zipWriter = await zipHelper.WriteZipStreamAsync(requestId))
            {
                var blob = await this.CreateATestBlobAsync(content);
                await zipWriter.WriteEntryAsync(blob.Name, blob);
            }
            Assert.IsTrue(await this.ContainerExistsAsync(ExportStorageProvider.GetIdHash(id)));
            Assert.IsTrue(await this.FileExistsAsync(ExportStorageProvider.GetIdHash(id), ExportZipStorageHelper.GetZipBlobName(requestId)));
            var readString = await this.GetStringFromZipBlobAsync(ExportStorageProvider.GetIdHash(id), ExportZipStorageHelper.GetZipBlobName(requestId), true);
            Assert.AreEqual(content, readString);
        }

        [TestMethod]
        [TestCategory("FCT"), Ignore]
        public async Task ExportZip_GetZipStream()
        {
            var content = "test content 123";
            var id = RecordCreator.RandomId;
            var requestId = ExportStorageProvider.GetNewRequestId();
            var zipHelper = new ExportZipStorageHelper(this.BlobClient, new ConsoleLogger());
            await zipHelper.InitializeAsync(id);
            using (var zipWriter = await zipHelper.WriteZipStreamAsync(requestId))
            {
                var blob = await this.CreateATestBlobAsync(content);
                await zipWriter.WriteEntryAsync(blob.Name, blob);
            }
            Assert.IsTrue(await this.ContainerExistsAsync(ExportStorageProvider.GetIdHash(id)));
            Assert.IsTrue(await this.FileExistsAsync(ExportStorageProvider.GetIdHash(id), ExportZipStorageHelper.GetZipBlobName(requestId)));
            var zipStream = await zipHelper.GetZipStreamAsync(requestId);
            var readString = this.ExtractStringFromZipStream(zipStream, true);
            Assert.AreEqual(content, readString);
        }

        [TestMethod]
        [TestCategory("FCT"), Ignore]
        public async Task ExportZip_GetZipFileSize()
        {
            var content = "test content 123";
            var id = RecordCreator.RandomId;
            var requestId = ExportStorageProvider.GetNewRequestId();
            var zipHelper = new ExportZipStorageHelper(this.BlobClient, new ConsoleLogger());
            await zipHelper.InitializeAsync(id);
            using (var zipWriter = await zipHelper.WriteZipStreamAsync(requestId))
            {
                var blob = await this.CreateATestBlobAsync(content);
                await zipWriter.WriteEntryAsync(blob.Name, blob);
            }
            Console.WriteLine("id " + id + " zip container " + ExportStorageProvider.GetIdHash(id) + " requestid " + requestId);
            Assert.IsTrue(await this.ContainerExistsAsync(ExportStorageProvider.GetIdHash(id)));
            Assert.IsTrue(await this.FileExistsAsync(ExportStorageProvider.GetIdHash(id), ExportZipStorageHelper.GetZipBlobName(requestId)));
            var size = await zipHelper.GetZipFileSizeAsync(requestId);
            Assert.AreEqual(206, size);
        }

        [TestMethod]
        [TestCategory("FCT"), Ignore]
        public async Task ExportZip_GetZipStorageUri()
        {
            var content = "test content 123";
            var id = RecordCreator.RandomId;
            var requestId = ExportStorageProvider.GetNewRequestId();
            var zipHelper = new ExportZipStorageHelper(this.BlobClient, new ConsoleLogger());
            await zipHelper.InitializeAsync(id);
            using (var zipWriter = await zipHelper.WriteZipStreamAsync(requestId))
            {
                var blob = await this.CreateATestBlobAsync(content);
                await zipWriter.WriteEntryAsync(blob.Name, blob);
            }
            Console.WriteLine("id " + id + " zip container " + ExportStorageProvider.GetIdHash(id) + " requestid " + requestId);
            Assert.IsTrue(await this.ContainerExistsAsync(ExportStorageProvider.GetIdHash(id)));
            Assert.IsTrue(await this.FileExistsAsync(ExportStorageProvider.GetIdHash(id), ExportZipStorageHelper.GetZipBlobName(requestId)));
            var zipBlob = zipHelper.GetZipBlob(requestId);
            Assert.AreEqual("https://127.0.0.1:10000/devstoreaccount1/" 
                + ExportStorageProvider.GetIdHash(id) 
                +  "/" 
                + ExportZipStorageHelper.GetZipBlobName(requestId),
                zipBlob.Uri.ToString());
        }

        [TestMethod]
        [TestCategory("FCT"), Ignore]
        public async Task ExportZip_DeleteZipStorage()
        {
            var content = "test content 123";
            var id = RecordCreator.RandomId;
            var requestId = ExportStorageProvider.GetNewRequestId();
            var zipHelper = new ExportZipStorageHelper(this.BlobClient, new ConsoleLogger());
            await zipHelper.InitializeAsync(id);
            using (var zipWriter = await zipHelper.WriteZipStreamAsync(requestId))
            {
                var blob = await this.CreateATestBlobAsync(content);
                await zipWriter.WriteEntryAsync(blob.Name, blob);
            }
            Console.WriteLine("id " + id + " zip container " + ExportStorageProvider.GetIdHash(id) + " requestid " + requestId);
            Assert.IsTrue(await this.ContainerExistsAsync(ExportStorageProvider.GetIdHash(id)));
            Assert.IsTrue(await this.FileExistsAsync(ExportStorageProvider.GetIdHash(id), ExportZipStorageHelper.GetZipBlobName(requestId)));
            var deleted = await zipHelper.DeleteZipStorageAsync(requestId);
            Assert.IsTrue(deleted);
            Assert.IsFalse(await this.FileExistsAsync(ExportStorageProvider.GetIdHash(id), ExportZipStorageHelper.GetZipBlobName(requestId)));
        }

        private async Task<CloudBlockBlob> CreateATestBlobAsync(string content, string name=null)
        {
            var container = this.BlobClient.GetContainerReference("testblobs");
            await container.CreateIfNotExistsAsync();
            if (string.IsNullOrEmpty(name))
            {
                name = RecordCreator.GetRandomAzureBlobName() + ".json";
            }
            var blob = container.GetBlockBlobReference(name);
            await blob.UploadTextAsync(content);
            return blob;
        }
    }
}
