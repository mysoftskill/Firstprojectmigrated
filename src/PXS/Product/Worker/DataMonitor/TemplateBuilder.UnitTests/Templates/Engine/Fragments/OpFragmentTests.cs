// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.TemplateBuilder.UnitTests.Engine
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.TemplateBuilder;
    using Microsoft.PrivacyServices.Common.TemplateBuilder.Fragments;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class OpFragmentTests
    {
        private readonly Mock<IContext> mockCtx = new Mock<IContext>();

        private IContext ctx;

        private class TestFragment : OpFragment
        {
            public override FragmentType Type => FragmentType.Invalid;

            public PropResponse NextResponse { get; set; } = PropResponse.Ok;
            public IEnumerable<string> MissingProps { get; set; } = Enumerable.Empty<string>();
            public IDictionary<string, string> Props { get; } = new Dictionary<string, string>();

            protected override string Prefix => FragmentConsts.OpPrefix + "tf ";
            protected override string Suffix => " tf" + FragmentConsts.OpSuffix;

            public override int Parse(IContext context, string source, int idxStart, int length)
            {
                return this.ParseTag("ctx", source, idxStart, length).IdxNext;
            }

            public override ICollection<string> GetVariables() => ListHelper.EmptyList<string>();

            public override StringBuilder Render(IContext context, StringBuilder target, object model)
            {
                return target ?? new StringBuilder();
            }

            protected override PropResponse SetPropery(string name, string value)
            {
                this.Props.Add(name, value);
                return this.NextResponse;
            }

            protected override IEnumerable<string> VerifyRequiredProperties() => this.MissingProps;
        }

        [TestInitialize]
        public void Init()
        {
            this.ctx = this.mockCtx.Object;
        }

        [TestMethod]
        [ExpectedException(typeof(TemplateParseException))]
        [DataRow("[[<te ", null, null)]
        [DataRow("[[<t", null, null)]
        [DataRow("Hello", null, null)]
        [DataRow("[[<tf ", 1, 4)]
        [DataRow("[[<tf tf>]]", null, null)]
        [DataRow("[[<tf  tf>]]", null, 8)]
        [DataRow("[[<tf  te>]]", null, null)]
        public void ParseTagThrowsIfInputStringHasBadPrefixOrSuffixOrLength(
            string input,
            int? idx,
            int? len)
        {
            int idxActual = idx ?? 0;
            int lenActual = len ?? input.Length;
            new TestFragment().Parse(this.ctx, input, idxActual, lenActual);
        }

        [TestMethod]
        [ExpectedException(typeof(TemplateParseException))]
        [DataRow(PropResponse.BadFormat)]
        [DataRow(PropResponse.NotSupported)]
        [DataRow(PropResponse.Duplicate)]
        [DataRow(PropResponse.Invalid)]
        public void ParseTagThrowsIfSetPropertyDoesNotReturnOk(PropResponse response)
        {
            const string Input = "[[<tf i:0 tf>]]";

            TestFragment frag = new TestFragment { NextResponse = response };

            frag.Parse(this.ctx, Input, 0, Input.Length);
        }

        [TestMethod]
        [DataRow("[[<tf toby:dog tf>]]", "toby", "dog")]
        [DataRow("[[<tf  toby:dog tf>]]", "toby", "dog")]
        [DataRow("[[<tf toby :dog tf>]]", "toby", "dog")]
        [DataRow("[[<tf  toby :dog tf>]]", "toby", "dog")]
        [DataRow("[[<tf toby: dog tf>]]", "toby", "dog")]
        [DataRow("[[<tf toby:dog  tf>]]", "toby", "dog")]
        [DataRow("[[<tf toby: dog  tf>]]", "toby", "dog")]
        [DataRow("[[<tf toby:\"dog\" tf>]]", "toby", "dog")]
        [DataRow("[[<tf toby: \"dog\" tf>]]", "toby", "dog")]
        [DataRow("[[<tf toby:\"dog\"  tf>]]", "toby", "dog")]
        [DataRow("[[<tf toby: \"dog\"  tf>]]", "toby", "dog")]
        [DataRow("[[<tf toby:\'dog\' tf>]]", "toby", "dog")]
        [DataRow("[[<tf toby: \'dog\' tf>]]", "toby", "dog")]
        [DataRow("[[<tf toby:\'dog\'  tf>]]", "toby", "dog")]
        [DataRow("[[<tf toby: \'dog\'  tf>]]", "toby", "dog")]
        [DataRow("[[<tf toby: \' dog \'  tf>]]", "toby", " dog ")]
        [DataRow("[[<tf toby: \" dog \"  tf>]]", "toby", " dog ")]
        [DataRow("[[<tf toby: \' \\\'dog\\\' \' tf>]]", "toby", " 'dog\' ")]
        [DataRow("[[<tf toby: \" \\\"dog\\\" \" tf>]]", "toby", " \"dog\" ")]
        [DataRow("[[<tf toby: \' \"dog\" \' tf>]]", "toby", " \"dog\" ")]
        [DataRow("[[<tf toby: \" \'dog\' \" tf>]]", "toby", " 'dog' ")]
        public void ParseTagFindsSinglePropertyValuesAndSetsThemOnTestObject(
            string input,
            string expectedName,
            string expectedValue)
        {
            TestFragment frag = new TestFragment();
            int result;

            result = frag.Parse(this.ctx, input, 0, input.Length);

            Assert.AreEqual(input.Length, result);
            Assert.AreEqual(1, frag.Props.Count);
            Assert.AreEqual(expectedName, frag.Props.Keys.First());
            Assert.AreEqual(expectedValue, frag.Props.Values.First());
        }

        [TestMethod]
        [ExpectedException(typeof(TemplateParseException))]
        [DataRow("[[<tf :dog tf>]]")] // name empty
        [DataRow("[[<tf toby tf>]]")] // missing separator + value
        [DataRow("[[<tf toby  tf>]]")] // missing separator + value
        [DataRow("[[<tf toby: tf>]]")] // missing value
        [DataRow("[[<tf toby:  tf>]]")] // missing value
        [DataRow("[[<tf toby:\" tf>]]")] // value delimeter open but missing value and close 
        [DataRow("[[<tf toby:\' tf>]]")] // value delimeter open but missing value and close
        [DataRow("[[<tf toby:\"dog tf>]]")] // value delimeter open but missing close
        [DataRow("[[<tf toby:\'dog tf>]]")] // value delimeter open but missing close
        [DataRow("[[<tf toby:\"\" tf>]]")] // value empty
        [DataRow("[[<tf toby:\'\' tf>]]")] // value empty
        [DataRow("[[<tf toby:\"dog\\\" tf>]]")] // last delimeter is escaped
        [DataRow("[[<tf toby:\'dog\\\' tf>]]")] // last delimeter is escaped
        public void ParseTagThrowsWhenBadCharacterSequencesFound(string input)
        {
            new TestFragment().Parse(this.ctx, input, 0, input.Length);
        }

        [TestMethod]
        [DataRow("[[<tf toby:tobers bailey:\"bails\" lulu:'loops' tf>]]", null, null)]
        [DataRow("[[<tf toby:tobers   bailey:\"bails\"   lulu:'loops' tf>]]", null, null)]
        [DataRow("[[<tf toby:tobers  bailey:\"bails\"lulu:'loops' tf>]]", null, null)]
        [DataRow("[[<tf toby:tobers  bailey:'bails'lulu:\"loops\" tf>]]", null, null)]
        [DataRow("1234567890[[<tf toby:tobers bailey:\"bails\" lulu:'loops' tf>]][[<tf tobers:toby tf>]]1234567890", 10, 33)]
        public void ParseTagFindsMultiPropertyValues(
            string input,
            int? idx,
            int? lenOffset)
        {
            TestFragment frag = new TestFragment();
            int idxActual = idx ?? 0;
            int lenActual = input.Length - (idxActual + (lenOffset ?? 0));
            int result;

            result = frag.Parse(this.ctx, input, idxActual, lenActual);

            Assert.AreEqual(idxActual + lenActual, result);
            Assert.AreEqual(3, frag.Props.Count);
            Assert.IsTrue(frag.Props.ContainsKey("toby"));
            Assert.IsTrue(frag.Props.ContainsKey("bailey"));
            Assert.IsTrue(frag.Props.ContainsKey("lulu"));
            Assert.AreEqual("tobers", frag.Props["toby"]);
            Assert.AreEqual("bails", frag.Props["bailey"]);
            Assert.AreEqual("loops", frag.Props["lulu"]);
        }

        [TestMethod]
        [ExpectedException(typeof(TemplateParseException))]
        public void ParseTagThrowsIfPropertiesMissing()
        {
            const string Input = "[[<tf toby:dog tf>]]";

            TestFragment frag = new TestFragment();
            frag.MissingProps = new List<string> { "bailey", "lulu" };

            frag.Parse(this.ctx, Input, 0, Input.Length);
        }
    }
}
