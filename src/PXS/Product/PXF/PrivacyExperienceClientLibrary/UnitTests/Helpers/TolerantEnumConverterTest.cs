//---------------------------------------------------------------------
// <copyright>
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//---------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.UnitTests.Helpers
{
    using System;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;

    [TestClass]
    public class TolerantEnumConverterTest
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings { Converters = new[] { new TolerantEnumConverter() } };

        private enum StandardEnum
        {
            First = 1,
            Second,
            Third,
            Fourth,
            Fifth,
        }

        private enum StandardEnumWithDefault
        {
            Default = 0,
            First = 1,
            Second,
            Third,
            Fourth,
            Fifth,
        }

        [Flags]
        private enum FlagsEnum : int
        {
            None = 0,
            One = 1,
            Two = 2,
            Three = 4,
        }

        [Flags]
        private enum BigFlagsEnum : ulong
        {
            None = 0,
            One = 1,
            Two = 2,
            Three = 4,
            BigValue = ((ulong)1 << 63),
        }

        /// <summary>
        /// Tests deserializing unknown entities in non-null cases.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Flags")]
        [TestMethod]
        public void NonNullIntFlags()
        {
            var template = new
            {
                Flags = FlagsEnum.None
            };

            var result = JsonConvert.DeserializeAnonymousType("{ \"Flags\": \"One, Two, Three\" }", template, Settings);
            Assert.AreEqual(FlagsEnum.One | FlagsEnum.Two | FlagsEnum.Three, result.Flags);

            result = JsonConvert.DeserializeAnonymousType("{ \"Flags\": 3 }", template, Settings);
            Assert.AreEqual(FlagsEnum.One | FlagsEnum.Two, result.Flags);

            // Unrecognized flag is skipped.
            result = JsonConvert.DeserializeAnonymousType("{ \"Flags\": \"One, Two, Three, NotARealFlag\" }", template, Settings);
            Assert.AreEqual(FlagsEnum.One | FlagsEnum.Two | FlagsEnum.Three, result.Flags);

            result = JsonConvert.DeserializeAnonymousType("{ \"Flags\": \"Not, Any, Of, These, Flags, Exist\" }", template, Settings);
            Assert.AreEqual(FlagsEnum.None, result.Flags);
        }

        /// <summary>
        /// Tests deserializing unknown entities in null cases.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Flags")]
        [TestMethod]
        public void NullIntFlags()
        {
            var template = new
            {
                Flags = (FlagsEnum?)FlagsEnum.None
            };

            var result = JsonConvert.DeserializeAnonymousType("{ \"Flags\": \"One, Two, Three\" }", template, Settings);
            Assert.AreEqual(FlagsEnum.One | FlagsEnum.Two | FlagsEnum.Three, result.Flags);

            result = JsonConvert.DeserializeAnonymousType("{ \"Flags\": 3 }", template, Settings);
            Assert.AreEqual(FlagsEnum.One | FlagsEnum.Two, result.Flags);

            // Unrecognized flag is skipped.
            result = JsonConvert.DeserializeAnonymousType("{ \"Flags\": \"One, Two, Three, NotARealFlag\" }", template, Settings);
            Assert.AreEqual(FlagsEnum.One | FlagsEnum.Two | FlagsEnum.Three, result.Flags);

            result = JsonConvert.DeserializeAnonymousType("{ \"Flags\": \"Not, Any, Of, These, Flags, Exist\" }", template, Settings);
            Assert.AreEqual(FlagsEnum.None, result.Flags);
        }

        /// <summary>
        /// Tests deserializing unknown entities in non-null cases.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Flags")]
        [TestMethod]
        public void NonNullULongFlags()
        {
            var template = new
            {
                Flags = BigFlagsEnum.None
            };

            var result = JsonConvert.DeserializeAnonymousType("{ \"Flags\": \"One, Two, Three\" }", template, Settings);
            Assert.AreEqual(BigFlagsEnum.One | BigFlagsEnum.Two | BigFlagsEnum.Three, result.Flags);

            result = JsonConvert.DeserializeAnonymousType("{ \"Flags\": \"One, Two, Three, BigValue\" }", template, Settings);
            Assert.AreEqual(BigFlagsEnum.One | BigFlagsEnum.Two | BigFlagsEnum.Three | BigFlagsEnum.BigValue, result.Flags);

            // Unrecognized flag is skipped.
            result = JsonConvert.DeserializeAnonymousType("{ \"Flags\": \"One, Two, Three, NotARealFlag\" }", template, Settings);
            Assert.AreEqual(BigFlagsEnum.One | BigFlagsEnum.Two | BigFlagsEnum.Three, result.Flags);

            result = JsonConvert.DeserializeAnonymousType("{ \"Flags\": \"Not, Any, Of, These, Flags, Exist\" }", template, Settings);
            Assert.AreEqual(BigFlagsEnum.None, result.Flags);
        }

        /// <summary>
        /// Tests deserializing unknown entities in non-null cases.
        /// </summary>
        [TestMethod]
        public void NonNullStandardEnum()
        {
            var template = new
            {
                Value = StandardEnum.First,
            };

            var result = JsonConvert.DeserializeAnonymousType("{ \"Value\": \"First\" }", template, Settings);
            Assert.AreEqual(StandardEnum.First, result.Value);

            result = JsonConvert.DeserializeAnonymousType("{ \"Value\": 1 }", template, Settings);
            Assert.AreEqual(StandardEnum.First, result.Value);

            // Unrecognized flag gets marked as "0".
            result = JsonConvert.DeserializeAnonymousType("{ \"Value\": \"InvalidValue\" }", template, Settings);
            Assert.AreEqual((StandardEnum)0, result.Value);

            result = JsonConvert.DeserializeAnonymousType("{ \"Value\": null }", template, Settings);
            Assert.AreEqual((StandardEnum)0, result.Value);

            result = JsonConvert.DeserializeAnonymousType("{ \"Value\": \"\" }", template, Settings);
            Assert.AreEqual((StandardEnum)0, result.Value);
        }

        /// <summary>
        /// Tests deserializing unknown entities in non-null cases.
        /// </summary>
        [TestMethod]
        public void NonNullStandardEnumWithDefault()
        {
            var template = new
            {
                Value = StandardEnumWithDefault.First,
            };

            var result = JsonConvert.DeserializeAnonymousType("{ \"Value\": \"First\" }", template, Settings);
            Assert.AreEqual(StandardEnumWithDefault.First, result.Value);

            // Unrecognized flag gets marked as "0".
            result = JsonConvert.DeserializeAnonymousType("{ \"Value\": \"InvalidValue\" }", template, Settings);
            Assert.AreEqual(StandardEnumWithDefault.Default, result.Value);

            result = JsonConvert.DeserializeAnonymousType("{ \"Value\": null }", template, Settings);
            Assert.AreEqual(StandardEnumWithDefault.Default, result.Value);

            result = JsonConvert.DeserializeAnonymousType("{ \"Value\": \"\" }", template, Settings);
            Assert.AreEqual(StandardEnumWithDefault.Default, result.Value);
        }


        /// <summary>
        /// Tests deserializing unknown entities in non-null cases.
        /// </summary>
        [TestMethod]
        public void NullStandardEnum()
        {
            var template = new
            {
                Value = (StandardEnum?)StandardEnum.First,
            };

            var result = JsonConvert.DeserializeAnonymousType("{ \"Value\": \"First\" }", template, Settings);
            Assert.AreEqual(StandardEnum.First, result.Value);

            // Unrecognized flag gets marked as "0".
            result = JsonConvert.DeserializeAnonymousType("{ \"Value\": \"InvalidValue\" }", template, Settings);
            Assert.AreEqual(null, result.Value);

            result = JsonConvert.DeserializeAnonymousType("{ \"Value\": null }", template, Settings);
            Assert.AreEqual(null, result.Value);

            result = JsonConvert.DeserializeAnonymousType("{ \"Value\": \"\" }", template, Settings);
            Assert.AreEqual(null, result.Value);
        }
    }
}
