// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.UnitTests.Filters
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;
    using System.Web.Http.Controllers;
    using System.Web.Http.Filters;
    using System.Web.Http.Hosting;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.Helpers;
    using Microsoft.Membership.MemberServices.PrivacyAdapters;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Filters;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common;
    using Microsoft.Membership.MemberServices.Test.Common;
    using Microsoft.OData.Client;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Newtonsoft.Json;

    using Microsoft.PrivacyServices.Common.Azure;

    [TestClass]
    public class UnhandledExceptionFilterTest
    {
        [TestMethod]
        public async Task UnhandledExceptionFilterHandlesPxfAdapterException_PdpSearchHistoryError()
        {
            PxfAdapterException exception = new PxfAdapterException(
                Guid.NewGuid().ToString(),
                PrivacySourceId.PdpSearchHistory,
                AdapterErrorCode.PdpSearchHistoryBotDetection,
                400,
                "pdp specific diagnostic header info here",
                new DataServiceQueryException("this is a DataServiceQueryException from PDP"));
            Error expectedError = new Error(ErrorCode.TooManyRequests, exception.ToString());
            HttpStatusCode expectedStatusCode = (HttpStatusCode)429;

            await ValidateUnhandledExceptionFilter(exception, expectedStatusCode, expectedError);
        }

        [TestMethod]
        public async Task UnhandledExceptionFilterHandlesPxfAdapterException()
        {
            PxfAdapterException exception = new PxfAdapterException("UnitTest", "UnitTestPartnerName", 429, "This is a partner 429 error");
            Error expectedError = new Error(ErrorCode.TooManyRequests, exception.ToString());
            HttpStatusCode expectedStatusCode = (HttpStatusCode)429;

            await ValidateUnhandledExceptionFilter(exception, expectedStatusCode, expectedError);
        }

        [TestMethod]
        public async Task UnhandledExceptionFilterHandlesOperationCanceledException()
        {
            OperationCanceledException exception = new OperationCanceledException("this is a task canceled exception");
            Error expectedError = new Error(ErrorCode.PartnerTimeout, exception.ToString());
            HttpStatusCode expectedStatusCode = HttpStatusCode.GatewayTimeout;

            await ValidateUnhandledExceptionFilter(exception, expectedStatusCode, expectedError);
        }

        [TestMethod]
        public async Task UnhandledExceptionFilterReturnsResponseWithException()
        {
            Exception exception = new Exception("This is an exception");
            Error expectedError = new Error(ErrorCode.Unknown, exception.ToString());
            HttpStatusCode expectedStatusCode = HttpStatusCode.InternalServerError;

            await ValidateUnhandledExceptionFilter(exception, expectedStatusCode, expectedError);
        }

        private static async Task ValidateUnhandledExceptionFilter(Exception exception, HttpStatusCode expectedStatusCode, Error expectedError)
        {
            // Create HttpActionContext with request for the test
            Mock<HttpActionDescriptor> httpActionDescriptor = new Mock<HttpActionDescriptor> { CallBase = true };
            HttpRequestMessage request = new HttpRequestMessage();
            request.Properties.Add(HttpPropertyKeys.HttpConfigurationKey, new HttpConfiguration());
            HttpActionContext httpActionContext = new HttpActionContext(new HttpControllerContext(), httpActionDescriptor.Object);
            httpActionContext.ControllerContext.Request = request;
            HttpActionExecutedContext context = new HttpActionExecutedContext(httpActionContext, exception);

            // Create exception filter and execute the test
            Mock<ILogger> mockLogger = TestMockFactory.CreateLogger();
            UnhandledExceptionFilterAttribute filter = new UnhandledExceptionFilterAttribute(mockLogger.Object);
            filter.OnException(context);

            Assert.IsNotNull(context.Response);
            Assert.AreEqual(expectedStatusCode, context.Response.StatusCode);

            string responseContentValue = await context.Response.Content.ReadAsStringAsync();
            Error result = await Task.Run(() => JsonConvert.DeserializeObject<Error>(responseContentValue));

            Assert.IsNotNull(result);
            EqualityHelper.AreEqual(expectedError, result);
        }
    }
}