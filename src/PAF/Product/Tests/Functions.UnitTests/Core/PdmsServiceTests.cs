namespace Microsoft.PrivacyServices.AzureFunctions.UnitTests.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.AzureFunctions.Common;
    using Microsoft.PrivacyServices.AzureFunctions.Common.Configuration;
    using Microsoft.PrivacyServices.AzureFunctions.Common.DataAccessors;
    using Microsoft.PrivacyServices.AzureFunctions.Common.Models;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class PdmsServiceTests
    {
        private readonly Mock<IHttpClientWrapper> httpClientWrapperMock;
        private readonly Mock<ILogger> loggerMock;
        private readonly Mock<IAuthenticationProvider> authenticationProviderMock;
        private readonly Mock<IFunctionConfiguration> configurationMock;

        public PdmsServiceTests()
        {
            this.httpClientWrapperMock = new Mock<IHttpClientWrapper>();
            this.loggerMock = new Mock<ILogger>();
            this.authenticationProviderMock = new Mock<IAuthenticationProvider>();
            this.configurationMock = new Mock<IFunctionConfiguration>();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ConstructorThrowsIfConfigurationIsNull()
        {
            _ = new PdmsService(null, this.loggerMock.Object, this.httpClientWrapperMock.Object, this.authenticationProviderMock.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ConstructorThrowsIfLoggerIsNull()
        {
            _ = new PdmsService(this.configurationMock.Object, null, this.httpClientWrapperMock.Object, this.authenticationProviderMock.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ConstructorThrowsIfHttpClientWrapperIsNull()
        {
            _ = new PdmsService(this.configurationMock.Object, this.loggerMock.Object, null, this.authenticationProviderMock.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ConstructorThrowsIfAuthenticationProvidernIsNull()
        {
            _ = new PdmsService(this.configurationMock.Object, this.loggerMock.Object, this.httpClientWrapperMock.Object, null);
        }

        [TestMethod]
        public void ConstructorWorks()
        {
            PdmsService service = new PdmsService(this.configurationMock.Object, this.loggerMock.Object, this.httpClientWrapperMock.Object, this.authenticationProviderMock.Object);
            Assert.IsNotNull(service);
        }

        [TestMethod]
        public async Task GetVariantRequestReturnVariantRequestOnSuccessAsync()
        {
            var variantRequestId = Guid.NewGuid();
            var variantRequest = new VariantRequest()
            {
                Id = variantRequestId.ToString()
            };

            // Configure mocks
            string apiUrl = $"api/v2/variantRequests('{variantRequestId}')";
            this.httpClientWrapperMock.Setup(hcMock => hcMock.GetAsync<VariantRequest>(apiUrl, It.IsAny<Func<Task<string>>>())).Returns(Task.FromResult(variantRequest));

            PdmsService service = new PdmsService(this.configurationMock.Object, this.loggerMock.Object, this.httpClientWrapperMock.Object, this.authenticationProviderMock.Object);
            Assert.IsNotNull(service);

            // call the service
            var result = await service.GetVariantRequestAsync(variantRequestId).ConfigureAwait(false);

            // verify result
            Assert.IsNotNull(result);
            Assert.AreEqual(variantRequestId.ToString(), result.Id);
            this.httpClientWrapperMock.Verify(hcMock => hcMock.GetAsync<VariantRequest>(It.IsAny<string>(), It.IsAny<Func<Task<string>>>()), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task GetVariantRequestThrowsIfVariantRequestNotFoundAsync()
        {
            var variantRequestId = Guid.NewGuid();
            var variantRequest = new VariantRequest()
            {
                Id = variantRequestId.ToString()
            };

            // Configure mocks
            string apiUrl = $"api/v2/variantRequests('{variantRequestId}')";
            this.httpClientWrapperMock.Setup(hcMock => hcMock.GetAsync<VariantRequest>(apiUrl, It.IsAny<Func<Task<string>>>())).Returns(Task.FromResult((VariantRequest)null));

            PdmsService service = new PdmsService(this.configurationMock.Object, this.loggerMock.Object, this.httpClientWrapperMock.Object, this.authenticationProviderMock.Object);
            Assert.IsNotNull(service);

            // call the service
            await service.GetVariantRequestAsync(variantRequestId).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task GetVariantRequestThrowsIfVariantRequestIdIsEmptyAsync()
        {
            PdmsService service = new PdmsService(this.configurationMock.Object, this.loggerMock.Object, this.httpClientWrapperMock.Object, this.authenticationProviderMock.Object);
            Assert.IsNotNull(service);

            // call the service
            await service.GetVariantRequestAsync(Guid.Empty).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task UpdateVariantRequestReturnsUpdatedVariantRequestOnSuccessAsync()
        {
            var variantRequestId = Guid.NewGuid();
            var variantRequest = new VariantRequest()
            {
                Id = variantRequestId.ToString()
            };

            // Configure mocks
            string apiUrl = $"api/v2/variantRequests('{variantRequest.Id}')";
            this.httpClientWrapperMock.Setup(hcMock => hcMock.UpdateAsync<VariantRequest>(HttpMethod.Put, apiUrl, It.IsAny<Func<Task<string>>>(), variantRequest)).Returns(Task.FromResult(variantRequest));

            PdmsService service = new PdmsService(this.configurationMock.Object, this.loggerMock.Object, this.httpClientWrapperMock.Object, this.authenticationProviderMock.Object);
            Assert.IsNotNull(service);

            // call the service
            var result = await service.UpdateVariantRequestAsync(variantRequest).ConfigureAwait(false);

            // verify result
            Assert.IsNotNull(result);
            Assert.AreEqual(variantRequest.Id, result.Id);
            this.httpClientWrapperMock.Verify(hcMock => hcMock.UpdateAsync<VariantRequest>(HttpMethod.Put, It.IsAny<string>(), It.IsAny<Func<Task<string>>>(), variantRequest), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task UpdateVariantRequestThrowsIfVariantRequestIsNullAsync()
        {
            PdmsService service = new PdmsService(this.configurationMock.Object, this.loggerMock.Object, this.httpClientWrapperMock.Object, this.authenticationProviderMock.Object);
            Assert.IsNotNull(service);

            // call the service
            await service.UpdateVariantRequestAsync((VariantRequest)null).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task UpdateVariantRequestThrowsIfUpdateFailsAsync()
        {
            var variantRequestId = Guid.NewGuid();
            var variantRequest = new VariantRequest()
            {
                Id = variantRequestId.ToString()
            };

            // Configure mocks
            string apiUrl = $"api/v2/variantRequests('{variantRequest.Id}')";
            this.httpClientWrapperMock.Setup(hcMock => hcMock.UpdateAsync<VariantRequest>(HttpMethod.Put, apiUrl, It.IsAny<Func<Task<string>>>(), variantRequest)).Returns(Task.FromResult((VariantRequest)null));

            PdmsService service = new PdmsService(this.configurationMock.Object, this.loggerMock.Object, this.httpClientWrapperMock.Object, this.authenticationProviderMock.Object);
            Assert.IsNotNull(service);

            // call the service
            await service.UpdateVariantRequestAsync(variantRequest).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ApproveVariantRequestSuccessAsync()
        {
            var variantRequestId = Guid.NewGuid();
            var variantRequest = new VariantRequest()
            {
                Id = variantRequestId.ToString(),
                ETag = "testingETag"
            };

            string apiUrl = $"api/v2/variantRequests('{variantRequest.Id}')/v2.approve";
            string getApiUrl = $"api/v2/variantRequests('{variantRequest.Id}')";
            string etag = "testingETag";
            var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Accepted
            };

            this.httpClientWrapperMock.Setup(hcMock => hcMock.GetAsync<VariantRequest>(getApiUrl, It.IsAny<Func<Task<string>>>())).Returns(Task.FromResult(variantRequest)).Verifiable();

            // Approves the variant request
            this.httpClientWrapperMock.Setup(hcMock => hcMock.PostAsync(apiUrl, It.IsAny<Func<Task<string>>>(), etag)).Returns(Task.FromResult(responseMessage)).Verifiable();

            IPdmsService service = new PdmsService(this.configurationMock.Object, this.loggerMock.Object, this.httpClientWrapperMock.Object, this.authenticationProviderMock.Object);

            Assert.IsTrue(await service.ApproveVariantRequestAsync(variantRequestId).ConfigureAwait(false));

            this.httpClientWrapperMock.VerifyAll();
        }

        [TestMethod]
        public async Task ApproveVariantRequestDoesNotExistAsync()
        {
            var variantRequestId = Guid.NewGuid();
            var variantRequest = new VariantRequest()
            {
                Id = variantRequestId.ToString(),
                ETag = "testingETag"
            };

            string apiUrl = $"api/v2/variantRequests('{variantRequest.Id}')/v2.approve";
            string getApiUrl = $"api/v2/variantRequests('{variantRequest.Id}')";
            string etag = "testingETag";
            var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Accepted
            };

            this.httpClientWrapperMock.Setup(hcMock => hcMock.GetAsync<VariantRequest>(getApiUrl, It.IsAny<Func<Task<string>>>())).Returns(Task.FromResult((VariantRequest)null)).Verifiable();

            // Approves the variant request
            this.httpClientWrapperMock.Setup(hcMock => hcMock.PostAsync(apiUrl, It.IsAny<Func<Task<string>>>(), etag)).Returns(Task.FromResult(responseMessage)).Verifiable();

            IPdmsService service = new PdmsService(this.configurationMock.Object, this.loggerMock.Object, this.httpClientWrapperMock.Object, this.authenticationProviderMock.Object);

            Assert.IsFalse(await service.ApproveVariantRequestAsync(variantRequestId).ConfigureAwait(false));

            this.httpClientWrapperMock.Verify(hcMock => hcMock.GetAsync<VariantRequest>(getApiUrl, It.IsAny<Func<Task<string>>>()), Times.Once);
            this.httpClientWrapperMock.Verify(hcMock => hcMock.PostAsync(apiUrl, It.IsAny<Func<Task<string>>>(), etag), Times.Never);
        }

        [TestMethod]
        public async Task ApproveVariantRequestFailsToApproveAsync()
        {
            var variantRequestId = Guid.NewGuid();
            var variantRequest = new VariantRequest()
            {
                Id = variantRequestId.ToString(),
                ETag = "testingETag"
            };

            string apiUrl = $"api/v2/variantRequests('{variantRequest.Id}')/v2.approve";
            string getApiUrl = $"api/v2/variantRequests('{variantRequest.Id}')";
            string etag = "testingETag";
            var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest
            };

            this.httpClientWrapperMock.Setup(hcMock => hcMock.GetAsync<VariantRequest>(getApiUrl, It.IsAny<Func<Task<string>>>())).Returns(Task.FromResult(variantRequest)).Verifiable();

            // Approves the variant request
            this.httpClientWrapperMock.Setup(hcMock => hcMock.PostAsync(apiUrl, It.IsAny<Func<Task<string>>>(), etag)).Returns(Task.FromResult(responseMessage)).Verifiable();

            IPdmsService service = new PdmsService(this.configurationMock.Object, this.loggerMock.Object, this.httpClientWrapperMock.Object, this.authenticationProviderMock.Object);

            Assert.IsFalse(await service.ApproveVariantRequestAsync(variantRequestId).ConfigureAwait(false));
            this.httpClientWrapperMock.VerifyAll();
        }

        [TestMethod]
        public async Task DeleteVariantRequestOnSuccessAsync()
        {
            var variantRequestId = Guid.NewGuid();
            var variantRequest = new VariantRequest()
            {
                Id = variantRequestId.ToString(),
                ETag = "testingETag"
            };

            string apiUrl = $"api/v2/variantRequests('{variantRequest.Id}')";
            string getApiUrl = $"api/v2/variantRequests('{variantRequest.Id}')";
            string etag = "testingETag";
            var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Accepted
            };

            this.httpClientWrapperMock.Setup(hcMock => hcMock.GetAsync<VariantRequest>(getApiUrl, It.IsAny<Func<Task<string>>>())).Returns(Task.FromResult(variantRequest)).Verifiable();

            // Approves the variant request
            this.httpClientWrapperMock.Setup(hcMock => hcMock.DeleteAsync(apiUrl, It.IsAny<Func<Task<string>>>(), etag)).Returns(Task.FromResult(responseMessage)).Verifiable();

            IPdmsService service = new PdmsService(this.configurationMock.Object, this.loggerMock.Object, this.httpClientWrapperMock.Object, this.authenticationProviderMock.Object);

            Assert.IsTrue(await service.DeleteVariantRequestAsync(variantRequestId).ConfigureAwait(false));

            this.httpClientWrapperMock.VerifyAll();
        }

        [TestMethod]
        public async Task DeleteVariantRequestDoesNotExistAsync()
        {
            var variantRequestId = Guid.NewGuid();
            var variantRequest = new VariantRequest()
            {
                Id = variantRequestId.ToString(),
                ETag = "testingETag"
            };

            string apiUrl = $"api/v2/variantRequests('{variantRequest.Id}')";
            string getApiUrl = $"api/v2/variantRequests('{variantRequest.Id}')";
            string etag = "testingETag";
            var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Accepted
            };

            this.httpClientWrapperMock.Setup(hcMock => hcMock.GetAsync<VariantRequest>(getApiUrl, It.IsAny<Func<Task<string>>>())).Returns(Task.FromResult((VariantRequest)null)).Verifiable();

            // Deletes the variant request
            this.httpClientWrapperMock.Setup(hcMock => hcMock.DeleteAsync(apiUrl, It.IsAny<Func<Task<string>>>(), etag)).Returns(Task.FromResult(responseMessage)).Verifiable();

            IPdmsService service = new PdmsService(this.configurationMock.Object, this.loggerMock.Object, this.httpClientWrapperMock.Object, this.authenticationProviderMock.Object);

            Assert.IsTrue(await service.DeleteVariantRequestAsync(variantRequestId).ConfigureAwait(false));

            this.httpClientWrapperMock.Verify(hcMock => hcMock.GetAsync<VariantRequest>(getApiUrl, It.IsAny<Func<Task<string>>>()), Times.Once);
            this.httpClientWrapperMock.Verify(hcMock => hcMock.PostAsync(apiUrl, It.IsAny<Func<Task<string>>>(), etag), Times.Never);
        }

        [TestMethod]
        public async Task DeleteVariantRequestFailsToDeleteAsync()
        {
            var variantRequestId = Guid.NewGuid();
            var variantRequest = new VariantRequest()
            {
                Id = variantRequestId.ToString(),
                ETag = "testingETag"
            };

            string apiUrl = $"api/v2/variantRequests('{variantRequest.Id}')";
            string getApiUrl = $"api/v2/variantRequests('{variantRequest.Id}')";
            string etag = "testingETag";
            var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest
            };

            this.httpClientWrapperMock.Setup(hcMock => hcMock.GetAsync<VariantRequest>(getApiUrl, It.IsAny<Func<Task<string>>>())).Returns(Task.FromResult(variantRequest)).Verifiable();

            // Deletes the variant request
            this.httpClientWrapperMock.Setup(hcMock => hcMock.DeleteAsync(apiUrl, It.IsAny<Func<Task<string>>>(), etag)).Returns(Task.FromResult(responseMessage)).Verifiable();

            IPdmsService service = new PdmsService(this.configurationMock.Object, this.loggerMock.Object, this.httpClientWrapperMock.Object, this.authenticationProviderMock.Object);

            Assert.IsFalse(await service.DeleteVariantRequestAsync(variantRequestId).ConfigureAwait(false));

            this.httpClientWrapperMock.VerifyAll();
        }

        [TestMethod]
        public async Task GetVariantDefinitionReturnVariantDefinitionOnSuccessAsync()
        {
            Guid variantDefinitionId = Guid.NewGuid();
            VariantDefinition variantDefinition = new VariantDefinition()
            {
                Capabilities = new List<string>() { "Test Capabilities" },
                DataTypes = new List<string>() { "Test DataTypes" },
                SubjectTypes = new List<string>() { "Test Subject Types" }
            };

            // Configure mocks
            string apiUrl = $"/api/v2/VariantDefinitions('{variantDefinitionId}')?$select=dataTypes,subjectTypes,capabilities";
            this.httpClientWrapperMock.Setup(hcMock => hcMock.GetAsync<VariantDefinition>(apiUrl, It.IsAny<Func<Task<string>>>())).Returns(Task.FromResult(variantDefinition));

            PdmsService service = new PdmsService(this.configurationMock.Object, this.loggerMock.Object, this.httpClientWrapperMock.Object, this.authenticationProviderMock.Object);
            Assert.IsNotNull(service);

            // call the service
            var result = await service.GetVariantDefinitionAsync(variantDefinitionId);

            // verify result
            Assert.IsNotNull(result);
            this.VerifyVariantDefinition(variantDefinition, result);
            this.httpClientWrapperMock.Verify(hcMock => hcMock.GetAsync<VariantDefinition>(It.IsAny<string>(), It.IsAny<Func<Task<string>>>()), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task GetVariantDefinitionThrowsIfVariantRequestNotFoundAsync()
        {
            var variantDefinitionId = Guid.NewGuid();
            VariantDefinition variantDefinition = new VariantDefinition()
            {
                Capabilities = new List<string>() { "Test Capabilities" },
                DataTypes = new List<string>() { "Test DataTypes" },
                SubjectTypes = new List<string>() { "Test Subject Types" }
            };

            // Configure mocks
            string apiUrl = $"/api/v2/VariantDefinitions('{variantDefinitionId}')?$select=dataTypes,subjectTypes,capabilities";
            this.httpClientWrapperMock.Setup(hcMock => hcMock.GetAsync<VariantDefinition>(apiUrl, It.IsAny<Func<Task<string>>>())).Returns(Task.FromResult((VariantDefinition)null));

            PdmsService service = new PdmsService(this.configurationMock.Object, this.loggerMock.Object, this.httpClientWrapperMock.Object, this.authenticationProviderMock.Object);
            Assert.IsNotNull(service);

            // call the service
            var result = await service.GetVariantDefinitionAsync(variantDefinitionId);

            // verify result
            Assert.IsNotNull(result);
            this.VerifyVariantDefinition(variantDefinition, result);
            this.httpClientWrapperMock.Verify(hcMock => hcMock.GetAsync<VariantDefinition>(It.IsAny<string>(), It.IsAny<Func<Task<string>>>()), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task GetVariantDefinitionThrowsIfVariantIdIsEmptyAsync()
        {
            PdmsService service = new PdmsService(this.configurationMock.Object, this.loggerMock.Object, this.httpClientWrapperMock.Object, this.authenticationProviderMock.Object);
            Assert.IsNotNull(service);

            // call the service
            await service.GetVariantDefinitionAsync(Guid.Empty);
        }

        private void VerifyVariantDefinition(VariantDefinition actual, VariantDefinition expected)
        {
            actual.Capabilities.Zip(expected.Capabilities, (act, exp) =>
            {
                Assert.AreEqual(act, exp);
                return true;
            });
            actual.DataTypes.Zip(expected.DataTypes, (act, exp) =>
            {
                Assert.AreEqual(act, exp);
                return true;
            });
            actual.SubjectTypes.Zip(expected.SubjectTypes, (act, exp) =>
            {
                Assert.AreEqual(act, exp);
                return true;
            });
        }
    }
}
