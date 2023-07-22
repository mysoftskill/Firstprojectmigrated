// -------------------------------------------------------------------------
// <copyright>
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.Helpers.Scheduler
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Scheduler;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Scheduler.Interfaces;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class DailyWorkerBaseTests
    {
        private ILogger logger = new ConsoleLogger();
        
        [TestMethod]
        public async Task CheckForWorkInsertSucceeds()
        {
            var mockStorage = new Mock<IDailyWorkerTrackingStorage>(MockBehavior.Strict);
            mockStorage
                .Setup(s => s.InsertAsync(It.IsAny<DailyWorkerTracking>()))
                .Returns<DailyWorkerTracking>(t => Task.FromResult(new DailyTrackerResult { CurrentRowValue = t, InsertOrUpdateSuccess = true }));

            var worker = new NoWorkDailyWorker(mockStorage.Object, this.logger);
            var result = await worker.CheckForWorkAsync(DateTime.UtcNow, null, new CancellationToken());
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Success);
            Assert.IsTrue(result.WorkReady);
        }

        [TestMethod]
        public async Task CheckForWorkAlreadyCompleted()
        {
            var mockStorage = new Mock<IDailyWorkerTrackingStorage>(MockBehavior.Strict);
            mockStorage
                .Setup(s => s.InsertAsync(It.IsAny<DailyWorkerTracking>()))
                .Returns<DailyWorkerTracking>(t =>
                {
                    t.WorkCompleted = true;
                    return Task.FromResult(new DailyTrackerResult { CurrentRowValue = t, InsertOrUpdateSuccess = false });
                });

            var worker = new NoWorkDailyWorker(mockStorage.Object, this.logger);
            var result = await worker.CheckForWorkAsync(DateTime.UtcNow, null, new CancellationToken());
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Success);
            Assert.IsFalse(result.WorkReady);
        }

        [TestMethod]
        public async Task CheckForWorkBeforeExpirationTime()
        {
            var startTime = DateTime.UtcNow;
            var mockStorage = new Mock<IDailyWorkerTrackingStorage>(MockBehavior.Strict);
            mockStorage
                .Setup(s => s.InsertAsync(It.IsAny<DailyWorkerTracking>()))
                .Returns<DailyWorkerTracking>(t =>
                {
                    var workTracker = new DailyWorkerTracking(t.WorkItemName, startTime.Date)
                    {
                        WorkStartedTime = startTime.AddSeconds(-1),
                        WorkCompleted = false,
                    };
                    return Task.FromResult(new DailyTrackerResult { CurrentRowValue = workTracker, InsertOrUpdateSuccess = false });
                });

            var worker = new NoWorkDailyWorker(mockStorage.Object, this.logger);
            var result = await worker.CheckForWorkAsync(startTime, null, new CancellationToken());
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Success);
            Assert.IsFalse(result.WorkReady);
        }

        [TestMethod]
        public async Task CheckForWorkAfterExpirationTime()
        {
            var startTime = DateTime.UtcNow;
            var mockStorage = new Mock<IDailyWorkerTrackingStorage>(MockBehavior.Strict);
            mockStorage
                .Setup(s => s.InsertAsync(It.IsAny<DailyWorkerTracking>()))
                .Returns<DailyWorkerTracking>(t =>
                {
                    var workTracker = new DailyWorkerTracking(t.WorkItemName, startTime.Date)
                    {
                        WorkStartedTime = startTime.AddSeconds(-11),
                        WorkCompleted = false,
                    };
                    return Task.FromResult(new DailyTrackerResult { CurrentRowValue = workTracker, InsertOrUpdateSuccess = false });
                });
            mockStorage
                .Setup(s => s.UpdateAsync(It.IsAny<DailyWorkerTracking>()))
                .Returns<DailyWorkerTracking>(t => Task.FromResult(new DailyTrackerResult { CurrentRowValue = t, InsertOrUpdateSuccess = true }));

            var worker = new NoWorkDailyWorker(mockStorage.Object, this.logger);
            var result = await worker.CheckForWorkAsync(startTime, null, new CancellationToken());
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Success);
            Assert.IsTrue(result.WorkReady);
            mockStorage.Verify(s => s.UpdateAsync(It.IsAny<DailyWorkerTracking>()), Times.Once);
        }

        [TestMethod]
        public async Task CheckForWorkAfterExpirationTimeFailedUpdate()
        {
            var startTime = DateTime.UtcNow;
            var mockStorage = new Mock<IDailyWorkerTrackingStorage>(MockBehavior.Strict);
            mockStorage
                .Setup(s => s.InsertAsync(It.IsAny<DailyWorkerTracking>()))
                .Returns<DailyWorkerTracking>(t =>
                {
                    var workTracker = new DailyWorkerTracking(t.WorkItemName, startTime.Date)
                    {
                        WorkStartedTime = startTime.AddSeconds(-11),
                        WorkCompleted = false,
                    };
                    return Task.FromResult(new DailyTrackerResult { CurrentRowValue = workTracker, InsertOrUpdateSuccess = false });
                });
            mockStorage
                .Setup(s => s.UpdateAsync(It.IsAny<DailyWorkerTracking>()))
                .Returns<DailyWorkerTracking>(t => Task.FromResult(new DailyTrackerResult { CurrentRowValue = t, InsertOrUpdateSuccess = false }));

            var worker = new NoWorkDailyWorker(mockStorage.Object, this.logger);
            var result = await worker.CheckForWorkAsync(startTime, null, new CancellationToken());
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Success);
            Assert.IsFalse(result.WorkReady);
            mockStorage.Verify(s => s.UpdateAsync(It.IsAny<DailyWorkerTracking>()), Times.Once);
        }

        [TestMethod]
        public async Task MarkWorkCompletedTest()
        {
            var startTime = DateTime.UtcNow;
            var mockStorage = new Mock<IDailyWorkerTrackingStorage>(MockBehavior.Strict);
            mockStorage
                .Setup(s => s.RetrieveAsync(It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns<string, DateTime>((w, d) => Task.FromResult(new DailyWorkerTracking(w, d) { WorkCompleted = false }));
            mockStorage
                .Setup(s => s.UpdateAsync(It.IsAny<DailyWorkerTracking>()))
                .Returns<DailyWorkerTracking>(t => Task.FromResult(new DailyTrackerResult { CurrentRowValue = t, InsertOrUpdateSuccess = true }));

            var worker = new NoWorkDailyWorker(mockStorage.Object, this.logger);
            worker.DailyWorkItemName = worker.WorkItemName;
            var result = await worker.MarkWorkCompletedAsync(startTime, null, new CancellationToken());
            Assert.IsTrue(result.Success);
            mockStorage.Verify(s => s.UpdateAsync(It.IsAny<DailyWorkerTracking>()), Times.Once);
        }

        [TestMethod]
        public async Task MarkWorkCompletedWhenAlreadyComplete()
        {
            var startTime = DateTime.UtcNow;
            var mockStorage = new Mock<IDailyWorkerTrackingStorage>(MockBehavior.Strict);
            mockStorage
                .Setup(s => s.RetrieveAsync(It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns<string, DateTime>((w, d) => Task.FromResult(new DailyWorkerTracking(w, d) { WorkCompleted = true }));
            mockStorage
                .Setup(s => s.UpdateAsync(It.IsAny<DailyWorkerTracking>()))
                .Returns<DailyWorkerTracking>(t => Task.FromResult(new DailyTrackerResult { CurrentRowValue = t, InsertOrUpdateSuccess = true }));

            var worker = new NoWorkDailyWorker(mockStorage.Object, this.logger);
            worker.DailyWorkItemName = worker.WorkItemName;
            try
            {
                await worker.MarkWorkCompletedAsync(startTime, null, new CancellationToken());
                Assert.Fail("Expected exception was not thrown.");
            }
            catch (WorkerOperationAbortedException)
            {
                // this exception is expected
            }

            mockStorage.Verify(s => s.UpdateAsync(It.IsAny<DailyWorkerTracking>()), Times.Never);
        }

        [TestMethod]
        public async Task RefreshWorkStartedTimeTest()
        {
            var startTime = DateTime.UtcNow;
            var mockStorage = new Mock<IDailyWorkerTrackingStorage>(MockBehavior.Strict);
            mockStorage
                .Setup(s => s.RetrieveAsync(It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns<string, DateTime>((w, d) => Task.FromResult(new DailyWorkerTracking(w, d) { WorkCompleted = false, WorkStartedTime = new DateTimeOffset(2015, 1, 1, 0, 0, 0, TimeSpan.FromDays(0)) }));
            mockStorage
                .Setup(s => s.UpdateAsync(It.IsAny<DailyWorkerTracking>()))
                .Returns<DailyWorkerTracking>(t => Task.FromResult(new DailyTrackerResult { CurrentRowValue = t, InsertOrUpdateSuccess = true }));

            var worker = new NoWorkDailyWorker(mockStorage.Object, new DateTime(2015, 1, 1, 0, 0, 0, DateTimeKind.Utc), this.logger);
            worker.DailyWorkItemName = worker.WorkItemName;
            await worker.Refresh(new CancellationToken());
            mockStorage.Verify(s => s.UpdateAsync(It.IsAny<DailyWorkerTracking>()), Times.Once);
        }

        [TestMethod]
        public async Task RefreshWorkStartedTimeWhenAlreadyComplete()
        {
            var startTime = DateTime.UtcNow;
            var mockStorage = new Mock<IDailyWorkerTrackingStorage>(MockBehavior.Strict);
            mockStorage
                .Setup(s => s.RetrieveAsync(It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns<string, DateTime>((w, d) => Task.FromResult(new DailyWorkerTracking(w, d) { WorkCompleted = true, WorkStartedTime = new DateTimeOffset(2015, 1, 1, 0, 0, 0, TimeSpan.FromDays(0)) }));
            mockStorage
                .Setup(s => s.UpdateAsync(It.IsAny<DailyWorkerTracking>()))
                .Returns<DailyWorkerTracking>(t => Task.FromResult(new DailyTrackerResult { CurrentRowValue = t, InsertOrUpdateSuccess = true }));

            var worker = new NoWorkDailyWorker(mockStorage.Object, new DateTime(2015, 1, 1, 0, 0, 0, DateTimeKind.Utc), this.logger);
            worker.DailyWorkItemName = worker.WorkItemName;
            try
            {
                await worker.Refresh(new CancellationToken());
                Assert.Fail("Expected exception was not thrown.");
            }
            catch (WorkerOperationAbortedException)
            {
                // this exception is expected
            }

            mockStorage.Verify(s => s.UpdateAsync(It.IsAny<DailyWorkerTracking>()), Times.Never);
        }

        private class NoWorkDailyWorker : DailyWorkerBase
        {
            public NoWorkDailyWorker(IDailyWorkerTrackingStorage workTrackingStorage, ILogger logger)
                : base(workTrackingStorage, logger)
            {
            }

            public NoWorkDailyWorker(IDailyWorkerTrackingStorage workTrackingStorage, DateTime workDate, ILogger logger)
                : base(workTrackingStorage, logger)
            {
                this.WorkDate = workDate;
            }

            public override TimeSpan WorkExpirationTimeSpan
            {
                get { return TimeSpan.FromSeconds(10); }
            }

            public override TimeSpan RunInterval
            {
                get { return TimeSpan.FromMinutes(1); }
            }

            public override RunIntervalType IntervalType
            {
                get { return RunIntervalType.FromLastFinish; }
            }

            public override TimeSpan WorkUtcOffset
            {
                get { return TimeSpan.FromMinutes(0); }
            }

            public override string WorkItemName
            {
                get { return "NoWork"; }
            }

            public override string OperationName
            {
                get { return "NoWorkOperation"; }
            }

            public override Task<WorkResult> DoWorkAsync(DateTime startTime, WorkOperationEvent workEvent, CancellationToken cancellationToken)
            {
                Console.WriteLine("Doing work.");
                return Task.FromResult(WorkResult.Succeeded);
            }

            public Task Refresh(CancellationToken cancellationToken)
            {
                return this.RefreshWorkStartedTimeAsync(cancellationToken);
            }
        }
    }
}
