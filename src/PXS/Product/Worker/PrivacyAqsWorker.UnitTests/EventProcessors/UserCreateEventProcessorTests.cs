// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.UnitTests.EventProcessors
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.Common;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.EventProcessors;
    using Microsoft.Membership.MemberServices.Privacy.Core.AQS;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using SocialAccessorV4;

    [TestClass]
    public class UserCreateEventProcessorTests
    {
        private Mock<ICounter> counter;

        private Mock<ICounterFactory> counterFactory;

        private Mock<IMsaIdentityServiceAdapter> identityMock;

        [TestInitialize]
        public void Initialize()
        {
            this.counter = new Mock<ICounter>();
            this.counterFactory = new Mock<ICounterFactory>(MockBehavior.Strict);
            this.identityMock = new Mock<IMsaIdentityServiceAdapter>(MockBehavior.Strict);
        }

        [TestMethod]
        public async Task ShouldCalculateCorrectValuesAsync()
        {
            const string aggregationKey = "ABC123";
            ulong puid = ulong.Parse(aggregationKey, NumberStyles.AllowHexSpecifier);

            const long cidValue = 0xabcde123;
            this.identityMock.Setup(im => im.GetSigninNameInformationsAsync(It.IsAny<IEnumerable<long>>())).ReturnsAsync(
                new AdapterResponse<IEnumerable<ISigninNameInformation>>
                {
                    Result = new[]
                    {
                        new SigninNameInformation
                        {
                            Puid = (long)puid,
                            Cid = cidValue
                        }
                    }
                });

            var evt = new CDPEvent2
            {
                AggregationKey = aggregationKey
            };

            this.counterFactory.Setup(cf => cf.GetCounter(CounterCategoryNames.MsaAccountCreate, "success", CounterType.Rate)).Returns(this.counter.Object);

            var handler = new UserCreateEventProcessor(this.identityMock.Object, this.counterFactory.Object);
            AdapterResponse<IList<AccountCreateInformation>> result = await handler.ProcessCreateItemsAsync(new[] { evt }).ConfigureAwait(false);

            AccountCreateInformation resultCreateInfo = result.Result.FirstOrDefault();
            Assert.IsNotNull(resultCreateInfo);
            Assert.AreEqual(puid, resultCreateInfo.Puid);
            Assert.AreEqual(IdConverter.AnidFromPuid(puid), resultCreateInfo.Anid);
            Assert.AreEqual(IdConverter.OpidFromPuid(puid), resultCreateInfo.Opid);
            Assert.AreEqual(cidValue, resultCreateInfo.Cid);

            this.counter.Verify(c => c.IncrementBy(1));
        }

        [TestMethod]
        public async Task ShouldHandleGetSigningNameFailure()
        {
            this.identityMock.Setup(im => im.GetSigninNameInformationsAsync(It.IsAny<IEnumerable<long>>())).ReturnsAsync(
                new AdapterResponse<IEnumerable<ISigninNameInformation>>
                {
                    Error = new AdapterError(AdapterErrorCode.EmptyResponse, "This is a test", 500)
                });

            var evt = new CDPEvent2
            {
                AggregationKey = "ABC123"
            };

            this.counterFactory.Setup(cf => cf.GetCounter(CounterCategoryNames.MsaAccountCreate, "failure", CounterType.Rate)).Returns(this.counter.Object);

            var handler = new UserCreateEventProcessor(this.identityMock.Object, this.counterFactory.Object);
            AdapterResponse<IList<AccountCreateInformation>> result = await handler.ProcessCreateItemsAsync(new[] { evt }).ConfigureAwait(false);
            Assert.IsFalse(result.IsSuccess);

            this.counter.Verify(c => c.IncrementBy(1));
            this.counter.Verify(c => c.IncrementBy(1, "getsigninnames"));
        }

        [TestMethod]
        public async Task ShouldHandleIfCidNoLongerExists()
        {
            this.identityMock.Setup(im => im.GetSigninNameInformationsAsync(It.IsAny<IEnumerable<long>>())).ReturnsAsync(
                new AdapterResponse<IEnumerable<ISigninNameInformation>>
                {
                    Result = new SigninNameInformation[0]
                });

            var evt = new CDPEvent2
            {
                AggregationKey = "ABC123"
            };

            this.counterFactory.Setup(cf => cf.GetCounter(CounterCategoryNames.MsaAccountCreate, "failure", CounterType.Rate)).Returns(this.counter.Object);
            this.counterFactory.Setup(cf => cf.GetCounter(CounterCategoryNames.MsaAccountCreate, "success", CounterType.Rate)).Returns(this.counter.Object);

            var handler = new UserCreateEventProcessor(this.identityMock.Object, this.counterFactory.Object);
            AdapterResponse<IList<AccountCreateInformation>> result = await handler.ProcessCreateItemsAsync(new[] { evt }).ConfigureAwait(false);
            Assert.IsTrue(result.IsSuccess);

            this.counter.Verify(c => c.IncrementBy(0)); // success up by 0
            this.counter.Verify(c => c.IncrementBy(1)); // failure up by 1
            this.counter.Verify(c => c.IncrementBy(1, "missingcid")); // specific failure
        }
    }
}
