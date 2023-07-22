using System;
using Microsoft.PrivacyServices.UX.Configuration;
using Microsoft.PrivacyServices.UX.Models.AppBootstrap;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.PrivacyServices.UX.Tests.Models
{
    [TestClass]
    public class NgpLockdownModelTests
    {
        [TestMethod]
        public void Ctor_Initializes_Properties_Properly()
        {
            var model = new NgpLockdownModel();

            Assert.IsFalse(model.IsActive);
            Assert.IsNull(model.StartedUtc);
            Assert.IsNull(model.EndedUtc);
        }

        [TestMethod]
        public void CreateFromConfig_ForcedKind_AlwaysActive()
        {
            var config = new Mock<INgpLockdownConfig>(MockBehavior.Strict);
            config.SetupGet(c => c.Kind).Returns(NgpLockdownKind.Forced);

            var model = NgpLockdownModel.CreateFromConfig(config.Object);

            Assert.IsTrue(model.IsActive);
            Assert.IsFalse(string.IsNullOrEmpty(model.StartedUtc));
            Assert.IsFalse(string.IsNullOrEmpty(model.EndedUtc));
        }

        [TestMethod]
        public void CreateFromConfig_ScheduledKind_InvalidStartedUtc_NotActive()
        {
            var config = new Mock<INgpLockdownConfig>(MockBehavior.Strict);
            config.SetupGet(c => c.Kind).Returns(NgpLockdownKind.Scheduled);
            config.SetupGet(c => c.StartedUtc).Returns("invalid value");
            config.SetupGet(c => c.EndedUtc).Returns("2018-05-20");

            var model = NgpLockdownModel.CreateFromConfig(config.Object);

            Assert.IsFalse(model.IsActive);
            Assert.IsNull(model.StartedUtc);
            Assert.IsNull(model.EndedUtc);
        }

        [TestMethod]
        public void CreateFromConfig_ScheduledKind_InvalidEndedUtc_NotActive()
        {
            var config = new Mock<INgpLockdownConfig>(MockBehavior.Strict);
            config.SetupGet(c => c.Kind).Returns(NgpLockdownKind.Scheduled);
            config.SetupGet(c => c.StartedUtc).Returns("2018-05-20");
            config.SetupGet(c => c.EndedUtc).Returns("invalid value");

            var model = NgpLockdownModel.CreateFromConfig(config.Object);

            Assert.IsFalse(model.IsActive);
            Assert.IsNull(model.StartedUtc);
            Assert.IsNull(model.EndedUtc);
        }

        [TestMethod]
        public void CreateFromConfig_ScheduledKind_DateOutOfRange_NotActive()
        {
            var config = new Mock<INgpLockdownConfig>(MockBehavior.Strict);
            config.SetupGet(c => c.Kind).Returns(NgpLockdownKind.Scheduled);
            config.SetupGet(c => c.StartedUtc).Returns("2000-01-01");
            config.SetupGet(c => c.EndedUtc).Returns("2001-01-01");

            var model = NgpLockdownModel.CreateFromConfig(config.Object);

            Assert.IsFalse(model.IsActive);
            Assert.IsNull(model.StartedUtc);
            Assert.IsNull(model.EndedUtc);
        }

        [TestMethod]
        public void CreateFromConfig_ScheduledKind_DateInRange_Active()
        {
            var today = DateTimeOffset.UtcNow.Date;

            var config = new Mock<INgpLockdownConfig>(MockBehavior.Strict);
            config.SetupGet(c => c.Kind).Returns(NgpLockdownKind.Scheduled);
            config.SetupGet(c => c.StartedUtc).Returns(today.AddDays(-1).ToString("o"));
            config.SetupGet(c => c.EndedUtc).Returns(today.AddDays(1).ToString("o"));

            var model = NgpLockdownModel.CreateFromConfig(config.Object);

            Assert.IsTrue(model.IsActive);
            Assert.IsFalse(string.IsNullOrEmpty(model.StartedUtc));
            Assert.IsFalse(string.IsNullOrEmpty(model.EndedUtc));
        }
    }
}
