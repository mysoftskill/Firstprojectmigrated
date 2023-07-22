// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.UnitTests.EventProcessors
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading.Tasks;

    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.Common;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.EventProcessors;
    using Microsoft.Membership.MemberServices.Privacy.Core.AQS;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Microsoft.PrivacyServices.Common.Azure;

    [TestClass]
    public class UserDeleteEventHandlerV2Tests : SharedTestFunctions
    {
        private Mock<ICounter> counter;

        private Mock<ICounterFactory> counterFactory;

        private Mock<ILogger> logger;

        private Mock<IClock> clock = new Mock<IClock>();

        public static IEnumerable<object[]> GenerateMyException()
        {
            var counterFactory = CreateMockCounterFactory();

            var cdpEvent2Helper = new CdpEvent2Helper(CreateMockConf(), new Mock<ILogger>().Object);

            var message = "Value cannot be null.\r\nParameter name: ";

            List<string> parameters = new List<string>
            {
                "counterFactory",
                "eventHelper",
                "logger",
                "clock"
            };

            var logger = new Mock<ILogger>(MockBehavior.Loose);
            var clock = new Mock<IClock>(MockBehavior.Loose);

            var returnObject = new List<object[]>
            {
                new object[]
                {
                    null,
                    cdpEvent2Helper,
                    message + parameters[0],
                    logger.Object,
                    clock.Object
                },
                new object[]
                {
                    counterFactory.Object,
                    null,
                    message + parameters[1],
                    logger.Object,
                    clock.Object
                },
                new object[]
                {
                    counterFactory.Object,
                    cdpEvent2Helper,
                    message + parameters[2],
                    null,
                    clock.Object
                },
                new object[]
                {
                    counterFactory.Object,
                    cdpEvent2Helper,
                    message + parameters[3],
                    logger.Object,
                    null,
                }
            };

            return returnObject;
        }

        [TestInitialize]
        public void Initialize()
        {
            Sll.ResetContext();

            this.counter = new Mock<ICounter>();
            this.counterFactory = new Mock<ICounterFactory>(MockBehavior.Strict);
            this.logger = new Mock<ILogger>(MockBehavior.Strict);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Sll.ResetContext();
        }

        [DataTestMethod]
        [DataRow("", "missingcid")]
        [DataRow("cid:123abc", "missingpreverifier")]
        public async Task ShouldErrorIfDataMissing(string data, string specificFailure)
        {
            this.counterFactory.Setup(cf => cf.GetCounter(CounterCategoryNames.MsaAccountClose, "failure", CounterType.Rate)).Returns(this.counter.Object);
            
            var evtHandler = new UserDeleteEventProcessorV2(
                new CdpEvent2Helper(null, this.logger.Object),
                this.clock.Object,
                this.counterFactory.Object,
                this.logger.Object);

            CDPEvent2 evt = MakeEvent(
                "abcdef",
                new UserDelete
                {
                    Property = new[]
                    {
                        new EventDataBaseProperty
                        {
                            Name = "CredentialName",
                            ExtendedData = data
                        }
                    }
                });

            AdapterResponse<AccountDeleteInformation> result = await evtHandler.ProcessDeleteItemAsync(evt).ConfigureAwait(false);
            Assert.IsFalse(result.IsSuccess);

            // Should increment overall failure and specific instance
            this.counter.Verify(c => c.Increment(), Times.Once);
            this.counter.Verify(c => c.Increment(specificFailure), Times.Once);
        }

        [DataTestMethod]
        [DataRow(AccountCloseReason.UserAccountAgedOut, 2)]
        [DataRow(AccountCloseReason.UserAccountClosed, 1)]
        [DataRow(AccountCloseReason.UserAccountCreationFailure, 0)]
        [DataRow(AccountCloseReason.UserAccountClosed, -1)]
        public async Task ShouldLogDeleteWithReasons(AccountCloseReason expectedReason, int reason)
        {
            this.clock.Setup(c => c.UtcNow).Returns(DateTimeOffset.Parse("2019-01-01"));
            this.counterFactory.Setup(cf => cf.GetCounter(CounterCategoryNames.MsaAccountClose, "success", CounterType.Rate)).Returns(this.counter.Object);
            Mock<ICounter> mockAgeOutCounter = new Mock<ICounter>(MockBehavior.Strict);
            mockAgeOutCounter.Setup(c => c.Increment());
            this.counterFactory.Setup(cf => cf.GetCounter(CounterCategoryNames.MsaAgeOut, "success", CounterType.Rate)).Returns(mockAgeOutCounter.Object);

            var evtHandler = new UserDeleteEventProcessorV2(
                new CdpEvent2Helper(null, this.logger.Object),
                this.clock.Object,
                this.counterFactory.Object,
                this.logger.Object);
            const string puid = "123abc";
            CDPEvent2 evt = MakeEvent(
                puid,
                new UserDelete
                {
                    Property = new[]
                    {
                        new EventDataBaseProperty
                        {
                            Name = "CredentialName",
                            ExtendedData = $"UserDeleteReason:{reason},cid:123abc,GdprPreVerifier:gdpr{(reason == 2 ? ",LastSuccessSignIn:2000-01-01T14:05:00Z,Suspended:false" : "")}"
                        }
                    }
                });

            AdapterResponse<AccountDeleteInformation> result = await evtHandler.ProcessDeleteItemAsync(evt).ConfigureAwait(false);

            AccountDeleteInformation resultDeleteInformation = result.Result;
            Assert.AreEqual(long.Parse(puid, NumberStyles.AllowHexSpecifier), resultDeleteInformation.Puid);
            Assert.AreEqual(expectedReason, resultDeleteInformation.Reason);

            this.counter.Verify(c => c.Increment(), Times.Once);
        }

        [DataTestMethod]
        [DataRow("2000-01-01", "3000-01-01", "false", MsaAgeOutErrorCode.LastLoginTimeInFuture)]
        [DataRow("2004-01-01", "2000-01-01", "false", MsaAgeOutErrorCode.LastLoginLessThan5Years)]
        [DataRow("2001-01-01", "2000-01-01", "false", MsaAgeOutErrorCode.LastLoginLessThan2Years)]
        [DataRow("2020-01-01", "2000-01-01", "", MsaAgeOutErrorCode.MissingIsSuspendedValue)]
        [DataRow("2020-01-01", "", "false", MsaAgeOutErrorCode.MissingLastLoginTime)]
        public async Task ShouldLogMsaAgeOutErrors(string currentTime, string lastSuccessSignInTime, string suspendedValue, MsaAgeOutErrorCode expectedErrorCode)
        {
            this.clock.Setup(c => c.UtcNow).Returns(DateTimeOffset.Parse(currentTime));
            Mock<ICounter> mockAgeOutCounter = new Mock<ICounter>(MockBehavior.Strict);
            mockAgeOutCounter.Setup(c => c.Increment());
            mockAgeOutCounter.Setup(c => c.Increment(expectedErrorCode.ToString()));
            this.counterFactory.Setup(cf => cf.GetCounter(CounterCategoryNames.MsaAgeOut, "failure", CounterType.Rate)).Returns(mockAgeOutCounter.Object);
            this.logger.Setup(c => c.Error(nameof(MsaAgeOutEventValidation), It.IsAny<string>()));
            DateTimeOffset ? lastSuccessSignIn = null;

            if (!string.IsNullOrWhiteSpace(lastSuccessSignInTime))
            {
                lastSuccessSignIn = DateTimeOffset.Parse(lastSuccessSignInTime);
            }

            this.counterFactory.Setup(cf => cf.GetCounter(CounterCategoryNames.MsaAccountClose, "success", CounterType.Rate)).Returns(this.counter.Object);

            var evtHandler = new UserDeleteEventProcessorV2(
                new CdpEvent2Helper(null, this.logger.Object),
                this.clock.Object,
                this.counterFactory.Object,
                this.logger.Object);
            const string puid = "123abc";
            CDPEvent2 evt = MakeEvent(
                puid,
                new UserDelete
                {
                    Property = new[]
                    {
                        new EventDataBaseProperty
                        {
                            Name = "CredentialName",
                            ExtendedData = $"UserDeleteReason:2," +
                                           "cid:123abc," +
                                           "GdprPreVerifier:gdpr," +
                                           $"LastSuccessSignIn:{(lastSuccessSignIn.HasValue ? lastSuccessSignIn.Value.ToString("yyyy-MM-ddTHH:MM:ssZ") : "")}," +
                                           $"Suspended:{suspendedValue}"
                        }
                    }
                });

            AdapterResponse<AccountDeleteInformation> result = await evtHandler.ProcessDeleteItemAsync(evt).ConfigureAwait(false);

            AccountDeleteInformation resultDeleteInformation = result.Result;
            Assert.AreEqual(long.Parse(puid, NumberStyles.AllowHexSpecifier), resultDeleteInformation.Puid);
            Assert.AreEqual(AccountCloseReason.UserAccountAgedOut, resultDeleteInformation.Reason);

            this.counter.Verify(c => c.Increment(), Times.Once);
            mockAgeOutCounter.Verify(c => c.Increment(), Times.Once);
            mockAgeOutCounter.Verify(c => c.Increment(expectedErrorCode.ToString()), Times.Once);
            this.logger
                .Verify(
                    c =>
                        c.Error(
                            nameof(MsaAgeOutEventValidation),
                            It.Is<string>(s => s.StartsWith($"ErrorCode: {expectedErrorCode}"))),
                    Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        [DynamicData(nameof(GenerateMyException), DynamicDataSourceType.Method)]
        public void UserDeleteEventProcessorExceptionHandling(
            ICounterFactory counterFactory,
            CdpEvent2Helper eventHelper,
            string expectedErrorMessage,
            ILogger logger,
            IClock clock)
        {
            UserDeleteEventProcessorV2 userDeleteEventProcessor = new UserDeleteEventProcessorV2(
                eventHelper,
                clock,
                counterFactory,
                logger);
        }
    }
}
