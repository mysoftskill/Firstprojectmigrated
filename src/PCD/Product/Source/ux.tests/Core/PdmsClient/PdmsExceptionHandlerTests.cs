using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Osgs.Infra.Monitoring.AspNetCore;
using Microsoft.PrivacyServices.DataManagement.Client;
using Microsoft.PrivacyServices.DataManagement.Client.V2;
using Microsoft.PrivacyServices.Identity;
using Microsoft.PrivacyServices.UX.Core.PdmsClient;
using Microsoft.PrivacyServices.UX.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PdmsClientBaseException = Microsoft.PrivacyServices.DataManagement.Client.BaseException;

namespace Microsoft.PrivacyServices.UX.Tests.Core.PdmsClient
{
    [TestClass]
    public class PdmsExceptionHandlerTests
    {
        private ExceptionContext context;

        private PdmsClientExceptionHandler exceptionHandler;

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
            exceptionHandler = new PdmsClientExceptionHandler();
            errorResult = new AjaxErrorFilter.JsonErrorModel();
        }

        [TestMethod]
        public void HandlePdmsClientBaseException_NotAuthenticatedError()
        {
            // Arrange
            context.Exception = CreatePdmsException<NotAuthenticatedError>();

            // Act
            var response = exceptionHandler.HandleAjaxError(context, errorResult);

            // Assert
            Assert.IsTrue(response);
            Assert.AreEqual("notAuthenticated", errorResult.ErrorCode);
            Assert.AreEqual(System.Net.HttpStatusCode.Unauthorized, errorResult.HttpStatusCode);
        }

        [TestMethod]
        public void HandlePdmsClientBaseException_NotAuthorizedError_User_SecurityGroup()
        {
            // Arrange
            context.Exception = CreatePdmsException<NotAuthorizedError.User.SecurityGroup>();

            // Act
            var response = exceptionHandler.HandleAjaxError(context, errorResult);

            // Assert
            Assert.IsTrue(response);
            Assert.AreEqual("badSecurityGroups", errorResult.ErrorCode);
            Assert.AreEqual(System.Net.HttpStatusCode.Forbidden, errorResult.HttpStatusCode);
        }

        [TestMethod]
        public void HandlePdmsClientBaseException_NotAuthorizedError_User_ServiceTree()
        {
            // Arrange
            context.Exception = CreatePdmsException<NotAuthorizedError.User.ServiceTree>();

            // Act
            var response = exceptionHandler.HandleAjaxError(context, errorResult);

            // Assert
            Assert.IsTrue(response);
            Assert.AreEqual("badServiceTree", errorResult.ErrorCode);
            Assert.AreEqual(System.Net.HttpStatusCode.Forbidden, errorResult.HttpStatusCode);
        }

        [TestMethod]
        public void HandlePdmsClientBaseException_NotAuthorizedError_User_Non_ServiceViewer()
        {
            // Arrange
            context.Exception = CreatePdmsException<NotAuthorizedError.User>();

            // Act
            var response = exceptionHandler.HandleAjaxError(context, errorResult);

            // Assert
            Assert.IsTrue(response);
            Assert.AreEqual("notAuthorized", errorResult.ErrorCode);
            Assert.AreEqual(System.Net.HttpStatusCode.Forbidden, errorResult.HttpStatusCode);
        }

        [TestMethod]
        public void HandlePdmsClientBaseException_NotAuthorizedError_User_ServiceViewer()
        {
            // Arrange
            context.Exception = CreatePdmsException<NotAuthorizedError.User>()
                                    .WithProperty("Role", "ServiceViewer");

            // Act
            var response = exceptionHandler.HandleAjaxError(context, errorResult);

            // Assert
            Assert.IsTrue(response);
            Assert.AreEqual("notAuthorized_serviceViewer", errorResult.ErrorCode);
            Assert.AreEqual(System.Net.HttpStatusCode.Forbidden, errorResult.HttpStatusCode);
        }

        [TestMethod]
        public void HandlePdmsClientBaseException_ExpiredError_ETagMismatch()
        {
            // Arrange
            var expectedValue = "valueString";
            context.Exception = CreatePdmsException<ExpiredError.ETagMismatch>()
                                    .WithProperty("Value", expectedValue);

            // Act
            var response = exceptionHandler.HandleAjaxError(context, errorResult);

            // Assert
            Assert.IsTrue(response);
            Assert.AreEqual("eTagMismatch", errorResult.ErrorCode);
            Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, errorResult.HttpStatusCode);
            Assert.AreEqual(expectedValue, errorResult.Data["value"]);
        }

        [TestMethod]
        public void HandlePdmsClientBaseException_BadArgumentError_InvalidArgument_UnsupportedCharacter()
        {
            // Arrange
            var expectedTarget = "targetString";
            context.Exception = CreatePdmsException<BadArgumentError.InvalidArgument.UnsupportedCharacter>()
                                    .WithProperty("Target", expectedTarget);

            // Act
            var response = exceptionHandler.HandleAjaxError(context, errorResult);

            // Assert
            Assert.IsTrue(response);
            Assert.AreEqual("invalidCharacter", errorResult.ErrorCode);
            Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, errorResult.HttpStatusCode);
            Assert.AreEqual(expectedTarget, errorResult.Data["target"]);
        }

        [TestMethod]
        public void HandlePdmsClientBaseException_BadArgumentError_InvalidArgument()
        {
            // Arrange
            var expectedTarget = "targetString";
            context.Exception = CreatePdmsException<BadArgumentError.InvalidArgument>()
                                    .WithProperty("Target", expectedTarget);

            // Act
            var response = exceptionHandler.HandleAjaxError(context, errorResult);

            // Assert
            Assert.IsTrue(response);
            Assert.AreEqual("invalidInput", errorResult.ErrorCode);
            Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, errorResult.HttpStatusCode);
            Assert.AreEqual(expectedTarget, errorResult.Data["target"]);
        }

        [TestMethod]
        public void HandlePdmsClientBaseException_ConflictError_AlreadyExists_ClaimedByOwner()
        {
            // Arrange
            var expectedTarget = "targetString";
            var expectedOwnerId = "ownerIdString";

            context.Exception = CreatePdmsException<ConflictError.AlreadyExists.ClaimedByOwner>()
                                    .WithProperty("Target", expectedTarget)
                                    .WithProperty("OwnerId", expectedOwnerId);

            // Act
            var response = exceptionHandler.HandleAjaxError(context, errorResult);

            // Assert
            Assert.IsTrue(response);
            Assert.AreEqual("alreadyExists", errorResult.ErrorCode);
            Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, errorResult.HttpStatusCode);
            Assert.AreEqual(expectedTarget, errorResult.Data["target"]);
            Assert.AreEqual(expectedOwnerId, errorResult.Data["ownerId"]);
        }

        [TestMethod]
        public void HandlePdmsClientBaseException_ConflictError_AlreadyExists()
        {
            // Arrange
            var expectedTarget = "targetString";
            context.Exception = CreatePdmsException<ConflictError.AlreadyExists>()
                                    .WithProperty("Target", expectedTarget);

            // Act
            var response = exceptionHandler.HandleAjaxError(context, errorResult);

            // Assert
            Assert.IsTrue(response);
            Assert.AreEqual("alreadyExists", errorResult.ErrorCode);
            Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, errorResult.HttpStatusCode);
            Assert.AreEqual(expectedTarget, errorResult.Data["target"]);
        }

        [TestMethod]
        public void HandlePdmsClientBaseException_ConflictError_DoesNotExist_Non_Icm()
        {
            // Arrange
            var expectedTarget = "targetString";
            context.Exception = CreatePdmsException<ConflictError.DoesNotExist>()
                                    .WithProperty("Target", expectedTarget);

            // Act
            var response = exceptionHandler.HandleAjaxError(context, errorResult);

            // Assert
            Assert.IsTrue(response);
            Assert.AreEqual("doesNotExist", errorResult.ErrorCode);
            Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, errorResult.HttpStatusCode);
            Assert.AreEqual(expectedTarget, errorResult.Data["target"]);
        }

        [TestMethod]
        public void HandlePdmsClientBaseException_ConflictError_DoesNotExist_Icm()
        {
            // Arrange
            var expectedTarget = "icm.connectorId";
            context.Exception = CreatePdmsException<ConflictError.DoesNotExist>()
                                    .WithProperty("Target", "icm.connectorId");

            // Act
            var response = exceptionHandler.HandleAjaxError(context, errorResult);

            // Assert
            Assert.IsTrue(response);
            Assert.AreEqual("doesNotExist_icm", errorResult.ErrorCode);
            Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, errorResult.HttpStatusCode);
            Assert.AreEqual(expectedTarget, errorResult.Data["target"]);
        }

        [TestMethod]
        public void HandlePdmsClientBaseException_ConflictError_InvalidValue_Immutable()
        {
            // Arrange
            context.Exception = CreatePdmsException<ConflictError.InvalidValue.Immutable>();

            // Act
            var response = exceptionHandler.HandleAjaxError(context, errorResult);

            // Assert
            Assert.IsTrue(response);
            Assert.AreEqual("immutableValue", errorResult.ErrorCode);
            Assert.AreEqual(System.Net.HttpStatusCode.Conflict, errorResult.HttpStatusCode);
        }

        [TestMethod]
        public void HandlePdmsClientBaseException_ConflictError_LinkedEntityExists()
        {
            // Arrange
            context.Exception = CreatePdmsException<ConflictError.LinkedEntityExists>();

            // Act
            var response = exceptionHandler.HandleAjaxError(context, errorResult);

            // Assert
            Assert.IsTrue(response);
            Assert.AreEqual("hasDependentEntity", errorResult.ErrorCode);
            Assert.AreEqual(System.Net.HttpStatusCode.Conflict, errorResult.HttpStatusCode);
        }

        [TestMethod]
        public void HandlePdmsClientBaseException_ConflictError_PendingCommandsExists()
        {
            // Arrange
            context.Exception = CreatePdmsException<ConflictError.PendingCommandsExists>();

            // Act
            var response = exceptionHandler.HandleAjaxError(context, errorResult);

            // Assert
            Assert.IsTrue(response);
            Assert.AreEqual("hasPendingCommands", errorResult.ErrorCode);
            Assert.AreEqual(System.Net.HttpStatusCode.Conflict, errorResult.HttpStatusCode);
        }

        [TestMethod]
        public void HandlePdmsClientBaseException_NotFoundError()
        {
            // Arrange
            context.Exception = CreatePdmsException<NotFoundError>();

            // Act
            var response = exceptionHandler.HandleAjaxError(context, errorResult);

            // Assert
            Assert.IsTrue(response);
            Assert.AreEqual("notFound", errorResult.ErrorCode);
            Assert.AreEqual(System.Net.HttpStatusCode.NotFound, errorResult.HttpStatusCode);
        }

        [TestMethod]
        public void HandlePdmsClientBaseException_NotFoundError_ServiceTree()
        {
            // Arrange
            context.Exception = new DataManagement.Client.ServiceTree.NotFoundError(new Guid());

            // Act
            var response = exceptionHandler.HandleAjaxError(context, errorResult);

            // Assert
            Assert.IsTrue(response);
            Assert.AreEqual("notFound", errorResult.ErrorCode);
            Assert.AreEqual(System.Net.HttpStatusCode.NotFound, errorResult.HttpStatusCode);
        }

        [TestMethod]
        public void HandlePdmsClientBaseException_Returns_False()
        {
            context.Exception = CreatePdmsException<FakeException>();

            Assert.IsFalse(exceptionHandler.HandleAjaxError(context, null));
        }

        [TestMethod]
        public void HandleArgumentValidationException()
        {
            // Arrange
            var expectedParamName = "paramNameString";
            var expectedMessage = "messageString";

            context.Exception = new ArgumentValidationException(expectedParamName, expectedMessage);

            // Act
            var response = exceptionHandler.HandleAjaxError(context, errorResult);

            // Assert
            Assert.IsTrue(response);
            Assert.AreEqual("invalidInput", errorResult.ErrorCode);
            Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, errorResult.HttpStatusCode);
            Assert.AreEqual(expectedParamName, errorResult.Data["target"]);
        }

        [TestMethod]
        public void HandleAjaxError_Returns_False()
        {
            context.Exception = new Exception();

            Assert.IsFalse(exceptionHandler.HandleAjaxError(context, null));
        }

        private T CreatePdmsException<T>()
        {
            return (T)FormatterServices.GetUninitializedObject(typeof(T));
        }
    }

    class FakeException : PdmsClientBaseException
    {
        public FakeException(IHttpResult result) : base(result) { }
    }
}
