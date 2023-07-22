// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.UnitTests.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Actions;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Data;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Store;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class TimeApplicabilityActionTests
    {
        private readonly Mock<IModelManipulator> mockModel = new Mock<IModelManipulator>();
        private readonly Mock<IExecuteContext> mockExecCtx = new Mock<IExecuteContext>();
        private readonly Mock<IActionFactory> mockFact = new Mock<IActionFactory>();
        private readonly Mock<IParseContext> mockParseCtx = new Mock<IParseContext>();
        private readonly Mock<IActionStore> mockStore = new Mock<IActionStore>();

        private readonly CancellationTokenSource cancelSource = new CancellationTokenSource();

        private const string DefTag = "tag";

        private TimeApplicabilityAction testObj;

        private IExecuteContext execCtx;
        private IActionFactory fact;
        private IParseContext parseCtx;
        private IActionStore store;

        private (TimeApplicabilityAction.Args, ActionRefCore, TimeApplicabilityDef, object) SetupTestObj(
            DateTimeOffset? now = null,
            string timeZoneId = null,
            IDictionary<DayOfWeekExt, ICollection<TimeRange>> allowed = null,
            IDictionary<DateTime, ICollection<TimeRangeOverride>> overrides = null)
        {
            object modelIn = new object();

            IDictionary<string, ModelValue> argXform = new Dictionary<string, ModelValue>();

            TimeApplicabilityDef def = new TimeApplicabilityDef
            {
                AllowedDaysAndTimes = allowed,
                Overrides = overrides,
            };

            TimeApplicabilityAction.Args args = new TimeApplicabilityAction.Args
            {
                TimeZoneId = timeZoneId,
                Now = now,
            };

            this.mockModel
                .Setup(o => o.TransformTo<TimeApplicabilityAction.Args>(It.IsAny<object>()))
                .Returns(args);

            this.testObj = new TimeApplicabilityAction(this.mockModel.Object);

            Assert.IsTrue(this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, TimeApplicabilityActionTests.DefTag, def));
            Assert.IsTrue(this.testObj.ExpandDefinition(this.parseCtx, this.store));
            Assert.IsTrue(this.testObj.Validate(this.parseCtx, null));

            this.mockModel.Setup(o => o.MergeModels(this.execCtx, modelIn, null, argXform)).Returns(args);

            return (args, new ActionRefCore { ArgTransform = argXform }, def, modelIn);
        }

        [TestInitialize]
        public void Init()
        {
            this.mockExecCtx.SetupGet(o => o.NowUtc).Returns(DateTimeOffset.Parse("2006-04-15T15:01:00-07:00"));
            this.mockExecCtx.SetupGet(o => o.OperationStartTime).Returns(DateTimeOffset.Parse("2006-04-15T15:00:00-07:00"));
            this.mockExecCtx.SetupGet(o => o.CancellationToken).Returns(this.cancelSource.Token);
            this.mockExecCtx.SetupGet(o => o.IsSimulation).Returns(false);

            this.parseCtx = this.mockParseCtx.Object;
            this.execCtx = this.mockExecCtx.Object;
            this.store = this.mockStore.Object;
            this.fact = this.mockFact.Object;
        }

        [TestMethod]
        public async Task ExecuteParsesArguments()
        {
            ActionRefCore refCore;
            object modelIn;

            (_, refCore, _, modelIn) = this.SetupTestObj();

            // test
            await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // verify
            this.mockModel.Verify(o => o.MergeModels(this.execCtx, modelIn, null, refCore.ArgTransform), Times.Once);
        }

        [TestMethod]
        public async Task ExecuteUsesDefaultPacificTimeZoneIfNoneSpecified()
        {
            ActionRefCore refCore;
            object modelIn;

            (_, refCore, _, modelIn) = this.SetupTestObj();

            // test
            await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // verify
            this.mockExecCtx.Verify(
                o => o.Log(It.Is<string>(p => p.Contains("Pacific Time (US & Canada)"))),
                Times.Once);
        }

        [TestMethod]
        public async Task ExecuteUsesSpecifiedTimeZoneIfValid()
        {
            ActionRefCore refCore;
            object modelIn;

            (_, refCore, _, modelIn) = this.SetupTestObj(timeZoneId: "Hawaiian Standard Time");

            // test
            await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // verify
            this.mockExecCtx.Verify(
                o => o.Log(It.Is<string>(p => p.Contains("Hawaii"))),
                Times.Once);
        }

        [TestMethod]
        [DataRow("2018-07-01T00:00:00Z", "(Sunday, Weekend)")]
        [DataRow("2018-07-02T00:00:00Z", "(Monday, Weekday)")]
        [DataRow("2018-07-03T00:00:00Z", "(Tuesday, Weekday)")]
        [DataRow("2018-07-04T00:00:00Z", "(Wednesday, Weekday)")]
        [DataRow("2018-07-05T00:00:00Z", "(Thursday, Weekday)")]
        [DataRow("2018-07-06T00:00:00Z", "(Friday, Weekday)")]
        [DataRow("2018-07-07T00:00:00Z", "(Saturday, Weekend)")]
        public async Task ExecuteCorrectlyDeterminesDayOfWeekFromInputDate(
            string date,
            string expected)
        {
            ActionRefCore refCore;
            object modelIn;

            (_, refCore, _, modelIn) = this.SetupTestObj(
                now: DateTime.Parse(date, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
                timeZoneId: "UTC");

            // test
            await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // verify
            this.mockExecCtx.Verify(
                o => o.Log(It.Is<string>(p => p.Contains(expected))),
                Times.Once);
        }

        [TestMethod]
        public async Task ExecuteReturnsFalseIfNoAllowedDatesOrOverridesSpecified()
        {
            ActionRefCore refCore;
            object modelIn;

            ExecuteResult result;

            (_, refCore, _, modelIn) = this.SetupTestObj();

            // test
            result = await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // verify
            Assert.IsFalse(result.Continue);
            this.mockExecCtx.Verify(
                o => o.Log(It.Is<string>(p => p.Contains("processing should NOT continue"))),
                Times.Once);
        }

        [TestMethod]
        [DataRow(true, null, null, 12, false, true)]
        [DataRow(true, 0, null, 12, false, true)]
        [DataRow(true, null, 24, 12, false, true)]
        [DataRow(true, 0, 24, 12, false, true)]
        [DataRow(true, 11, 13, 12, false, true)]
        [DataRow(false, 11, 13, 10, false, false)]
        [DataRow(false, 11, 13, 14, false, false)]
        [DataRow(true, null, null, 12, true, false)]
        [DataRow(true, 0, null, 12, true, false)]
        [DataRow(true, null, 24, 12, true, false)]
        [DataRow(true, 0, 24, 12, true, false)]
        [DataRow(true, 11, 13, 12, true, false)]
        [DataRow(false, 11, 13, 10, true, false)]
        [DataRow(false, 11, 13, 14, true, false)]
        public async Task ExecuteReturnsCorrectResultWhenOverridePresent(
            bool expectOverrideFound,
            int? startHour,
            int? endHour,
            int currentHour,
            bool exclude,
            bool expectedResult)
        {
            ActionRefCore refCore;
            object modelIn;

            IDictionary<DateTime, ICollection<TimeRangeOverride>> overrides =
                new Dictionary<DateTime, ICollection<TimeRangeOverride>>
                {
                    {
                        new DateTime(2018, 7, 1),
                        new[]
                        {
                            new TimeRangeOverride
                            {
                                Start = startHour != null ? TimeSpan.FromHours(startHour.Value) : (TimeSpan?)null,
                                End = endHour != null ? TimeSpan.FromHours(endHour.Value) : (TimeSpan?)null,
                                Exclude = exclude,
                            }
                        }
                    }
                };

            ExecuteResult result;

            (_, refCore, _, modelIn) = this.SetupTestObj(
                now: new DateTimeOffset(2018, 7, 1, currentHour, 0, 0, TimeSpan.Zero),
                timeZoneId: "UTC",
                overrides: overrides);

            // test
            result = await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // verify
            Assert.AreEqual(expectedResult, result.Continue);
            this.mockExecCtx.Verify(
                o => o.Log(
                    It.Is<string>(
                        p => p.Contains("continuning to process the containing action set") &&
                             p.Contains("Override range"))),
                expectOverrideFound ? Times.Once() : Times.Never());
        }

        // the tests that have a null initial parameter don't always show up in the test results, so add a dummy non-null initial
        //  value to all tests to force them to show up.
        [TestMethod]
        [DataRow(DayOfWeekExt.Sunday, null, null, 12, true)]
        [DataRow(DayOfWeekExt.Sunday, 0, null, 12, true)]
        [DataRow(DayOfWeekExt.Sunday, null, 24, 12, true)]
        [DataRow(DayOfWeekExt.Sunday, 0, 24, 12, true)]
        [DataRow(DayOfWeekExt.Sunday, 11, 13, 12, true)]
        [DataRow(DayOfWeekExt.Sunday, 11, 13, 10, false)]
        [DataRow(DayOfWeekExt.Sunday, 11, 13, 14, false)]
        [DataRow(DayOfWeekExt.Weekend, null, null, 12, true)]
        [DataRow(DayOfWeekExt.Weekend, 0, null, 12, true)]
        [DataRow(DayOfWeekExt.Weekend, null, 24, 12, true)]
        [DataRow(DayOfWeekExt.Weekend, 0, 24, 12, true)]
        [DataRow(DayOfWeekExt.Weekend, 11, 13, 12, true)]
        [DataRow(DayOfWeekExt.Weekend, 11, 13, 10, false)]
        [DataRow(DayOfWeekExt.Weekend, 11, 13, 14, false)]
        public async Task ExecuteReturnsTrueWhenAllowedValuePresent(
            DayOfWeekExt dow,
            int? startHour,
            int? endHour,
            int currentHour,
            bool expectedResult)
        {
            ActionRefCore refCore;
            object modelIn;

            IDictionary<DayOfWeekExt, ICollection<TimeRange>> allowed =
                new Dictionary<DayOfWeekExt, ICollection<TimeRange>>
                {
                    {
                        dow,
                        new[]
                        {
                            new TimeRange
                            {
                                Start = startHour != null ? TimeSpan.FromHours(startHour.Value) : (TimeSpan?)null,
                                End = endHour != null ? TimeSpan.FromHours(endHour.Value) : (TimeSpan?)null,
                            }
                        }
                    }
                };

            ExecuteResult result;

            (_, refCore, _, modelIn) = this.SetupTestObj(
                now: new DateTimeOffset(2018, 7, 1, currentHour, 0, 0, TimeSpan.Zero),
                timeZoneId: "UTC",
                allowed: allowed);

            // test
            result = await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // verify
            Assert.AreEqual(expectedResult, result.Continue);

            this.mockExecCtx.Verify(
                o => o.Log(
                    It.Is<string>(
                        p => p.Contains("continuning to process the containing action set") &&
                             p.Contains("Time range"))),
                expectedResult ? Times.Once() : Times.Never());
        }

        [TestMethod]
        public async Task ExecuteReturnsFalseWhenOverrideHasBothExcludeAndIncludeForSameTime()
        {
            ActionRefCore refCore;
            object modelIn;

            IDictionary<DateTime, ICollection<TimeRangeOverride>> overrides =
                new Dictionary<DateTime, ICollection<TimeRangeOverride>>
                {
                    {
                        new DateTime(2018, 7, 1), new[] 
                        {
                            new TimeRangeOverride { Exclude = true },
                            new TimeRangeOverride { Exclude = false }
                        }
                    },
                };

            ExecuteResult result;

            (_, refCore, _, modelIn) = this.SetupTestObj(
                now: new DateTimeOffset(2018, 7, 1, 12, 0, 0, TimeSpan.Zero),
                timeZoneId: "UTC",
                overrides: overrides);

            // test
            result = await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // verify
            Assert.IsFalse(result.Continue);

            this.mockExecCtx.Verify(
                o => o.Log(
                    It.Is<string>(
                        p => p.Contains("continuning to process the containing action set") &&
                             p.Contains("Override range"))),
                Times.Once);
        }


        [TestMethod]
        public async Task ExecuteReturnsFalseWhenOverrideExcludesButAllowedDayAndTimeAllows()
        {
            ActionRefCore refCore;
            object modelIn;

            IDictionary<DateTime, ICollection<TimeRangeOverride>> overrides =
                new Dictionary<DateTime, ICollection<TimeRangeOverride>>
                {
                    {
                        new DateTime(2018, 7, 1), new[]
                        {
                            new TimeRangeOverride { Exclude = true },
                            new TimeRangeOverride { Exclude = false }
                        }
                    },
                };

            IDictionary<DayOfWeekExt, ICollection<TimeRange>> allowed =
                new Dictionary<DayOfWeekExt, ICollection<TimeRange>>
                {
                    { DayOfWeekExt.Sunday, new[] { new TimeRange() } }
                };


            ExecuteResult result;

            (_, refCore, _, modelIn) = this.SetupTestObj(
                now: new DateTimeOffset(2018, 7, 1, 12, 0, 0, TimeSpan.Zero),
                timeZoneId: "UTC",
                overrides: overrides,
                allowed: allowed);

            // test
            result = await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // verify
            Assert.IsFalse(result.Continue);

            this.mockExecCtx.Verify(
                o => o.Log(
                    It.Is<string>(
                        p => p.Contains("continuning to process the containing action set") &&
                             p.Contains("Override range"))),
                Times.Once);
        }
    }
}