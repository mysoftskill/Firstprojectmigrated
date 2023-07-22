// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.TemplateBuilder.UnitTests.Store
{
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.PrivacyServices.Common.TemplateBuilder;
    using Microsoft.PrivacyServices.Common.TemplateBuilder.Engine;
    using Microsoft.PrivacyServices.Common.TemplateBuilder.Fragments;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class TemplateParserTests
    {
        private readonly Mock<IModelManipulator> mockModel = new Mock<IModelManipulator>();
        private readonly Mock<IFragmentFactory> mockFragFact = new Mock<IFragmentFactory>(MockBehavior.Strict);
        private readonly Mock<IFragment> mockFrag = new Mock<IFragment>(MockBehavior.Strict);
        private readonly Mock<IContext> mockContext = new Mock<IContext>();

        private IContext ctx;

        [TestInitialize]
        public void Init()
        {
            this.mockFragFact.Setup(o => o.CreateTemplateFragment()).Returns(this.mockFrag.Object);

            this.mockFrag
                .Setup(o => o.Parse(It.IsAny<IContext>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(0);

            this.ctx = this.mockContext.Object;
        }

        [TestMethod]
        public void ParseReturnsTemplateWithMatchingPropertiesFromDefinition()
        {
            TemplateDef def = new TemplateDef { Tag = "TemplateTag", Text = "TemplateText" };

            TemplateParser testObj = new TemplateParser(this.mockModel.Object, this.mockFragFact.Object);

            // test
            IParsedTemplate result = testObj.Parse(this.ctx, def);

            // verify
            Assert.IsNotNull(result);
            Assert.AreEqual(def.Tag, result.Tag);
        }

        [TestMethod]
        public void ParseCreatesTemplateFragmentAndParsesTextFromDefinition()
        {
            TemplateDef def = new TemplateDef { Tag = "TemplateTag", Text = "TemplateText" };

            TemplateParser testObj = new TemplateParser(this.mockModel.Object, this.mockFragFact.Object);

            // test
            testObj.Parse(this.ctx, def);

            // verify
            this.mockFragFact.Verify(o => o.CreateTemplateFragment(), Times.Once);
            this.mockFrag.Verify(o => o.Parse(this.ctx, def.Text, 0, def.Text.Length), Times.Once);
        }
    }
}
