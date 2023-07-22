// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.TemplateBuilder.UnitTests.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.PrivacyServices.Common.TemplateBuilder;
    using Microsoft.PrivacyServices.Common.TemplateBuilder.Fragments;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.UnitTests.TestUtility;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class VarFragmentTests
    {
        private readonly Mock<IModelManipulator> mockModel = new Mock<IModelManipulator>();
        private readonly Mock<IContext> mockCtx = new Mock<IContext>();

        private IContext ctx;

        [TestInitialize]
        public void Init()
        {
            this.ctx = this.mockCtx.Object;
        }

        [TestMethod]
        [DataRow("[[<var sel:data f:n0 d:0 var>]]", "data", "n0", "0")]
        [DataRow("[[<var d:0 f:n0 sel:data var>]]", "data", "n0", "0")]
        [DataRow("[[<var sel:data fmt:n0 def:0 var>]]", "data", "n0", "0")]
        [DataRow("[[<var selector:data format:n0 default:0 var>]]", "data", "n0", "0")]
        [DataRow("[[<var selector:data var>]]", "data", null, null)]
        [DataRow("[[<var selector:data format:n0 var>]]", "data", "n0", null)]
        [DataRow("[[<var selector:data default:0 var>]]", "data", null, "0")]
        [DataRow("[[<var sel:data.nested.stuff fmt:n0 def:0 var>]]", "data.nested.stuff", "n0", "0")]
        [DataRow("[[<var sel:. fmt:n0 def:0 var>]]", ".", "n0", "0")]
        public void ParsePopulatesPropertiesWithExpectedValues(
            string input,
            string var,
            string fmt,
            string defVal)
        {
            VarFragment frag = new VarFragment(this.mockModel.Object);

            string expected;
            int result;

            expected = "[VAR sel:'{0}' format:{1} default:{2}]".FormatInvariant(
                var,
                fmt != null ? ("'" + fmt + "'") : "null",
                defVal != null ? ("'" + defVal + "'") : "null");

            // test
            result = frag.Parse(this.ctx, input, 0, input.Length);

            // verify
            Assert.AreEqual(input.Length, result);
            Assert.AreEqual(expected, frag.ToString());
        }

        [TestMethod]
        [ExpectedException(typeof(TemplateParseException))]
        [DataRow("[[<var s:data1 v:data2 var>]]")] // duplicate var
        [DataRow("[[<var s:data1 f:f1 f:f2 var>]]")] // duplicate format
        [DataRow("[[<var s:data1 d:0 d:1 var>]]")] // duplicate default
        [DataRow("[[<var var>]]")] // no properties
        [DataRow("[[<var fmt:n0 def:0 var>]]")] // missing var but has other properties
        [DataRow("[[<var toby:dog var>]]")] // unknown property
        public void ParseThrowsIfBadOrMissingProperties(string input)
        {
            VarFragment frag = new VarFragment(this.mockModel.Object);
            frag.Parse(this.ctx, input, 0, input.Length);
        }

        [TestMethod]
        [DataRow("[[<var s:data2 var>]]", "data2")]
        [DataRow("[[<var s:data1 var>]]", "data1")]
        public void GetVariablesReturnsListOfReferencedVariables(
            string input,
            string var)
        {
            ICollection<string> vars;
            VarFragment frag = new VarFragment(this.mockModel.Object);

            frag.Parse(this.ctx, input, 0, input.Length);

            // test
            vars = frag.GetVariables();

            // test
            Assert.AreEqual(1, vars.Count);
            Assert.AreEqual(var, vars.First());
        }
        
        private static IEnumerable<object[]> RenderArgs => 
            new List<object[]>
            {
                new object[] { "[[<var s:$ var>]]", 0, "0", "$" },
                new object[] { "[[<var s:$ var>]]", "data", "data", "$" },
                new object[] { "[[<var s:$ f:n0 var>]]", 10000, "10,000", "$" },
                new object[] { "[[<var s:$ f:n0 d:tobers var>]]", null, "tobers", "$" },
                new object[] 
                {
                    "[[<var s:$ f:yyyyMMddHHmmss var>]]", DateTimeOffset.Parse("2018-01-02 03:04:05"), "20180102030405", "$"
                },
                new object[] { "[[<var s:data f:yyyyMMddHHmmss var>]]", "toby", "toby", "data" },
                new object[] { "[[<var s:data.data2 f:yyyyMMddHHmmss var>]]", "ybot", "ybot", "data.data2" },
            };

        [TestMethod]
        [DynamicData(nameof(VarFragmentTests.RenderArgs))]
        public void RenderCorrectlyRendersValue(
            string input,
            object data,
            string expected,
            string expectedSelector)
        {
            VarFragment frag = new VarFragment(this.mockModel.Object);
            string result;
            object outVal;

            this.mockModel
                .Setup(o => o.TryExtractValue(It.IsAny<IContext>(), It.IsAny<object>(), It.IsAny<string>(), out outVal))
                .OutCallback((IContext c, object p, string k, out object r) => r = data)
                .Returns(data != null);

            frag.Parse(this.ctx, input, 0, input.Length);

            // test
            result = frag.Render(this.ctx, null, data).ToString();

            // test
            Assert.AreEqual(expected, result);
        }

    }
}
