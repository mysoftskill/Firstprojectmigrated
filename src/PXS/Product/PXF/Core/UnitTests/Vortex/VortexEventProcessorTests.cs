// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.Vortex
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.Core.Vortex;
    using Microsoft.Membership.MemberServices.Privacy.Core.Vortex.Event;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.PcfAdapter;
    using Microsoft.Membership.MemberServices.Test.Common;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Newtonsoft.Json.Linq;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     Tests <see cref="VortexEventProcessor" />
    /// </summary>
    [TestClass]
    public class VortexEventProcessorTests
    {
        private readonly Policy policy = Policies.Current;

        private Mock<IMsaIdentityServiceAdapter> mockIMsaIdentityServiceAdapter;

        private Mock<ILogger> mockLogger;

        private Mock<IPcfAdapter> mockPcfAdapter;

        public static IEnumerable<object[]> CreateVortexEventProcessorTestData()
        {
            var info = new VortexRequestInformation { IsWatchdogRequest = false, WasCompressed = false, RequestTime = DateTimeOffset.UtcNow };
            var msaIdentity = GreateMockMsaIdentityServiceAdapter();
            var logger = TestMockFactory.CreateLogger();
            var data = new List<object[]>
            {
                new object[] { info, null, logger.Object, "Value cannot be null.\r\nParameter name: msaIdentityServiceAdapter" },
                new object[] { info, msaIdentity.Object, null, "Value cannot be null.\r\nParameter name: logger" },
                new object[] { null, msaIdentity.Object, logger.Object, "Value cannot be null.\r\nParameter name: info" }
            };
            return data;
        }

        public static Mock<IMsaIdentityServiceAdapter> GreateMockMsaIdentityServiceAdapter()
        {
            var mockMsaIdentity = new Mock<IMsaIdentityServiceAdapter>(MockBehavior.Strict);
            mockMsaIdentity
                .Setup(c => c.GetGdprDeviceDeleteVerifierAsync(It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<string>()))
                .ReturnsAsync(new AdapterResponse<string> { Result = "i_am_a_device_delete_verifier_token" });
            return mockMsaIdentity;
        }

        [TestMethod]
        [DynamicData(nameof(CreateVortexEventProcessorTestData), DynamicDataSourceType.Method)]
        public void CreateVortexEventProcessorNullHandling(
            VortexRequestInformation info,
            IMsaIdentityServiceAdapter msaIdentityServiceAdapter,
            ILogger logger,
            string expectedMessage)
        {
            try
            {
                var requestGuid = Guid.NewGuid();
                var evt = JObject.Parse(VortexTestSettings.JsonEvent).ToObject<VortexEvent>();
                var processor = new VortexEventProcessor(evt, info, this.policy, requestGuid, msaIdentityServiceAdapter, logger);
                Assert.Fail("should not be here");
            }
            catch (ArgumentNullException e)
            {
                Assert.AreEqual(expectedMessage, e.Message);
            }
        }

        [TestInitialize]
        public void Init()
        {
            this.mockPcfAdapter = new Mock<IPcfAdapter>(MockBehavior.Strict);
            this.mockLogger = TestMockFactory.CreateLogger();
            this.mockIMsaIdentityServiceAdapter = GreateMockMsaIdentityServiceAdapter();
        }

        [TestMethod]
        public async Task VortexEventProcessorInvalidInput()
        {
            DataTypes.KnownIds ids = this.policy.DataTypes.Ids;
            VortexTestSettings.CreateSignalWriterMocks(
                ids,
                out Mock<IPcfAdapter> pcfAdapter,
                out Mock<IMsaIdentityServiceAdapter> msaIdentityServiceAdapter);

            var evt = JObject.Parse(VortexTestSettings.BadJsonEvent).ToObject<VortexEvent>();
            var info = new VortexRequestInformation
            {
                IsWatchdogRequest = false,
                WasCompressed = false,
                HadServerName = false,
                HadUserAgent = false,
                ServedBy = "doesnotmatter",
                UserAgent = "dontcare",
                RequestTime = DateTimeOffset.MinValue,
            };
            var processor = new VortexEventProcessor(evt, info, this.policy, Guid.Empty, msaIdentityServiceAdapter.Object, new ConsoleLogger());

            ServiceResponse<VortexEventProcessingResults> results = await processor.ProcessAsync().ConfigureAwait(false);

            Assert.IsFalse(results.IsSuccess);
            Assert.IsNotNull(results.Error.Code);
            Assert.AreEqual(results.Error.Code, "InvalidInput");
        }

        /// <summary>
        ///     Checks that Delete Feed is not called if we failed to write to event grid
        /// </summary>
        /// <returns>Test task</returns>
        [TestMethod]
        public async Task VortexEventProcessorShouldNotCallDeleteFeedIfPcfFails()
        {
            Policy policy = Policies.Current;
            Guid requestGuid = Guid.NewGuid();
            var pcfAdapter = new Mock<IPcfAdapter>(MockBehavior.Strict);
            const string msg = "Unique exception message";
            pcfAdapter.Setup(ega => ega.PostCommandsAsync(It.IsAny<List<PrivacyRequest>>())).Throws(new Exception(msg));

            var msaIdentityServiceAdapter = new Mock<IMsaIdentityServiceAdapter>(MockBehavior.Strict);
            msaIdentityServiceAdapter
                .Setup(c => c.GetGdprDeviceDeleteVerifierAsync(It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<string>()))
                .ReturnsAsync(new AdapterResponse<string> { Result = "i_am_a_device_delete_verifier_token" });

            var evt = JObject.Parse(VortexTestSettings.JsonEvent).ToObject<VortexEvent>();
            var info = new VortexRequestInformation { IsWatchdogRequest = false, WasCompressed = false, RequestTime = DateTimeOffset.UtcNow };
            var processor = new VortexEventProcessor(evt, info, policy, requestGuid, msaIdentityServiceAdapter.Object, new ConsoleLogger());

            bool hadException = false;

            try
            {
                ServiceResponse<VortexEventProcessingResults> results = await processor.ProcessAsync().ConfigureAwait(false);
                Assert.IsTrue(results.IsSuccess);
                Assert.IsNotNull(results.Result.PcfDeleteRequests);
                Assert.IsTrue(results.Result.PcfDeleteRequests.All(r => r.ControllerApplicable), "ControllerApplicable should be true");
                Assert.IsTrue(results.Result.PcfDeleteRequests.All(r => !r.ProcessorApplicable), "ProcessorApplicable should be false");
                foreach (DeleteRequest req in results.Result.PcfDeleteRequests)
                {
                    Assert.AreEqual(requestGuid, req.RequestGuid);
                }

                await pcfAdapter.Object.PostCommandsAsync(results.Result.PcfDeleteRequests.Cast<PrivacyRequest>().ToList()).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                hadException = true;
                Assert.AreEqual(msg, e.Message);
            }

            Assert.IsTrue(hadException);
        }

        [TestMethod]
        public async Task VortexEventProcessorShouldNotProcessAnyFailedAdapterResponse()
        {
            Policy policy = Policies.Current;
            DataTypes.KnownIds ids = policy.DataTypes.Ids;
            VortexTestSettings.CreateSignalWriterMocks(
                ids,
                out Mock<IPcfAdapter> pcfAdapter,
                out Mock<IMsaIdentityServiceAdapter> msaIdentityServiceAdapter);

            msaIdentityServiceAdapter
                .Setup(c => c.GetGdprDeviceDeleteVerifierAsync(It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<string>()))
                .ReturnsAsync(
                    new AdapterResponse<string>
                    {
                        Error = new AdapterError(AdapterErrorCode.InvalidInput, "invalid input", (int)HttpStatusCode.InternalServerError),
                        Result = null
                    });

            var evt = JObject.Parse(VortexTestSettings.LegacyJsonEvent).ToObject<VortexEvent>();
            var info = new VortexRequestInformation { IsWatchdogRequest = false, WasCompressed = false, RequestTime = DateTimeOffset.UtcNow };
            var processor = new VortexEventProcessor(evt, info, policy, Guid.Empty, msaIdentityServiceAdapter.Object, new ConsoleLogger());

            //Act
            ServiceResponse<VortexEventProcessingResults> results = await processor.ProcessAsync().ConfigureAwait(false);

            //Assert
            Assert.IsFalse(results.IsSuccess);
            Assert.AreEqual(ErrorCode.PartnerError.ToString(), results.Error.Code);
        }

        /// <summary>
        ///     Verifies that if the event doesn't have a correlation vector, that it doesn't break
        /// </summary>
        /// <returns>Test task</returns>
        [TestMethod]
        public async Task VortexEventProcessorShouldProcessEventWithoutCorrelationVector()
        {
            Policy policy = Policies.Current;
            DataTypes.KnownIds ids = policy.DataTypes.Ids;
            VortexTestSettings.CreateSignalWriterMocks(
                ids,
                out Mock<IPcfAdapter> pcfAdapter,
                out Mock<IMsaIdentityServiceAdapter> msaIdentityServiceAdapter);

            var evt = JObject.Parse(VortexTestSettings.LegacyJsonEventNoCv).ToObject<VortexEvent>();
            var info = new VortexRequestInformation { IsWatchdogRequest = false, WasCompressed = false, RequestTime = DateTimeOffset.UtcNow };
            var processor = new VortexEventProcessor(evt, info, policy, Guid.Empty, msaIdentityServiceAdapter.Object, new ConsoleLogger());

            ServiceResponse<VortexEventProcessingResults> results = await processor.ProcessAsync().ConfigureAwait(false);
            await pcfAdapter.Object.PostCommandsAsync(results.Result.PcfDeleteRequests.Cast<PrivacyRequest>().ToList()).ConfigureAwait(false);

            VortexTestSettings.VerifyDeleteRequestProcessed(pcfAdapter, msaIdentityServiceAdapter, ids, false);
        }

        /// <summary>
        ///     Checks that the <see cref="VortexEventProcessor" /> handles the common schema v2.0 events
        /// </summary>
        /// <returns>Test task</returns>
        [TestMethod]
        public async Task VortexEventProcessorShouldProcessVersion20Events()
        {
            Policy policy = Policies.Current;
            DataTypes.KnownIds ids = policy.DataTypes.Ids;
            VortexTestSettings.CreateSignalWriterMocks(
                ids,
                out Mock<IPcfAdapter> pcfAdapter,
                out Mock<IMsaIdentityServiceAdapter> msaIdentityServiceAdapter);

            var evt = JObject.Parse(VortexTestSettings.LegacyJsonEvent).ToObject<VortexEvent>();
            var info = new VortexRequestInformation { IsWatchdogRequest = false, WasCompressed = false, RequestTime = DateTimeOffset.UtcNow };
            var processor = new VortexEventProcessor(evt, info, policy, Guid.Empty, msaIdentityServiceAdapter.Object, new ConsoleLogger());

            ServiceResponse<VortexEventProcessingResults> results = await processor.ProcessAsync().ConfigureAwait(false);
            await pcfAdapter.Object.PostCommandsAsync(results.Result.PcfDeleteRequests.Cast<PrivacyRequest>().ToList()).ConfigureAwait(false);

            VortexTestSettings.VerifyDeleteRequestProcessed(pcfAdapter, msaIdentityServiceAdapter, ids);
        }

        /// <summary>
        ///     Checks that the <see cref="VortexEventProcessor" /> handles the common schema v2.1 events
        /// </summary>
        /// <returns>Test task</returns>
        [TestMethod]
        public async Task VortexEventProcessorShouldProcessVersion21Events()
        {
            Policy policy = Policies.Current;
            DataTypes.KnownIds ids = policy.DataTypes.Ids;

            VortexTestSettings.CreateSignalWriterMocks(
                ids,
                out Mock<IPcfAdapter> pcfAdapter,
                out Mock<IMsaIdentityServiceAdapter> msaIdentityServiceAdapter);

            var evt = JObject.Parse(VortexTestSettings.JsonEvent).ToObject<VortexEvent>();
            var info = new VortexRequestInformation { IsWatchdogRequest = false, WasCompressed = false, RequestTime = DateTimeOffset.UtcNow };
            var processor = new VortexEventProcessor(evt, info, policy, Guid.Empty, msaIdentityServiceAdapter.Object, new ConsoleLogger());

            ServiceResponse<VortexEventProcessingResults> results = await processor.ProcessAsync().ConfigureAwait(false);
            await pcfAdapter.Object.PostCommandsAsync(results.Result.PcfDeleteRequests.Cast<PrivacyRequest>().ToList()).ConfigureAwait(false);

            VortexTestSettings.VerifyDeleteRequestProcessed(pcfAdapter, msaIdentityServiceAdapter, ids);
        }

        [TestMethod]
        public async Task VortexEventProcessorShouldThrowExceptionWithInvalidDeviceId()
        {
            Policy policy = Policies.Current;
            DataTypes.KnownIds ids = policy.DataTypes.Ids;
            VortexTestSettings.CreateSignalWriterMocks(
                ids,
                out Mock<IPcfAdapter> pcfAdapter,
                out Mock<IMsaIdentityServiceAdapter> msaIdentityServiceAdapter);

            var evt = JObject.Parse(VortexTestSettings.LegacyJsonEvent).ToObject<VortexEvent>();
            evt.LegacyDeviceId = null;
            var info = new VortexRequestInformation { IsWatchdogRequest = false, WasCompressed = false, RequestTime = DateTimeOffset.UtcNow  };
            var processor = new VortexEventProcessor(evt, info, policy, Guid.Empty, msaIdentityServiceAdapter.Object, new ConsoleLogger());

            //Act
            ServiceResponse<VortexEventProcessingResults> results = await processor.ProcessAsync().ConfigureAwait(false);

            //Assert
            Assert.IsFalse(results.IsSuccess);
            Assert.AreEqual(ErrorCode.InvalidInput.ToString(), results.Error.Code);
        }
    }
}
