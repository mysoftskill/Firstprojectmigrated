// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests
{
    using System;

    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Privacy.Core.Converters;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// RequestContext Converter Test
    /// </summary>
    [TestClass]
    public class RequestContextConverterTest
    {
        [TestInitialize]
        public void TestInitialize()
        {
            Sll.ResetContext();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Sll.ResetContext();
        }

        [TestMethod]
        public void ToAdapterRequestContextTest()
        {
            string correlationVector = "UrVLoTMlfEm1vFZT.0";
            Sll.Context.Vector = CorrelationVector.Extend(correlationVector);
            RequestContext requestContext = RequestContext.CreateOldStyle(new Uri("https://unittest"), null, null, default(long), default(long), null, null, null, null, 257, new string[0]);

            PrivacyAdapters.IPxfRequestContext convertedContext = requestContext.ToAdapterRequestContext();

            Assert.IsNotNull(convertedContext);
            
            // The CV gets extended by appending a .0
            Assert.AreEqual(Sll.Context.Vector.Value, convertedContext.CV.Value);
            Assert.IsNull(convertedContext.UserProxyTicket);

        }

        [TestMethod]
        public void ToAdapterRequestContextMissingCorrelationVectorTest()
        {
            RequestContext requestContext = RequestContext.CreateOldStyle(new Uri("https://unittest"), null, null, default(long), default(long), null, null, null, null, 772, new string[0]);

            PrivacyAdapters.IPxfRequestContext convertedContext = requestContext.ToAdapterRequestContext();

            Assert.IsNotNull(convertedContext);

            // The CV is null when none is present in the SLL context.
            Assert.IsNull(convertedContext.CV);
            Assert.IsNull(convertedContext.UserProxyTicket);
        }
    }
}