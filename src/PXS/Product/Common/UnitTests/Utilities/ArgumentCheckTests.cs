// <copyright>
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Membership.MemberServices.Common.UnitTests.Utilities
{
    using System;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ArgumentCheckTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ThrowIfNullEmptyOrWhiteSpaceShouldThrowWhenNull()
        {
            ArgumentCheck.ThrowIfNullEmptyOrWhiteSpace(null, "param");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ThrowIfNullEmptyOrWhiteSpaceShouldThrowWhenEmpty()
        {
            ArgumentCheck.ThrowIfNullEmptyOrWhiteSpace(string.Empty, "param");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ThrowIfNullEmptyOrWhiteSpaceShouldThrowWhenOnlyWhitespace()
        {
            ArgumentCheck.ThrowIfNullEmptyOrWhiteSpace("   ", "param");
        }

        [TestMethod]
        public void ThrowIfNullEmptyOrWhiteSpaceShouldNotThrowWhenOnlyNotEmpty()
        {
            ArgumentCheck.ThrowIfNullEmptyOrWhiteSpace("TobyTheDog", "param");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ThrowIfNullShouldThrowWhenNull()
        {
            ArgumentCheck.ThrowIfNull(null, "param");
        }

        [TestMethod]
        public void ThrowIfNullShouldNotThrowWhenNotNull()
        {
            ArgumentCheck.ThrowIfNull(new object(), "param");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ThrowIfLessThanShouldThrowWhenLessThan()
        {
            ArgumentCheck.ThrowIfLessThan(5, 10, "param");
        }

        [TestMethod]
        public void ThrowIfLessThanShouldNotThrowWhenNotLessThan()
        {
            ArgumentCheck.ThrowIfLessThan(10, 5, "param");
        }

        [TestMethod]
        public void ThrowIfLessThanShouldNotThrowWhenEqual()
        {
            ArgumentCheck.ThrowIfLessThan(5, 5, "param");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ThrowIfLessThanOrEqualShouldThrowWhenLessThan()
        {
            ArgumentCheck.ThrowIfLessThanOrEqualTo(5, 10, "param");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ThrowIfLessThanOrEqualShouldThrowWhenEqual()
        {
            ArgumentCheck.ThrowIfLessThanOrEqualTo(5, 5, "param");
        }

        [TestMethod]
        public void ThrowIfLessThanOrEqualShouldNotThrowWhenNotLessThanOrEqual()
        {
            ArgumentCheck.ThrowIfLessThanOrEqualTo(10, 5, "param");
        }
    }
}
