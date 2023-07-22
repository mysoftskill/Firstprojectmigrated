// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.UnitTests
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class RequestContextTests
    {
        [TestMethod]
        public void TestPortalDetection()
        {
            var portals = new Dictionary<string, string>
            {
                { "1", "PCD_Test" },
                { "2", "MSGRAPH_Test" },
                { "3", "MEEPORTAL_Test" },
                { "4", "PXSTEST_Test" }
            };
            RequestContext rc;

            rc = new RequestContext(new AadIdentity("1", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));
            Assert.AreEqual(Portal.Pcd, rc.GetPortal(portals));
            rc = new RequestContext(new AadIdentity("2", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));
            Assert.AreEqual(Portal.MsGraph, rc.GetPortal(portals));
            rc = new RequestContext(new AadIdentity("99", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));
            Assert.AreEqual(Portal.Unknown, rc.GetPortal(portals));

            rc = new RequestContext(
                new MsaSelfIdentity(
                    Guid.NewGuid().ToString(),
                    Guid.NewGuid().ToString(),
                    42,
                    42,
                    42,
                    Guid.NewGuid().ToString(),
                    3,
                    42,
                    Guid.NewGuid().ToString(),
                    null,
                    false));
            Assert.AreEqual(Portal.Amc, rc.GetPortal(portals));
            rc = new RequestContext(
                new MsaSelfIdentity(
                    Guid.NewGuid().ToString(),
                    Guid.NewGuid().ToString(),
                    42,
                    42,
                    42,
                    Guid.NewGuid().ToString(),
                    4,
                    42,
                    Guid.NewGuid().ToString(),
                    null,
                    false));
            Assert.AreEqual(Portal.PxsTest, rc.GetPortal(portals));
            rc = new RequestContext(
                new MsaSelfIdentity(
                    Guid.NewGuid().ToString(),
                    Guid.NewGuid().ToString(),
                    42,
                    42,
                    42,
                    Guid.NewGuid().ToString(),
                    99,
                    42,
                    Guid.NewGuid().ToString(),
                    null,
                    false));
            Assert.AreEqual(Portal.Unknown, rc.GetPortal(portals));
        }
    }
}
