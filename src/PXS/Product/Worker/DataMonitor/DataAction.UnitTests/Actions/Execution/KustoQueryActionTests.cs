// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.UnitTests.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.PrivacyServices.KustoHelpers;
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.PrivacyServices.Common.TemplateBuilder;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Actions;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Data;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Store;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class KustoQueryActionTests
    {
        private class KustoQueryActionTestException : Exception
        {
            public KustoQueryActionTestException(string message) : base(message) { }
        }

        private readonly Mock<IKustoClientFactory> mockKustoFact = new Mock<IKustoClientFactory>();
        private readonly Mock<IModelManipulator> mockModel = new Mock<IModelManipulator>();
        private readonly Mock<IExecuteContext> mockExecCtx = new Mock<IExecuteContext>();
        private readonly Mock<ITemplateStore> mockTemplateStore = new Mock<ITemplateStore>();
        private readonly Mock<IActionFactory> mockFact = new Mock<IActionFactory>();
        private readonly Mock<IParseContext> mockParseCtx = new Mock<IParseContext>();
        private readonly Mock<IActionStore> mockActionStore = new Mock<IActionStore>();
        private readonly Mock<IKustoClient> mockKustoClient = new Mock<IKustoClient>();
        private readonly Mock<IDataReader> mockDataReader = new Mock<IDataReader>();

        private readonly CancellationTokenSource cancelSource = new CancellationTokenSource();

        private const string DefTag = "ContextTag";

        private KustoQueryAction testObj;

        private IExecuteContext execCtx;
        private IActionFactory fact;
        private IParseContext parseCtx;
        private IActionStore store;

        [TestInitialize]
        public void Init()
        {
            this.parseCtx = this.mockParseCtx.Object;
            this.execCtx = this.mockExecCtx.Object;
            this.store = this.mockActionStore.Object;
            this.fact = this.mockFact.Object;

            this.mockExecCtx.SetupGet(o => o.Tag).Returns(KustoQueryActionTests.DefTag);
            this.mockExecCtx.SetupGet(o => o.CancellationToken).Returns(this.cancelSource.Token);
            this.mockExecCtx.SetupGet(o => o.IsSimulation).Returns(false);

            this.mockKustoFact
                .Setup(o => o.CreateClient(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(this.mockKustoClient.Object);

            this.mockKustoClient
                .Setup(o => o.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<KustoQueryOptions>()))
                .ReturnsAsync(this.mockDataReader.Object);

            this.mockTemplateStore
                .Setup(o => o.Render(It.IsAny<IParseContext>(), It.IsAny<TemplateRef>(), It.IsAny<object>()))
                .Returns((IContext ctx, TemplateRef tref, object data) => tref.Inline);

            this.mockTemplateStore
                .Setup(o => o.ValidateReference(It.IsAny<IContext>(), It.IsAny<TemplateRef>())).Returns(true);

            this.testObj = new KustoQueryAction(
                this.mockModel.Object,
                this.mockKustoFact.Object,
                this.mockTemplateStore.Object);
        }

        private (KustoQueryAction.Args, ActionRefCore, KustoQueryDef, object, object, object) SetupTestObj(
            DataTable table = null,
            IDictionary<string, object> queryArgsResult = null)
        {
            const string CounterSuffix = "CounterSuffix";

            DataSet dataSet;
            object queryArgsModel = null;
            object modelResult = new object();
            object modelIn = new object();

            IDictionary<string, ModelValue> argXform = new Dictionary<string, ModelValue>();

            KustoQueryDef def = new KustoQueryDef
            {
                Query = new TemplateRef { Inline = "query" },
                ClusterUrl = "cluster",
                Database = "database"
            };

            KustoQueryAction.Args args = new KustoQueryAction.Args
            {
                CounterSuffix = CounterSuffix,
                QueryParameters = queryArgsResult,
            };

            dataSet = new DataSet();
            dataSet.Tables.Add(table ?? new DataTable());

            this.mockKustoClient.Setup(o => o.ConvertToDataSet(It.IsAny<IDataReader>())).Returns(dataSet);

            this.mockTemplateStore
                .Setup(o => o.Render(It.IsAny<IContext>(), It.IsAny<TemplateRef>(), It.IsAny<object>()))
                .Returns(def.Query.Inline);

            this.mockModel.Setup(o => o.CreateEmpty()).Returns(modelResult);

            this.mockModel
                .Setup(o => o.TransformTo<KustoQueryAction.Args>(It.IsAny<object>()))
                .Returns(args);

            this.testObj = new KustoQueryAction(this.mockModel.Object, this.mockKustoFact.Object, this.mockTemplateStore.Object);

            Assert.IsTrue(this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, KustoQueryActionTests.DefTag, def));
            Assert.IsTrue(this.testObj.ExpandDefinition(this.parseCtx, this.store));
            Assert.IsTrue(this.testObj.Validate(this.parseCtx, null));

            this.mockModel
                .Setup(
                    o => o.MergeModels(
                        this.execCtx, 
                        modelIn, 
                        null,
                        It.Is<ICollection<KeyValuePair<string, ModelValue>>>(p => object.ReferenceEquals(p, argXform))))
                .Returns(args);

            return (args, new ActionRefCore { ArgTransform = argXform }, def, modelIn, modelResult, queryArgsModel);
        }

        [TestMethod]
        public async Task ExecuteParsesArguments()
        {
            KustoQueryAction.Args args;
            ActionRefCore refCore;
            object modelIn;

            (args, refCore, _, modelIn, _, _) = this.SetupTestObj();

            // test
            await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // verify
            this.mockModel.Verify(
                o => o.MergeModels(
                    this.execCtx, 
                    modelIn, 
                    null,
                    It.Is<ICollection<KeyValuePair<string, ModelValue>>>(p => object.ReferenceEquals(p, refCore.ArgTransform))),
                Times.Once);
            this.mockModel.Verify(o => o.TransformTo<KustoQueryAction.Args>(args), Times.Once);
        }

        [TestMethod]
        public async Task ExecuteParsesQueryArgumentsIfProvided()
        {
            IDictionary<string, object> queryArgsResult = new Dictionary<string, object>();
            KustoQueryAction.Args args;
            ActionRefCore refCore;
            object modelQueryArgs;
            object modelIn;

            (args, refCore, _, modelIn, _, modelQueryArgs) = this.SetupTestObj(queryArgsResult: queryArgsResult);

            // test
            await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // verify
            this.mockModel.Verify(
                o => o.MergeModels(
                    this.execCtx, 
                    modelIn, 
                    null,
                    It.Is<ICollection<KeyValuePair<string, ModelValue>>>(p => object.ReferenceEquals(p, refCore.ArgTransform))),
                Times.Once);

            this.mockModel.Verify(o => o.TransformTo<KustoQueryAction.Args>(args), Times.Once);
        }

        [TestMethod]
        public async Task ExecuteRendersQuery()
        {
            KustoQueryDef def;
            ActionRefCore refCore;
            object modelIn;

            (_, refCore, def, modelIn, _, _) = this.SetupTestObj();

            // test
            await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // verify
            this.mockTemplateStore.Verify(o => o.Render(this.execCtx, def.Query, modelIn), Times.Once);
        }

        [TestMethod]
        public async Task ExecuteUsesQueryOverrrideIfOneProvided()
        {
            KustoQueryAction.Args args;
            ActionRefCore refCore;
            object modelIn;

            (args, refCore, _, modelIn, _, _) = this.SetupTestObj();

            args.QueryTagOverride = "QUERYTAGOVERRIDE";

            // test
            await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // verify
            this.mockTemplateStore.Verify(
                o => o.Render(
                    this.execCtx,
                    It.Is<TemplateRef>(p => p.Inline == null && args.QueryTagOverride.Equals(p.TemplateTag)),
                    modelIn),
                Times.Once);
        }

        [TestMethod]
        public async Task ExecuteSendsQueryToKustoWithNoParametersIfNoneProvided()
        {
            ActionRefCore refCore;
            KustoQueryDef def;
            object modelIn;

            Func<KustoQueryOptions, bool> queryOptValidator =
                o =>
                {
                    Assert.IsNotNull(o.Parameters);
                    Assert.AreEqual(0, o.Parameters.Count);
                    return true;
                };

            (_, refCore, def, modelIn, _, _) = this.SetupTestObj();

            // test
            await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // verify
            this.mockKustoFact.Verify(
                o => o.CreateClient(def.ClusterUrl, def.Database, KustoQueryActionTests.DefTag),
                Times.Once);

            this.mockKustoClient
                .Verify(
                    o => o.ExecuteQueryAsync(def.Query.Inline, It.Is<KustoQueryOptions>(p => queryOptValidator(p))),
                    Times.Once);
            this.mockKustoClient.Verify(o => o.ConvertToDataSet(this.mockDataReader.Object), Times.Once);
        }

        [TestMethod]
        public async Task ExecuteLogsEventAndIncrementsCounterOnSuccess()
        {
            KustoQueryAction.Args args;
            ActionRefCore refCore;
            object modelIn;

            (args, refCore, _, modelIn, _, _) = this.SetupTestObj();

            // test
            await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // verify
            this.mockExecCtx.Verify(
                o => o.ReportActionEvent(
                    "success",
                    this.testObj.Type,
                    this.testObj.Tag,
                    It.Is<IDictionary<string, string>>(p => p == null || p.Count == 0)));

            this.mockExecCtx.Verify(
                o => o.IncrementCounter("Kusto Queries Executed", this.testObj.Tag, args.CounterSuffix, 1));
        }

        [TestMethod]
        public async Task ExecuteLogsEventAndIncrementsCounterOnFailure()
        {
            const string ErrMsg = "ERROR";

            KustoQueryAction.Args args;
            ActionRefCore refCore;
            object modelIn;

            (args, refCore, _, modelIn, _, _) = this.SetupTestObj();

            this.mockKustoClient
                .Setup(o => o.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<KustoQueryOptions>()))
                .Returns(Task.FromException<IDataReader>(new KustoQueryActionTestException(ErrMsg)));

            // test
            await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // validate
            this.mockExecCtx.Verify(
                o => o.ReportActionError(
                    "error",
                    this.testObj.Type,
                    this.testObj.Tag,
                    ErrMsg,
                    It.Is<IDictionary<string, string>>(p => p == null || p.Count == 0)));

            this.mockExecCtx.Verify(
                o => o.IncrementCounter("Kusto Query Errors", this.testObj.Tag, args.CounterSuffix, 1));
        }

        private class TestData
        {
            public TestData(int d1, string d2) { this.D1 = d1; this.D2 = d2; }
            public int D1 { get; set; }
            public string D2 { get; set; }
        }

        [TestMethod]
        public async Task ExecuteSendsQueryToKustoWithParametersIfSomeProvided()
        {
            IDictionary<string, object> queryArgsResult = new Dictionary<string, object>();
            ActionRefCore refCore;
            KustoQueryDef def;
            object modelIn;

            Func<KustoQueryOptions, bool> queryOptValidator =
                o =>
                {
                    Assert.IsNotNull(o.Parameters);
                    Assert.AreEqual(queryArgsResult.Count, o.Parameters.Count);
                    Assert.AreEqual("1", o.Parameters["intVal"]);
                    Assert.AreEqual(@"""1d""", o.Parameters["stringVal"]);
                    Assert.AreEqual("[1,2]", o.Parameters["listIntVal"]);
                    Assert.AreEqual(@"[{""D1"":1,""D2"":""a""},{""D1"":2,""D2"":""b""}]", o.Parameters["listComplexVal"]);
                    return true;
                };

            queryArgsResult["intVal"] = 1;
            queryArgsResult["stringVal"] = "1d";
            queryArgsResult["listIntVal"] = new List<int> { 1, 2 };
            queryArgsResult["listComplexVal"] = new List<TestData> { new TestData(1, "a"), new TestData(2, "b") };

            (_, refCore, def, modelIn, _, _) = this.SetupTestObj(queryArgsResult: queryArgsResult);

            // test
            await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // verify
            this.mockKustoFact.Verify(
                o => o.CreateClient(def.ClusterUrl, def.Database, KustoQueryActionTests.DefTag),
                Times.Once);

            this.mockKustoClient.Verify(
                o => o.ExecuteQueryAsync(def.Query.Inline, It.Is<KustoQueryOptions>(p => queryOptValidator(p))),
                Times.Once);

            this.mockKustoClient.Verify(o => o.ConvertToDataSet(this.mockDataReader.Object), Times.Once);
        }

        [TestMethod]
        public async Task ExecutePopulatesResultWithResultingTable()
        {
            ActionRefCore refCore;
            DataTable table = new DataTable();
            object modelResult;
            object modelIn;

            (_, refCore, _, modelIn, modelResult, _) = this.SetupTestObj(table: table);

            // test
            await this.testObj.ExecuteAsync(this.execCtx, refCore, modelIn);

            // verify
            this.mockModel.Verify(o => o.CreateEmpty(), Times.Once);

            this.mockModel.Verify(
                o => o.AddSubmodel(this.execCtx, modelResult, "Table00", table, MergeMode.ReplaceExisting),
                Times.Once);
            this.mockModel.Verify(
                o => o.AddSubmodel(
                    It.IsAny<IContext>(), 
                    It.IsAny<object>(), 
                    It.IsAny<string>(), 
                    It.IsAny<object>(),
                    It.IsAny<MergeMode>()),
                Times.Once);
        }
    }
}
