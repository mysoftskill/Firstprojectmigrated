// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Common.UnitTests.Utilities
{
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ValueExtenderTests
    {
        [TestMethod]
        public void ToStringInvariantShouldConvertByte()
        {
            const byte Value = 65;
            Assert.AreEqual("65", Value.ToStringInvariant());
        }

        [TestMethod]
        public void ToStringInvariantShouldConvertShort()
        {
            const short Value = 65;
            Assert.AreEqual("65", Value.ToStringInvariant());
        }

        [TestMethod]
        public void ToStringInvariantShouldConvertInt()
        {
            const int Value = 100000;
            Assert.AreEqual("100000", Value.ToStringInvariant());
        }

        [TestMethod]
        public void ToStringInvariantShouldConvertLong()
        {
            const long Value = 65000000;
            Assert.AreEqual("65000000", Value.ToStringInvariant());
        }

        [TestMethod]
        public void ToStringInvariantShouldConvertBool()
        {
            const bool Value = false;
            Assert.AreEqual("False", Value.ToStringInvariant());
        }

        [TestMethod]
        public void ToStringInvariantShouldConvertNullableByte()
        {
            byte? value = null;
            Assert.AreEqual(string.Empty, value.ToStringInvariant());
        }

        [TestMethod]
        public void ToStringInvariantShouldConvertNullableShort()
        {
            short? value = null;
            Assert.AreEqual(string.Empty, value.ToStringInvariant());
        }

        [TestMethod]
        public void ToStringInvariantShouldConvertNullableInt()
        {
            int? value = null;
            Assert.AreEqual(string.Empty, value.ToStringInvariant());
        }

        [TestMethod]
        public void ToStringInvariantShouldConvertNullableLong()
        {
            long? value = null;
            Assert.AreEqual(string.Empty, value.ToStringInvariant());
        }

        [TestMethod]
        public void ToStringInvariantShouldConvertNullableBool()
        {
            bool? value = null;
            Assert.AreEqual(string.Empty, value.ToStringInvariant());
        }
    }
}
