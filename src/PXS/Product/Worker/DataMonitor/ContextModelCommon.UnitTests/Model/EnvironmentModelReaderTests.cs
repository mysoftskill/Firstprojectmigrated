// ---------------------------------------------------------------------------
// <copyright file="AggregatingModelManipulatorTests.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.UnitTests.Utility.Model
{
    using System;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class EnvironmentModelReaderTests
    {
        private readonly Mock<IContext> mockCtx = new Mock<IContext>();
        private readonly Mock<IClock> mockClock = new Mock<IClock>();

        private EnvironmentModelReader testObj;
        private IContext ctx;

        [TestInitialize]
        public void Init()
        {
            this.testObj = new EnvironmentModelReader(this.mockClock.Object);

            this.ctx = this.mockCtx.Object;

            this.mockClock.SetupGet(o => o.UtcNow).Returns(DateTimeOffset.Parse("2018-01-01 00:00:00Z"));
        }

        [TestMethod]
        public void TryExtractReturnsNullWhenUnknownSelectorPassed()
        {
            object value;
            bool result;

            // test 
            result = this.testObj.TryExtractValue(this.ctx, null, "#.TobyDog", null, out value);

            // verify
            Assert.IsFalse(result);
            Assert.IsNull(value);
        }

        [TestMethod]
        public void TryExtractReturnsReturnsTimeWhenUtcNowRequested()
        {
            object value;
            bool result;

            // test 
            result = this.testObj.TryExtractValue(this.ctx, null, "#.Time.Now.Utc", null, out value);

            // verify
            Assert.IsTrue(result);
            Assert.AreEqual(this.mockClock.Object.UtcNow, value);
        }

        [TestMethod]
        public void TryExtractReturnsReturnsTimeConvertedToPSTWhenUtcLocalRequested()
        {
            DateTimeOffset expected = new DateTimeOffset(2017, 12, 31, 16, 0, 0, 0, TimeSpan.FromHours(-8));

            object value;
            bool result;

            // test 
            result = this.testObj.TryExtractValue(this.ctx, null, @"#.Time.Now.Local(""Pacific Standard Time"")", null, out value);

            // verify
            Assert.IsTrue(result);
            Assert.AreEqual(expected, value);
        }
    }
}
