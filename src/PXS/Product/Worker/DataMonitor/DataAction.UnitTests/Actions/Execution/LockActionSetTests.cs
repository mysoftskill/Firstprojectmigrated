// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.UnitTests.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Locks;
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.PrivacyServices.Common.Exceptions;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Actions;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Data;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Store;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class LockActionSetTests
    {
        private readonly Mock<IModelManipulator> mockModel = new Mock<IModelManipulator>();
        private readonly Mock<IExecuteContext> mockExecCtx = new Mock<IExecuteContext>();
        private readonly Mock<IActionFactory> mockFact = new Mock<IActionFactory>();
        private readonly Mock<IParseContext> mockParseCtx = new Mock<IParseContext>();
        private readonly Mock<IActionStore> mockStore = new Mock<IActionStore>();
        private readonly Mock<ILockManager> mockLock = new Mock<ILockManager>();
        private readonly Mock<ILockLease> mockLease = new Mock<ILockLease>();

        private readonly CancellationTokenSource cancelSource = new CancellationTokenSource();

        private const string DefTag = "tag";

        private LockActionSet testObj;

        private IExecuteContext execCtx;
        private IActionFactory fact;
        private IParseContext parseCtx;
        private IActionStore store;

        private Mock<IAction> SetupMockAction(
            string tag,
            bool execContinue,
            bool addToStore)
        {
            Mock<IAction> action = new Mock<IAction>();

            action.SetupGet(o => o.Tag).Returns(tag);

            action
                .Setup(
                    o => o.ParseAndProcessDefinition(
                        It.IsAny<IParseContext>(),
                        It.IsAny<IActionFactory>(),
                        It.IsAny<string>(),
                        It.IsAny<object>()))
                .Returns(true);

            action.Setup(o => o.ExpandDefinition(It.IsAny<IParseContext>(), It.IsAny<IActionStore>())).Returns(true);

            action
                .Setup(o => o.Validate(It.IsAny<IParseContext>(), It.IsAny<IDictionary<string, ModelValue>>()))
                .Returns(true);

            action
                .Setup(
                    o => o.ExecuteAsync(
                        It.IsAny<IExecuteContext>(), 
                        It.IsAny<ActionRefCore>(), 
                        It.IsAny<object>()))
                .ReturnsAsync(new ExecuteResult(execContinue));

            if (addToStore)
            {
                this.mockStore.Setup(o => o.GetAction(tag)).Returns(action.Object);
                this.mockFact.Setup(o => o.Create(tag)).Returns(action.Object);
            }

            return action;
        }

        private (LockActionSet.Args, ActionRefCore, LockActionSetDef, object) SetupTestObj(
            bool reportContinueOnLockFailure = false,
            TimeSpan? runFreq = null,
            LockActionSet.Args args = null,
            string actionTag = LockActionSetTests.DefTag)
        {
            object modelIn = new object();

            IDictionary<string, ModelValue> argXform = new Dictionary<string, ModelValue>
            {
                { "LockGroupName", new ModelValue { Const = 1 } },
                { "LeaseTime", new ModelValue { Const = 1 } },
                { "LockName", new ModelValue { Const = 1 } },
            };

            LockActionSetDef def = new LockActionSetDef
            {
                Actions = new List<ActionRef> { new ActionRef { Tag = actionTag, ExecutionOrder = 1 } },
            };

            args = args ?? new LockActionSet.Args
            {
                LockGroupName = "groupName",
                RunFrequency = runFreq,
                LeaseTime = TimeSpan.FromHours(1),
                LockName = "lockName",
                ReportContinueOnLockFailure = reportContinueOnLockFailure,
            };

            this.mockModel
                .Setup(o => o.TransformTo<LockActionSet.Args>(It.IsAny<object>()))
                .Returns(args);

            this.testObj = new LockActionSet(this.mockModel.Object, this.mockLock.Object);

            Assert.IsTrue(this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, "localTag", def));
            Assert.IsTrue(this.testObj.ExpandDefinition(this.parseCtx, this.store));
            Assert.IsTrue(this.testObj.Validate(this.parseCtx, argXform));

            this.mockModel.Setup(o => o.MergeModels(this.execCtx, modelIn, null, argXform)).Returns(args);

            return (args, new ActionRefCore { ArgTransform = argXform }, def, modelIn);
        }

        [TestInitialize]
        public void Init()
        {
            this.SetupMockAction(LockActionSetTests.DefTag, true, true);

            this.mockExecCtx.SetupGet(o => o.NowUtc).Returns(DateTimeOffset.Parse("2006-04-15T15:01:00-07:00"));
            this.mockExecCtx.SetupGet(o => o.OperationStartTime).Returns(DateTimeOffset.Parse("2006-04-15T15:00:00-07:00"));
            this.mockExecCtx.SetupGet(o => o.CancellationToken).Returns(this.cancelSource.Token);
            this.mockExecCtx.SetupGet(o => o.IsSimulation).Returns(false);

            this.mockLock
                .Setup(
                    o => o.AttemptAcquireAsync(
                        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<bool>()))
                .ReturnsAsync(this.mockLease.Object);

            this.parseCtx = this.mockParseCtx.Object;
            this.execCtx = this.mockExecCtx.Object;
            this.store = this.mockStore.Object;
            this.fact = this.mockFact.Object;
        }

        private async Task RunParamValidateTestAsync(
            string lockName,
            string lockGroup,
            TimeSpan leaseTime,
            string expectedContextLog)
        {
            LockActionSet.Args args;
            ActionRefCore refCore;
            object modelIn;

            args = new LockActionSet.Args
            {
                LockGroupName = lockGroup,
                LeaseTime = leaseTime,
                LockName = lockName,
            };

            (_, refCore, _, modelIn) = this.SetupTestObj(args: args);

            try
            {
                await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);
            }
            catch (ActionExecuteException)
            {
                this.mockExecCtx.Verify(o => o.LogError(It.Is<string>(p => p.Contains(expectedContextLog))), Times.Once);
                throw;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ActionExecuteException))]
        [DataRow(" ")]
        [DataRow("")]
        [DataRow(null)]
        public async Task ExecuteThrowsIfInvalidLockNameParameters(string value)
        {
            await this.RunParamValidateTestAsync(value, "group", TimeSpan.FromMinutes(1), "non-empty lock name");
        }

        [TestMethod]
        [ExpectedException(typeof(ActionExecuteException))]
        [DataRow(" ")]
        [DataRow("")]
        [DataRow(null)]
        public async Task ExecuteThrowsIfInvalidGroupNameParameters(string value)
        {
            await this.RunParamValidateTestAsync("lock", value, TimeSpan.FromMinutes(1), "non-empty lock group name");
        }

        [TestMethod]
        [ExpectedException(typeof(ActionExecuteException))]
        [DataRow(-1)]
        [DataRow(0)]
        public async Task ExecuteThrowsIfInvalidLeaseTimeParameters(int minutes)
        {
            await this.RunParamValidateTestAsync("lock", "group", TimeSpan.FromMinutes(minutes), "Lease time must be greater than 0");
        }

        [TestMethod]
        [ExpectedException(typeof(OperationCanceledException))]
        public async Task ExecuteThrowsAndReleasesLeaseIfCancelSignaledAfterLeaseAcquired()
        {
            LockActionSet.Args args;
            ActionRefCore refCore;
            object modelIn;

            (args, refCore, _, modelIn) = this.SetupTestObj();

            this.mockLock
                .Setup(o => o.AttemptAcquireAsync(args.LockGroupName, args.LockName, It.IsAny<string>(), args.LeaseTime, true))
                .Callback((string x, string x2, string x3, TimeSpan x4, bool x5) => { this.cancelSource.Cancel(); })
                .ReturnsAsync(this.mockLease.Object);

            // test
            try
            {
                await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);
            }
            catch(OperationCanceledException)
            {
                this.mockLock.Verify(
                    o => o.AttemptAcquireAsync(args.LockGroupName, args.LockName, It.IsAny<string>(), args.LeaseTime, true),
                    Times.Once);

                this.mockLease.Verify(o => o.ReleaseAsync(false), Times.Once);

                throw;
            }
        }

        [TestMethod]
        public async Task ExecuteFetchesParametersAndAttemptsToAcquireLease()
        {
            LockActionSet.Args args;
            ActionRefCore refCore;
            object modelIn;

            (args, refCore, _, modelIn) = this.SetupTestObj();

            // test
            await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // verify
            this.mockModel.Verify(o => o.MergeModels(this.execCtx, modelIn, null, refCore.ArgTransform), Times.Once);
            this.mockLock.Verify(
                o => o.AttemptAcquireAsync(args.LockGroupName, args.LockName, It.IsAny<string>(), args.LeaseTime, true),
                Times.Once);
        }

        [TestMethod]
        public async Task ExecuteExecutesAllInnerActionsIfLockAcquired()
        {
            const string ActionTag = LockActionSetTests.DefTag + "ACQUIREDLOCK";

            Mock<IAction> mockAction;

            LockActionSetDef def;
            ActionRefCore refCore;
            object modelIn;

            ExecuteResult result;

            mockAction = this.SetupMockAction(ActionTag, true, true);

            (_, refCore, def, modelIn) = this.SetupTestObj(actionTag: ActionTag);

            // test
            result = await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // verify
            Assert.IsTrue(result.Continue);
            mockAction.Verify(o => o.ExecuteAsync(this.execCtx, def.Actions[0], modelIn), Times.Once);
        }

        [TestMethod]
        public async Task ExecuteExecutesNoInnerActionsIfLockNotAcquired()
        {
            const string ActionTag = LockActionSetTests.DefTag + "NOLOCK";

            Mock<IAction> mockAction;

            LockActionSet.Args args;
            ActionRefCore refCore;
            object modelIn;

            mockAction = this.SetupMockAction(ActionTag, true, true);

            (args, refCore, _, modelIn) = this.SetupTestObj(actionTag: ActionTag);

            this.mockLock
                .Setup(o => o.AttemptAcquireAsync(args.LockGroupName, args.LockName, It.IsAny<string>(), args.LeaseTime, true))
                .ReturnsAsync((ILockLease)null);

            // test
            await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // verify
            mockAction.Verify(
                o => o.ExecuteAsync(
                    It.IsAny<IExecuteContext>(), 
                    It.IsAny<ActionRefCore>(), 
                    It.IsAny<object>()), 
                Times.Never);
        }

        [TestMethod]
        public async Task ExecuteReturnsNoContinueIfLockAcquiredButInnerActionSetReturnsNoContinue()
        {
            const string ActionTag = LockActionSetTests.DefTag + "NOCONTINUE";

            Mock<IAction> mockAction;

            LockActionSetDef def;
            ActionRefCore refCore;
            object modelIn;

            ExecuteResult result;

            mockAction = this.SetupMockAction(ActionTag, false, true);

            (_, refCore, def, modelIn) = this.SetupTestObj(actionTag: ActionTag);

            // test
            result = await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // verify
            Assert.IsFalse(result.Continue);
            mockAction.Verify(o => o.ExecuteAsync(this.execCtx, def.Actions[0], modelIn), Times.Once);
        }

        [TestMethod]
        public async Task ExecuteReturnsNoContinueIfLockWasNotAcquiredAndFlagToReportContinueWasNotSetInArgs()
        {
            LockActionSet.Args args;
            ActionRefCore refCore;
            object modelIn;

            ExecuteResult result;

            (args, refCore, _, modelIn) = this.SetupTestObj(reportContinueOnLockFailure: false);

            this.mockLock
                .Setup(o => o.AttemptAcquireAsync(args.LockGroupName, args.LockName, It.IsAny<string>(), args.LeaseTime, true))
                .ReturnsAsync((ILockLease)null);

            // test
            result = await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // verify
            Assert.IsFalse(result.Continue);
        }

        [TestMethod]
        public async Task ExecuteReturnsContinueIfLockWasNotAcquiredAndFlagToReportContinueWasSetInArgs()
        {
            LockActionSet.Args args;
            ActionRefCore refCore;
            object modelIn;

            ExecuteResult result;

            (args, refCore, _, modelIn) = this.SetupTestObj(reportContinueOnLockFailure: true);

            this.mockLock
                .Setup(o => o.AttemptAcquireAsync(args.LockGroupName, args.LockName, It.IsAny<string>(), args.LeaseTime, true))
                .ReturnsAsync((ILockLease)null);

            // test
            result = await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // verify
            Assert.IsTrue(result.Continue);
        }

        [TestMethod]
        public async Task ExecuteReleasesLockIfThereIsNoRunFrequency()
        {
            ActionRefCore refCore;
            object modelIn;

            (_, refCore, _, modelIn) = this.SetupTestObj(runFreq: null);

            // test
            await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // verify
            this.mockLease.Verify(o => o.ReleaseAsync(false));
        }

        [TestMethod]
        public async Task ExecuteReleasesLockIfThereIsARunFrequencyAndTheExtensionPeriodIsNotGreaterThanZero()
        {
            ActionRefCore refCore;
            object modelIn;

            (_, refCore, _, modelIn) = this.SetupTestObj(runFreq: TimeSpan.Zero);

            // test
            await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // verify
            this.mockLease.Verify(o => o.ReleaseAsync(false));
        }

        [TestMethod]
        public async Task ExecuteRenewsLockIfThereIsARunFrequencyAndTheExtensionPeriodIsGreaterThanZero()
        {
            ActionRefCore refCore;
            TimeSpan expectedExtension;
            TimeSpan runFreq = TimeSpan.FromDays(1);
            object modelIn;

            (_, refCore, _, modelIn) = this.SetupTestObj(runFreq: runFreq);

            // extension is adjusted by the amount of time spent on the task thus far
            expectedExtension = runFreq - (this.execCtx.NowUtc - this.execCtx.OperationStartTime);

            // test
            await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // verify
            this.mockLease.Verify(o => o.RenewAsync(expectedExtension));
        }
    }
}
