namespace PrivacyVsoWorker.UnitTests.Utility
{
    using Microsoft.Membership.MemberServices.Privacy.PrivacyVsoWorker.Utility;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class AgentTests
    {
        [DataTestMethod]
        [DataRow("0af48407-1f22-4a6f-9ecb-e2d77d37125f", "ID Directory- Enterprise & Core Services", "Identity")]
        public void AgentTitleGenerateTest(string agentId, string serviceGroupName, string orgName)
        {
            var agent = new Agent
            {
                AgentId = agentId,
                ServiceGroupName = serviceGroupName,
                OrganizationName = orgName
            };

            Assert.AreEqual("[ID Directory- Enterprise & Core Services][Identity] - [0af48407-1f22-4a6f-9ecb-e2d77d37125f]", agent.GenerateTitle());
        }
    }
}
