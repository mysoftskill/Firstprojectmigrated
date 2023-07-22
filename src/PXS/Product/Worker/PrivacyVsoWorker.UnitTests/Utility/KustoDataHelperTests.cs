namespace PrivacyVsoWorker.UnitTests.Utility
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.PrivacyVsoWorker.Utility;
    using Microsoft.Membership.PrivacyServices.KustoHelpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class KustoDataHelperTests
    {
        [TestMethod]
        public void GetAgentsWithNoConnectorIdTest()
        {
            var mockKustoClientFactory = new Mock<IKustoClientFactory>();
            var mockkustoConfig = new Mock<IKustoConfig>();

            IKustoDataHelper kustoDataHelper = new KustoDataHelper(mockKustoClientFactory.Object, mockkustoConfig.Object);
            Task<List<Agent>> agentsResponse = kustoDataHelper.GetAgentsWithNoConnectorIdAsync();

            //Assert
            Assert.IsNotNull(agentsResponse);

            //Verify
            mockKustoClientFactory.Verify(
                c => c.CreateClient(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Once);
        }
    }
}
