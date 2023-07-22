// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.PrivacyServices.KustoHelpers.UnitTests
{
    using System.Data;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    using Kusto.Cloud.Platform.Data;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.PrivacyServices.KustoHelpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.PrivacyServices.Common.Azure;

    using Moq;

    [TestClass]
    public class IntegrationTests
    {
        private readonly Mock<IFixedIntervalRetryConfiguration> mockFixedCfg = new Mock<IFixedIntervalRetryConfiguration>();
        private readonly Mock<IRetryStrategyConfiguration> mockRetryCfg = new Mock<IRetryStrategyConfiguration>();
        private readonly Mock<IAadTokenAuthConfiguration> mockAuthCfg = new Mock<IAadTokenAuthConfiguration>();
        private readonly Mock<ICertificateProvider> mockCertProvider = new Mock<ICertificateProvider>();
        private readonly Mock<ICertificateConfiguration> mockCertCfg = new Mock<ICertificateConfiguration>();
        private readonly Mock<IKustoConfig> mockKustoCfg = new Mock<IKustoConfig>();
        private readonly Mock<ILogger> mockLog = new Mock<ILogger>();

        private class KustoConfig : IKustoConfig
        {
            public string DefaultClusterUrl => "https://ngpreporting.kusto.windows.net:443";
            public string DefaultDatabaseName => "Ngpreporting";
            public string DefaultKustoAppName => "NGPPXSKustoAccessTest";
            public IRetryStrategyConfiguration RetryStrategy { get; set; }
        }

        [TestInitialize]
        public void Init()
        {
            this.mockFixedCfg.SetupGet(o => o.RetryIntervalInMilliseconds).Returns(100);
            this.mockFixedCfg.SetupGet(o => o.RetryCount).Returns(1);

            this.mockCertProvider.Setup(p => p.GetClientCertificate(It.IsAny<ICertificateConfiguration>())).Returns<X509Certificate2>(null);

            this.mockCertCfg.SetupGet(o => o.Thumbprint).Returns("<INSERT THUMBPRINT HERE>");

            this.mockAuthCfg.SetupGet(o => o.RequestSigningCertificateConfiguration).Returns(this.mockCertCfg.Object);
            this.mockAuthCfg.SetupGet(o => o.AadAppId).Returns("705363a0-5817-47fb-ba32-59f47ce80bb7");

            this.mockRetryCfg.SetupGet(o => o.FixedIntervalRetryConfiguration).Returns(this.mockFixedCfg.Object);
            this.mockRetryCfg.SetupGet(o => o.RetryMode).Returns(RetryMode.FixedInterval);

            this.mockKustoCfg.SetupGet(o => o.DefaultDatabaseName).Returns("NGPReporting");
            this.mockKustoCfg.SetupGet(o => o.DefaultKustoAppName).Returns("NGPPXSKustoAccessTest");
            this.mockKustoCfg.SetupGet(o => o.DefaultClusterUrl).Returns("https://ngpreporting.kusto.windows.net:443");
            this.mockKustoCfg.SetupGet(o => o.RetryStrategy).Returns(this.mockRetryCfg.Object);
        }

        const string Query =
@"
let excludedAgents = datatable(Id:long, Data:string)
[
    1, ""data1"",
    2, ""data2"",
];
//
excludedAgents | order by Id asc | take 1;";

        [Ignore]
        [TestMethod]
        public async Task ExecuteQueryTest()
        {
            IKustoClientFactory factory;
            DataSet result;
            
            factory = new KustoClientFactory(this.mockAuthCfg.Object, this.mockCertProvider.Object, this.mockKustoCfg.Object, this.mockLog.Object);

            using (IKustoClient client = factory.CreateClient(null, null, "TESTQUERYTAG"))
            {
                IDataReader reader = await client.ExecuteQueryAsync(IntegrationTests.Query);
                result = reader.ToDataSet();
            }

            Assert.IsTrue(result.Tables.Count > 0);
            Assert.AreEqual(1, result.Tables[0].Rows.Count);
            Assert.AreEqual(1L, result.Tables[0].Rows[0]["Id"]);
            Assert.AreEqual("data1", result.Tables[0].Rows[0]["Data"]);
        }
    }
}
