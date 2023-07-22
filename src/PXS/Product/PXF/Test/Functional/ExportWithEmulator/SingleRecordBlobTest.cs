// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.FunctionalTests
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Privacy.DataContracts.ExportTypes;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.ExportStorageHelper;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Azure.Storage;

    

    [TestClass]
    public class SingleRecordBlobTest : StorageEmulatorBase
    {
        [TestInitialize]
        public void Init()
        {
            this.mockAzureStorageConfiguration.SetupGet(c => c.UseEmulator).Returns(true);
            this.StartEmulator();
        }

        [TestMethod]
        [TestCategory("FCT"), Ignore]
        public async Task ExportSingleRecordBlob_CreateAndGet()
        {
            var statusRecord = RecordCreator.CreateStatusRecord();
            var singleRecordBlob = new SingleRecordBlobHelper<ExportStatusRecord>(
                this.BlobClient,
                "test" + Guid.NewGuid().ToString("N").ToLowerInvariant(),
                new ConsoleLogger());
            await singleRecordBlob.InitializeAsync(statusRecord.ExportId);
            await singleRecordBlob.CreateRecordAsync(statusRecord);
            var getStatusRecord = await singleRecordBlob.GetRecordAsync(true);
            Assert.IsNotNull(getStatusRecord);
            EqualityWithDataContractsHelper.AreEqual(statusRecord, getStatusRecord);
        }

        [TestMethod]
        [TestCategory("FCT"), Ignore]
        public async Task ExportSingleRecordBlob_GetNotFoundReturnsNull()
        {
            var singleRecordBlob = new SingleRecordBlobHelper<ExportStatusRecord>(
                this.BlobClient,
                "test" + Guid.NewGuid().ToString("N").ToLowerInvariant(),
                new ConsoleLogger());
            await singleRecordBlob.InitializeAsync(ExportStorageProvider.GetNewRequestId());
            var rec = await singleRecordBlob.GetRecordAsync(true);
            Assert.IsNull(rec);
        }


        [TestMethod]
        [TestCategory("FCT"), Ignore]
        public async Task ExportSingleRecordBlob_GetNotFoundThrows()
        {
            var singleRecordBlob = new SingleRecordBlobHelper<ExportStatusRecord>(
                this.BlobClient,
                "test" + Guid.NewGuid().ToString("N").ToLowerInvariant(),
                new ConsoleLogger());
            await singleRecordBlob.InitializeAsync(ExportStorageProvider.GetNewRequestId());
            try
            {
                var rec = await singleRecordBlob.GetRecordAsync(false);
            }
            catch (StorageException ex)
            {
                Assert.AreEqual((int)HttpStatusCode.NotFound, ex.RequestInformation.HttpStatusCode);
                return;
            }
            Assert.Fail("Get with false param should throw");
        }

        [TestMethod]
        [TestCategory("FCT"), Ignore]
        public async Task ExportSingleRecordBlob_SimpleUpdate()
        {
            var statusRecord = RecordCreator.CreateStatusRecord();
            var singleRecordBlob = new SingleRecordBlobHelper<ExportStatusRecord>(
                this.BlobClient,
                "test" + Guid.NewGuid().ToString("N").ToLowerInvariant(),
                new ConsoleLogger());
            await singleRecordBlob.InitializeAsync(statusRecord.ExportId);
            await singleRecordBlob.CreateRecordAsync(statusRecord);
            var getStatusRecord = await singleRecordBlob.GetRecordAsync(true);
            Assert.IsNotNull(getStatusRecord);
            EqualityWithDataContractsHelper.AreEqual(statusRecord, getStatusRecord);
            getStatusRecord.IsComplete = true;
            getStatusRecord.LastSessionEnd = DateTimeOffset.UtcNow;
            var etag = await singleRecordBlob.UpsertRecordAsync(getStatusRecord);
            var getUpdatedStatusRecord = await singleRecordBlob.GetRecordAsync(true);
            EqualityWithDataContractsHelper.AreEqual(getStatusRecord, getUpdatedStatusRecord);
        }

        [TestMethod]
        [TestCategory("FCT"), Ignore]
        public async Task ExportSingleRecordBlob_EtagPreventsUpdate()
        {
            string containerName = "test" + Guid.NewGuid().ToString("N").ToLowerInvariant();
            Console.WriteLine("container is " + containerName);
            var statusRecord = RecordCreator.CreateStatusRecord();
            var singleRecordBlob = new SingleRecordBlobHelper<ExportStatusRecord>(this.BlobClient, containerName, new ConsoleLogger());
            await singleRecordBlob.InitializeAsync(statusRecord.ExportId);
            await singleRecordBlob.CreateRecordAsync(statusRecord);
            var getStatusRecord = await singleRecordBlob.GetRecordAsync(true);
            Assert.IsNotNull(getStatusRecord);
            EqualityWithDataContractsHelper.AreEqual(statusRecord, getStatusRecord);
            getStatusRecord.IsComplete = true;
            getStatusRecord.LastSessionEnd = DateTimeOffset.UtcNow;
            Console.WriteLine("actor 1 etag for " + containerName + "/" + statusRecord.ExportId);

            //==== 2nd actor updates the record
            var singleRecordBlob2 = new SingleRecordBlobHelper<ExportStatusRecord>(this.BlobClient, containerName, new ConsoleLogger());
            await singleRecordBlob2.InitializeAsync(statusRecord.ExportId);
            var getStatusRecord2 = await singleRecordBlob2.GetRecordAsync(true);
            getStatusRecord2.IsComplete = true;
            getStatusRecord2.LastSessionEnd = DateTimeOffset.UtcNow;
            getStatusRecord2.LastError = "2nd Actor sets error";
            Console.WriteLine("actor 2 etag for " + containerName + "/" + statusRecord.ExportId);
            var etagFromActor2 = await singleRecordBlob2.UpsertRecordAsync(getStatusRecord2);
            Console.WriteLine("actor 2 after update " + etagFromActor2);

            //=====
            try
            {
                Console.WriteLine("actor 1 etag before update for " + containerName + "/" + statusRecord.ExportId);
                var etag = await singleRecordBlob.UpsertRecordAsync(getStatusRecord);
                Console.WriteLine("etag after actor 1 update " + etag);
            }
            catch (StorageException ex)
            {
                Console.WriteLine("etag prvented actor 1 from updating the blob");
                Assert.AreEqual((int)HttpStatusCode.PreconditionFailed, ex.RequestInformation.HttpStatusCode);
                return;
            }
            Assert.Fail("2nd update should get an exception");
        }

        [TestMethod]
        [TestCategory("FCT"), Ignore]
        public async Task ExportSingleRecordBlob_EtagPrevents2ndCreate()
        {
            string containerName = "test" + Guid.NewGuid().ToString("N").ToLowerInvariant();
            Console.WriteLine("container is " + containerName);
            var statusRecord = RecordCreator.CreateStatusRecord();
            var singleRecordBlob = new SingleRecordBlobHelper<ExportStatusRecord>(this.BlobClient, containerName, new ConsoleLogger());
            await singleRecordBlob.InitializeAsync(statusRecord.ExportId);
            await singleRecordBlob.CreateRecordAsync(statusRecord);
            var getStatusRecord = await singleRecordBlob.GetRecordAsync(true);
            Assert.IsNotNull(getStatusRecord);
            EqualityWithDataContractsHelper.AreEqual(statusRecord, getStatusRecord);
            getStatusRecord.IsComplete = true;
            getStatusRecord.LastSessionEnd = DateTimeOffset.UtcNow;
            Console.WriteLine("actor 1 etag for " + containerName + "/" + statusRecord.ExportId);

            //==== 2nd actor also tries to create the record
            var singleRecordBlob2 = new SingleRecordBlobHelper<ExportStatusRecord>(this.BlobClient, containerName, new ConsoleLogger());
            await singleRecordBlob2.InitializeAsync(statusRecord.ExportId);
            try
            {
                var etag = await singleRecordBlob2.CreateRecordAsync(statusRecord);
            }
            catch (StorageException ex)
            {
                Console.WriteLine("etag prvented actor 2 from creating the blob over the existing one");
                Assert.AreEqual((int)HttpStatusCode.Conflict, ex.RequestInformation.HttpStatusCode);
                return;
            }
            Assert.Fail("2nd create should get an exception");
        }

        [TestMethod]
        [TestCategory("FCT"), Ignore]
        public async Task ExportSingleRecordBlob_SimpleDelete()
        {
            string containerName = "test" + Guid.NewGuid().ToString("N").ToLowerInvariant();
            Console.WriteLine("container is " + containerName);
            var statusRecord = RecordCreator.CreateStatusRecord();
            var singleRecordBlob = new SingleRecordBlobHelper<ExportStatusRecord>(this.BlobClient, containerName, new ConsoleLogger());
            await singleRecordBlob.InitializeAsync(statusRecord.ExportId);
            await singleRecordBlob.CreateRecordAsync(statusRecord);
            var getStatusRecord = await singleRecordBlob.GetRecordAsync(true);
            Assert.IsNotNull(getStatusRecord);
            EqualityWithDataContractsHelper.AreEqual(statusRecord, getStatusRecord);
            getStatusRecord.IsComplete = true;
            getStatusRecord.LastSessionEnd = DateTimeOffset.UtcNow;
            Console.WriteLine("actor 1 etag for " + containerName + "/" + statusRecord.ExportId);

            var singleRecordBlob2 = new SingleRecordBlobHelper<ExportStatusRecord>(this.BlobClient, containerName, new ConsoleLogger());
            await singleRecordBlob2.InitializeAsync(statusRecord.ExportId);
            var deleted = await singleRecordBlob2.DeleteRecordAsync();
            Assert.AreEqual(true, deleted);
            try
            {
                await singleRecordBlob2.GetRecordAsync(false);
            }
            catch (StorageException ex)
            {
                Assert.AreEqual((int)HttpStatusCode.NotFound, ex.RequestInformation.HttpStatusCode);
                return;
            }
            Assert.Fail("should be not found after delete");
        }

        [TestMethod]
        [TestCategory("FCT"), Ignore]
        public async Task ExportSingleRecordBlob_SimpleInitializeAndDelete()
        {
            string containerName = "test" + Guid.NewGuid().ToString("N").ToLowerInvariant();
            Console.WriteLine("container is " + containerName);
            var statusRecord = RecordCreator.CreateStatusRecord();
            var singleRecordBlob = new SingleRecordBlobHelper<ExportStatusRecord>(this.BlobClient, containerName, new ConsoleLogger());
            await singleRecordBlob.InitializeAsync(statusRecord.ExportId);
            await singleRecordBlob.CreateRecordAsync(statusRecord);
            var getStatusRecord = await singleRecordBlob.GetRecordAsync(true);
            Assert.IsNotNull(getStatusRecord);
            EqualityWithDataContractsHelper.AreEqual(statusRecord, getStatusRecord);
            getStatusRecord.IsComplete = true;
            getStatusRecord.LastSessionEnd = DateTimeOffset.UtcNow;
            Console.WriteLine("actor 1 etag for " + containerName + "/" + statusRecord.ExportId);

            var singleRecordBlob2 = new SingleRecordBlobHelper<ExportStatusRecord>(this.BlobClient, containerName, new ConsoleLogger());
            await singleRecordBlob2.InitializeAsync(null);
            var deleted = await singleRecordBlob2.InitializeAndDeleteAsync(statusRecord.ExportId);
            Assert.AreEqual(true, deleted);
            try
            {
                await singleRecordBlob2.GetRecordAsync(false);
            }
            catch (StorageException ex)
            {
                Assert.AreEqual((int)HttpStatusCode.NotFound, ex.RequestInformation.HttpStatusCode);
                return;
            }
            Assert.Fail("should be not found after delete");
        }


        [TestMethod]
        [TestCategory("FCT"), Ignore]
        public async Task ExportSingleRecordBlob_DeleteContainerAndEverythingUnderIt()
        {
            string containerName = "test" + Guid.NewGuid().ToString("N").ToLowerInvariant();
            Console.WriteLine("container is " + containerName);
            var singleRecordBlob = new SingleRecordBlobHelper<ExportStatusRecord>(this.BlobClient, containerName, new ConsoleLogger());
            var recs = new List<ExportStatusRecord>();
            for (int i=0; i<5; i++)
            {
                var statusRecord = RecordCreator.CreateStatusRecord();
                recs.Add(statusRecord);
                await singleRecordBlob.InitializeAsync(statusRecord.ExportId);
                await singleRecordBlob.CreateRecordAsync(statusRecord);
            }

            var singleRecordBlob2 = new SingleRecordBlobHelper<ExportStatusRecord>(this.BlobClient, containerName, new ConsoleLogger());
            var deleted = await singleRecordBlob2.DeleteContainerAsync();
            Assert.AreEqual(true, deleted);

            foreach(var rec in recs)
            {
                await singleRecordBlob.InitializeAsync(rec.ExportId);
                var getRec = await singleRecordBlob.GetRecordAsync(true);
                Assert.IsNull(getRec);
            }
        }

        [TestMethod]
        [TestCategory("FCT"), Ignore]
        public async Task ExportSingleRecordBlob_SimpleListAsc()
        {
            string containerName = "test" + Guid.NewGuid().ToString("N").ToLowerInvariant();
            Console.WriteLine("container is " + containerName);
            var singleRecordBlob = new SingleRecordBlobHelper<ExportStatusRecord>(this.BlobClient, containerName, new ConsoleLogger());
            var recs = new List<ExportStatusRecord>();
            for (int i = 0; i < 100; i++)
            {
                var statusRecord = RecordCreator.CreateStatusRecord();
                recs.Add(statusRecord);
                await singleRecordBlob.InitializeAsync(statusRecord.ExportId);
                await singleRecordBlob.CreateRecordAsync(statusRecord);
            }

            for (int i = 0; i < 200; i++)
            {
                var statusRecord = RecordCreator.CreateStatusRecord();
                await singleRecordBlob.InitializeAsync(statusRecord.ExportId);
                await singleRecordBlob.CreateRecordAsync(statusRecord);
            }

            var singleRecordBlob2 = new SingleRecordBlobHelper<ExportStatusRecord>(this.BlobClient, containerName, new ConsoleLogger());
            await singleRecordBlob2.InitializeAsync(null);
            var listedRecs = await singleRecordBlob2.ListRecordsAscendingAsync(null, 100);
            Assert.IsNotNull(listedRecs);
            Assert.AreEqual(recs.Count, listedRecs.Count);
            for (int i=0; i<100; i++)
            {
                EqualityWithDataContractsHelper.AreEqual(recs[i], listedRecs[i]);
            }
        }

        [TestMethod]
        [TestCategory("FCT"), Ignore]
        public async Task ExportSingleRecordBlob_SimpleListDesc()
        {
            string containerName = "test" + Guid.NewGuid().ToString("N").ToLowerInvariant();
            Console.WriteLine("container is " + containerName);
            var singleRecordBlob = new SingleRecordBlobHelper<ExportStatusRecord>(this.BlobClient, containerName, new ConsoleLogger());
            var recs = new List<ExportStatusRecord>();
            for (int i = 0; i < 200; i++)
            {
                var statusRecord = RecordCreator.CreateStatusRecord();
                await singleRecordBlob.InitializeAsync(statusRecord.ExportId);
                await singleRecordBlob.CreateRecordAsync(statusRecord);
            }
            for (int i = 0; i < 100; i++)
            {
                var statusRecord = RecordCreator.CreateStatusRecord();
                recs.Add(statusRecord);
                await singleRecordBlob.InitializeAsync(statusRecord.ExportId);
                await singleRecordBlob.CreateRecordAsync(statusRecord);
            }

            var singleRecordBlob2 = new SingleRecordBlobHelper<ExportStatusRecord>(this.BlobClient, containerName, new ConsoleLogger());
            await singleRecordBlob2.InitializeAsync(null);
            var listedRecs = await singleRecordBlob2.ListRecordsDescendingAsync(null, 100);
            Assert.IsNotNull(listedRecs);
            Assert.AreEqual(recs.Count, listedRecs.Count);
            for (int i = 0; i < 100; i++)
            {
                EqualityWithDataContractsHelper.AreEqual(recs[99 - i], listedRecs[i]);
            }
        }

        [TestMethod]
        [TestCategory("FCT"), Ignore]
        public async Task ExportSingleRecordBlob_SimpleListDescFewAvailable()
        {
            string containerName = "test" + Guid.NewGuid().ToString("N").ToLowerInvariant();
            Console.WriteLine("container is " + containerName);
            var singleRecordBlob = new SingleRecordBlobHelper<ExportStatusRecord>(this.BlobClient, containerName, new ConsoleLogger());
            var recs = new List<ExportStatusRecord>();
            for (int i = 0; i < 20; i++)
            {
                var statusRecord = RecordCreator.CreateStatusRecord();
                recs.Add(statusRecord);
                await singleRecordBlob.InitializeAsync(statusRecord.ExportId);
                await singleRecordBlob.CreateRecordAsync(statusRecord);
            }

            var singleRecordBlob2 = new SingleRecordBlobHelper<ExportStatusRecord>(this.BlobClient, containerName, new ConsoleLogger());
            await singleRecordBlob2.InitializeAsync(null);
            var listedRecs = await singleRecordBlob2.ListRecordsDescendingAsync(null, 100);
            Assert.IsNotNull(listedRecs);
            Assert.AreEqual(recs.Count, listedRecs.Count);
            for (int i = 0; i < 20; i++)
            {
                EqualityWithDataContractsHelper.AreEqual(recs[19 - i], listedRecs[i]);
            }
        }
    }
}
