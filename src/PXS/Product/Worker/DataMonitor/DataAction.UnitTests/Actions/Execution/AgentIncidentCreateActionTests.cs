// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.UnitTests.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.PrivacyServices.Common.Exceptions;
    using Microsoft.PrivacyServices.Common.TemplateBuilder;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Actions;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Data;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Store;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Utility.Incidents;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Newtonsoft.Json;

    [TestClass]
    public class AgentIncidentCreateActionTests
    {
        private class AgentIncidentCreateActionTestException : Exception
        {
            public AgentIncidentCreateActionTestException(string message) : base(message) { }
        }

        private readonly Mock<IModelManipulator> mockModel = new Mock<IModelManipulator>();
        private readonly Mock<IIncidentCreator> mockCreator = new Mock<IIncidentCreator>();
        private readonly Mock<IExecuteContext> mockExecCtx = new Mock<IExecuteContext>();
        private readonly Mock<IActionFactory> mockFact = new Mock<IActionFactory>();
        private readonly Mock<ITemplateStore> mockTemplateStore = new Mock<ITemplateStore>();
        private readonly Mock<IParseContext> mockParseCtx = new Mock<IParseContext>();
        private readonly Mock<IActionStore> mockActionStore = new Mock<IActionStore>();

        private readonly CancellationTokenSource cancelSource = new CancellationTokenSource();

        private const string DefTag = "tag";

        private AgentIncidentCreateAction testObj;

        private IExecuteContext execCtx;
        private IActionFactory fact;
        private IParseContext parseCtx;
        private IActionStore store;

        private (ActionRefCore, object, AgentIncidentCreateDef, AgentIncidentCreateAction.Args) SetupTestObj(
            IncidentCreateResult result = null,
            string agentId = null,
            string assetGroupId = null,
            string ownerId = null)
        {
            const string CounterSuffix = "CounterSuffix";

            object modelIn = new object();

            IDictionary<string, ModelValue> argXform = new Dictionary<string, ModelValue>
            {
                { "AgentId", new ModelValue { Const = 1 } },
                { "Severity", new ModelValue { Const = 1 } },
                { "CounterSuffix", new ModelValue { Const = CounterSuffix } }
            };

            AgentIncidentCreateDef def = new AgentIncidentCreateDef
            {
                Keywords = new TemplateRef { Inline = "Text" },
                Title = new TemplateRef { Inline = "Text" },
                Body = new TemplateRef { Inline = "Text" },
                
                EventName = "event"
            };

            AgentIncidentCreateAction.Args args = new AgentIncidentCreateAction.Args
            {
                CounterSuffix = CounterSuffix,
                AssetGroupId = assetGroupId,
                AgentId = agentId,
                OwnerId = ownerId,
                Severity = 3,
            };

            this.mockModel
                .Setup(o => o.TransformTo<AgentIncidentCreateAction.Args>(It.IsAny<object>()))
                .Returns(args);

            this.mockCreator
                .Setup(o => o.CreateIncidentAsync(It.IsAny<CancellationToken>(), It.IsAny<AgentIncident>()))
                .ReturnsAsync(result ?? new IncidentCreateResult(IncidentFileStatus.Invalid));

            this.testObj = new AgentIncidentCreateAction(
                this.mockModel.Object, this.mockTemplateStore.Object, this.mockCreator.Object);

            Assert.IsTrue(
                this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, AgentIncidentCreateActionTests.DefTag, def));

            Assert.IsTrue(this.testObj.ExpandDefinition(this.parseCtx, this.store));
            Assert.IsTrue(this.testObj.Validate(this.parseCtx, argXform));

            this.mockModel.Setup(o => o.MergeModels(this.execCtx, modelIn, null, argXform)).Returns(args);

            return (new ActionRefCore { ArgTransform = argXform }, modelIn, def, args);
        }

        [TestInitialize]
        public void Init()
        {
            this.mockExecCtx.SetupGet(o => o.NowUtc).Returns(DateTimeOffset.Parse("2006-04-15T15:01:00-07:00"));
            this.mockExecCtx.SetupGet(o => o.OperationStartTime).Returns(DateTimeOffset.Parse("2006-04-15T15:00:00-07:00"));
            this.mockExecCtx.SetupGet(o => o.CancellationToken).Returns(this.cancelSource.Token);
            this.mockExecCtx.SetupGet(o => o.IsSimulation).Returns(false);

            this.mockTemplateStore
                .Setup(o => o.Render(It.IsAny<IContext>(), It.IsAny<TemplateRef>(), It.IsAny<object>()))
                .Returns((IContext ctx, TemplateRef tref, object data) => tref.Inline);

            this.mockTemplateStore
                .Setup(o => o.ValidateReference(It.IsAny<IContext>(), It.IsAny<TemplateRef>())).Returns(true);

            this.parseCtx = this.mockParseCtx.Object;
            this.execCtx = this.mockExecCtx.Object;
            this.store = this.mockActionStore.Object;
            this.fact = this.mockFact.Object;
        }

        [TestMethod]
        public async Task ExecuteParsesArguments()
        {
            ActionRefCore refCore;
            object modelIn;

            (refCore, modelIn, _, _) = this.SetupTestObj(agentId: "agentId");

            // test
            await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // verify
            this.mockModel.Verify(o => o.MergeModels(this.execCtx, modelIn, null, refCore.ArgTransform), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(ActionExecuteException))]
        public async Task ExecuteThrowsIfIncidentIsConstructedWithEmptyTitle()
        {
            AgentIncidentCreateDef def;
            ActionRefCore refCore;
            object modelIn;

            (refCore, modelIn, def, _) = this.SetupTestObj(agentId: "agentId");

            this.mockTemplateStore
                .Setup(o => o.Render(this.execCtx, def.Title, It.IsAny<object>()))
                .Returns((string)null);

            try
            {
                await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);
            }
            catch (ActionExecuteException e)
            {
                Assert.IsTrue(e.Message.Contains("Errors found validating incident"));
                throw;
            }
        }

        [TestMethod]
        public async Task ExecuteCreatesIncident()
        {
            const string Keywords = "keywords";
            const string AgentId = "agentId";
            const string Title = "title";
            const string Body = "body";

            AgentIncidentCreateDef def;
            ActionRefCore refCore;
            object modelIn;

            (refCore, modelIn, def, _) = this.SetupTestObj(agentId: AgentId);

            Func<AgentIncident, bool> validator =
                inc =>
                {
                    Assert.AreEqual(Keywords, inc.Keywords);
                    Assert.AreEqual(Title, inc.Title);
                    Assert.AreEqual(Body, inc.Body);

                    Assert.AreEqual(AgentId, inc.AgentId);
                    Assert.IsNull(inc.AssetGroupId);
                    Assert.IsNull(inc.OwnerId);

                    Assert.AreEqual(def.EventName, inc.EventName);
                    return true;
                };

            this.mockTemplateStore.Setup(o => o.Render(this.execCtx, def.Keywords, It.IsAny<object>())).Returns(Keywords);
            this.mockTemplateStore.Setup(o => o.Render(this.execCtx, def.Title, It.IsAny<object>())).Returns(Title);
            this.mockTemplateStore.Setup(o => o.Render(this.execCtx, def.Body, It.IsAny<object>())).Returns(Body);

            // test
            await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // validate
            this.mockCreator.Verify(
                o => o.CreateIncidentAsync(this.execCtx.CancellationToken, It.Is<AgentIncident>(p => validator(p))));
        }

        [TestMethod]
        public async Task ExecuteCreatesIncidentWithOverrideEventNameIfOneProvided()
        {
            const string Keywords = "keywords";
            const string AgentId = "agentId";
            const string Title = "title";
            const string Body = "body";

            AgentIncidentCreateAction.Args args;
            AgentIncidentCreateDef def;
            ActionRefCore refCore;
            object modelIn;

            (refCore, modelIn, def, args) = this.SetupTestObj(agentId: AgentId);

            args.EventNameOverride = "EVENTOVERRIDE";

            Func<AgentIncident, bool> validator =
                inc =>
                {
                    Assert.AreEqual(args.EventNameOverride, inc.EventName);
                    return true;
                };

            this.mockTemplateStore.Setup(o => o.Render(this.execCtx, def.Keywords, It.IsAny<object>())).Returns(Keywords);
            this.mockTemplateStore.Setup(o => o.Render(this.execCtx, def.Title, It.IsAny<object>())).Returns(Title);
            this.mockTemplateStore.Setup(o => o.Render(this.execCtx, def.Body, It.IsAny<object>())).Returns(Body);

            // test
            await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // validate
            this.mockCreator.Verify(
                o => o.CreateIncidentAsync(this.execCtx.CancellationToken, It.Is<AgentIncident>(p => validator(p))));
        }

        [TestMethod]
        public async Task ExecuteRendersBodyAndTitle()
        {
            AgentIncidentCreateDef def;
            ActionRefCore refCore;
            object modelIn;

            (refCore, modelIn, def, _) = this.SetupTestObj(agentId: "agentId");

            // test
            await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // verify
            this.mockTemplateStore.Verify(o => o.Render(this.execCtx, def.Body, modelIn), Times.Once);
            this.mockTemplateStore.Verify(o => o.Render(this.execCtx, def.Title, modelIn), Times.Once);
        }

        [TestMethod]
        public async Task ExecuteUsesBodyTagOverrideIfOneProvided()
        {
            AgentIncidentCreateAction.Args args;
            ActionRefCore refCore;
            object modelIn;

            (refCore, modelIn, _, args) = this.SetupTestObj(agentId: "agentId");

            this.mockTemplateStore
                .Setup(o => o.Render(It.IsAny<IContext>(), It.IsAny<TemplateRef>(), It.IsAny<object>()))
                .Returns((IContext ctx, TemplateRef tref, object data) => "OVERRIDETEMPLATE");

            args.BodyTagOverride = "BODYTAGOVERRIDE";

            // test
            await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // verify
            this.mockTemplateStore.Verify(
                o => o.Render(
                    this.execCtx, 
                    It.Is<TemplateRef>(p => p.Inline == null && args.BodyTagOverride.Equals(p.TemplateTag)), 
                    modelIn), 
                Times.Once);
        }

        [TestMethod]
        public async Task ExecuteLogsEventAndIncrementsCounterOnSuccess()
        {
            const string Keywords = "keywords";
            const string AgentId = "agentId";
            const string Title = "title";
            const string Body = "body";

            AgentIncidentCreateAction.Args args;
            AgentIncidentCreateDef def;
            ActionRefCore refCore;
            object modelIn;

            (refCore, modelIn, def, args) = this.SetupTestObj(agentId: AgentId);

            this.mockTemplateStore.Setup(o => o.Render(this.execCtx, def.Keywords, It.IsAny<object>())).Returns(Keywords);
            this.mockTemplateStore.Setup(o => o.Render(this.execCtx, def.Title, It.IsAny<object>())).Returns(Title);
            this.mockTemplateStore.Setup(o => o.Render(this.execCtx, def.Body, It.IsAny<object>())).Returns(Body);

            // test
            await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // validate
            this.mockExecCtx.Verify(
                o => o.ReportActionEvent(
                    "success",
                    this.testObj.Type,
                    this.testObj.Tag,
                    It.Is<IDictionary<string, string>>(
                        p =>
                            p[DataActionConsts.ExceptionDataIncidentAgent].Equals(AgentId) &&
                            p[DataActionConsts.ExceptionDataIncidentTitle].Equals(Title) &&
                            p[DataActionConsts.ExceptionDataIncidentEvent].Equals(def.EventName) &&
                            p[DataActionConsts.ExceptionDataIncidentSev].Equals(args.Severity.ToStringInvariant()))));

            this.mockExecCtx.Verify(
                o => o.IncrementCounter("Incidents Filed", this.testObj.Tag, args.CounterSuffix, 1));
        }

        [TestMethod]
        public async Task ExecuteLogsEventAndIncrementsCounterOnFailure()
        {
            const string Response = "RESPONSE";
            const string ErrMsg = "ERROR";
            const string Keywords = "keywords";
            const string AgentId = "agentId";
            const string Title = "title";
            const string Body = "body";

            AgentIncidentCreateActionTestException exText;
            AgentIncidentCreateAction.Args args;
            AgentIncidentCreateDef def;
            ActionRefCore refCore;
            object modelIn;

            (refCore, modelIn, def, args) = this.SetupTestObj(agentId: AgentId);

            this.mockTemplateStore.Setup(o => o.Render(this.execCtx, def.Keywords, It.IsAny<object>())).Returns(Keywords);
            this.mockTemplateStore.Setup(o => o.Render(this.execCtx, def.Title, It.IsAny<object>())).Returns(Title);
            this.mockTemplateStore.Setup(o => o.Render(this.execCtx, def.Body, It.IsAny<object>())).Returns(Body);

            exText = new AgentIncidentCreateActionTestException(ErrMsg);
            exText.Data[DataActionConsts.ExceptionDataIncidentRawResponse] = Response;
            
            this.mockCreator
                .Setup(o => o.CreateIncidentAsync(It.IsAny<CancellationToken>(), It.IsAny<AgentIncident>()))
                .Returns(Task.FromException<IncidentCreateResult>(exText));

            try
            {
                // test
                await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);
                Assert.Fail("Did not throw");
            }
            catch (AgentIncidentCreateActionTestException)
            {
                // validate
                this.mockExecCtx.Verify(
                    o => o.ReportActionError(
                        "error",
                        this.testObj.Type,
                        this.testObj.Tag,
                        ErrMsg,
                        It.Is<IDictionary<string, string>>(
                            p =>
                                p[DataActionConsts.ExceptionDataIncidentAgent].Equals(AgentId) &&
                                p[DataActionConsts.ExceptionDataIncidentTitle].Equals(Title) &&
                                p[DataActionConsts.ExceptionDataIncidentEvent].Equals(def.EventName) &&
                                p[DataActionConsts.ExceptionDataIncidentSev].Equals(args.Severity.ToStringInvariant()) &&
                                p[DataActionConsts.ExceptionDataIncidentRawResponse].Equals(Response))));

                this.mockExecCtx.Verify(
                    o => o.IncrementCounter("Incident Filing Errors", this.testObj.Tag, args.CounterSuffix, 1));
            }
        }

        [TestMethod]
        public async Task ExecuteReturnsIncidentCreateResult()
        {
            const string AgentId = "agentId";
            const string Title = "title";
            const long Id = 101;

            AgentIncidentCreateDef def;
            IncidentCreateResult incidentCreateResult;
            ActionRefCore refCore;
            object modelIn;

            incidentCreateResult = new IncidentCreateResult(Id, IncidentFileStatus.Created);

            (refCore, modelIn, def, _) = this.SetupTestObj(agentId: AgentId);

            Func<object, bool> validator =
                source =>
                {
                    // this cast only works because this unit test assembly has "internals visible to" granted to it by the 
                    //  main assembly- source is an anonymous type and their generates class is declared internal
                    dynamic validateObj = source;

                    Assert.IsNotNull(source);

                    Assert.AreEqual(Title, validateObj.Title);

                    Assert.AreEqual(AgentId, validateObj.AgentId);
                    Assert.IsNull(validateObj.AssetGroupId);
                    Assert.IsNull(validateObj.OwnerId);

                    Assert.AreEqual(incidentCreateResult.Status, validateObj.IncidentStatus);
                    Assert.AreEqual(incidentCreateResult.Id, validateObj.IncidentId);

                    return true;
                };

            this.mockTemplateStore.Setup(o => o.Render(this.execCtx, def.Title, It.IsAny<object>())).Returns(Title);

            this.mockCreator
                .Setup(o => o.CreateIncidentAsync(It.IsAny<CancellationToken>(), It.IsAny<AgentIncident>()))
                .ReturnsAsync(incidentCreateResult);

            // test
            await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // validate
            this.mockModel.Verify(o => o.TransformFrom(It.Is<object>(p => validator(p))));
        }
    }
}