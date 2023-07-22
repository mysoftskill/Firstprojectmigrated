namespace Microsoft.PrivacyServices.DataManagement.FunctionalTests.FrontdoorTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.DataManagement.Client;
    using Microsoft.PrivacyServices.DataManagement.Client.V2;
    using Microsoft.PrivacyServices.DataManagement.FunctionalTests.Setup;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class HistoryItemsTests : TestBase
    {
        [TestMethod]
        public async Task WhenIReadHistoryItemsByEntityIdForDataAgent()
        {
            // arrange
            IDataManagementClient client = TestSetup.PdmsClientInstance;
            HistoryItemFilterCriteria filter = new HistoryItemFilterCriteria
            {
                EntityId = await GetAnAgentIdAsync().ConfigureAwait(false)
            };

            // act
            IHttpResult<Collection<HistoryItem>> result = await client.HistoryItems
                .ReadByFiltersAsync(TestSetup.RequestContext, filter).ConfigureAwait(false);
            List<HistoryItem> historyItems = result?.Response?.Value.ToList();

            // assert
            Assert.IsNotNull(result);
            Assert.AreEqual(HttpStatusCode.OK, result.HttpStatusCode);
            Assert.IsNotNull(historyItems);
            Assert.IsTrue(historyItems.Count > 0);
        }

        [TestMethod]
        public async Task WhenIReadHistoryItemsByEntityIdForDataOwner()
        {
            // arrange
            IDataManagementClient client = TestSetup.PdmsClientInstance;
            HistoryItemFilterCriteria filter = new HistoryItemFilterCriteria
            {
                EntityId = await GetADataOwnerIdAsync().ConfigureAwait(false)
            };

            // act
            IHttpResult<Collection<HistoryItem>> result = await client.HistoryItems
                .ReadByFiltersAsync(TestSetup.RequestContext, filter).ConfigureAwait(false);
            List<HistoryItem> historyItems = result?.Response?.Value.ToList();

            // assert
            Assert.IsNotNull(result);
            Assert.AreEqual(HttpStatusCode.OK, result.HttpStatusCode);
            Assert.IsNotNull(historyItems);
            Assert.IsTrue(historyItems.Count > 0);
        }

        [TestMethod]
        public async Task QueryHistoryItemsSinceBeforeDateAsync()
        {
            // create a data agent and filter for history
            var agent = await CreateNewDataAgentAsync().ConfigureAwait(false);
            IDataManagementClient client = TestSetup.PdmsClientInstance;
            var filter = new HistoryItemFilterCriteria
            {
                EntityId = agent.Id,
                EntityUpdatedBefore = DateTimeOffset.UtcNow
            };

            // get the history result for the default new data agent history
            IHttpResult<Collection<HistoryItem>> firstResult = await client.HistoryItems
                .ReadByFiltersAsync(TestSetup.RequestContext, filter).ConfigureAwait(false);
            List<HistoryItem> firstHistoryItems = firstResult?.Response?.Value.ToList();

            // remove the old connection and add a new connection to the agent
            agent.ConnectionDetails.Remove(ReleaseState.PreProd);
            agent.ConnectionDetails.Add(ReleaseState.PreProd, new ConnectionDetail()
            {
                AadAppIds = Enumerable.Empty<Guid>().Append(Guid.NewGuid()).Append(Guid.NewGuid()),
                AuthenticationType = AuthenticationType.AadAppBasedAuth,
                Protocol = Policies.Current.Protocols.Ids.CommandFeedV1,
                ReleaseState = ReleaseState.PreProd,
                AgentReadiness = AgentReadiness.ProdReady
            });
            var agentResponse = await TestSetup.PdmsClientInstance.DataAgents
                .UpdateAsync(agent, TestSetup.RequestContext).ConfigureAwait(false);

            // get the history result with the connection details updated after the before date
            IHttpResult<Collection<HistoryItem>> secondResult = await client.HistoryItems
                .ReadByFiltersAsync(TestSetup.RequestContext, filter).ConfigureAwait(false);
            List<HistoryItem> secondHistoryItems = secondResult?.Response?.Value.ToList();

            // change the filter to the present and retrieve the item history again
            filter = new HistoryItemFilterCriteria
            {
                EntityId = agent.Id,
                EntityUpdatedBefore = DateTimeOffset.UtcNow
            };
            IHttpResult<Collection<HistoryItem>> thirdResult = await client.HistoryItems
                .ReadByFiltersAsync(TestSetup.RequestContext, filter).ConfigureAwait(false);
            List<HistoryItem> thirdHistoryItems = thirdResult?.Response?.Value.ToList();

            // assert that the filter EntityUpdatedBefore date filtered new history items
            Assert.AreEqual(firstHistoryItems.Count, secondHistoryItems.Count);
            Assert.IsTrue(thirdHistoryItems.Count > secondHistoryItems.Count);

            await CleanupDataAgent(agentResponse.Response).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task CorrectHistoryItemIsReceivedForTheEntityAsync()
        {
            // create a data agent and filter for history
            var agent = await CreateNewDataAgentAsync().ConfigureAwait(false);
            IDataManagementClient client = TestSetup.PdmsClientInstance;
            var filter = new HistoryItemFilterCriteria
            {
                EntityId = agent.Id
            };

            // get the history result for the new data agent
            IHttpResult<Collection<HistoryItem>> result = await client.HistoryItems
                .ReadByFiltersAsync(TestSetup.RequestContext, filter).ConfigureAwait(false);
            List<HistoryItem> historyItems = result?.Response?.Value.ToList();

            // assert that the item history contains a single entry for the new agent
            Assert.AreEqual(historyItems.Count, 1);
            Assert.AreEqual(historyItems[0].Entity.Id, agent.Id);

            await CleanupDataAgent(agent).ConfigureAwait(false);
        }
    }
}