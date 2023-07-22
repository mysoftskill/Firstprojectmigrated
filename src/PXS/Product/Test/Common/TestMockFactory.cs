// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Test.Common
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using System.Xml;
    using Microsoft.Membership.MemberServices.Adapters.Common;
    using Microsoft.Membership.MemberServices.Adapters.Factory;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Contracts.Adapter.MsaTokenProvider;
    using Microsoft.PrivacyServices.Common.Azure;

    using Moq;


    /// <summary>
    /// Test Mock Factory
    /// </summary>
    public static class TestMockFactory
    {
        public const string CertificateSubject = "Subject";
        public const string CertificateIssuer = "Issuer";

        public const string TestAccountId = "6BcCIQAAAAAAAAAA"; // AccountId should correspond to the below test subscription id.
        public const string TestSubscriptionId = "6BcCIQAAAAAAAAMA"; // SubscriptionId must be valid format and encoding to support BdkId.cs APIs
        public const string TestSubscriptionPartnerId = "OneDrive";
        public const string TestPaymentMethodId = "fake payment method ID";
        public const string TestMemberViewCaller = "MemberviewDevTest";
        public const long TestBalanceId = 19201208;

        public const string SubscriptionStatusEnabled = "ENABLED";
        public const string SubscriptionStatusExpired = "EXPIRED";

        public const string OfficeReferralType = "O15SyndicatedSubscriptionXml";

        public const string ErrorCodePaymentDeclined = "0x80047611";

        public static Mock<ILogger> CreateLogger()
        {
            Mock<ILogger> mockLogger = new Mock<ILogger>();
            mockLogger.Setup((m) => m.Information(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
            mockLogger.Setup((m) => m.Verbose(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
            mockLogger.Setup((m) => m.Error(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
            mockLogger.Setup((m) => m.Warning(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())); 
            return mockLogger;
        }

        public static Mock<IRetryStrategyConfiguration> CreateFixedIntervalRetryStrategyConfiguration()
        {
            Mock<IFixedIntervalRetryConfiguration> mockFixedIntervalRetryStrategyConfiguration = new Mock<IFixedIntervalRetryConfiguration>(MockBehavior.Strict);
            mockFixedIntervalRetryStrategyConfiguration.Setup(m => m.RetryCount).Returns(() => 3);
            mockFixedIntervalRetryStrategyConfiguration.Setup(m => m.RetryIntervalInMilliseconds).Returns(() => 100);

            Mock<IRetryStrategyConfiguration> mockRetryStrategyConfiguration = new Mock<IRetryStrategyConfiguration>(MockBehavior.Strict);

            mockRetryStrategyConfiguration.Setup(m => m.RetryMode).Returns(() => RetryMode.FixedInterval);
            mockRetryStrategyConfiguration.Setup(m => m.FixedIntervalRetryConfiguration).Returns(() => mockFixedIntervalRetryStrategyConfiguration.Object);
            return mockRetryStrategyConfiguration;
        }

        public static Mock<IServicePointConfiguration> CreateServicePointConfiguration()
        {
            Mock<IServicePointConfiguration> mockServicePointConfiguration = new Mock<IServicePointConfiguration>(MockBehavior.Strict);
            mockServicePointConfiguration.Setup(m => m.ConnectionLeaseTimeout).Returns(1);
            mockServicePointConfiguration.Setup(m => m.ConnectionLimit).Returns(1);
            mockServicePointConfiguration.Setup(m => m.MaxIdleTime).Returns(1);

            return mockServicePointConfiguration;
        }

        public static Mock<ICounterFactory> CreateCounterFactory()
        {
            Mock<ICounter> mockCounter = new Mock<ICounter>();
            Mock<ICounterFactory> mockCounterFactory = new Mock<ICounterFactory>();
            mockCounterFactory.Setup(t => t.GetCounter(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CounterType>())).Returns(mockCounter.Object);
            return mockCounterFactory;
        }

        public static Mock<ICertificateProvider> CreateCertificateProvider(X509Certificate2 certificate)
        {
            Mock<ICertificateProvider> mockCertificateProvider = new Mock<ICertificateProvider>(MockBehavior.Strict);
            mockCertificateProvider.Setup(m => m.GetClientCertificate(It.IsAny<ICertificateConfiguration>())).Returns(certificate);
            return mockCertificateProvider;
        }
        public static Mock<ICertificateConfiguration> CreateCertificateConfiguration()
        {
            Mock<ICertificateConfiguration> mockCertificateConfiguration = new Mock<ICertificateConfiguration>();
            mockCertificateConfiguration.SetupGet(t => t.Issuer).Returns(CertificateIssuer);
            mockCertificateConfiguration.SetupGet(t => t.Subject).Returns(CertificateSubject);
            return mockCertificateConfiguration;
        }

        public static Mock<IAdapterContext> CreateAdapterContext(
            ILogger logger,
            ICounterFactory counterFactory)
        {
            Mock<IAdapterContext> mockAdapterContext = new Mock<IAdapterContext>();
            mockAdapterContext.SetupGet(m => m.Logger).Returns(logger);
            mockAdapterContext.SetupGet(m => m.CounterFactory).Returns(counterFactory);
            return mockAdapterContext;
        }

        public static Mock<IAdapterContext> CreateAdapterContext()
        {
            Mock<ILogger> mockLogger = TestMockFactory.CreateLogger();
            Mock<ICounterFactory> mockCounterFactory = TestMockFactory.CreateCounterFactory();
            Mock<IAdapterContext> mockAdapterContext = new Mock<IAdapterContext>();
            mockAdapterContext.SetupGet(m => m.Logger).Returns(mockLogger.Object);
            mockAdapterContext.SetupGet(m => m.CounterFactory).Returns(mockCounterFactory.Object);
            return mockAdapterContext;
        }

        public static Mock<IMsaTokenProvider> CreateMsaTokenProvider()
        {
            Mock<IMsaTokenProvider> mockTokenProvider = new Mock<IMsaTokenProvider>(MockBehavior.Strict);
            mockTokenProvider.Setup(p => p.GetTokenAsync(It.IsAny<bool>())).Returns(Task.FromResult(new GetTokenResponse
            {
                Token = "A token",
                Expiry = DateTime.UtcNow.AddDays(1)
            }));
            return mockTokenProvider;
        }

        public static Mock<IMsaTokenProviderFactory> CreateMsaTokenProviderFactory()
        {
            Mock<IMsaTokenProvider> mockTokenProvider = CreateMsaTokenProvider();

            Mock<IMsaTokenProviderFactory> mockRepo = new Mock<IMsaTokenProviderFactory>(MockBehavior.Strict);
            mockRepo.Setup(r => r.GetTokenProvider(It.IsAny<string>())).Returns(mockTokenProvider.Object);

            return mockRepo;
        }
    }
}
