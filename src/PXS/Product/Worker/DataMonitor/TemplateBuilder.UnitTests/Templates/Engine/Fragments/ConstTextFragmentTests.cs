// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.TemplateBuilder.UnitTests.Engine
{
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.TemplateBuilder.Fragments;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class ConstTextFragmentTests
    {
        private readonly Mock<IContext> mockCtx = new Mock<IContext>();

        private IContext ctx;

        [TestInitialize]
        public void Init()
        {
            this.ctx = this.mockCtx.Object;
        }

        [TestMethod]
        public void ParseCopiesInputStringGivenIndexAndLengthBoundsAndReturnsItWhenRenderIsCalled()
        {
            const string Expected = "ThisIsATest";
            const string Text = "0ThisIsATest0";

            ConstTextFragment testObj = new ConstTextFragment();

            // test
            testObj.Parse(this.ctx, Text, 1, Text.Length - 2);

            // verify
            Assert.AreEqual("[CONST text:'" + Expected + "']", testObj.ToString());
        }

        [TestMethod]
        public void ParseCopiesInputStringGivenIndexAndLengthBoundsAndUnescapesDelimeterSequenceAndReturnsItWhenRenderIsCalled()
        {
            const string Expected = "[[<ThisIsATest>]]";
            const string Text = "0\\[\\[\\<ThisIsATest\\>\\]\\]0";

            ConstTextFragment testObj = new ConstTextFragment();

            // test
            testObj.Parse(this.ctx, Text, 1, Text.Length - 2);

            // verify
            Assert.AreEqual("[CONST text:'" + Expected + "']", testObj.ToString());
        }

        [TestMethod]
        public void RenderCopiesInternalStringToOutput()
        {
            const string Expected = "ThisIsATest";
            const string Text = "ThisIsATest";

            ConstTextFragment testObj = new ConstTextFragment();

            string result;

            testObj.Parse(this.ctx, Text, 0, Text.Length);

            // test
            result = testObj.Render(this.ctx, null, null).ToString();

            // verify
            Assert.AreEqual(Expected, result);
        }
    }
}
