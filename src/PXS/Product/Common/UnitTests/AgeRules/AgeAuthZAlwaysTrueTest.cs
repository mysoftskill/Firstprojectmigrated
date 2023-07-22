// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.UnitTests
{
    using System;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class AgeAuthZAlwaysTrueTest
    {
        private readonly IAgeAuthZRules rules = new AgeAuthZAlwaysTrue();

        [TestMethod]
        public void AgeAuthZAlwaysTrueCanDeleteReturnsTrue()
        {
            Assert.IsTrue(this.rules.CanDelete(null));
        }

        [TestMethod]
        public void AgeAuthZAlwaysTrueCanViewReturnsTrue()
        {
            Assert.IsTrue(this.rules.CanView(null));
        }
    }
}
