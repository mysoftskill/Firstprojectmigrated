// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.Helpers.Locks
{
    using System;
    using System.Net;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Locks;
    using Microsoft.Azure.Cosmos.Table;

    using Moq;

    using Microsoft.PrivacyServices.Common.Azure;

    [TestClass]
    public class AzureTableLockManagerTests
    {
        private const string LockTable = "LockTable";

        private readonly Mock<IAzureStorageProvider> mockStorage = new Mock<IAzureStorageProvider>();
        private readonly Mock<ICloudTable> mockTable = new Mock<ICloudTable>();
        private readonly Mock<ILogger> mockLog = new Mock<ILogger>();
        private readonly Mock<IClock> mockClock = new Mock<IClock>();

        private readonly TableResult updatetResult = new TableResult();
        private readonly TableResult insertResult = new TableResult();
        private readonly TableResult deleteResult = new TableResult();
        private readonly TableResult getResult = new TableResult();

        private AzureTableLockManager testObj;

        [TestInitialize]
        public void Init()
        {
            this.mockStorage.Setup(o => o.GetCloudTableAsync(It.IsAny<string>())).ReturnsAsync(this.mockTable.Object);
            this.mockTable.Setup(o => o.InsertAsync(It.IsAny<ITableEntity>(), It.IsAny<bool>())).ReturnsAsync(this.insertResult);
            this.mockTable.Setup(o => o.ReplaceAsync(It.IsAny<ITableEntity>(), It.IsAny<bool>())).ReturnsAsync(this.updatetResult);
            this.mockTable.Setup(o => o.DeleteAsync(It.IsAny<ITableEntity>(), It.IsAny<bool>())).ReturnsAsync(this.deleteResult);
            this.mockTable.Setup(
                o => o.QuerySingleRowAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(this.getResult);

            this.mockClock.SetupGet(o => o.UtcNow).Returns(new DateTimeOffset(2006, 04, 15, 15, 0, 0, TimeSpan.Zero));

            this.testObj = new AzureTableLockManager(
                this.mockStorage.Object, this.mockLog.Object, this.mockClock.Object, AzureTableLockManagerTests.LockTable);
        }

        [TestMethod]
        public async Task AcquireFetchesTableFromStorage()
        {
            // test
            await this.testObj.AttemptAcquireAsync("any", "any", "any", TimeSpan.FromHours(1));

            // verify
            this.mockStorage.Verify(o => o.GetCloudTableAsync(AzureTableLockManagerTests.LockTable), Times.Once);
        }

        [TestMethod]
        public async Task AcquireInsertsAndDoesNotUpdateIfLockDoesNotExistAndTriesInsertFirst()
        {
            const string Group = "LockGroup";
            const string Owner = "LockOwner";
            const string Name = "LockName";

            TimeSpan duration = TimeSpan.FromSeconds(20060415);
            DateTime expectedDate = this.mockClock.Object.UtcNow.Add(duration).DateTime;

            Func<ITableEntity, bool> verifer = 
                o =>
                {
                    AzureTableLockEntry e = (AzureTableLockEntry)o;
                    Assert.AreEqual(Group, e.PartitionKey);
                    Assert.AreEqual(Name, e.RowKey);
                    Assert.AreEqual(Owner, e.OwnerTaskId);
                    Assert.AreEqual(expectedDate, e.LockExpires);
                    return true;
                };

            ILockLease result;

            this.insertResult.Result = new AzureTableLockEntry();
            this.insertResult.HttpStatusCode = 200;

            // test
            result = await this.testObj.AttemptAcquireAsync(Group, Name, Owner, duration);

            // verify
            Assert.IsNotNull(result);
            this.mockTable.Verify(o => o.InsertAsync(It.Is<ITableEntity>(p => verifer(p)), true), Times.Once);
            this.mockTable.Verify(o => o.ReplaceAsync(It.IsAny<ITableEntity>(), It.IsAny<bool>()), Times.Never);
            this.mockTable.Verify(
                o => o.QuerySingleRowAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        [TestMethod]
        public async Task AcquireInsertsAndDoesNotUpdateIfLockDoesNotExistAndTriesGetFirst()
        {
            const string Group = "LockGroup";
            const string Owner = "LockOwner";
            const string Name = "LockName";

            TimeSpan duration = TimeSpan.FromSeconds(20060415);
            DateTime expectedDate = this.mockClock.Object.UtcNow.Add(duration).DateTime;

            Func<ITableEntity, bool> verifer =
                o =>
                {
                    AzureTableLockEntry e = (AzureTableLockEntry)o;
                    Assert.AreEqual(Group, e.PartitionKey);
                    Assert.AreEqual(Name, e.RowKey);
                    Assert.AreEqual(Owner, e.OwnerTaskId);
                    Assert.AreEqual(expectedDate, e.LockExpires);
                    return true;
                };

            ILockLease result;

            this.insertResult.Result = new AzureTableLockEntry();
            this.insertResult.HttpStatusCode = 200;

            // test
            result = await this.testObj.AttemptAcquireAsync(Group, Name, Owner, duration, true);

            // verify
            Assert.IsNotNull(result);
            this.mockTable.Verify(o => o.InsertAsync(It.Is<ITableEntity>(p => verifer(p)), true), Times.Once);
            this.mockTable.Verify(o => o.QuerySingleRowAsync(Group, Name, true), Times.Once);
            this.mockTable.Verify(o => o.ReplaceAsync(It.IsAny<ITableEntity>(), It.IsAny<bool>()), Times.Never);
        }

        [TestMethod]
        public async Task AcquireGetsAndDoesNotInsertIfLockExist()
        {
            const string Group = "LockGroup";
            const string Owner = "LockOwner";
            const string Name = "LockName";

            TimeSpan duration = TimeSpan.FromSeconds(20060415);
            ILockLease result;

            this.getResult.HttpStatusCode = 200;
            this.getResult.Result = new AzureTableLockEntry { PartitionKey = Group, RowKey = Name, LockExpires = DateTime.MinValue };
            this.updatetResult.HttpStatusCode = this.getResult.HttpStatusCode;
            this.updatetResult.Result = this.getResult.Result;

            // test
            result = await this.testObj.AttemptAcquireAsync(Group, Name, Owner, duration, true);

            // verify
            Assert.IsNotNull(result);
            this.mockTable.Verify(o => o.InsertAsync(It.IsAny<ITableEntity>(), true), Times.Never);
            this.mockTable.Verify(o => o.QuerySingleRowAsync(Group, Name, true), Times.Once);
        }

        [TestMethod]
        public async Task AcquireUpdatesIfFetchedLockAndLockIsNotOwned()
        {
            const string Group = "LockGroup";
            const string Owner = "LockOwner";
            const string Name = "LockName";

            TimeSpan duration = TimeSpan.FromSeconds(20060415);
            DateTime expectedDate = this.mockClock.Object.UtcNow.Add(duration).DateTime;
            ILockLease result;

            Func<ITableEntity, bool> verifer =
                o =>
                {
                    AzureTableLockEntry e = (AzureTableLockEntry)o;
                    Assert.AreEqual(Group, e.PartitionKey);
                    Assert.AreEqual(Name, e.RowKey);
                    Assert.AreEqual(Owner, e.OwnerTaskId);
                    Assert.AreEqual(expectedDate, e.LockExpires);
                    return true;
                };

            this.getResult.HttpStatusCode = 200;
            this.getResult.Result = new AzureTableLockEntry { PartitionKey = Group, RowKey = Name, LockExpires = DateTime.MinValue };
            this.updatetResult.HttpStatusCode = this.getResult.HttpStatusCode;
            this.updatetResult.Result = this.getResult.Result;

            // test
            result = await this.testObj.AttemptAcquireAsync(Group, Name, Owner, duration, true);

            // verify
            Assert.IsNotNull(result);
            this.mockTable.Verify(o => o.QuerySingleRowAsync(Group, Name, true), Times.Once);
            this.mockTable.Verify(o => o.ReplaceAsync(It.Is<ITableEntity>(p => verifer(p)), It.IsAny<bool>()), Times.Once);
        }

        [TestMethod]
        public async Task AcquireUpdatesIfFetchedLockAndLockOwnedBySelf()
        {
            const string Group = "LockGroup";
            const string Owner = "LockOwner";
            const string Name = "LockName";

            TimeSpan duration = TimeSpan.FromSeconds(20060415);
            DateTime expectedDate = this.mockClock.Object.UtcNow.Add(duration).DateTime;
            DateTime expiry = this.mockClock.Object.UtcNow.AddMinutes(1).DateTime;
            ILockLease result;

            Func<ITableEntity, bool> verifer =
                o =>
                {
                    AzureTableLockEntry e = (AzureTableLockEntry)o;
                    Assert.AreEqual(Group, e.PartitionKey);
                    Assert.AreEqual(Name, e.RowKey);
                    Assert.AreEqual(Owner, e.OwnerTaskId);
                    Assert.AreEqual(expectedDate, e.LockExpires);
                    return true;
                };

            this.getResult.HttpStatusCode = 200;
            this.getResult.Result = new AzureTableLockEntry { PartitionKey = Group, RowKey = Name, LockExpires = expiry, OwnerTaskId = Owner };
            this.updatetResult.HttpStatusCode = this.getResult.HttpStatusCode;
            this.updatetResult.Result = this.getResult.Result;

            // test
            result = await this.testObj.AttemptAcquireAsync(Group, Name, Owner, duration, true);

            // verify
            Assert.IsNotNull(result);
            this.mockTable.Verify(o => o.QuerySingleRowAsync(Group, Name, true), Times.Once);
            this.mockTable.Verify(o => o.ReplaceAsync(It.Is<ITableEntity>(p => verifer(p)), It.IsAny<bool>()), Times.Once);
        }

        [TestMethod]
        public async Task AcquireUpdatesIfFetchedLockAndLockOwnedByOtherButIsExpired()
        {
            const string Group = "LockGroup";
            const string Owner = "LockOwner";
            const string Name = "LockName";

            TimeSpan duration = TimeSpan.FromSeconds(20060415);
            DateTime expectedDate = this.mockClock.Object.UtcNow.Add(duration).DateTime;
            DateTime expiry = this.mockClock.Object.UtcNow.AddMinutes(-1).DateTime;
            ILockLease result;

            Func<ITableEntity, bool> verifer =
                o =>
                {
                    AzureTableLockEntry e = (AzureTableLockEntry)o;
                    Assert.AreEqual(Group, e.PartitionKey);
                    Assert.AreEqual(Name, e.RowKey);
                    Assert.AreEqual(Owner, e.OwnerTaskId);
                    Assert.AreEqual(expectedDate, e.LockExpires);
                    return true;
                };

            this.getResult.HttpStatusCode = 200;
            this.getResult.Result = new AzureTableLockEntry { PartitionKey = Group, RowKey = Name, LockExpires = expiry, OwnerTaskId = "NOPE" };
            this.updatetResult.HttpStatusCode = this.getResult.HttpStatusCode;
            this.updatetResult.Result = this.getResult.Result;

            // test
            result = await this.testObj.AttemptAcquireAsync(Group, Name, Owner, duration, true);

            // verify
            Assert.IsNotNull(result);
            this.mockTable.Verify(o => o.QuerySingleRowAsync(Group, Name, true), Times.Once);
            this.mockTable.Verify(o => o.ReplaceAsync(It.Is<ITableEntity>(p => verifer(p)), It.IsAny<bool>()), Times.Once);
        }

        [TestMethod]
        public async Task AcquireReturnsNullIfFetchedLockAndLockOwnedByOtherAndNotExpired()
        {
            const string Group = "LockGroup";
            const string Owner = "LockOwner";
            const string Name = "LockName";

            TimeSpan duration = TimeSpan.FromSeconds(20060415);
            DateTime expiry = this.mockClock.Object.UtcNow.AddMinutes(1).DateTime;
            ILockLease result;

            this.getResult.HttpStatusCode = 200;
            this.getResult.Result = new AzureTableLockEntry { PartitionKey = Group, RowKey = Name, LockExpires = expiry, OwnerTaskId = "NOPE" };
            this.updatetResult.HttpStatusCode = this.getResult.HttpStatusCode;
            this.updatetResult.Result = this.getResult.Result;

            // test
            result = await this.testObj.AttemptAcquireAsync(Group, Name, Owner, duration, true);

            // verify
            Assert.IsNull(result);
            this.mockTable.Verify(o => o.QuerySingleRowAsync(Group, Name, true), Times.Once);
            this.mockTable.Verify(o => o.ReplaceAsync(It.IsAny<ITableEntity>(), It.IsAny<bool>()), Times.Never);
        }
        
        [TestMethod]
        public async Task LeaseRenewReturnsTrueIfUpdatedLock()
        {
            const string Group = "LockGroup";
            const string Owner = "LockOwner";
            const string Name = "LockName";

            TimeSpan duration = TimeSpan.FromSeconds(20060415);
            DateTime expectedDate = this.mockClock.Object.UtcNow.Add(duration).DateTime;
            DateTime expiry = this.mockClock.Object.UtcNow.AddMinutes(-1).DateTime;

            Func<ITableEntity, bool> verifer =
                o =>
                {
                    AzureTableLockEntry e = (AzureTableLockEntry)o;
                    Assert.AreEqual(Group, e.PartitionKey);
                    Assert.AreEqual(Name, e.RowKey);
                    Assert.AreEqual(Owner, e.OwnerTaskId);
                    Assert.AreEqual(expectedDate, e.LockExpires);
                    return true;
                };

            ILockLease lease;
            bool result;

            this.getResult.HttpStatusCode = 200;
            this.getResult.Result = new AzureTableLockEntry { PartitionKey = Group, RowKey = Name, LockExpires = expiry, OwnerTaskId = "NONE" };
            this.updatetResult.HttpStatusCode = this.getResult.HttpStatusCode;
            this.updatetResult.Result = this.getResult.Result;

            lease = await this.testObj.AttemptAcquireAsync(Group, Name, Owner, duration, true);

            this.mockTable.Invocations.Clear();

            // test
            result = await lease.RenewAsync(duration);

            // verify
            Assert.IsTrue(result);
            this.mockTable.Verify(o => o.ReplaceAsync(It.Is<ITableEntity>(p => verifer(p)), It.IsAny<bool>()), Times.Once);
        }

        [TestMethod]
        public async Task LeaseRenewReturnsFalseIfFailedToUpdateLock()
        {
            const string Group = "LockGroup";
            const string Owner = "LockOwner";
            const string Name = "LockName";

            TimeSpan duration = TimeSpan.FromSeconds(20060415);
            DateTime expectedDate = this.mockClock.Object.UtcNow.Add(duration).DateTime;
            DateTime expiry = this.mockClock.Object.UtcNow.AddMinutes(-1).DateTime;

            Func<ITableEntity, bool> verifer =
                o =>
                {
                    AzureTableLockEntry e = (AzureTableLockEntry)o;
                    Assert.AreEqual(Group, e.PartitionKey);
                    Assert.AreEqual(Name, e.RowKey);
                    Assert.AreEqual(Owner, e.OwnerTaskId);
                    Assert.AreEqual(expectedDate, e.LockExpires);
                    return true;
                };

            ILockLease lease;
            bool result;

            this.getResult.HttpStatusCode = 200;
            this.getResult.Result = new AzureTableLockEntry { PartitionKey = Group, RowKey = Name, LockExpires = expiry, OwnerTaskId = "NONE" };
            this.updatetResult.HttpStatusCode = this.getResult.HttpStatusCode;
            this.updatetResult.Result = this.getResult.Result;

            lease = await this.testObj.AttemptAcquireAsync(Group, Name, Owner, duration, true);

            this.updatetResult.HttpStatusCode = (int)HttpStatusCode.PreconditionFailed;
            this.updatetResult.Result = null;

            this.mockTable.Invocations.Clear();

            // test
            result = await lease.RenewAsync(duration);

            // verify
            Assert.IsFalse(result);
            this.mockTable.Verify(o => o.ReplaceAsync(It.Is<ITableEntity>(p => verifer(p)), It.IsAny<bool>()), Times.Once);
        }

        [TestMethod]
        public async Task LeaseReleaseUpdatesIfNotInPurgeMode()
        {
            const string Group = "LockGroup";
            const string Owner = "LockOwner";
            const string Name = "LockName";

            TimeSpan duration = TimeSpan.FromSeconds(20060415);
            DateTime expiry = this.mockClock.Object.UtcNow.AddMinutes(-1).DateTime;

            Func<ITableEntity, bool> verifer =
                o =>
                {
                    AzureTableLockEntry e = (AzureTableLockEntry)o;
                    Assert.AreEqual(Group, e.PartitionKey);
                    Assert.AreEqual(Name, e.RowKey);
                    Assert.IsTrue(string.IsNullOrEmpty(e.OwnerTaskId));
                    Assert.AreEqual(AzureTableLockManager.UnownedExpiryTime, e.LockExpires);
                    return true;
                };

            ILockLease lease;

            this.getResult.HttpStatusCode = 200;
            this.getResult.Result = new AzureTableLockEntry { PartitionKey = Group, RowKey = Name, LockExpires = expiry, OwnerTaskId = "NONE" };
            this.updatetResult.HttpStatusCode = this.getResult.HttpStatusCode;
            this.updatetResult.Result = this.getResult.Result;

            lease = await this.testObj.AttemptAcquireAsync(Group, Name, Owner, duration, true);

            this.mockTable.Invocations.Clear();

            // test
            await lease.ReleaseAsync(false);

            // verify
            this.mockTable.Verify(o => o.ReplaceAsync(It.Is<ITableEntity>(p => verifer(p)), It.IsAny<bool>()), Times.Once);
            this.mockTable.Verify(o => o.DeleteAsync(It.IsAny<ITableEntity>(), It.IsAny<bool>()), Times.Never);
        }

        [TestMethod]
        public async Task LeaseReleaseDeletesIfInPurgeMode()
        {
            const string Group = "LockGroup";
            const string Owner = "LockOwner";
            const string Name = "LockName";

            TimeSpan duration = TimeSpan.FromSeconds(20060415);
            DateTime expiry = this.mockClock.Object.UtcNow.AddMinutes(-1).DateTime;

            Func<ITableEntity, bool> verifer =
                o =>
                {
                    AzureTableLockEntry e = (AzureTableLockEntry)o;
                    Assert.AreEqual(Group, e.PartitionKey);
                    Assert.AreEqual(Name, e.RowKey);
                    return true;
                };

            ILockLease lease;

            this.getResult.HttpStatusCode = 200;
            this.getResult.Result = new AzureTableLockEntry { PartitionKey = Group, RowKey = Name, LockExpires = expiry, OwnerTaskId = "NONE" };
            this.updatetResult.HttpStatusCode = this.getResult.HttpStatusCode;
            this.updatetResult.Result = this.getResult.Result;

            lease = await this.testObj.AttemptAcquireAsync(Group, Name, Owner, duration, true);

            this.mockTable.Invocations.Clear();

            // test
            await lease.ReleaseAsync(true);

            // verify
            this.mockTable.Verify(o => o.ReplaceAsync(It.IsAny<ITableEntity>(), It.IsAny<bool>()), Times.Never);
            this.mockTable.Verify(o => o.DeleteAsync(It.Is<ITableEntity>(p => verifer(p)), It.IsAny<bool>()), Times.Once);
        }
    }
}
