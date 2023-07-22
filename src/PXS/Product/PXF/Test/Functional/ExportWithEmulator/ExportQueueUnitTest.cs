// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.FunctionalTests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.ExportStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.DataContracts.ExportTypes;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ExportQueueUnitTest : StorageEmulatorBase
    {
        [TestMethod]
        [TestCategory("FCT"), Ignore]
        public async Task ExportQueue_PostAndGet()
        {
            var queue = new ExportQueue(this.QueueClient, "testqueue");
            await queue.InitializeAsync(new TimeSpan(0, 1, 0), new TimeSpan(0, 0, 3), new TimeSpan(0, 0, 5)).ConfigureAwait(false);
            await queue.ClearMessagesAsync().ConfigureAwait(false);
            var baseQueueMsg = new BaseQueueMessage
            {
                Action = "ExportTask" + Guid.NewGuid(),
                RequestId = ExportStorageProvider.GetNewRequestId()
            };
            await queue.AddMessageAsync(baseQueueMsg).ConfigureAwait(false);
            BaseQueueMessage msg = await queue.GetMessageAsync().ConfigureAwait(false);
            Assert.IsNotNull(msg);
            Assert.AreEqual(baseQueueMsg.Action, msg.Action);
            Assert.AreEqual(baseQueueMsg.RequestId, msg.RequestId);
            await queue.CompleteMessageAsync(msg).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("FCT"), Ignore]
        public async Task ExportQueue_PostAndRepeatGetAfterVisibilityTime()
        {
            var queue = new ExportQueue(this.QueueClient, "testqueue");
            await queue.InitializeAsync(new TimeSpan(0, 1, 0), new TimeSpan(0, 0, 3), new TimeSpan(0, 0, 5)).ConfigureAwait(false);
            await queue.ClearMessagesAsync().ConfigureAwait(false);
            var baseQueueMsg = new BaseQueueMessage
            {
                Action = "ExportTask" + Guid.NewGuid(),
                RequestId = ExportStorageProvider.GetNewRequestId()
            };
            await queue.AddMessageAsync(baseQueueMsg).ConfigureAwait(false);
            BaseQueueMessage msg = await queue.GetMessageAsync().ConfigureAwait(false);
            Assert.IsNotNull(msg);
            Assert.AreEqual(baseQueueMsg.Action, msg.Action);
            Assert.AreEqual(baseQueueMsg.RequestId, msg.RequestId);
            Thread.Sleep(3100);
            BaseQueueMessage msg2 = await queue.GetMessageAsync().ConfigureAwait(false);
            Assert.IsNotNull(msg2);
            Assert.AreEqual(baseQueueMsg.Action, msg2.Action);
            Assert.AreEqual(baseQueueMsg.RequestId, msg2.RequestId);
            await queue.CompleteMessageAsync(msg2).ConfigureAwait(false);
        }

        [TestInitialize]
        public void Init()
        {
            this.mockAzureStorageConfiguration.SetupGet(c => c.UseEmulator).Returns(true);
            this.StartEmulator();
        }
    }
}
