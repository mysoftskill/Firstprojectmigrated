// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.Helpers.WorkerTasks
{
    using System;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Logging.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks;
    using Microsoft.PrivacyServices.Common.Telemetry;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Microsoft.PrivacyServices.Common.Azure;

    [TestClass]
    public class MultiInstanceTaskTests
    {
        private class TestCfg : ITaskConfig
        {
            public string TaskType { get; } = "TestTaskType";
            public string Tag { get; } = "TestTaskType";
            public int InstanceCount { get; } = 1;
            public int DelayOnExceptionMinutes { get; } = 0;
        }

        private class MultiInstanceTaskTestException : Exception
        {
            public MultiInstanceTaskTestException() { }
            public MultiInstanceTaskTestException(string msg) : base(msg) { }
        }

        private class MultiInstanceTaskTestTelemetryException : TaskTelemetryException
        {
            public MultiInstanceTaskTestTelemetryException(TaskTelemetryEvent e) : base(e) { }
        }

        private class TestTelemetryEvent : TaskTelemetryEvent
        {
            public string Data { get; set; }
        }

        private class TestTask : MultiInstanceTask<ITaskConfig>
        {
            private readonly SemaphoreSlim runOnceWaiter = new SemaphoreSlim(0, 1);

            private TaskCompletionSource<TimeSpan?> tcs = new TaskCompletionSource<TimeSpan?>();
            private int runOnceCount;

            public TestTask(
                ITaskConfig config, 
                ICounterFactory factory,
                ITelemetryLogger telemetryLogger,
                ILogger traceLogger) : 
                base(config, factory, telemetryLogger, traceLogger)
            {
            }

            public string LastTaskId { get; private set; }

            public string CtxItem { get; set; }

            public string CtxOp { get; set; }

            public bool IsCancelled => this.CancelToken.IsCancellationRequested;

            public int RunOnceCount => Volatile.Read(ref this.runOnceCount);

            protected override Task<TimeSpan?> RunOnceAsync(OperationContext ctx)
            {
                this.LastTaskId = ctx.TaskId;

                ctx.Item = this.CtxItem;
                ctx.Op = this.CtxOp;

                Interlocked.Increment(ref this.runOnceCount);

                this.runOnceWaiter.Release();
                
                return this.tcs.Task;
            }

            public Task WaitForRunOnceAsync(int timeout)
            {
                return this.runOnceWaiter.WaitAsync(timeout);
            }

            public void TriggerRunOnceTask(Exception e)
            {
                TaskCompletionSource<TimeSpan?> current = this.tcs;

                this.tcs = new TaskCompletionSource<TimeSpan?>();

                if (e != null)
                {
                    current.SetException(e);
                }
                else
                {
                    current.SetResult(null);
                }
            }
        }

        private readonly Mock<ITelemetryLogger> mockTelemetryLog = new Mock<ITelemetryLogger>();
        private readonly Mock<ICounterFactory> mockCounters = new Mock<ICounterFactory>();
        private readonly Mock<ILogger> mockTraceLog = new Mock<ILogger>();

        private TestTask testObj;

        [TestInitialize]
        public void Init()
        {
            this.testObj = new TestTask(
                new TestCfg(), 
                this.mockCounters.Object, 
                this.mockTelemetryLog.Object,
                this.mockTraceLog.Object);
        }

        [TestMethod]
        [Timeout(10_000)]
        public async Task RunSingleInstanceRunsExactlyOnceAndSelfTerminates()
        {
            const string TaskId = "taskId12313";

            Task task = this.testObj.RunSingleInstanceOnePassAsync(TaskId, "traceContext");

            // give the spawned task a bit of time to setup
            await this.testObj.WaitForRunOnceAsync(1000);

            Assert.IsFalse(task.IsCanceled || task.IsCompleted || task.IsFaulted);
            Assert.AreEqual(1, this.testObj.RunOnceCount);
            Assert.AreEqual(TaskId, this.testObj.LastTaskId);
            this.mockTraceLog.Verify(
                o => o.Verbose(It.IsAny<string>(), It.Is<string>(s => s.Contains("Starting")), It.IsAny<object[]>()), Times.Once);
            this.mockTraceLog.Verify(
                o => o.Verbose(It.IsAny<string>(), It.Is<string>(s => s.Contains("Terminating")), It.IsAny<object[]>()), Times.Never);

            this.testObj.TriggerRunOnceTask(null);

            await Task.Delay(100);

            Assert.IsTrue(task.IsCompleted);
            Assert.AreEqual(1, this.testObj.RunOnceCount);
            this.mockTraceLog.Verify(
                o => o.Verbose(It.IsAny<string>(), It.Is<string>(s => s.Contains("Starting")), It.IsAny<object[]>()), Times.Once);
            this.mockTraceLog.Verify(
                o => o.Verbose(It.IsAny<string>(), It.Is<string>(s => s.Contains("Terminating")), It.IsAny<object[]>()), Times.Once);
        }

        [TestMethod]
        [Timeout(10_000)]
        public async Task RunSingleInstancePropagatesExceptions()
        {
            const string TaskId = "taskId12312";

            MultiInstanceTaskTestException exception = new MultiInstanceTaskTestException();

            Task task = this.testObj.RunSingleInstanceOnePassAsync(TaskId, "traceContext");

            // give the spawned task a bit of time to setup
            await this.testObj.WaitForRunOnceAsync(1000);

            Assert.IsFalse(task.IsCanceled || task.IsCompleted || task.IsFaulted);
            Assert.AreEqual(1, this.testObj.RunOnceCount);
            this.mockTraceLog.Verify(
                o => o.Verbose(It.IsAny<string>(), It.Is<string>(s => s.Contains("Starting")), It.IsAny<object[]>()), Times.Once);
            this.mockTraceLog.Verify(
                o => o.Verbose(It.IsAny<string>(), It.Is<string>(s => s.Contains("Terminating")), It.IsAny<object[]>()), Times.Never);

            this.testObj.TriggerRunOnceTask(exception);

            try { await task; } catch { }

            Assert.IsTrue(task.IsFaulted);
            Assert.IsNotNull(task.Exception?.InnerExceptions);
            Assert.AreSame(exception, task.Exception.InnerExceptions.First());

            Assert.AreEqual(1, this.testObj.RunOnceCount);
            this.mockTraceLog.Verify(
                o => o.Verbose(It.IsAny<string>(), It.Is<string>(s => s.Contains("Starting")), It.IsAny<object[]>()), Times.Once);
            this.mockTraceLog.Verify(
                o => o.Verbose(It.IsAny<string>(), It.Is<string>(s => s.Contains("Terminating")), It.IsAny<object[]>()), Times.Once);
            this.mockTraceLog.Verify(
                o => o.Error(
                    It.IsAny<string>(), It.Is<string>(s => s.Contains("] Error processing work item")), It.IsAny<object[]>()), 
                Times.Once);
        }
        
        [TestMethod]
        [Timeout(10_000)]
        public async Task RunSingleInstancePropagatesExceptionsWhenExceptionIsFatal()
        {
            const string TaskId = "taskId12312";

            SEHException exception = new SEHException();

            Task task = this.testObj.RunSingleInstanceOnePassAsync(TaskId, "traceContext");

            // give the spawned task a bit of time to setup
            await this.testObj.WaitForRunOnceAsync(1000);

            Assert.IsFalse(task.IsCanceled || task.IsCompleted || task.IsFaulted);
            Assert.AreEqual(1, this.testObj.RunOnceCount);
            this.mockTraceLog.Verify(
                o => o.Verbose(It.IsAny<string>(), It.Is<string>(s => s.Contains("Starting")), It.IsAny<object[]>()), Times.Once);
            this.mockTraceLog.Verify(
                o => o.Verbose(It.IsAny<string>(), It.Is<string>(s => s.Contains("Terminating")), It.IsAny<object[]>()), Times.Never);

            this.testObj.TriggerRunOnceTask(exception);

            try { await task; } catch { }

            Assert.IsTrue(task.IsFaulted);
            Assert.IsNotNull(task.Exception?.InnerExceptions);
            Assert.AreSame(exception, task.Exception.InnerExceptions.First());

            Assert.AreEqual(1, this.testObj.RunOnceCount);
            this.mockTraceLog.Verify(
                o => o.Verbose(It.IsAny<string>(), It.Is<string>(s => s.Contains("Starting")), It.IsAny<object[]>()), Times.Once);
            this.mockTraceLog.Verify(
                o => o.Verbose(It.IsAny<string>(), It.Is<string>(s => s.Contains("Terminating")), It.IsAny<object[]>()), Times.Once);
            this.mockTraceLog.Verify(
                o => o.Error(
                    It.IsAny<string>(), It.Is<string>(s => s.Contains("] Fatal error processing work item")), It.IsAny<object[]>()),
                Times.Once);
        }

        [TestMethod]
        [Timeout(10_000)]
        public async Task RunSingleInstanceLogsErrorEventWithDefaultEventTypeWhenExceptionIsNotTelemetryException()
        {
            const string CtxItem = "item12312";
            const string TaskId = "taskId12312";
            const string CtxOp = "op12312";
            const string Msg = "msgr12312";

            MultiInstanceTaskTestException exception = new MultiInstanceTaskTestException(Msg);

            Func<TaskTelemetryEvent, bool> verifier = 
                e =>
                {
                    Assert.AreEqual(typeof(TaskTelemetryEvent), e.GetType());
                    Assert.AreEqual(TaskId + "[0]", e.TaskId);
                    Assert.AreEqual(CtxItem, e.Item);
                    Assert.AreEqual(CtxOp, e.Operation);
                    Assert.AreEqual(Msg, e.Details);
                    return true;
                };

            Task task;

            this.testObj.CtxItem = CtxItem;
            this.testObj.CtxOp = CtxOp;

            task = this.testObj.RunSingleInstanceOnePassAsync(TaskId, "traceContext");

            // give the spawned task a bit of time to setup
            await this.testObj.WaitForRunOnceAsync(1000);

            Assert.IsFalse(task.IsCanceled || task.IsCompleted || task.IsFaulted);
            Assert.AreEqual(1, this.testObj.RunOnceCount);
            this.mockTraceLog.Verify(
                o => o.Verbose(It.IsAny<string>(), It.Is<string>(s => s.Contains("Starting")), It.IsAny<object[]>()), Times.Once);
            this.mockTraceLog.Verify(
                o => o.Verbose(It.IsAny<string>(), It.Is<string>(s => s.Contains("Terminating")), It.IsAny<object[]>()), Times.Never);

            this.testObj.TriggerRunOnceTask(exception);

            await Task.Delay(100);

            this.mockTelemetryLog.Verify(o => o.LogError(It.Is<TaskTelemetryEvent>(p => verifier(p))), Times.Once);

            Assert.AreEqual(1, this.testObj.RunOnceCount);
            this.mockTraceLog.Verify(
                o => o.Verbose(It.IsAny<string>(), It.Is<string>(s => s.Contains("Starting")), It.IsAny<object[]>()), Times.Once);
            this.mockTraceLog.Verify(
                o => o.Verbose(It.IsAny<string>(), It.Is<string>(s => s.Contains("Terminating")), It.IsAny<object[]>()), Times.Once);
        }

        [TestMethod]
        [Timeout(10_000)]
        public async Task RunSingleInstanceLogsContainedEventWhenExceptionIsTelemetryException()
        {
            const string CtxItem = "item123124";
            const string TaskId = "taskId123124";
            const string CtxOp = "op123124";
            const string Data = "data123124";
            const string Msg = "msg123124";

            MultiInstanceTaskTestTelemetryException exception;
            TestTelemetryEvent testEvent = new TestTelemetryEvent { Data = Data, Details = Msg };

            exception = new MultiInstanceTaskTestTelemetryException(testEvent);

            Func<TaskTelemetryEvent, bool> verifier =
                e =>
                {
                    TestTelemetryEvent eactual = e as TestTelemetryEvent;

                    Assert.IsNotNull(eactual);
                    Assert.AreEqual(TaskId + "[0]", eactual.TaskId);
                    Assert.AreEqual(CtxItem, eactual.Item);
                    Assert.AreEqual(CtxOp, eactual.Operation);
                    Assert.AreEqual(Data, eactual.Data);
                    Assert.AreEqual(Msg, eactual.Details);
                    return true;
                };

            Task task;

            this.testObj.CtxItem = CtxItem;
            this.testObj.CtxOp = CtxOp;

            task = this.testObj.RunSingleInstanceOnePassAsync(TaskId, "traceContext");

            // give the spawned task a bit of time to setup
            await this.testObj.WaitForRunOnceAsync(1000);

            Assert.IsFalse(task.IsCanceled || task.IsCompleted || task.IsFaulted);
            Assert.AreEqual(1, this.testObj.RunOnceCount);
            this.mockTraceLog.Verify(
                o => o.Verbose(It.IsAny<string>(), It.Is<string>(s => s.Contains("Starting")), It.IsAny<object[]>()), Times.Once);
            this.mockTraceLog.Verify(
                o => o.Verbose(It.IsAny<string>(), It.Is<string>(s => s.Contains("Terminating")), It.IsAny<object[]>()), Times.Never);

            this.testObj.TriggerRunOnceTask(exception);

            await Task.Delay(100);

            this.mockTelemetryLog.Verify(o => o.LogError(It.Is<TaskTelemetryEvent>(p => verifier(p))), Times.Once);

            Assert.AreEqual(1, this.testObj.RunOnceCount);
            this.mockTraceLog.Verify(
                o => o.Verbose(It.IsAny<string>(), It.Is<string>(s => s.Contains("Starting")), It.IsAny<object[]>()), Times.Once);
            this.mockTraceLog.Verify(
                o => o.Verbose(It.IsAny<string>(), It.Is<string>(s => s.Contains("Terminating")), It.IsAny<object[]>()), Times.Once);
        }

        [TestMethod]
        [Timeout(10_000)]
        public async Task StartRunsTheTaskRepeatedlyUntilStopIsCalled()
        {
            Task terminateTask;

            // pass 1
            this.testObj.Start();

            await this.testObj.WaitForRunOnceAsync(1000);

            Assert.IsFalse(this.testObj.IsCancelled);
            Assert.AreEqual(1, this.testObj.RunOnceCount);
            this.mockTraceLog.Verify(
                o => o.Verbose(It.IsAny<string>(), It.Is<string>(s => s.Contains("Starting")), It.IsAny<object[]>()), Times.Once);
            this.mockTraceLog.Verify(
                o => o.Verbose(It.IsAny<string>(), It.Is<string>(s => s.Contains("Terminating")), It.IsAny<object[]>()), Times.Never);

            // pass 2- allow it to proceed
            this.testObj.TriggerRunOnceTask(null);

            await this.testObj.WaitForRunOnceAsync(1000);

            Assert.IsFalse(this.testObj.IsCancelled);
            Assert.AreEqual(2, this.testObj.RunOnceCount);
            this.mockTraceLog.Verify(
                o => o.Verbose(It.IsAny<string>(), It.Is<string>(s => s.Contains("Starting")), It.IsAny<object[]>()), Times.Once);
            this.mockTraceLog.Verify(
                o => o.Verbose(It.IsAny<string>(), It.Is<string>(s => s.Contains("Terminating")), It.IsAny<object[]>()), Times.Never);

            // pass 3- attempt terminate
            terminateTask = this.testObj.StopAsync();

            await Task.Delay(100);

            Assert.IsTrue(this.testObj.IsCancelled);
            Assert.IsFalse(terminateTask.IsCanceled || terminateTask.IsCompleted || terminateTask.IsFaulted);

            this.testObj.TriggerRunOnceTask(null);

            await Task.Delay(100);

            // task should not have been invoked again
            Assert.AreEqual(2, this.testObj.RunOnceCount);

            Assert.IsTrue(terminateTask.IsCompleted);
            this.mockTraceLog.Verify(
                o => o.Verbose(It.IsAny<string>(), It.Is<string>(s => s.Contains("Starting")), It.IsAny<object[]>()), Times.Once);
            this.mockTraceLog.Verify(
                o => o.Verbose(It.IsAny<string>(), It.Is<string>(s => s.Contains("Terminating")), It.IsAny<object[]>()), Times.Once);
        }
    }
}
