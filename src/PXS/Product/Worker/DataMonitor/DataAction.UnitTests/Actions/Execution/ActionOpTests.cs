// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.UnitTests.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Actions;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Data;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Store;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Newtonsoft.Json.Linq;

    [TestClass]
    public class ActionOpTests
    {
        public interface IActionInternal<in T>
        {
            bool ProcessDefinition(IParseContext context, IActionFactory factory, T definition);

            Task<(bool Continue, object Result)> ExecuteAsync(
                IExecuteContext context,
                ActionRefCore actionRef,
                object model);

            bool ValidateAndNormalize(IContext context);
        }

        public class MockDef : IValidatable
        {
            private readonly IActionInternal<MockDef> mock;

            public MockDef(IActionInternal<MockDef> mock = null) => this.mock = mock;

            public string Data { get; set; }

            public bool ValidateAndNormalize(IContext context) => this.mock?.ValidateAndNormalize(context) ?? true;
        }

        private class TestAction<T> : ActionOp<T>
            where T : class
        {
            private readonly IActionInternal<T> derived;

            public TestAction(IModelManipulator mm, IActionInternal<T> derived) : base(mm) => this.derived = derived;

            public ICollection<string> RequiredParamsActual { private get; set; }
            public bool RequiresDefinitionActual { private get; set; }

            public override string Type => "TESTTYPE";

            protected override ICollection<string> RequiredParams => this.RequiredParamsActual;

            protected override DefinitionMode DefinitionMode => 
                this.RequiresDefinitionActual ? DefinitionMode.Required : DefinitionMode.Forbidden;

            protected override Task<(bool Continue, object Result)> ExecuteInternalAsync(
                IExecuteContext context,
                ActionRefCore actionRef,
                object model)
            {
                return this.derived.ExecuteAsync(context, actionRef, model);
            }

            protected override bool ProcessAndStoreDefinition(
                IParseContext context, 
                IActionFactory factory, 
                T definition)
            {
                return this.derived.ProcessDefinition(context, factory, definition);
            }
        }

        private readonly Mock<IActionInternal<MockDef>> mockDerivedMockDef = new Mock<IActionInternal<MockDef>>();
        private readonly Mock<IModelManipulator> mockModel = new Mock<IModelManipulator>();
        private readonly Mock<IExecuteContext> mockExecCtx = new Mock<IExecuteContext>();
        private readonly Mock<IActionFactory> mockFact = new Mock<IActionFactory>();
        private readonly Mock<IParseContext> mockParseCtx = new Mock<IParseContext>();
        private readonly Mock<IActionStore> mockStore = new Mock<IActionStore>();

        private readonly CancellationTokenSource cancelSource = new CancellationTokenSource();

        private TestAction<MockDef> testObj;

        private IExecuteContext execCtx;
        private IActionFactory fact;
        private IParseContext parseCtx;

        [TestInitialize]
        public void Init()
        {
            this.mockDerivedMockDef
                .Setup(o => o.ProcessDefinition(It.IsAny<IParseContext>(), It.IsAny<IActionFactory>(), It.IsAny<MockDef>()))
                .Returns(true);

            this.testObj = new TestAction<MockDef>(this.mockModel.Object, this.mockDerivedMockDef.Object);

            this.mockExecCtx.SetupGet(o => o.CancellationToken).Returns(this.cancelSource.Token);
            this.mockExecCtx.SetupGet(o => o.IsSimulation).Returns(false);

            this.parseCtx = this.mockParseCtx.Object;
            this.execCtx = this.mockExecCtx.Object;
            this.fact = this.mockFact.Object;
        }

        [TestMethod]
        public void ParseAndProcessLogsErrorIfTagNull()
        {
            bool result;

            // test
            result = this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, null, null);

            // verify
            Assert.IsFalse(result);
            Assert.IsFalse(this.testObj.IsValid);
            this.mockParseCtx.Verify(
                o => o.LogError(It.Is<string>(p => p.Contains("must contain have a non-empty tag"))),
                Times.Once);
        }

        [TestMethod]
        public void ParseAndProcessLogsErrorIfTagEmpty()
        {
            bool result;

            // test
            result = this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, "  ", null);

            // verify
            Assert.IsFalse(result);
            Assert.IsFalse(this.testObj.IsValid);
            this.mockParseCtx.Verify(
                o => o.LogError(It.Is<string>(p => p.Contains("must contain have a non-empty tag"))),
                Times.Once);
        }

        [TestMethod]
        public void ParseSetsTagCorrectly()
        {
            const string Tag = "testTag";

            bool result;

            // test
            result = this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, Tag, null);

            // verify
            Assert.IsTrue(result);
            Assert.IsTrue(this.testObj.IsValid);
            Assert.AreEqual(Tag, this.testObj.Tag);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ParseThrowsIfAlreadyInitializedSetsTagCorrectly()
        {
            const string Tag = "testTag";

            this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, Tag, null);

            // test
            this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, Tag, null);
        }

        [TestMethod]
        public void ParseLogsErrorIfDefinitionNotProvidedWhenRequired()
        {
            const string Tag = "testTag";

            bool result;

            this.testObj.RequiresDefinitionActual = true;

            // test
            result = this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, Tag, null);

            // verify
            Assert.IsFalse(result);
            Assert.IsFalse(this.testObj.IsValid);
            this.mockParseCtx.Verify(
                o => o.LogError(It.Is<string>(p => p.Contains("require a definition object of type"))),
                Times.Once);
        }

        [TestMethod]
        public void ParseLogsErrorIfDefinitionProvidedWhenNotRequired()
        {
            const string Tag = "testTag";

            bool result;

            this.testObj.RequiresDefinitionActual = false;

            // test
            result = this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, Tag, new MockDef());

            // verify
            Assert.IsFalse(result);
            Assert.IsFalse(this.testObj.IsValid);
            this.mockParseCtx.Verify(
                o => o.LogError(It.Is<string>(p => p.Contains("require no action definition, but a definition of type"))),
                Times.Once);
        }

        [TestMethod]
        public void ParseParsesJTokenDefinitionIntoObject()
        {
            const string Tag = "testTag";
            const string Data = "ParseParsesJTokenDefinitionIntoObject";

            JToken def = new JObject(new JProperty("Data", Data));
            bool result;

            this.testObj.RequiresDefinitionActual = true;

            // test
            result = this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, Tag, def);

            // verify
            Assert.IsTrue(result);
            Assert.IsTrue(this.testObj.IsValid);
            this.mockDerivedMockDef.Verify(
                o => o.ProcessDefinition(this.parseCtx, this.fact, It.Is<MockDef>(p => p.Data.Equals(Data))),
                Times.Once);
        }

        [TestMethod]
        public void ParseParsesJTokenDefinitionContainingExtraProperties()
        {
            const string Tag = "testTag";
            const string Data = "ParseParsesJTokenDefinitionIntoObject";

            JToken def = new JObject(new JProperty("NoProp", Data), new JProperty("Data", Data));
            bool result;

            this.testObj.RequiresDefinitionActual = true;

            // test
            result = this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, Tag, def);

            // verify
            Assert.IsTrue(result);
            Assert.IsTrue(this.testObj.IsValid);
            this.mockDerivedMockDef.Verify(
                o => o.ProcessDefinition(this.parseCtx, this.fact, It.Is<MockDef>(p => p.Data.Equals(Data))),
                Times.Once);
        }

        [TestMethod]
        public void ParseLogsErrorMessageIfCannotParseJTokenDefinitionIntoObject()
        {
            const string Tag = "testTag";

            JToken def = new JArray(new JObject());
            bool result;

            this.testObj.RequiresDefinitionActual = true;

            // test
            result = this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, Tag, def);

            // verify
            Assert.IsFalse(result);
            Assert.IsFalse(this.testObj.IsValid);
            this.mockParseCtx.Verify(
                o => o.LogError(
                    It.IsAny<Exception>(),
                    It.Is<string>(p => p.Contains("Parse failure deserializing JToken for"))),
                Times.Once);
        }

        [TestMethod]
        public void ParseParsesStringDefinitionContainingJson()
        {
            const string Tag = "testTag";
            const string Data = "ParseParsesJTokenDefinitionIntoObject";

            const string Json = "{'Data':'" + Data + "'}";

            bool result;

            this.testObj.RequiresDefinitionActual = true;

            // test
            result = this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, Tag, Json);

            // verify
            Assert.IsTrue(result);
            Assert.IsTrue(this.testObj.IsValid);
            this.mockDerivedMockDef.Verify(
                o => o.ProcessDefinition(this.parseCtx, this.fact, It.Is<MockDef>(p => p.Data.Equals(Data))),
                Times.Once);
        }

        [TestMethod]
        public void ParseParsesStringDefinitionContainingExtraProperties()
        {
            const string Tag = "testTag";
            const string Data = "ParseParsesJTokenDefinitionIntoObject";

            const string Json = "{'Data':'" + Data + "','OtherData': 'other'}";

            bool result;

            this.testObj.RequiresDefinitionActual = true;

            // test
            result = this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, Tag, Json);

            // verify
            Assert.IsTrue(result);
            Assert.IsTrue(this.testObj.IsValid);
            this.mockDerivedMockDef.Verify(
                o => o.ProcessDefinition(this.parseCtx, this.fact, It.Is<MockDef>(p => p.Data.Equals(Data))),
                Times.Once);
        }

        [TestMethod]
        public void ParseLogsErrorMessageIfCannotParseStringDefinitionIntoObject()
        {
            const string Tag = "testTag";
            const string Data = "ParseParsesJTokenDefinitionIntoObject";

            const string Json = "{'Data':'" + Data + "'"; // deliberately missing training curly brace
            bool result;

            this.testObj.RequiresDefinitionActual = true;

            // test
            result = this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, Tag, Json);

            // verify
            Assert.IsFalse(result);
            Assert.IsFalse(this.testObj.IsValid);
            this.mockParseCtx.Verify(
                o => o.LogError(
                    It.IsAny<Exception>(),
                    It.Is<string>(p => p.Contains("Parse failure deserializing JSON for"))),
                Times.Once);
        }

        [TestMethod]
        public void ParseTreatsEmptyStringDefinitionAsNullObject()
        {
            const string Tag = "testTag";

            bool result;

            // test
            result = this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, Tag, string.Empty);

            // verify
            Assert.IsTrue(result);
            Assert.IsTrue(this.testObj.IsValid);
            this.mockDerivedMockDef.Verify(
                o => o.ProcessDefinition(this.parseCtx, this.fact, It.Is<MockDef>(p => p == null)),
                Times.Once);
        }

        [TestMethod]
        public void ParseLogsErrorIfDefinitionNotASupportedObjectType()
        {
            const string Tag = "testTag";

            bool result;

            // test
            result = this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, Tag, new object());

            // verify
            Assert.IsFalse(result);
            Assert.IsFalse(this.testObj.IsValid);
            this.mockParseCtx.Verify(
                o => o.LogError(
                    It.Is<string>(p => p.Contains("action must be either an JToken, an ActionDef, a string containing"))),
                Times.Once);
            this.mockParseCtx.Verify(
                o => o.LogError(
                    It.Is<string>(p => p.Contains("action must be either an JToken, an ActionDef, a string containing"))),
                Times.Once);
        }

        [TestMethod]
        public void ParseParsesActionDefDefinitionContainingExtraProperties()
        {
            const string Tag = "testTag";
            const string Data = "ParseParsesJTokenDefinitionIntoObject";

            const string Json = "{'Data':'" + Data + "'}";

            ActionDef def = new ActionDef { Def = Json, Tag = Tag, Type = this.testObj.Type };
            bool result;

            this.testObj.RequiresDefinitionActual = true;

            // test
            result = this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, Tag, def);

            // verify
            Assert.IsTrue(result);
            Assert.IsTrue(this.testObj.IsValid);
            this.mockDerivedMockDef.Verify(
                o => o.ProcessDefinition(this.parseCtx, this.fact, It.Is<MockDef>(p => p.Data.Equals(Data))),
                Times.Once);
        }

        [TestMethod]
        public void ParseSetsUpContextCallsDerviedParserAndClearsContextOnCompletion()
        {
            const string Tag = "testTag";

            MockDef def = new MockDef();
            bool result;

            this.testObj.RequiresDefinitionActual = true;

            // test
            result = this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, Tag, def);

            // verify
            Assert.IsTrue(result);
            Assert.IsTrue(this.testObj.IsValid);
            this.mockParseCtx.Verify(o => o.OnActionStart(ActionType.Parse, Tag), Times.Once);
            this.mockParseCtx.Verify(o => o.PushErrorIntroMessage(It.IsAny<Func<string>>()), Times.Once);
            this.mockDerivedMockDef.Verify(o => o.ProcessDefinition(this.parseCtx, this.fact, def));
            this.mockParseCtx.Verify(o => o.OnActionEnd(), Times.Once);
        }
        
        [TestMethod]
        public void ParseReturnsFalseIfDefinitionFailsToValidate()
        {
            MockDef def;
            bool result;

            def = new MockDef(this.mockDerivedMockDef.Object);

            // test
            result = this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, "tag", def);

            // verify
            Assert.IsFalse(result);
            Assert.IsFalse(this.testObj.IsValid);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ExpandDefinitionThrowsIfObjectNotInitialized()
        {
            this.testObj.ExpandDefinition(this.parseCtx, this.mockStore.Object);
        }
        
        [TestMethod]
        public void ExpandDefinitionReturnsTrue()
        {
            bool result;

            this.testObj.RequiresDefinitionActual = false;
            this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, "testTag", null);

            // test
            result = this.testObj.ExpandDefinition(this.parseCtx, this.mockStore.Object);

            // verify
            Assert.IsTrue(this.testObj.IsValid);
            Assert.IsTrue(result);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ValidateParseThrowsIfObjectNotInitialized()
        {
            this.testObj.Validate(this.parseCtx, new Dictionary<string, ModelValue>());
        }
        
        [TestMethod]
        public void ValidateParseLogsErrorIfParameterNameIsEmpty()
        {
            IDictionary<string, ModelValue> args = 
                new Dictionary<string, ModelValue> { { string.Empty, new ModelValue() } };

            bool result;

            this.testObj.RequiresDefinitionActual = false;
            this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, "testTag", null);

            // test
            result = this.testObj.Validate(this.parseCtx, args);

            // verify
            Assert.IsFalse(result);
            Assert.IsFalse(this.testObj.IsValid);
            this.mockParseCtx.Verify(
                o => o.LogError(
                    It.Is<string>(p => p.Contains("parameter names must be non-empty"))),
                Times.Once);
        }

        [TestMethod]
        public void ValidateParseLogsErrorIfNoParametersProvidedButSomeAreRequired()
        {
            bool result;

            this.testObj.RequiresDefinitionActual = false;
            this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, "testTag", null);
            this.testObj.RequiredParamsActual = new[] { "p1" };

            // test
            result = this.testObj.Validate(this.parseCtx, null);

            // verify
            Assert.IsFalse(result);
            Assert.IsFalse(this.testObj.IsValid);
            this.mockParseCtx.Verify(
                o => o.LogError(
                    It.Is<string>(p => p.Contains("the following required parameters are not specified: p1"))),
                Times.Once);
        }

        [TestMethod]
        public void ValidateParseLogsErrorIfNotAllRequiredParametersProvided()
        {
            IDictionary<string, ModelValue> args =
                new Dictionary<string, ModelValue> { { "p1", new ModelValue() } };
            bool result;

            this.testObj.RequiresDefinitionActual = false;
            this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, "testTag", null);
            this.testObj.RequiredParamsActual = new[] { "p1", "p2" };

            // test
            result = this.testObj.Validate(this.parseCtx, args);

            // verify
            Assert.IsFalse(result);
            Assert.IsFalse(this.testObj.IsValid);
            this.mockParseCtx.Verify(
                o => o.LogError(
                    It.Is<string>(p => p.Contains("the following required parameters are not specified: p2"))),
                Times.Once);
        }

        [TestMethod]
        public void ValidateParseLogsReturnsTrueIfAllRequiredParametersAreSpecified()
        {
            IDictionary<string, ModelValue> args =
                new Dictionary<string, ModelValue> { { "p1", new ModelValue() }, { "p2", new ModelValue() } };
            bool result;

            this.testObj.RequiresDefinitionActual = false;
            this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, "testTag", null);
            this.testObj.RequiredParamsActual = new[] { "p1" };

            // test
            result = this.testObj.Validate(this.parseCtx, args);

            // verify
            Assert.IsTrue(result);
            Assert.IsTrue(this.testObj.IsValid);
        }

        [TestMethod]
        public void ValidateParseLogsReturnsTrueIfNoParametersRequredAndNoneSpecified()
        {
            bool result;

            this.testObj.RequiresDefinitionActual = false;
            this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, "testTag", null);
            this.testObj.RequiredParamsActual = new string[0];

            // test
            result = this.testObj.Validate(this.parseCtx, null);

            // verify
            Assert.IsTrue(result);
            Assert.IsTrue(this.testObj.IsValid);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task ExecuteThrowsIfObjectNotInitialized()
        {
            // test
            await this.testObj.ExecuteAsync(this.execCtx, new ActionRefCore(), null);
        }

        [TestMethod]
        public async Task ExecuteSetsUpContextLogsActivityCallsDerivedClassAndCleansUp()
        {
            const string Desc = "invocation description";
            const string Tag = "testTag";
            const bool Continue = true;

            ExecuteResult result;
            ActionRef aref = new ActionRef { Description = Desc };
            object model = new object();

            this.mockDerivedMockDef
                .Setup(o => o.ExecuteAsync(It.IsAny<IExecuteContext>(), It.IsAny<ActionRefCore>(), It.IsAny<object>()))
                .ReturnsAsync((Continue, null));

            this.testObj.RequiresDefinitionActual = true;
            this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, Tag, new MockDef());

            // test
            result = await this.testObj.ExecuteAsync(this.execCtx, aref, model);

            // verify
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Continue);
            this.mockExecCtx.Verify(o => o.OnActionStart(ActionType.Execute, Tag), Times.Once);
            this.mockExecCtx.Verify(o => o.OnActionEnd(), Times.Once);
            this.mockExecCtx.Verify(o => o.LogVerbose(It.Is<string>(p => p.Contains(": " + Desc))), Times.Once);
            this.mockDerivedMockDef.Verify(o => o.ExecuteAsync(this.execCtx, aref, model), Times.Once);
        }

        [TestMethod]
        public async Task ExecuteSkipsTransformingModelWhenResultModelIsNull()
        {
            const string Tag = "testTag";

            ActionRef aref;
            object actionModel = new object();

            this.mockDerivedMockDef
                .Setup(o => o.ExecuteAsync(It.IsAny<IExecuteContext>(), It.IsAny<ActionRefCore>(), It.IsAny<object>()))
                .ReturnsAsync((true, null));

            aref = new ActionRef
            {
                ResultTransform = new Dictionary<string, ModelValueUpdate>
                {
                    { "test", new ModelValueUpdate { Const = 1 } },
                }
            };

            this.testObj.RequiresDefinitionActual = true;
            this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, Tag, new MockDef());

            // test
            await this.testObj.ExecuteAsync(this.execCtx, aref, actionModel);

            // verify
            this.mockModel.Verify(
                o => o.MergeModels(
                    It.IsAny<IContext>(),
                    It.IsAny<object>(),
                    It.IsAny<object>(),
                    It.IsAny<ICollection<KeyValuePair<string, ModelValue>>>()),
                Times.Never);
        }

        [TestMethod]
        public async Task ExecuteSkipsTransformingModelWhenResultTransformIsNull()
        {
            const string Tag = "testTag";

            ActionRef aref = new ActionRef();
            object actionModel = new object();
            object resultModel = new object();

            this.mockDerivedMockDef
                .Setup(o => o.ExecuteAsync(It.IsAny<IExecuteContext>(), It.IsAny<ActionRefCore>(), It.IsAny<object>()))
                .ReturnsAsync((true, resultModel));

            this.testObj.RequiresDefinitionActual = true;
            this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, Tag, new MockDef());

            // test
            await this.testObj.ExecuteAsync(this.execCtx, aref, actionModel);

            // verify
            this.mockModel.Verify(
                o => o.MergeModels(
                    It.IsAny<IContext>(),
                    It.IsAny<object>(),
                    It.IsAny<object>(),
                    It.IsAny<ICollection<KeyValuePair<string, ModelValue>>>()),
                Times.Never);
        }

        [TestMethod]
        public async Task ExecuteTransformsResultModelWhenResultModelIsNotNullAndTransformHasValues()
        {
            const string ExpectedName = "test";
            const string Tag = "testTag";

            ModelValueUpdate modelValueUpdate = new ModelValueUpdate();

            ActionRef aref;
            object actionModel = new object();
            object resultModel = new object();

            Func<ICollection<KeyValuePair<string, ModelValue>>, bool> verifier = 
                o =>
                {
                    Assert.AreEqual(1, o.Count);
                    Assert.AreEqual(ExpectedName, o.First().Key);
                    Assert.AreSame(modelValueUpdate, o.First().Value);
                    return true;
                };

            this.mockDerivedMockDef
                .Setup(o => o.ExecuteAsync(It.IsAny<IExecuteContext>(), It.IsAny<ActionRefCore>(), It.IsAny<object>()))
                .ReturnsAsync((true, resultModel));

            aref = new ActionRef
            {
                ResultTransform = new Dictionary<string, ModelValueUpdate> { { ExpectedName, modelValueUpdate } }
            };

            this.testObj.RequiresDefinitionActual = true;
            this.testObj.ParseAndProcessDefinition(this.parseCtx, this.fact, Tag, new MockDef());

            // test
            await this.testObj.ExecuteAsync(this.execCtx, aref, actionModel);

            // verify
            this.mockModel.Verify(
                o => o.MergeModels(
                    this.execCtx,
                    actionModel,
                    resultModel,
                    It.Is<ICollection<KeyValuePair<string, ModelValue>>>(p => verifier(p))),
                Times.Never);
        }
    }
}
