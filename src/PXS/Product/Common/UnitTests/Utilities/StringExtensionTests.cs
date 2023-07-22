// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.UnitTests.Utilities
{
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class StringManagerTests
    {
        [TestMethod]
        public void StartsWithIgnoreCaseReturnsCorrectValueWhenFullStringStartsWithPrefix()
        {
            Assert.IsTrue("FullString".StartsWithIgnoreCase("fulls"));
        }

        [TestMethod]
        public void StartsWithIgnoreCaseReturnsCorrectValueWhenFullStringDoesNotStartsWithPrefix()
        {
            Assert.IsFalse("FullString".StartsWithIgnoreCase("tring"));
        }

        [TestMethod]
        public void StartsWithIgnoreCaseReturnsFalseWhenPrefixIsNull()
        {
            Assert.IsFalse("FullString".StartsWithIgnoreCase(null));
        }

        [TestMethod]
        public void StartsWithIgnoreCaseReturnsTrueWhenPrefixIsEmpty()
        {
            Assert.IsTrue("FullString".StartsWithIgnoreCase(string.Empty));
        }

        [TestMethod]
        public void StartsWithIgnoreCaseReturnsFalseWhenStringIsEmptyAndPrefixIsNot()
        {
            Assert.IsFalse(string.Empty.StartsWithIgnoreCase("blah"));
        }
    }
}
