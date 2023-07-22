// ---------------------------------------------------------------------------
// <copyright file="Integration.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.UnitTests.Integration
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Logging.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Locks;
    using Microsoft.Membership.PrivacyServices.KustoHelpers;
    using Microsoft.Practices.Unity;
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataAction.UnitTests;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.PrivacyServices.Common.TemplateBuilder;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Data;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Store;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Utility.Email;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Utility.Incidents;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Microsoft.PrivacyServices.Common.Azure;

    [TestClass]
    public class IntegrationTests
    {
        private readonly Mock<ITemplateAccessor> mockTemplateRetriever = new Mock<ITemplateAccessor>();
        private readonly Mock<IActionAccessor> mockActionRetriever = new Mock<IActionAccessor>();
        private readonly Mock<IIncidentCreator> mockIncidentCreator = new Mock<IIncidentCreator>();
        private readonly Mock<IKustoClientFactory> mockKustoFact = new Mock<IKustoClientFactory>();
        private readonly Mock<ITelemetryLogger> mockTelemetryLogger = new Mock<ITelemetryLogger>();
        private readonly Mock<ICounterFactory> mockCounterFactory = new Mock<ICounterFactory>();
        private readonly Mock<ILockManager> mockLock = new Mock<ILockManager>();
        private readonly Mock<IMailSender> mockMailSender = new Mock<IMailSender>();
        private readonly Mock<ILockLease> mockLease = new Mock<ILockLease>();
        private readonly Mock<IClock> mockClock = new Mock<IClock>();

        private readonly IDictionary<string, Mock<IKustoClient>> mockKustoClients = new Dictionary<string, Mock<IKustoClient>>();

        private long incidentId;

        private readonly IUnityContainer container = new UnityContainer();

        private void SetupKustoQuery(
            DataSet result,
            string expectedTag,
            string expectedQuery)
        {
            Mock<IKustoClient> mockKusto = new Mock<IKustoClient>(MockBehavior.Strict);
            IDataReader reader = new Mock<IDataReader>().Object;

            mockKusto.Setup(o => o.ExecuteQueryAsync(expectedQuery, It.IsAny<KustoQueryOptions>())).ReturnsAsync(reader);
            mockKusto.Setup(o => o.ConvertToDataSet(reader)).Returns(result);
            mockKusto.Setup(o => o.Dispose()).Callback(() => { });

            this.mockKustoClients[expectedTag] = mockKusto;

            this.mockKustoFact
                .Setup(o => o.CreateClient(It.IsAny<string>(), It.IsAny<string>(), expectedTag))
                .Returns(mockKusto.Object);
        }

        [TestInitialize]
        public void Init()
        {
            this.container.RegisterInstance<ILogger>(new MockLogger());
            this.container.RegisterInstance<IUnityContainer>(this.container);
            this.container.RegisterInstance<IKustoClientFactory>(this.mockKustoFact.Object);

            this.container.RegisterInstance<IClock>(this.mockClock.Object);

            Microsoft.PrivacyServices.Common.ContextModelCommon.Setup.UnitySetup.RegisterAssemblyTypes(this.container);
            Microsoft.PrivacyServices.Common.TemplateBuilder.Setup.UnitySetup.RegisterAssemblyTypes(this.container);
            Microsoft.PrivacyServices.DataMonitor.DataAction.Setup.UnitySetup.RegisterAssemblyTypes(this.container);

            // override some of the registrations to exclude the implementations that require off box access
            this.container.RegisterInstance(this.mockTelemetryLogger.Object);
            this.container.RegisterInstance(this.mockCounterFactory.Object);
            this.container.RegisterInstance(this.mockTemplateRetriever.Object);
            this.container.RegisterInstance(this.mockActionRetriever.Object);
            this.container.RegisterInstance(this.mockIncidentCreator.Object);
            this.container.RegisterInstance(this.mockLock.Object);
            this.container.RegisterInstance(this.mockMailSender.Object);

            this.mockLock
                .Setup(
                    o => o.AttemptAcquireAsync(
                        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<bool>()))
                .ReturnsAsync(this.mockLease.Object);

            this.mockLease.Setup(o => o.RenewAsync(It.IsAny<TimeSpan?>())).ReturnsAsync(true);

            // incremement the id returned for each call to the method
            this.mockIncidentCreator
                .Setup(o => o.CreateIncidentAsync(It.IsAny<CancellationToken>(), It.IsAny<AgentIncident>()))
                .Returns(
                    (CancellationToken t, AgentIncident o) => 
                        Task.FromResult(new IncidentCreateResult(++this.incidentId, IncidentFileStatus.Created)));

            this.mockMailSender
                .Setup(o => o.SendEmailAsync(It.IsAny<CancellationToken>(), It.IsAny<EmailMessage>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            this.mockTemplateRetriever.Setup(o => o.RetrieveTemplatesAsync()).ReturnsAsync(TestTemplateStore.Templates);
            this.mockActionRetriever.Setup(o => o.RetrieveActionsAsync()).ReturnsAsync(TestActionStore.Actions);
        }

        [TestMethod]
        public async Task StoreCanParseDefinedActions()
        {
            IContextFactory contextFactory = this.container.Resolve<IContextFactory>();
            ITemplateStore templateStore = this.container.Resolve<ITemplateStore>();
            IActionStore store = this.container.Resolve<IActionStore>();

            IParseContext parseContext = contextFactory.Create<IParseContext>("Test");

            bool result;

            Assert.IsTrue(await templateStore.RefreshAsync(parseContext, false));

            // test
            result = await store.RefreshAsync(parseContext, false);

            // verify
            Assert.IsTrue(result);
            Assert.IsFalse(parseContext.HasErrors);
        }

        [TestMethod]
        public async Task StoreCanExecuteParsedActionsWhenAllApplicableAndConstQueryUsed()
        {
            const string LockGroup = "Sev4IncidentFiling";
            const string Agent0 = "Agent0";
            const string Agent1 = "Agent1";

            const string ExpectedKustoQuery =
                "AgentInfoTable | where AgentId !in ('Agent2','Agent3') | project AgentId";

            IContextFactory contextFactory = this.container.Resolve<IContextFactory>();
            ITemplateStore templates = this.container.Resolve<ITemplateStore>(); 
            IActionStore actions = this.container.Resolve<IActionStore>();

            IExecuteContext execContext;
            IParseContext parseContext;

            DataTable table = new DataTable();
            DataSet dataSet = new DataSet();

            Func<EmailMessage, bool> emailVerifier = 
                msg =>
                {
                    Assert.AreEqual(
                        "Incidents filed: <ul><li>Agent0: 1 (Created)</li><li>Agent1: 2 (Created)</li></ul></p>", 
                        msg.Body);
                    Assert.AreEqual("Incidents filed at 2006-04-14 15:00:00", msg.Subject);
                    Assert.AreEqual("derekm@microsoft.com", msg.FromAddress);
                    Assert.AreEqual(1, msg.ToAddresses.Count);
                    Assert.AreEqual("ngpincientresults@microsoft.com", msg.ToAddresses.First());
                    Assert.IsTrue(msg.CcAddresses == null || msg.CcAddresses.Count == 0);
                    return true;
                };

            Func<AgentIncident, string, bool> incidentVerifier =
                (inc, agent) =>
                {
                    string expectedTitle = "Incident for agent " + agent;
                    string expectedBody = agent + " is not doing stuff well";
                    bool verifyResult;

                    Assert.AreEqual("DeleteAlert", inc.EventName);
                    Assert.AreEqual(3, inc.Severity);
                    Assert.IsNull(inc.OwnerId);
                    Assert.IsNull(inc.AssetGroupId);

                    // done deliberately like this to make debugging which statement is failing easier
                    verifyResult = expectedTitle.Equals(inc.Title, StringComparison.Ordinal);
                    verifyResult = expectedBody.Equals(inc.Body, StringComparison.Ordinal) && verifyResult;
                    verifyResult = agent.Equals(inc.AgentId, StringComparison.Ordinal) && verifyResult;

                    return verifyResult;
                };

            object result;
            
            dataSet.Tables.Add(table);
            table.Columns.Add("AgentId", typeof(string));
            table.Rows.Add(Agent0);
            table.Rows.Add(Agent1);

            this.SetupKustoQuery(dataSet, "KustoIncidentFile.FindBadAgents", ExpectedKustoQuery);

            this.mockClock.SetupGet(o => o.UtcNow).Returns(new DateTimeOffset(2006, 4, 14, 15, 00, 00, TimeSpan.FromHours(-7)));

            parseContext = contextFactory.Create<IParseContext>("Test");

            Assert.IsTrue(await templates.RefreshAsync(parseContext, false));
            Assert.IsTrue(await actions.RefreshAsync(parseContext, false));

            execContext = contextFactory.Create<IExecuteContext>("Test");

            // test
            result = await actions.ExecuteActionAsync(execContext, new ActionRef { Tag = "KustoIncidentFile" });

            // verify
            Assert.IsNotNull(result);

            this.mockKustoClients["KustoIncidentFile.FindBadAgents"]
                .Verify(o => o.ExecuteQueryAsync(ExpectedKustoQuery, It.IsAny<KustoQueryOptions>()), Times.Once);

            this.mockMailSender.Verify(
                o => o.SendEmailAsync(CancellationToken.None, It.Is<EmailMessage>(p => emailVerifier(p)), null), 
                Times.Once);

            this.mockIncidentCreator.Verify(
                o => o.CreateIncidentAsync(CancellationToken.None, It.Is<AgentIncident>(p => incidentVerifier(p, Agent0))), 
                Times.Once);
            this.mockIncidentCreator.Verify(
                o => o.CreateIncidentAsync(CancellationToken.None, It.Is<AgentIncident>(p => incidentVerifier(p, Agent1))), 
                Times.Once);

            this.mockLock.Verify(
                o => o.AttemptAcquireAsync(LockGroup, Agent0, It.IsAny<string>(), TimeSpan.FromMinutes(30), true),
                Times.Once);
            this.mockLock.Verify(
                o => o.AttemptAcquireAsync(LockGroup, Agent1, It.IsAny<string>(), TimeSpan.FromMinutes(30), true),
                Times.Once);

            this.mockLease.Verify(o => o.RenewAsync(TimeSpan.FromHours(23)), Times.Exactly(2));
        }

        [TestMethod]
        public async Task StoreCanExecuteParsedActionsWhenAllApplicableAndRefArgsUsed()
        {
            const string LockGroup = "Sev4IncidentFiling";
            const string Agent0 = "Agent0";
            const string Agent1 = "Agent1";

            const string ExpectedKustoQuery =
                "AgentInfoTable | where AgentId !in ('Agent2','Agent3') | project AgentId";

            IContextFactory contextFactory = this.container.Resolve<IContextFactory>();
            ITemplateStore templates = this.container.Resolve<ITemplateStore>();
            IActionStore actions = this.container.Resolve<IActionStore>();
            ActionRef actionRef;

            IExecuteContext execContext;
            IParseContext parseContext;

            DataTable table = new DataTable();
            DataSet dataSet = new DataSet();

            Func<EmailMessage, bool> emailVerifier =
                msg =>
                {
                    Assert.AreEqual(
                        "Incidents filed: <ul><li>Agent0: 1 (Created)</li><li>Agent1: 2 (Created)</li></ul></p>",
                        msg.Body);
                    Assert.AreEqual("Incidents filed at 2006-04-14 15:00:00", msg.Subject);
                    Assert.AreEqual("derekm@microsoft.com", msg.FromAddress);
                    Assert.AreEqual(1, msg.ToAddresses.Count);
                    Assert.AreEqual("ngpincientresults@microsoft.com", msg.ToAddresses.First());
                    Assert.IsTrue(msg.CcAddresses == null || msg.CcAddresses.Count == 0);
                    return true;
                };

            Func<AgentIncident, string, bool> incidentVerifier =
                (inc, agent) =>
                {
                    string expectedTitle = "Incident for agent " + agent;
                    string expectedBody = agent + " is not doing stuff well";
                    bool verifyResult;

                    Assert.AreEqual("DeleteAlert", inc.EventName);
                    Assert.AreEqual(3, inc.Severity);
                    Assert.IsNull(inc.OwnerId);
                    Assert.IsNull(inc.AssetGroupId);

                    // done deliberately like this to make debugging which statement is failing easier
                    verifyResult = expectedTitle.Equals(inc.Title, StringComparison.Ordinal);
                    verifyResult = expectedBody.Equals(inc.Body, StringComparison.Ordinal) && verifyResult;
                    verifyResult = agent.Equals(inc.AgentId, StringComparison.Ordinal) && verifyResult;

                    return verifyResult;
                };

            object result;

            dataSet.Tables.Add(table);
            table.Columns.Add("AgentId", typeof(string));
            table.Rows.Add(Agent0);
            table.Rows.Add(Agent1);

            this.SetupKustoQuery(dataSet, "KustoIncidentFileRefArgsDef.FindBadAgents", ExpectedKustoQuery);

            this.mockClock.SetupGet(o => o.UtcNow).Returns(new DateTimeOffset(2006, 4, 14, 15, 00, 00, TimeSpan.FromHours(-7)));

            parseContext = contextFactory.Create<IParseContext>("Test");

            Assert.IsTrue(await templates.RefreshAsync(parseContext, false));
            Assert.IsTrue(await actions.RefreshAsync(parseContext, false));

            actionRef = new ActionRef
            {
                Tag = "KustoIncidentFileRefArgsDef",
                ArgTransform = new Dictionary<string, ModelValue>
                {
                    { "Consts.Severity", new ModelValue { Const = 4 } },
                    { "Consts.EmailFrom", new ModelValue { Const = "ngpincidentfiler@microsoft.com" } },
                    { "Consts.LockGroupName", new ModelValue { Const = "Sev4IncidentFiling" } },
                    { "Consts.ExcludedAgents", new ModelValue { Const = "'Agent2','Agent3'" } },
                }
            };

            execContext = contextFactory.Create<IExecuteContext>("Test");

            // test
            result = await actions.ExecuteActionAsync(execContext, actionRef);

            // verify
            Assert.IsNotNull(result);

            this.mockKustoClients["KustoIncidentFileRefArgsDef.FindBadAgents"]
                .Verify(o => o.ExecuteQueryAsync(ExpectedKustoQuery, It.IsAny<KustoQueryOptions>()), Times.Once);

            this.mockMailSender.Verify(
                o => o.SendEmailAsync(CancellationToken.None, It.Is<EmailMessage>(p => emailVerifier(p)), null), 
                Times.Once);

            this.mockIncidentCreator
                .Verify(
                    o => o.CreateIncidentAsync(CancellationToken.None, It.Is<AgentIncident>(p => incidentVerifier(p, Agent0))), 
                    Times.Once);
            this.mockIncidentCreator
                .Verify(
                    o => o.CreateIncidentAsync(CancellationToken.None, It.Is<AgentIncident>(p => incidentVerifier(p, Agent1))),
                    Times.Once);

            this.mockLock.Verify(
                o => o.AttemptAcquireAsync(LockGroup, Agent0, It.IsAny<string>(), TimeSpan.FromMinutes(30), true),
                Times.Once);
            this.mockLock.Verify(
                o => o.AttemptAcquireAsync(LockGroup, Agent1, It.IsAny<string>(), TimeSpan.FromMinutes(30), true),
                Times.Once);

            this.mockLease.Verify(o => o.RenewAsync(TimeSpan.FromHours(23)), Times.Exactly(2));
        }


        [TestMethod]
        public async Task NoIncidentsFiledOrMailSentIfTimeApplicabilityFails()
        {
            const string Agent0 = "Agent0";
            const string Agent1 = "Agent1";

            const string ExpectedKustoQuery =
                "AgentInfoTable | where AgentId !in ('Agent2','Agent3') | project AgentId";

            IContextFactory contextFactory = this.container.Resolve<IContextFactory>();
            ITemplateStore templates = this.container.Resolve<ITemplateStore>();
            IActionStore actions = this.container.Resolve<IActionStore>();

            IExecuteContext execContext;
            IParseContext parseContext;

            DataTable table = new DataTable();
            DataSet dataSet = new DataSet();

            object result;

            dataSet.Tables.Add(table);
            table.Columns.Add("AgentId", typeof(string));
            table.Rows.Add(Agent0);
            table.Rows.Add(Agent1);

            this.SetupKustoQuery(dataSet, "KustoIncidentFile.FindBadAgents", ExpectedKustoQuery);

            this.mockClock.SetupGet(o => o.UtcNow).Returns(new DateTimeOffset(2006, 4, 15, 15, 00, 00, TimeSpan.FromHours(-7)));

            parseContext = contextFactory.Create<IParseContext>("Test");

            Assert.IsTrue(await templates.RefreshAsync(parseContext, false));
            Assert.IsTrue(await actions.RefreshAsync(parseContext, false));

            execContext = contextFactory.Create<IExecuteContext>("Test");

            // test
            result = await actions.ExecuteActionAsync(execContext, new ActionRef { Tag = "KustoIncidentFile" });

            // verify
            Assert.IsNotNull(result);

            this.mockKustoClients["KustoIncidentFile.FindBadAgents"]
                .Verify(o => o.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<KustoQueryOptions>()), Times.Never);

            this.mockMailSender.Verify(
                o => o.SendEmailAsync(CancellationToken.None, It.IsAny<EmailMessage>(), null), 
                Times.Never);

            this.mockIncidentCreator.Verify(o => o.CreateIncidentAsync(CancellationToken.None, It.IsAny<AgentIncident>()), Times.Never);

            this.mockLock.Verify(
                o => o.AttemptAcquireAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<bool>()),
                Times.Never);
        }

        [TestMethod]
        public async Task NoIncidentsFiledForSpecificAgentIfAgentCannotAcquireLock()
        {
            const string LockGroup = "Sev4IncidentFiling";
            const string Agent0 = "Agent0";
            const string Agent1 = "Agent1";

            const string ExpectedKustoQuery =
                "AgentInfoTable | where AgentId !in ('Agent2','Agent3') | project AgentId";

            IContextFactory contextFactory = this.container.Resolve<IContextFactory>();
            ITemplateStore templates = this.container.Resolve<ITemplateStore>();
            IActionStore actions = this.container.Resolve<IActionStore>();

            IExecuteContext execContext;
            IParseContext parseContext;

            DataTable table = new DataTable();
            DataSet dataSet = new DataSet();

            Func<EmailMessage, bool> emailVerifier =
                msg =>
                {
                    Assert.AreEqual(
                        "Incidents filed: <ul><li>Agent1: 1 (Created)</li></ul></p>",
                        msg.Body);
                    Assert.AreEqual("Incidents filed at 2006-04-14 15:00:00", msg.Subject);
                    Assert.AreEqual("derekm@microsoft.com", msg.FromAddress);
                    Assert.AreEqual(1, msg.ToAddresses.Count);
                    Assert.AreEqual("ngpincientresults@microsoft.com", msg.ToAddresses.First());
                    Assert.IsTrue(msg.CcAddresses == null || msg.CcAddresses.Count == 0);
                    return true;
                };

            Func<AgentIncident, string, bool> incidentVerifier =
                (inc, agent) =>
                {
                    string expectedTitle = "Incident for agent " + agent;
                    string expectedBody = agent + " is not doing stuff well";

                    Assert.AreEqual("DeleteAlert", inc.EventName);
                    Assert.AreEqual(3, inc.Severity);
                    Assert.IsNull(inc.OwnerId);
                    Assert.IsNull(inc.AssetGroupId);

                    // done deliberately like this to make debugging which statement is failing easier
                    Assert.AreEqual(expectedTitle, inc.Title);
                    Assert.AreEqual(expectedBody, inc.Body);
                    Assert.AreEqual(agent, inc.AgentId);

                    return true;
                };


            object result;

            dataSet.Tables.Add(table);
            table.Columns.Add("AgentId", typeof(string));
            table.Rows.Add(Agent0);
            table.Rows.Add(Agent1);

            this.mockLock
                .Setup(
                    o => o.AttemptAcquireAsync(
                        It.IsAny<string>(), Agent0, It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<bool>()))
                .ReturnsAsync((ILockLease)null);

            this.SetupKustoQuery(dataSet, "KustoIncidentFile.FindBadAgents", ExpectedKustoQuery);

            this.mockClock.SetupGet(o => o.UtcNow).Returns(new DateTimeOffset(2006, 4, 14, 15, 00, 00, TimeSpan.FromHours(-7)));

            parseContext = contextFactory.Create<IParseContext>("Test");

            Assert.IsTrue(await templates.RefreshAsync(parseContext, false));
            Assert.IsTrue(await actions.RefreshAsync(parseContext, false));

            execContext = contextFactory.Create<IExecuteContext>("Test");

            // test
            result = await actions.ExecuteActionAsync(execContext, new ActionRef { Tag = "KustoIncidentFile" });

            // verify
            Assert.IsNotNull(result);

            this.mockKustoClients["KustoIncidentFile.FindBadAgents"]
                .Verify(o => o.ExecuteQueryAsync(ExpectedKustoQuery, It.IsAny<KustoQueryOptions>()), Times.Once);

            this.mockMailSender.Verify(
                o => o.SendEmailAsync(CancellationToken.None, It.Is<EmailMessage>(p => emailVerifier(p)), null), 
                Times.Once);

            this.mockIncidentCreator.Verify(
                o => o.CreateIncidentAsync(CancellationToken.None, It.IsAny<AgentIncident>()), Times.Once);

            this.mockIncidentCreator.Verify(
                o => o.CreateIncidentAsync(CancellationToken.None, It.Is<AgentIncident>(p => incidentVerifier(p, Agent1))), 
                Times.Once);

            this.mockLock.Verify(
                o => o.AttemptAcquireAsync(LockGroup, Agent0, It.IsAny<string>(), TimeSpan.FromMinutes(30), true),
                Times.Once);
            this.mockLock.Verify(
                o => o.AttemptAcquireAsync(LockGroup, Agent1, It.IsAny<string>(), TimeSpan.FromMinutes(30), true),
                Times.Once);

            this.mockLease.Verify(o => o.RenewAsync(TimeSpan.FromHours(23)), Times.Exactly(1));
        }
    }
}
