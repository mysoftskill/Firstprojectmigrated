// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Data.Edm;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ODataFilterTests
    {
        // TODO: This is not yet working.
        //[TestMethod]
        //public void ContainsAny()
        //{
        //    var filter = new ODataFilter(
        //        TestThing.EdmProperties,
        //        "containsAny(sources, 'foo')",
        //        TestThing.LookupProperty);

        //    Assert.IsTrue(filter.Matches(new TestThing(DateTimeOffset.UtcNow, "Foo", new[] { "foo" })));
        //    Assert.IsTrue(filter.Matches(new TestThing(DateTimeOffset.UtcNow, "Foo", new[] { "bar", "foo" })));
        //    Assert.IsTrue(filter.Matches(new TestThing(DateTimeOffset.UtcNow, "Foo", new[] { "foo", "bar" })));
        //    Assert.IsFalse(filter.Matches(new TestThing(DateTimeOffset.UtcNow, "Foo", null)));
        //    Assert.IsFalse(filter.Matches(new TestThing(DateTimeOffset.UtcNow, "Foo", new string[] { })));
        //    Assert.IsFalse(filter.Matches(new TestThing(DateTimeOffset.UtcNow, "Foo", new[] { "bar" })));
        //    Assert.IsFalse(filter.Matches(new TestThing(DateTimeOffset.UtcNow, "Foo", new[] { "bar", "baz" })));
        //}

        [TestMethod]
        public void BetweenDateTime()
        {
            var filter = new ODataFilter(
                TestThing.EdmProperties,
                "date ge datetimeoffset'2016-03-31T01:02:03.4444444' and date le datetimeoffset'2016-04-01T23:22:21.555'",
                TestThing.LookupProperty);

            Assert.IsTrue(filter.Matches(new TestThing(DateTimeOffset.Parse("2016-03-31T01:02:03.4444444"), "Foo")));
            Assert.IsTrue(filter.Matches(new TestThing(DateTimeOffset.Parse("2016-04-01T23:22:21.555"), "Foo")));
            Assert.IsTrue(filter.Matches(new TestThing(DateTimeOffset.Parse("2016-03-31T01:02:03.4444445"), "Foo")));
            Assert.IsFalse(filter.Matches(new TestThing(DateTimeOffset.Parse("2016-03-31T01:02:03.4444443"), "Foo")));
        }

        [TestMethod]
        public void Complicated()
        {
            var filter = new ODataFilter(
                TestThing.EdmProperties,
                "(date ge datetimeoffset'2016-04-02' or substringof(tolower(text), 'foo')) and length(trim(text)) ne 6",
                TestThing.LookupProperty);

            // Must be later than 2016-04-02 OR have 'foo' of any case anywhere in the string
            //    AND
            // The text length cannot be 6 after being trimmed

            Assert.IsTrue(filter.Matches(new TestThing(DateTimeOffset.Parse("2016-04-01"), "FOO")));
            Assert.IsTrue(filter.Matches(new TestThing(DateTimeOffset.Parse("2016-04-01"), "FooBarBaz")));
            Assert.IsTrue(filter.Matches(new TestThing(DateTimeOffset.Parse("2016-04-01"), "BarfOOd")));
            Assert.IsFalse(filter.Matches(new TestThing(DateTimeOffset.Parse("2016-04-01"), "  foobar  ")));

            Assert.IsTrue(filter.Matches(new TestThing(DateTimeOffset.Parse("2016-04-03"), "...")));
            Assert.IsTrue(filter.Matches(new TestThing(DateTimeOffset.Parse("2016-04-03"), "hah!")));
            Assert.IsTrue(filter.Matches(new TestThing(DateTimeOffset.Parse("2016-04-03"), "well this is a string that isn't 6 long")));
            Assert.IsFalse(filter.Matches(new TestThing(DateTimeOffset.Parse("2016-04-03"), "  foobar  ")));
        }

        [TestMethod]
        public void EqualityDateTime()
        {
            var filter = new ODataFilter(
                TestThing.EdmProperties,
                "date eq datetimeoffset'2016-04-01'",
                TestThing.LookupProperty);

            Assert.IsTrue(filter.Matches(new TestThing(DateTimeOffset.Parse("2016-04-01"), "Foo")));
            Assert.IsFalse(filter.Matches(new TestThing(DateTimeOffset.Parse("2016-04-02"), "Foo")));
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void FilterOfCompleteGarbage()
        {
            var filter = new ODataFilter(
                TestThing.EdmProperties,
                "just some random text here",
                TestThing.LookupProperty);
        }

        [TestMethod]
        public void GreaterThenEqualDateTime()
        {
            var filter = new ODataFilter(
                TestThing.EdmProperties,
                "date ge datetimeoffset'2016-04-01T13:32:56.007'",
                TestThing.LookupProperty);

            Assert.IsTrue(filter.Matches(new TestThing(DateTimeOffset.Parse("2016-04-01T13:32:56.007"), "Foo")));
            Assert.IsTrue(filter.Matches(new TestThing(DateTimeOffset.Parse("2016-04-01T14:32:56.007"), "Foo")));
            Assert.IsTrue(filter.Matches(new TestThing(DateTimeOffset.Parse("2016-04-02T03:32:56.007"), "Foo")));
            Assert.IsFalse(filter.Matches(new TestThing(DateTimeOffset.Parse("2016-03-02T03:32:56.007"), "Foo")));
        }

        [TestMethod]
        public void LessThanEqualDateTime()
        {
            var filter = new ODataFilter(
                TestThing.EdmProperties,
                "date le datetimeoffset'2016-04-01T13:32:56.777'",
                TestThing.LookupProperty);

            Assert.IsTrue(filter.Matches(new TestThing(DateTimeOffset.Parse("2016-04-01T13:32:56.777"), "Foo")));
            Assert.IsTrue(filter.Matches(new TestThing(DateTimeOffset.Parse("2016-03-01T13:32:56.777"), "Foo")));
            Assert.IsTrue(filter.Matches(new TestThing(DateTimeOffset.Parse("2016-04-01T03:32:56.777"), "Foo")));
            Assert.IsFalse(filter.Matches(new TestThing(DateTimeOffset.Parse("2016-04-02T13:32:56.777"), "Foo")));
        }

        [TestMethod]
        public void SubstringFunction()
        {
            var filter = new ODataFilter(
                TestThing.EdmProperties,
                "substringof(text, 'foo')",
                TestThing.LookupProperty);

            Assert.IsTrue(filter.Matches(new TestThing(DateTimeOffset.Parse("2016-04-01"), "foo")));
            Assert.IsTrue(filter.Matches(new TestThing(DateTimeOffset.Parse("2016-04-01"), "foobar")));
            Assert.IsTrue(filter.Matches(new TestThing(DateTimeOffset.Parse("2016-04-01"), "barfoo")));
            Assert.IsFalse(filter.Matches(new TestThing(DateTimeOffset.Parse("2016-04-01"), "FOO")));
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void UnknownComparator()
        {
            var filter = new ODataFilter(
                TestThing.EdmProperties,
                "date foo datetimeoffset'2016-04-01T13:32:56.22'",
                TestThing.LookupProperty);
        }

        private class TestThing
        {
            public static readonly Dictionary<string, EdmPrimitiveTypeKind> EdmProperties = new Dictionary<string, EdmPrimitiveTypeKind>
            {
                { "date", EdmPrimitiveTypeKind.DateTimeOffset },
                { "text", EdmPrimitiveTypeKind.String },
                { "sources", EdmPrimitiveTypeKind.String }
            };

            public static object LookupProperty(object obj, string propertyName)
            {
                if (obj == null)
                    throw new ArgumentNullException(nameof(obj));

                var thing = (TestThing)obj;
                switch (propertyName)
                {
                    case "date":
                        return thing.Timestamp;
                    case "text":
                        return thing.Text;
                    case "sources":
                        return thing.Sources;
                }
                throw new NotSupportedException($"{nameof(TestThing)} does not have property {propertyName}");
            }

            public string Text { get; }

            public DateTimeOffset Timestamp { get; }

            public string[] Sources { get; }

            public TestThing(DateTimeOffset timestamp, string text, string[] sources = null)
            {
                this.Timestamp = timestamp;
                this.Text = text;
                this.Sources = sources;
            }
        }
    }
}
