using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Osgs.Infra.Monitoring.AspNetCore;
using Microsoft.PrivacyServices.PrivacyOperation.Client;
using Microsoft.PrivacyServices.PrivacyOperation.Contracts;
using Microsoft.PrivacyServices.PrivacyOperation.Contracts.PrivacySubject;
using Microsoft.PrivacyServices.UX.Core.PxsClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.PrivacyServices.UX.Tests.Core.PxsClient
{
    [TestClass]
    public class PxsExceptionHandlerTests
    {
        private ExceptionContext context;

        private PxsClientExceptionHandler exceptionHandler;

        private AjaxErrorFilter.JsonErrorModel errorResult;

        [TestInitialize]
        public void Initialize()
        {
            var mockHttpContext = new Mock<HttpContext>(MockBehavior.Strict);
            var actionContext = new ActionContext(
                mockHttpContext.Object,
                new AspNetCore.Routing.RouteData(),
                new AspNetCore.Mvc.Abstractions.ActionDescriptor());

            context = new ExceptionContext(actionContext, new List<IFilterMetadata>());
            exceptionHandler = new PxsClientExceptionHandler();
            errorResult = new AjaxErrorFilter.JsonErrorModel();
        }

        [TestMethod]
        public void PrivacySubjectInvalidException()
        {
            // Arrange
            var expectedPropertyName = "propertyNameString";
            context.Exception = new PrivacySubjectInvalidException(expectedPropertyName);

            // Act
            var response = exceptionHandler.HandleAjaxError(context, errorResult);

            // Assert
            Assert.IsTrue(response);
            Assert.AreEqual("invalidInput", errorResult.ErrorCode);
            Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, errorResult.HttpStatusCode);
            Assert.AreEqual(expectedPropertyName, errorResult.Data["target"]);
        }

        [TestMethod]
        public void PrivacySubjectIncompleteException()
        {
            // Arrange
            context.Exception = new PrivacySubjectIncompleteException();

            // Act
            var response = exceptionHandler.HandleAjaxError(context, errorResult);

            // Assert
            Assert.IsTrue(response);
            Assert.AreEqual("incompleteInput", errorResult.ErrorCode);
            Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, errorResult.HttpStatusCode);
        }

        [TestMethod]
        public void PrivacyOperationClientException_TimeWindowExpired()
        {
            // Arrange
            context.Exception = new PrivacyOperationClientException(
                new Error(ErrorCode.TimeWindowExpired, "TimeWindowExpired test"));

            // Act
            var response = exceptionHandler.HandleAjaxError(context, errorResult);

            // Assert
            Assert.IsTrue(response);
            Assert.AreEqual("expiredMsaProxyTicket", errorResult.ErrorCode);
            Assert.AreEqual(System.Net.HttpStatusCode.Forbidden, errorResult.HttpStatusCode);
            Assert.AreEqual("ProxyTicket", errorResult.Data["target"]);
        }

        [TestMethod]
        public void PrivacyOperationClientException_InvalidClientCredentials()
        {
            // Arrange
            context.Exception = new PrivacyOperationClientException(
                new Error(ErrorCode.InvalidClientCredentials, "InvalidClientCredentials test"));

            // Act
            var response = exceptionHandler.HandleAjaxError(context, errorResult);

            // Assert
            Assert.IsTrue(response);
            Assert.AreEqual("invalidMsaProxyTicket", errorResult.ErrorCode);
            Assert.AreEqual(System.Net.HttpStatusCode.Forbidden, errorResult.HttpStatusCode);
            Assert.AreEqual("ProxyTicket", errorResult.Data["target"]);
        }

        [TestMethod]
        public void PrivacyOperationClientException_Unauthorized()
        {
            // Arrange
            context.Exception = new PrivacyOperationClientException(new Error(ErrorCode.Unauthorized, "Unauthorized test"));

            // Act
            var response = exceptionHandler.HandleAjaxError(context, errorResult);

            // Assert
            Assert.IsTrue(response);
            Assert.AreEqual("notAuthorized", errorResult.ErrorCode);
            Assert.AreEqual(System.Net.HttpStatusCode.Forbidden, errorResult.HttpStatusCode);
        }

        [TestMethod]
        public void PrivacyOperationClientException_Returns_False()
        {
            // Arrange
            context.Exception = new PrivacyOperationClientException(new Error());

            // Act
            var response = exceptionHandler.HandleAjaxError(context, errorResult);

            // Assert
            Assert.IsFalse(response);
        }

        [TestMethod]
        public void HandleAjaxError_Returns_False()
        {
            context.Exception = new Exception();
            Assert.IsFalse(exceptionHandler.HandleAjaxError(context, null));
        }
    }
}
