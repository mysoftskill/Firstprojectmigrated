// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.TemplateBuilder.UnitTests.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.PrivacyServices.Common.TemplateBuilder;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.PrivacyServices.Common.TemplateBuilder.Engine;
    using Microsoft.PrivacyServices.Common.TemplateBuilder.Fragments;

    using Moq;

    [TestClass]
    public class ParsedTemplateTests
    {
        private readonly Mock<IModelManipulator> mockModel = new Mock<IModelManipulator>();
        private readonly Mock<IFragment> mockFrag = new Mock<IFragment>();
        private readonly Mock<IContext> mockCtx = new Mock<IContext>();

        private IModelManipulator manip;
        private IContext ctx;

        [TestInitialize]
        public void Init()
        {
            this.mockFrag
                .Setup(o => o.Render(It.IsAny<IContext>(), It.IsAny<StringBuilder>(), It.IsAny<object>()))
                .Returns(new StringBuilder());

            this.manip = this.mockModel.Object;
            this.ctx = this.mockCtx.Object;
        }

        [TestMethod]
        public void ConstructorSetsTagAndAllowDataParamsPropertiesAppropriately()
        {
            TemplateDef def = new TemplateDef { Tag = "TobyTheDog" };

            // test
            ParsedTemplate testObjLocal = new ParsedTemplate(this.manip, def, this.mockFrag.Object);

            // verify
            Assert.AreEqual(def.Tag, testObjLocal.Tag);
        }

        [TestMethod]
        public void GetVariablesCallsIntoFragmentGetVariablesAndReturnsSameList()
        {
            ICollection<string> expected = new List<string> { "TEST1" };
            TemplateDef def = new TemplateDef { Tag = "TobyTheDog" };
            ParsedTemplate testObjLocal = new ParsedTemplate(this.manip, def, this.mockFrag.Object);

            ICollection<string> result;

            this.mockFrag.Setup(o => o.GetVariables()).Returns(expected);

            // test
            result = testObjLocal.GetVariables();

            // verify
            Assert.IsNotNull(result);
            Assert.AreEqual(expected.Count, result.Count);
            Assert.AreEqual(expected.First(), result.First());
        }

        [TestMethod]
        public void RenderMergesModelsAndCallsIntoFragmentRender()
        {
            TemplateDef def = new TemplateDef { Tag = "TobyTheDog" };
            ParsedTemplate testObjLocal = new ParsedTemplate(this.manip, def, this.mockFrag.Object);

            ICollection<KeyValuePair<string, ModelValue>> parameters = new List<KeyValuePair<string, ModelValue>>();
            object mergeResult = new object();
            object model = new object();

            this.mockModel
                .Setup(
                    o => o.MergeModels(
                        It.IsAny<IContext>(),
                        It.IsAny<object>(),
                        It.IsAny<object>(),
                        It.IsAny<ICollection<KeyValuePair<string, ModelValue>>>()))
                .Returns(mergeResult);

            // test
            testObjLocal.Render(this.ctx, parameters, model);

            // verify
            this.mockFrag.Verify(o => o.Render(this.ctx, It.IsAny<StringBuilder>(), mergeResult), Times.Once);

            this.mockModel.Verify(o => o.MergeModels(this.ctx, model, null, parameters), Times.Once);
        }
    }
}
 