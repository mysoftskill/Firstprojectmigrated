// -------------------------------------------------------------------------
// <copyright>
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.Helpers.Scheduler
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Privacy.Core.Helpers.Scheduler;
    using Common.Logging;

    using Microsoft.PrivacyServices.Common.Azure;

    [TestClass]
    public class WorkSchedulerTests
    {
        // How much slop in the number of runs in a given time interval does the test allow
        private double SlopFactor = 0.25;

        [TestMethod]
        [Ignore]
        public async Task TestTightLoop()
        {
            var testDuration = TimeSpan.FromSeconds(20);
            var runInterval = TimeSpan.FromMilliseconds(10);

            var worker = new NoWorkWorker(runInterval);
            var workerList = new List<Func<WorkerBase>>
            {
                () => worker,
            };
            var scheduler = new WorkScheduler(workerList, new ConsoleLogger());
            scheduler.Start();
            await Task.Delay(testDuration);

            // Despite the fact that I specify delay of 10ms, the system uses a low-resolution timer
            // which only has 16ms granularity
            double expectedRunCount = testDuration.TotalMilliseconds / TimeSpan.FromMilliseconds(16).TotalMilliseconds;
            Console.WriteLine("Expected runs is {0}, actual runs is {1}", expectedRunCount, worker.RunCount);
            Assert.AreEqual(expectedRunCount, (double)worker.RunCount, expectedRunCount * SlopFactor);
        }

        [TestMethod]
        [Ignore]
        public async Task TestMultipleWorkers()
        {
            var testDuration = TimeSpan.FromSeconds(20);
            var runInterval1 = TimeSpan.FromMilliseconds(10);
            var runInterval2 = TimeSpan.FromMilliseconds(64);

            var worker1 = new NoWorkWorker(runInterval1);
            var worker2 = new NoWorkWorker(runInterval2);
            var workerList = new List<Func<WorkerBase>>
            {
                () => worker1,
                () => worker2,
            };
            var scheduler = new WorkScheduler(workerList, new ConsoleLogger());
            scheduler.Start();
            await Task.Delay(testDuration);

            // Despite the fact that I specify delay of 10ms, the system uses a low-resolution timer
            // which only has 16ms granularity
            double expectedRunCount1 = testDuration.TotalMilliseconds / TimeSpan.FromMilliseconds(16).TotalMilliseconds;
            Console.WriteLine("Expected runs is {0}, worker1 actual runs is {1}", expectedRunCount1, worker1.RunCount);
            Assert.AreEqual(expectedRunCount1, (double)worker1.RunCount, expectedRunCount1 * SlopFactor);

            double expectedRunCount2 = testDuration.TotalMilliseconds / runInterval2.TotalMilliseconds;
            Console.WriteLine("Expected runs is {0}, worker2 actual runs is {1}", expectedRunCount2, worker2.RunCount);
            Assert.AreEqual(expectedRunCount2, (double)worker2.RunCount, expectedRunCount2 * SlopFactor);
        }

        [TestMethod]
        [Ignore]
        public async Task TestCancellationOfWork()
        {
            var testDuration = TimeSpan.FromSeconds(20);
            var cancelAfter = TimeSpan.FromSeconds(10);
            var runInterval = TimeSpan.FromMilliseconds(10);

            var worker = new NoWorkWorker(runInterval);
            var workerList = new List<Func<WorkerBase>>
            {
                () => worker,
            };
            var scheduler = new WorkScheduler(workerList, new ConsoleLogger());
            scheduler.Start();
            await Task.Delay(cancelAfter);
            scheduler.Dispose();
            await Task.Delay(testDuration - cancelAfter);

            double expectedRunCount = cancelAfter.TotalMilliseconds / TimeSpan.FromMilliseconds(16).TotalMilliseconds;
            Console.WriteLine("Expected runs is {0}, actual runs is {1}", expectedRunCount, worker.RunCount);
            Assert.AreEqual(expectedRunCount, (double)worker.RunCount, expectedRunCount * SlopFactor);
        }

        [TestMethod]
        [Ignore]
        public async Task TestCancellationDuringWaitStops()
        {
            var cancelAfter = TimeSpan.FromSeconds(1);
            var runInterval = TimeSpan.FromSeconds(60);

            var worker = new NoWorkWorker(runInterval);
            var workerList = new List<Func<WorkerBase>>
            {
                () => worker,
            };
            var scheduler = new WorkScheduler(workerList, new ConsoleLogger());
            scheduler.Start();
            await Task.Delay(cancelAfter);
            scheduler.Dispose();

            // Give it a few milliseconds to cancel and change state, then verify
            await Task.Delay(TimeSpan.FromMilliseconds(300));
            Assert.AreEqual(WorkerState.Stopped, worker.State);
        }

        [TestMethod]
        public async Task WorkNotMarkedCompleteOnFailure()
        {
            var testDuration = TimeSpan.FromSeconds(2);
            var runInterval = TimeSpan.FromSeconds(60);

            var worker = new NoWorkWorker(runInterval);
            worker.DoWorkResult = WorkResult.Failed(new NotSupportedException("Something blow up!"));
            var workerList = new List<Func<WorkerBase>>
            {
                () => worker,
            };
            var scheduler = new WorkScheduler(workerList, new ConsoleLogger());
            scheduler.Start();
            await Task.Delay(testDuration);
            Assert.IsFalse(worker.MarkedComplete);
        }

        private class NoWorkWorker : WorkerBase
        {
            private TimeSpan runInterval;

            public NoWorkWorker(TimeSpan runInterval)
            {
                this.runInterval = runInterval;
                this.RunCount = 0;
                this.DoWorkResult = WorkResult.Succeeded;
            }

            public int RunCount { get; set; }

            public bool MarkedComplete { get; private set; }

            public override TimeSpan RunInterval
            {
                get { return this.runInterval; }
            }

            public override RunIntervalType IntervalType
            {
                get { return RunIntervalType.FromLastFinish; }
            }

            public override string WorkItemName
            {
                get { return "NoWorkWorker"; }
            }

            public override string OperationName
            {
                get { return "LazyOp"; }
            }

            public WorkResult DoWorkResult { get; set; }

            public override Task<WorkResult> DoWorkAsync(DateTime startTime, WorkOperationEvent workEvent, CancellationToken cancellationToken)
            {
                this.RunCount++;
                return Task.FromResult(this.DoWorkResult);
            }

            public override Task<WorkResult> MarkWorkCompletedAsync(DateTime startTime, WorkOperationEvent workEvent, CancellationToken cancellationToken)
            {
                this.MarkedComplete = true;
                return base.MarkWorkCompletedAsync(startTime, workEvent, cancellationToken);
            }
        }
    }
}
