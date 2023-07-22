// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.UnitTests.Security
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http.Controllers;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Controllers;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Security;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class CorrelationVectorRequiredAttributeTest : AuthorizationAttributeTestBase
    {
        private readonly Mock<IAppConfiguration> appConfig = new Mock<IAppConfiguration>(MockBehavior.Strict);

        [TestInitialize]
        public void TestInitialize()
        {
            // Refer to the wiki below for more info. This prevents AppDomainUnloadedException in unit tests
            // https://osgwiki.com/wiki/SLL/SLL_v4/FAQ
            Sll.ResetContext();

            this.appConfig.Setup(m => m.IsFeatureFlagEnabledAsync(It.IsAny<string>(), true)).Returns(new ValueTask<bool>(false));
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Sll.ResetContext();
        }

        [TestMethod]
        public void CorrelationVectorRequiredAttributeExistsSuccess()
        {
            HttpActionContext context = CreateHttpActionContext(CreateController());
            context.Request.Headers.Add(CorrelationVector.HeaderName, "headerValue");

            var attribute = new CorrelationVectorRequiredAttribute();
            attribute.OnAuthorization(context);

            Assert.IsNull(context.Response);
        }

        [TestMethod]
        public async Task CorrelationVectorRequiredAttributeDoesNotExistErrors()
        {
            var expectedError = new Error(ErrorCode.InvalidInput, $"Request header did not contain a CV in the header: {CorrelationVector.HeaderName}");
            HttpActionContext context = CreateHttpActionContext(CreateController());

            var attribute = new CorrelationVectorRequiredAttribute();
            attribute.OnAuthorization(context);

            Assert.IsNotNull(context);
            Assert.IsNotNull(context.Response);

            Error actualError = await context.Response.Content.ReadAsAsync<Error>();
            Assert.IsNotNull(actualError);  
            EqualityHelper.AreEqual(expectedError, actualError);
        }

        private KeepAliveController CreateController()
        {
            return new KeepAliveController(this.appConfig.Object);
        }
    }
}
