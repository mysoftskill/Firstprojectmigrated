// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.UnitTests.Actions
{
    using System;
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Actions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class TimeApplicabilityDefTests
    {
        private readonly Mock<IContext> mockCtx = new Mock<IContext>();

        [TestMethod]
        [DataRow(null, null, true, null)]
        [DataRow(0, 24, true, null)]
        [DataRow(0, null, true, null)]
        [DataRow(null, 24, true, null)]
        [DataRow(2, 23, true, null)]
        [DataRow(-1, null, false, "start time must be a valid time of day")]
        [DataRow(24, null, false, "start time must be a valid time of day")]
        [DataRow(null, -1, false, "end time must be a valid time of day")]
        [DataRow(null, 25, false, "end time must be a valid time of day")]
        [DataRow(1, 1, false, "start time must be strictly before end time")]
        [DataRow(2, 1, false, "start time must be strictly before end time")]
        public void TimeRangeValidateReturnsCorrectValue(
            int? hourStart,
            int? hourEnd,
            bool expected,
            string expectedCtxText)
        {
            TimeRange testObj = new TimeRange
            {
                Start = hourStart.HasValue ? TimeSpan.FromHours(hourStart.Value) : (TimeSpan?)null,
                End = hourEnd.HasValue ? TimeSpan.FromHours(hourEnd.Value) : (TimeSpan?)null,
            };

            // test
            bool result = testObj.ValidateAndNormalize(this.mockCtx.Object);

            // validate
            Assert.AreEqual(expected, result);

            if (expectedCtxText != null)
            {
                this.mockCtx.Verify(o => o.LogError(It.Is<string>(p => p.Contains(expectedCtxText))), Times.Once);
            }
        }

        [TestMethod]
        public void TimeApplicabilityValidateReturnsTrueIfRangeAndOverridesAreNull()
        {
            TimeApplicabilityDef testObj = new TimeApplicabilityDef();

            bool result = testObj.ValidateAndNormalize(this.mockCtx.Object);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TimeApplicabilityValidateReturnsTrueIfAllItemsValid()
        {
            TimeApplicabilityDef testObj = new TimeApplicabilityDef
            {
                AllowedDaysAndTimes = new Dictionary<DayOfWeekExt, ICollection<TimeRange>>
                {
                    { DayOfWeekExt.Monday, new[] { new TimeRange { Start = TimeSpan.FromHours(10), End = TimeSpan.FromHours(16) } } }
                },
                Overrides = new Dictionary<DateTime, ICollection<TimeRangeOverride>>
                {
                    { 
                        new DateTime(2018, 07, 04),
                        new[] { new TimeRangeOverride { Start = TimeSpan.Zero, End = TimeSpan.FromHours(24), Exclude = true } }
                    },
                },
            };

            bool result = testObj.ValidateAndNormalize(this.mockCtx.Object);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TimeApplicabilityValidateReturnsFalseIfInvalidDayOfWeekSpecified()
        {
            TimeApplicabilityDef testObj = new TimeApplicabilityDef
            {
                AllowedDaysAndTimes = new Dictionary<DayOfWeekExt, ICollection<TimeRange>>
                {
                    { (DayOfWeekExt)int.MaxValue, null }
                },
            };

            // test
            bool result = testObj.ValidateAndNormalize(this.mockCtx.Object);

            // verify
            Assert.IsFalse(result);
            this.mockCtx.Verify(o => o.LogError(It.Is<string>(p => p.Contains("is not a valid day of the week"))), Times.Once);
        }

        [TestMethod]
        public void TimeApplicabilityValidateReturnsFalseIfInvalidTimeRangeSpecifiedInAllowedDaysAndTimes()
        {
            TimeApplicabilityDef testObj = new TimeApplicabilityDef
            {
                AllowedDaysAndTimes = new Dictionary<DayOfWeekExt, ICollection<TimeRange>>
                {
                    { DayOfWeekExt.Monday, new[] { new TimeRange { Start = TimeSpan.FromHours(-1) } } }
                },
            };

            // test
            bool result = testObj.ValidateAndNormalize(this.mockCtx.Object);

            // verify
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TimeApplicabilityValidateReturnsFalseIfTimeOfDaySpecifiedForOverrideDate()
        {
            TimeApplicabilityDef testObj = new TimeApplicabilityDef
            {
                Overrides = new Dictionary<DateTime, ICollection<TimeRangeOverride>>
                {
                    {
                        new DateTime(2018, 07, 04, 1, 1, 1),
                        new[] { new TimeRangeOverride { Start = TimeSpan.Zero, End = TimeSpan.FromHours(24), Exclude = true } }
                    },
                },
            };

            // test
            bool result = testObj.ValidateAndNormalize(this.mockCtx.Object);

            // verify
            Assert.IsFalse(result);
            this.mockCtx.Verify(o => o.LogError(It.Is<string>(p => p.Contains("must not specify a time of day"))), Times.Once);
        }

        [TestMethod]
        public void TimeApplicabilityValidateReturnsFalseIfInvalidTimeRangeSpecifiedInOverrides()
        {
            TimeApplicabilityDef testObj = new TimeApplicabilityDef
            {
                Overrides = new Dictionary<DateTime, ICollection<TimeRangeOverride>>
                {
                    {
                        new DateTime(2018, 07, 04),
                        new[] { new TimeRangeOverride { Start = TimeSpan.FromHours(-1) } }
                    },
                },
            };

            // test
            bool result = testObj.ValidateAndNormalize(this.mockCtx.Object);

            // verify
            Assert.IsFalse(result);
        }
    }
}
