// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.UnitTests.Utility.Model
{
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.PrivacyServices.Common.Exceptions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Newtonsoft.Json.Linq;

    [TestClass]
    public class JsonUtilsTest
    {
        private readonly Mock<IContext> mockCtx = new Mock<IContext>();

        [TestMethod]
        public void ExtractCollectionReturnsArrayOfMatchingElements()
        {
            JObject obj =
                new JObject(
                    new JProperty(
                        "labs",
                        new JArray(
                            new JObject(
                                new JProperty("name", "toby"),
                                new JProperty("color", "yellow")),
                            new JObject(
                                new JProperty("name", "bailey"),
                                new JProperty("color", "black")))));

            JArray result;

            // test
            result = JsonUtils.ExtractCollection(obj, "$..name");

            // vaildate
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("toby", (string)result[0]);
            Assert.AreEqual("bailey", (string)result[1]);
        }

        [TestMethod]
        [DataRow("hello", true)]
        [DataRow("hello.there", false)]
        [DataRow(@"""hello""", false)]
        [DataRow(@"""hello"".there", false)]
        [DataRow(@"hello.""there""", false)]
        public void IsSingleElementNonQuotedPathReturnsExpectedValue(
            string path,
            bool expected)
        {
            Assert.AreEqual(expected, JsonUtils.IsSingleElementNonQuotedPath(path));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPathException))]
        [DataRow(@"""hello.there.you", "path contains unterminated quote")]
        [DataRow(@"hello.""there.you", "path contains unterminated quote")]
        [DataRow(@"""hello""there.you", "a path separator or end of path is required after a quoted path element")]
        [DataRow(@"""hello""""there"".you", "a path separator or end of path is required after a quoted path element")]
        [DataRow("hello there.you", "unquoted path elements may not contain whitespace")]
        [DataRow("hello.there.x z.you", "unquoted path elements may not contain whitespace")]
        [DataRow("hello.there. .you", "path elements may not be zero-length (or empty without using a quoted path element)")]
        [DataRow("hello.there..you", "path elements may not be zero-length (or empty without using a quoted path element)")]
        [DataRow(".hello.there.you", "path elements may not be zero-length (or empty without using a quoted path element)")]
        [DataRow("hello.there.you.", "path elements may not be zero-length")]
        [DataRow("", "at least one path element must be present")]
        [DataRow(@"""""", "path elements may not be zero-length")]
        public void ParsePathThrowsErrorsWhenAppropriate(
            string path,
            string expectedMessageFragment)
        {
            try
            {
                JsonUtils.ParsePath(path);
            }
            catch (InvalidPathException e)
            {
                Assert.IsTrue(e.Message.Contains(expectedMessageFragment), "Actual: " + e.Message);
                throw;
            }
        }

        // "1" is specified multiple times below because we can then use it in the Moq verify call as all the data values
        //  are the same
        private static IEnumerable<object[]> ParsePathReturnsTheCorrectResultArgs =>
            new List<object[]>
            {
                new object[] { "hello", new[] { "hello" } },
                new object[] { "hello.there.you", new[] { "hello", "there", "you" } },
                new object[] { "hello . there . you", new[] { "hello", "there", "you" } },
                new object[] { @"""hello"".""there"".""you""", new[] { "hello", "there", "you" } },
                new object[] { @"""hello"" . ""there"" . ""you""", new[] { "hello", "there", "you" } },
                new object[] { @"""hello"".there.""you""", new[] { "hello", "there", "you" } },
                new object[] { @"""hello"" . there . ""you""", new[] { "hello", "there", "you" } },
                new object[] { @"hello.""there"".you", new[] { "hello", "there", "you" } },
                new object[] { @"hello . ""there"" . you", new[] { "hello", "there", "you" } },
                new object[] { @"$.hello.there.you", new[] { "hello", "there", "you" } },
            };

        [TestMethod]
        [DynamicData(nameof(JsonUtilsTest.ParsePathReturnsTheCorrectResultArgs))]
        public void ParsePathReturnsTheCorrectResult(
            string path,
            string[] expectedElements)
        {
            IList<string> result = JsonUtils.ParsePath(path);

            Assert.AreEqual(expectedElements.Length, result.Count);
            for (int i = 0; i < expectedElements.Length; ++i)
            {
                Assert.AreEqual(expectedElements[i], result[i], i + "th path element");
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPathException))]
        public void GetObjAndLeafThrowsIfIntermediateNodeNotNull()
        {
            JObject root = new JObject(new JProperty("invalid", 1));

            try
            {
                JsonUtils.GetContainerAndLeafPropName(this.mockCtx.Object, root, new[] { "invalid", "nope" });
            }
            catch (InvalidPathException e)
            {
                Assert.IsTrue(e.Message.Contains("Intermediate path elements must be objects"), "Actual: " + e.Message);
                throw;
            }
        }

        // "1" is specified multiple times below because we can then use it in the Moq verify call as all the data values
        //  are the same
        private static IEnumerable<object[]> GetObjAndLeafReturnsCorrectResultArgs =>
            new List<object[]>
            {
                new object[] { new[] { "f", "leaf" }, "f" },
                new object[] { new[] { "f", "new", "leaf" }, "f.new" },
                new object[] { new[] { "f", "s", "t", "leaf" }, "f.s.t" },
                new object[] { new[] { "f", "s", "t", "new", "new2", "new3", "leaf" }, "f.s.t.new.new2.new3" },
            };

        [TestMethod]
        [DynamicData(nameof(JsonUtilsTest.GetObjAndLeafReturnsCorrectResultArgs))]
        public void GetObjAndLeafReturnsExpectedValue(
            IList<string> pathList,
            string containerPath)
        {
            JObject root = 
                new JObject(
                    new JProperty(
                        "f",
                        new JObject(
                            new JProperty(
                                "s",
                                new JObject(
                                    new JProperty(
                                        "t",
                                        new JObject()))))));

            JObject container;
            string leaf;

            JObject intermediate;
            int i;

            (container, leaf) = JsonUtils.GetContainerAndLeafPropName(this.mockCtx.Object, root, pathList);

            Assert.IsNotNull(container, nameof(container));
            Assert.IsNotNull(leaf, nameof(leaf));
            Assert.AreEqual(pathList[pathList.Count - 1], leaf);
            Assert.AreEqual(containerPath, container.Path);

            for (i = 0, intermediate = root; i < pathList.Count - 1; ++i)
            {
                JProperty prop = intermediate.Property(pathList[i]);
                string errCtx = "path: " + intermediate.Path + ":" + pathList[i];

                Assert.IsNotNull(prop, errCtx);
                Assert.IsInstanceOfType(prop.Value, typeof(JObject), errCtx);

                intermediate = prop.Value as JObject;
                Assert.IsNotNull(intermediate, errCtx);
            }
        }
    }
}
