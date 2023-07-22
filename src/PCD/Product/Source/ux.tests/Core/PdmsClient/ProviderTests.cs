using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Osgs.Infra.Monitoring.Context;
using Microsoft.Osgs.Infra.Platform;
using Microsoft.PrivacyServices.DataManagement.Client;
using Microsoft.PrivacyServices.DataManagement.Client.AAD;
using Microsoft.PrivacyServices.UX.Configuration;
using Microsoft.PrivacyServices.UX.Core.PdmsClient;
using Microsoft.PrivacyServices.UX.Core.Security;
using Microsoft.PrivacyServices.UX.Tests.Mocks.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Ploeh.AutoFixture;

namespace Microsoft.PrivacyServices.UX.Tests.Core.PdmsClient
{
    [TestClass]
    public class ProviderTests
    {
        private Mock<DataManagement.Client.V2.IDataManagementClient> pdmsClient;

        private Mock<ICorrelationVectorContext> correlationVectorContext;

        private Mock<ServiceAzureActiveDirectoryProviderFactory> azureActiveDirectoryProviderFactory;

        Fixture fixture = new Fixture();

        [TestInitialize]
        public void Initialize()
        {
            pdmsClient = new Mock<DataManagement.Client.V2.IDataManagementClient>(MockBehavior.Strict);
            correlationVectorContext = new Mock<ICorrelationVectorContext>(MockBehavior.Strict);



        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Ctor_Throws_ArgumentNull_4()
        {
            new PdmsClientProvider(null, null, null, null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Ctor_Throws_ArgumentNull_3()
        {
            new PdmsClientProvider(pdmsClient.Object,null, null, null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Ctor_Throws_ArgumentNull_2()
        {
            new PdmsClientProvider(pdmsClient.Object, correlationVectorContext.Object, null, null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Ctor_Throws_ArgumentNull_1()
        {
            new PdmsClientProvider(pdmsClient.Object, correlationVectorContext.Object, Mock.Of<IJwtBearerTokenAccessor>(), null);
        }

        [TestMethod]
        public void DataOwner_Instance_Returns_ConfiguredInstance()
        {
            var mockConfig = MockAzureAdConfig.Create().Object;

            var provider =
                new PdmsClientProvider(pdmsClient.Object, correlationVectorContext.Object, Mock.Of<IJwtBearerTokenAccessor>(), Mock.Of<IAuthenticationProviderFactory>());
            Assert.AreSame(pdmsClient.Object, provider.Instance);
        }

        [TestMethod]
        public void RequestContext_Returns_NewPdmsClientRequestContext()
        {
            var mockConfig = MockAzureAdConfig.Create().Object;

            var correlationVector = new Mock<ICorrelationVector>(MockBehavior.Strict);
            correlationVectorContext.SetupGet(cvc => cvc.Current).Returns(correlationVector.Object);

            var provider =
                new PdmsClientProvider(pdmsClient.Object, correlationVectorContext.Object, Mock.Of<IJwtBearerTokenAccessor>(), Mock.Of<IAuthenticationProviderFactory>());

            correlationVector.SetupGet(cv => cv.Value).Returns("123");

            var context1 = provider.CreateNewRequestContext("mock jwt token");
            Assert.AreEqual("123", context1.CorrelationVector);

            correlationVector.SetupGet(cv => cv.Value).Returns("456");

            var context2 = provider.CreateNewRequestContext("another mock jwt token");
            Assert.AreEqual("456", context2.CorrelationVector);

            Assert.AreNotSame(context1, context2);
        }
    }
}
