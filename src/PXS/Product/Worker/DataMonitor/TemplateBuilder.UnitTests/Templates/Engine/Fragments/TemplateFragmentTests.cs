// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.TemplateBuilder.UnitTests.Engine
{
    using System.Collections.Generic;
    using System.Text;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.PrivacyServices.Common.TemplateBuilder;
    using Microsoft.PrivacyServices.Common.TemplateBuilder.Fragments;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Practices.ObjectBuilder2;
    using Microsoft.PrivacyServices.Common.Context;

    using Moq;

    [TestClass]
    public class TemplateFragmentTests
    {
        private const string FragConstToString = "[[[FCRTS]]]";
        private const string FragConstRender = "[[[FCR]]]";
        private const string FragToString = "<<<FTS>>>";
        private const string FragRender = "<<<FR>>>";
        private const string FragVar = "[[[FRAGVAR]]]";

        private const string FragInnerOk = "[[<ok ";
        
        private readonly Mock<IFragmentFactory> mockFragFact = new Mock<IFragmentFactory>(MockBehavior.Strict);
        private readonly Mock<IFragment> mockFragConst = new Mock<IFragment>();
        private readonly Mock<IFragment> mockFrag = new Mock<IFragment>();
        private readonly Mock<IContext> mockCtx = new Mock<IContext>();

        private IContext ctx;

        [TestInitialize]
        public void Init()
        {
            this.mockFragFact.Setup(o => o.CreateConstFragment()).Returns(this.mockFragConst.Object);
            this.mockFragFact.Setup(o => o.CreateOpFragment(TemplateFragmentTests.FragInnerOk)).Returns(this.mockFrag.Object);
            this.mockFragFact.Setup(o => o.CreateOpFragment("[[<fail ")).Throws<DependencyMissingException>();

            this.mockFragConst
                .Setup(o => o.Parse(It.IsAny<IContext>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns((IContext c, string s, int i, int l) => i + l);
            this.mockFrag
                .Setup(o => o.Parse(It.IsAny<IContext>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns((IContext c, string s, int i, int l) => i + (TemplateFragmentTests.FragInnerOk.Length * 2));

            this.mockFragConst
                .Setup(o => o.Render(It.IsAny<IContext>(), It.IsAny<StringBuilder>(), It.IsAny<object>()))
                .Returns(
                    (IContext c, StringBuilder sb, object d) =>
                        (sb ?? new StringBuilder()).Append(TemplateFragmentTests.FragConstRender));
            this.mockFrag
                .Setup(o => o.Render(It.IsAny<IContext>(), It.IsAny<StringBuilder>(), It.IsAny<object>()))
                .Returns(
                    (IContext c, StringBuilder sb, object d) =>
                        (sb ?? new StringBuilder()).Append(TemplateFragmentTests.FragRender));

            this.mockFragConst.Setup(o => o.GetVariables()).Returns(ListHelper.EmptyList<string>());
            this.mockFrag.Setup(o => o.GetVariables()).Returns(new[] { TemplateFragmentTests.FragVar });

            this.mockFragConst.Setup(o => o.ToString()).Returns(TemplateFragmentTests.FragConstToString);
            this.mockFrag.Setup(o => o.ToString()).Returns(TemplateFragmentTests.FragToString);

            this.ctx = this.mockCtx.Object;
        }

        [TestMethod]
        [DataRow("[[<ok  ok>]]IAm AConstant[[<ok  ok>]]", "[TEMPLATE]<<<<FTS>>>[[[FCRTS]]]<<<FTS>>>>", 1, 2)]
        [DataRow("IAm AConstant[[<ok  ok>]]IAmAlso AConstant", "[TEMPLATE]<[[[FCRTS]]]<<<FTS>>>[[[FCRTS]]]>", 2, 1)]
        [DataRow("[[<ok  ok>]]IAm AConstant", "[TEMPLATE]<<<<FTS>>>[[[FCRTS]]]>", 1, 1)]
        [DataRow("IAmAConstant[[<ok  ok>]]", "[TEMPLATE]<[[[FCRTS]]]<<<FTS>>>>", 1, 1)]
        [DataRow("[[<ok  ok>]]", "[TEMPLATE]<<<<FTS>>>>", 0, 1)]
        [DataRow("IAmAConstant", "[TEMPLATE]<[[[FCRTS]]]>", 1, 0)]
        public void ParsePopulatesPropertiesWithExpectedValues(
            string input,
            string expected,
            int countConst,
            int countFrag)
        {
            TemplateFragment frag = new TemplateFragment(this.mockFragFact.Object);

            int result;

            // test
            result = frag.Parse(this.ctx, input, 0, input.Length);

            // verify
            Assert.AreEqual(input.Length, result);
            Assert.AreEqual(expected, frag.ToString());

            this.mockFragConst.Verify(o => o.Parse(this.ctx, input, It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(countConst));
            this.mockFrag.Verify(o => o.Parse(this.ctx, input, It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(countFrag));
        }

        [TestMethod]
        [ExpectedException(typeof(TemplateParseException))]
        [DataRow("[[<fail fail>]]")] // unknown op
        [DataRow("[[<failfail>]]")] // unknown op
        public void ParseThrowsIfBadOrMissingProperties(string input)
        {
            TemplateFragment frag = new TemplateFragment(this.mockFragFact.Object);
            frag.Parse(this.ctx, input, 0, input.Length);
        }

        [TestMethod]
        [DataRow("[[<ok  ok>]]IAm AConstant[[<ok  ok>]]", "[[[FRAGVAR]]],[[[FRAGVAR]]]")]
        [DataRow("IAmAConstant[[<ok  ok>]]IAm AlsoAConstant", "[[[FRAGVAR]]]")]
        [DataRow("[[<ok  ok>]]IAm AConstant", "[[[FRAGVAR]]]")]
        [DataRow("IAm AConstant[[<ok  ok>]]", "[[[FRAGVAR]]]")]
        [DataRow("[[<ok  ok>]]", "[[[FRAGVAR]]]")]
        [DataRow("IAm AConstant", "")]
        public void GetVariablesReturnsListOfReferencedVariables(
            string input,
            string expected)
        {
            ICollection<string> vars;
            TemplateFragment frag = new TemplateFragment(this.mockFragFact.Object);

            frag.Parse(this.ctx, input, 0, input.Length);

            // test
            vars = frag.GetVariables();

            // test
            Assert.AreEqual(expected, string.Join(",", vars));
        }

        [TestMethod]
        [DataRow("[[<ok  ok>]]IAmAConstant[[<ok  ok>]]", "<<<FR>>>[[[FCR]]]<<<FR>>>", 1, 2)]
        [DataRow("IAmAConstant[[<ok  ok>]]IAm AlsoAConstant", "[[[FCR]]]<<<FR>>>[[[FCR]]]", 2, 1)]
        [DataRow("[[<ok  ok>]]IAm AConstant", "<<<FR>>>[[[FCR]]]", 1, 1)]
        [DataRow("IAm AConstant[[<ok  ok>]]", "[[[FCR]]]<<<FR>>>", 1, 1)]
        [DataRow("[[<ok  ok>]]", "<<<FR>>>", 0, 1)]
        [DataRow("IAm AConstant", "[[[FCR]]]", 1, 0)]
        public void RenderCorrectlyRendersValue(
            string input,
            string expected,
            int countConst,
            int countFrag)
        {
            TemplateFragment frag = new TemplateFragment(this.mockFragFact.Object);
            object data = new object();
            string result;

            frag.Parse(this.ctx, input, 0, input.Length);

            // test
            result = frag.Render(this.ctx, null, data).ToString();

            // test
            Assert.AreEqual(expected, result);
            this.mockFragConst.Verify(o => o.Render(this.ctx, It.IsAny<StringBuilder>(), data), Times.Exactly(countConst));
            this.mockFrag.Verify(o => o.Render(this.ctx, It.IsAny<StringBuilder>(), data), Times.Exactly(countFrag));
        }
    }
}
