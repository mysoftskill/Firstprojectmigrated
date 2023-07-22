// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests
{
    using System;
    using System.Collections.Generic;

    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    /// <summary>
    ///     DataManagementConfigLoader-Test
    /// </summary>
    [TestClass]
    public class DataManagementConfigLoaderTest
    {
        private readonly Mock<IAdaptersConfiguration> mockAdaptersConfiguration = new Mock<IAdaptersConfiguration>(MockBehavior.Strict);

        private readonly Mock<IPrivacyConfigurationManager> serviceConfiguration = new Mock<IPrivacyConfigurationManager>(MockBehavior.Strict);

        [TestInitialize]
        public void TestInitialize()
        {
            // reset context for SLL to prevent AppDomainUnloadedException
            Sll.ResetContext();

            var mockFixedIntervalRetryConfiguration = new Mock<IFixedIntervalRetryConfiguration>(MockBehavior.Strict);
            mockFixedIntervalRetryConfiguration.SetupGet(c => c.RetryCount).Returns(10);
            mockFixedIntervalRetryConfiguration.SetupGet(c => c.RetryIntervalInMilliseconds).Returns(100000000);

            var mockFixedRetryStrategyConfiguration = new Mock<IRetryStrategyConfiguration>(MockBehavior.Strict);
            mockFixedRetryStrategyConfiguration.SetupGet(c => c.RetryMode).Returns(RetryMode.FixedInterval);
            mockFixedRetryStrategyConfiguration.SetupGet(c => c.FixedIntervalRetryConfiguration).Returns(mockFixedIntervalRetryConfiguration.Object);

            var mocExponentialIntervalRetryConfiguration = new Mock<IExponentialBackOffRetryConfiguration>(MockBehavior.Strict);
            mocExponentialIntervalRetryConfiguration.SetupGet(c => c.RetryCount).Returns(10);
            mocExponentialIntervalRetryConfiguration.SetupGet(c => c.DeltaBackOffInMilliseconds).Returns(123);
            mocExponentialIntervalRetryConfiguration.SetupGet(c => c.MaxBackOffInMilliseconds).Returns(5000);
            mocExponentialIntervalRetryConfiguration.SetupGet(c => c.MinBackOffInMilliseconds).Returns(3000);

            Mock<IRetryStrategyConfiguration> mockExponentialRetryStrategyConfiguration = new Mock<IRetryStrategyConfiguration>(MockBehavior.Strict);
            mockExponentialRetryStrategyConfiguration.SetupGet(c => c.RetryMode).Returns(RetryMode.ExponentialBackOff);
            mockExponentialRetryStrategyConfiguration.SetupGet(c => c.ExponentialBackOffRetryConfiguration).Returns(mocExponentialIntervalRetryConfiguration.Object);

            this.mockAdaptersConfiguration
                .SetupGet(c => c.RetryStrategyConfigurations)
                .Returns(
                    new Dictionary<string, IRetryStrategyConfiguration>
                    {
                        { "FixedInterval", mockFixedRetryStrategyConfiguration.Object },
                        { "ExponentialInterval", mockExponentialRetryStrategyConfiguration.Object }
                    });
            this.mockAdaptersConfiguration.SetupGet(c => c.TimeoutInMilliseconds).Returns(5555);

            var mockCertificateConfiguration = new Mock<ICertificateConfiguration>(MockBehavior.Strict);
            mockCertificateConfiguration.SetupGet(c => c.Subject).Returns("My cert subject");

            this.serviceConfiguration.SetupGet(c => c.AdaptersConfiguration).Returns(this.mockAdaptersConfiguration.Object);

            var privacyExperienceServiceConfig = new Mock<IPrivacyExperienceServiceConfiguration>(MockBehavior.Strict);
            privacyExperienceServiceConfig.SetupGet(c => c.AdapterConfigurationSource).Returns(AdapterConfigurationSource.ConfigurationIniFile);

            this.mockAdaptersConfiguration.SetupGet(c => c.RingPartnerConfigOverrides).Returns(new Dictionary<string, IRingPartnerConfigOverride>());
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Sll.ResetContext();
        }
    }
}
