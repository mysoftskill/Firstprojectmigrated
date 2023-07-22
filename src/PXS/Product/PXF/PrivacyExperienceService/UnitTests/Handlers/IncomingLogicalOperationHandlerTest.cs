// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.UnitTests.Handlers
{
    using System.Net.Http;
    using System.Reflection;
    using System.Security.Principal;
    using System.Web.Http.Controllers;

    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using ExperienceContracts = Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Handlers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    /// <summary>
    ///     IncomingLogicalOperationHandler Test
    /// </summary>
    [TestClass]
    public class IncomingLogicalOperationHandlerTest
    {
        /// <summary>
        ///     Test the CV is initialized when there are request headers, but not one containing a CV.
        /// </summary>
        [TestMethod]
        public void InitializeSllContextVectorTestErrorResponseWithMissingVectorHeader()
        {
            var httpRequest = new HttpRequestMessage();
            httpRequest.Headers.Add(ExperienceContracts.HeaderNames.ProxyTicket, "proxy_Ticket");
            Assert.IsNull(Sll.Context.Vector);

            IncomingLogicalOperationHandler.InitializeSllContextVector(httpRequest);

            Assert.IsNotNull(Sll.Context.Vector);
        }

        /// <summary>
        ///     Test the CV is initialized when there are no request headers.
        /// </summary>
        [TestMethod]
        public void InitializeSllContextVectorTestErrorResponseWithNoHeaders()
        {
            var httpRequest = new HttpRequestMessage();
            Assert.IsNull(Sll.Context.Vector);

            IncomingLogicalOperationHandler.InitializeSllContextVector(httpRequest);

            Assert.IsNotNull(Sll.Context.Vector);
        }

        /// <summary>
        ///     Test a the CV is extended one when exists in the header.
        /// </summary>
        [TestMethod]
        public void InitializeSllContextVectorTestExtendVectorFromHeader()
        {
            string originalCorrelationVector = "JBx6BsLA+Uu5tDar.0";
            string expectedCorrelationVector = originalCorrelationVector + ".0";
            var httpRequest = new HttpRequestMessage();
            httpRequest.Headers.Add(ExperienceContracts.HeaderNames.CorrelationVector, originalCorrelationVector);
            Assert.IsNull(Sll.Context.Vector);

            IncomingLogicalOperationHandler.InitializeSllContextVector(httpRequest);

            Assert.IsNotNull(Sll.Context.Vector);
            Assert.AreEqual(expectedCorrelationVector, Sll.Context.Vector.ToString());
        }

        /// <summary>
        ///     Test if the CorrelationContext is set when exists in the header.
        /// </summary>
        [TestMethod]
        public void InitializeSllCorrelationContextTestCorrelationContextFromHeader()
        {
            string CorrelationContext = "v=1,ms.b.tel.partner=PXS,ms.b.tel.scenario=GetTimeline";
            var httpRequest = new HttpRequestMessage();
            httpRequest.Headers.Add(ExperienceContracts.HeaderNames.CorrelationContext, CorrelationContext);
            Assert.IsNull(Sll.Context.CorrelationContext);

            IncomingLogicalOperationHandler.InitializeSllCorrelationContext(httpRequest);

            Assert.IsNotNull(Sll.Context.CorrelationContext);
            Assert.AreEqual(CorrelationContext, Sll.Context.CorrelationContext.GetCorrelationContext());
        }

        /// <summary>
        ///     Test the CorrelationContext is initialized when there are request headers, but not one containing a cC.
        /// </summary>
        [TestMethod]
        public void InitializeSllCorrelationContextTestErrorResponseWithMissingVectorHeader()
        {
            var httpRequest = new HttpRequestMessage();
            httpRequest.Headers.Add(ExperienceContracts.HeaderNames.ProxyTicket, "proxy_Ticket");
            Assert.IsNull(Sll.Context.CorrelationContext);

            IncomingLogicalOperationHandler.InitializeSllCorrelationContext(httpRequest);

            Assert.IsNotNull(Sll.Context.CorrelationContext);
        }

        /// <summary>
        ///     Test the CorrelationContext is initialized when there are no request headers.
        /// </summary>
        [TestMethod]
        public void InitializeSllCorrelationContextTestErrorResponseWithNoHeaders()
        {
            var httpRequest = new HttpRequestMessage();
            Assert.IsNull(Sll.Context.CorrelationContext);

            IncomingLogicalOperationHandler.InitializeSllCorrelationContext(httpRequest);

            Assert.IsNotNull(Sll.Context.CorrelationContext);
        }

        /// <summary>
        ///     Test the caller name
        /// </summary>
        [DataTestMethod]
        public void CallerName()
        {
            var wrapper = new IncomingApiEventWrapper();

            var principal = new Mock<IPrincipal>(MockBehavior.Strict);
            var identity = new MsaSiteIdentity(nameof(IncomingLogicalOperationHandlerTest), 123456);
            principal.SetupGet(p => p.Identity).Returns(identity);

            var message = new HttpRequestMessage();
            message.SetRequestContext(
                new HttpRequestContext
                {
                    Principal = principal.Object
                });

            MethodInfo endEvent = typeof(IncomingLogicalOperationHandler).GetMethod("EndApiEventCommon", BindingFlags.Static | BindingFlags.NonPublic);

            Assert.IsNotNull(endEvent, $"Breaking change in implementation of {nameof(IncomingLogicalOperationHandler)} detected");
            endEvent.Invoke(null, new object[] { wrapper, message });

            string expectedCaller = identity.CallerNameFormatted;
            Assert.AreEqual(expectedCaller, wrapper.CallerName);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Sll.ResetContext();
        }

        [TestInitialize]
        public void TestInitialize()
        {
            // Refer to the wiki below for more info. This prevents AppDomainUnloadedException in unit tests
            // https://osgwiki.com/wiki/SLL/SLL_v4/FAQ
            Sll.ResetContext();
        }
    }
}
