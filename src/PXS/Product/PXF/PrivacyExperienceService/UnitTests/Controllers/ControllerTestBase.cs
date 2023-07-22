// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.UnitTests.Controllers
{
    using System;
    using System.Security.Principal;
    using System.Threading;

    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Test.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    public abstract class ControllerTestBase
    {
        [TestInitialize]
        public void TestInitialize()
        {
            Sll.ResetContext();

            this.TestPuid = RequestFactory.GeneratePuid();
            this.TestCid = RequestFactory.GenerateCid();
            this.TestCountry = "test_country";
            this.TestUserProxyTicket = "test_user_proxy_ticket";
            this.TestCorrelationVector = new CorrelationVector();
            this.TestFlights = new string[0];

            var mockPrincipal = new Mock<IPrincipal>();
            mockPrincipal.SetupGet(_ => _.Identity).Returns(this.StubMsaSelfIdentity());
            Thread.CurrentPrincipal = mockPrincipal.Object;
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Sll.ResetContext();
        }

        protected long TestCid { get; set; }

        protected CorrelationVector TestCorrelationVector { get; set; }

        protected string TestCountry { get; set; }

        protected string[] TestFlights { get; set; }

        protected long TestPuid { get; set; }

        protected IRequestContext TestRequestContext => this.CreateRequestContext(null);

        protected string TestUserProxyTicket { get; set; }

        protected IRequestContext CreateRequestContext(string callerName)
        {
            return RequestContext.CreateOldStyle(new Uri("https://unittest"),
                this.TestUserProxyTicket,
                null,
                this.TestPuid,
                this.TestPuid,
                this.TestCid,
                this.TestCid,
                this.TestCountry,
                callerName,
                11,
                this.TestFlights);
        }

        private MsaSelfIdentity StubMsaSelfIdentity()
        {
            return new MsaSelfIdentity(
                this.TestUserProxyTicket,
                string.Empty,
                0,
                this.TestPuid,
                0,
                null,
                0,
                null,
                null,
                null,
                false);
        }
    }
}
