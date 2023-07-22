// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.TemplateBuilder.UnitTests.Engine
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;

    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.PrivacyServices.Common.TemplateBuilder;
    using Microsoft.PrivacyServices.Common.TemplateBuilder.Fragments;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.UnitTests.TestUtility;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class ForeachFragmentTests
    {
        private const string FragToString = "[[[FRAGTOSTRING]]]";
        private const string FragRender = "[[[FR]]]";
        private const string FragVar = "[[[FRAGVAR]]]";

        private readonly Mock<IModelManipulator> mockModel = new Mock<IModelManipulator>();
        private readonly Mock<IFragmentFactory> mockFragFact = new Mock<IFragmentFactory>();
        private readonly Mock<IFragment> mockFrag = new Mock<IFragment>();
        private readonly Mock<IContext> mockCtx = new Mock<IContext>();

        private IContext ctx;

        [TestInitialize]
        public void Init()
        {
            this.mockFragFact.Setup(o => o.CreateTemplateFragment()).Returns(this.mockFrag.Object);

            this.mockFrag
                .Setup(o => o.Parse(It.IsAny<IContext>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns((IContext c, string s, int i, int l) => i + l);

            this.mockFrag
                .Setup(o => o.Render(It.IsAny<IContext>(), It.IsAny<StringBuilder>(), It.IsAny<object>()))
                .Returns(
                    (IContext c, StringBuilder sb, object d) =>
                    {
                        sb = sb ?? new StringBuilder();
                        sb.Append(ForeachFragmentTests.FragRender);
                        return sb;
                    });

            this.mockFrag
                .Setup(o => o.GetVariables())
                .Returns(new[] { ForeachFragmentTests.FragVar });

            this.mockFrag.Setup(o => o.ToString()).Returns(ForeachFragmentTests.FragToString);

            this.ctx = this.mockCtx.Object;
        }

        [TestMethod]
        [DataRow("[[<foreach sel:data sep:' ' foreach>]] [[<foreachend>]]", "data", " ", 38, 1)]
        [DataRow("[[<foreach sel:data sep:'<>' foreach>]] [[<foreachend>]]", "data", "<>", 39, 1)]
        [DataRow("[[<foreach sel:data foreach>]] [[<foreachend>]]", "data", null, 30, 1)]
        [DataRow("[[<foreach sel:data foreach>]] [[<foreachend>]]", "data", null, 30, 1)]
        [DataRow("[[<foreach selector:data foreach>]] [[<foreachend>]]", "data", null, 35, 1)]
        [DataRow("[[<foreach selector:data foreach>]][[<foreachend>]]", "data", null, 35, 0)]
        [DataRow("[[<foreach sel:. foreach>]] [[<foreachend>]]", ".", null, 27, 1)]
        [DataRow("[[<foreach sel:. foreach>]][[<foreachend>]]", ".", null, 27, 0)]
        [DataRow(
            "[[<foreach sel:. foreach>]][[<foreach sel:. foreach>]][[<foreachend>]][[<foreachend>]]", 
            ".", 
            null,
            27, 
            43)]
        public void ParsePopulatesPropertiesWithExpectedValues(
            string input,
            string var,
            string sep,
            int idxStart,
            int length)
        {
            ForeachFragment frag = new ForeachFragment(this.mockModel.Object, this.mockFragFact.Object);

            string expected;
            int result;

            expected = sep == null ? 
                $"[FOREACH sel:'{var}' sep:null]<{ForeachFragmentTests.FragToString}>" :
                $"[FOREACH sel:'{var}' sep:'{sep}']<{ForeachFragmentTests.FragToString}>";

            // test
            result = frag.Parse(this.ctx, input, 0, input.Length);

            // verify
            Assert.AreEqual(input.Length, result);
            Assert.AreEqual(expected, frag.ToString());
            this.mockFrag.Verify(o => o.Parse(this.ctx, input, idxStart, length));
        }

        [TestMethod]
        [ExpectedException(typeof(TemplateParseException))]
        [DataRow("[[<foreach sel:data1 v:data2 foreach>]] [[<foreachend>]]")] // duplicate var
        [DataRow("[[<foreach foreach>]] [[<foreachend>]]")] // no properties
        [DataRow("[[<foreach toby:dog foreach>]] [[<foreachend>]]")] // unknown property
        [DataRow("[[<foreach toby:dog foreach>]]")] // missing foreachend
        public void ParseThrowsIfBadOrMissingProperties(string input)
        {
            ForeachFragment frag = new ForeachFragment(this.mockModel.Object, this.mockFragFact.Object);
            frag.Parse(this.ctx, input, 0, input.Length);
        }

        [TestMethod]
        [DataRow("[[<foreach sel:data2 foreach>]] [[<foreachend>]]", "data2")]
        [DataRow("[[<foreach sel:data1 foreach>]] [[<foreachend>]]", "data1")]
        public void GetVariablesReturnsListOfReferencedVariables(
            string input,
            string var)
        {
            ICollection<string> vars;
            ForeachFragment frag = new ForeachFragment(this.mockModel.Object, this.mockFragFact.Object);

            frag.Parse(this.ctx, input, 0, input.Length);

            // test
            vars = frag.GetVariables();

            // test
            Assert.AreEqual(2, vars.Count);
            Assert.IsTrue(vars.Contains(var));
            Assert.IsTrue(vars.Contains(var + "[]." + ForeachFragmentTests.FragVar));
            this.mockFrag.Verify(o => o.GetVariables(), Times.Once);
        }

        // "1" is specified multiple times below because we can then use it in the Moq verify call as all the data values
        //  are the same
        private static IEnumerable<object[]> RenderArgs => 
            new List<object[]>
            {
                new object[] 
                {
                    "[[<foreach sel:$ foreach>]] [[<foreachend>]]",
                    new[] { 1 },
                    ForeachFragmentTests.FragRender,
                    "$",
                    1,
                },
                new object[]
                {
                    "[[<foreach sel:$ foreach>]] [[<foreachend>]]",
                    new[] { 1, 1 },
                    ForeachFragmentTests.FragRender + ForeachFragmentTests.FragRender,
                    "$",
                    2,
                },
                new object[]
                {
                    "[[<foreach sel:$ sep:SEP foreach>]] [[<foreachend>]]",
                    new[] { 1 },
                    ForeachFragmentTests.FragRender,
                    "$",
                    1,
                },
                new object[]
                {
                    "[[<foreach sel:$ sep:SEP foreach>]] [[<foreachend>]]",
                    new[] { 1, 1 },
                    ForeachFragmentTests.FragRender + "SEP" + ForeachFragmentTests.FragRender,
                    "$",
                    2,
                },
                new object[]
                {
                    "[[<foreach sel:$ foreach>]] [[<foreachend>]]",
                    new[] { 1, 1, 1 },
                    ForeachFragmentTests.FragRender + ForeachFragmentTests.FragRender + ForeachFragmentTests.FragRender,
                    "$",
                    3,
                },
                new object[]
                {
                    "[[<foreach sel:data.data2 foreach>]] [[<foreachend>]]",
                    new[] { 1, 1 },
                    ForeachFragmentTests.FragRender + ForeachFragmentTests.FragRender,
                    "data.data2",
                    2,
                },
            };

        [TestMethod]
        [DynamicData(nameof(ForeachFragmentTests.RenderArgs))]
        public void RenderCorrectlyRendersValue(
            string input,
            object data,
            string expected,
            string expectedSelector,
            int count)
        {
            ForeachFragment frag = new ForeachFragment(this.mockModel.Object, this.mockFragFact.Object);
            string result;
            object outVal;

            this.mockModel
                .Setup(o => o.TryExtractValue(It.IsAny<IContext>(), It.IsAny<object>(), It.IsAny<string>(), out outVal))
                .OutCallback((IContext c, object p, string k, out object r) => r = data)
                .Returns(true);

            this.mockModel.Setup(o => o.ToEnumerable(It.IsAny<object>())).Returns(data as IEnumerable);

            frag.Parse(this.ctx, input, 0, input.Length);

            // test
            result = frag.Render(this.ctx, null, data).ToString();

            // test
            Assert.AreEqual(expected, result);

            this.mockModel.Verify(o => o.TryExtractValue(this.ctx, data, expectedSelector, out outVal), Times.Once);

            this.mockFrag.Verify(o => o.Render(this.ctx, It.IsAny<StringBuilder>(), 1), Times.Exactly(count));
        }

        [TestMethod]
        public void RenderTreatsNotFoundVariableAsEmptyCollection()
        {
            const string Input = "[[<foreach sel:$ foreach>]] [[<foreachend>]]";

            ForeachFragment frag = new ForeachFragment(this.mockModel.Object, this.mockFragFact.Object);

            string result;

            frag.Parse(this.ctx, Input, 0, Input.Length);

            // test
            result = frag.Render(this.ctx, null, new object()).ToString();

            // verify
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Length);
        }
    }
}
