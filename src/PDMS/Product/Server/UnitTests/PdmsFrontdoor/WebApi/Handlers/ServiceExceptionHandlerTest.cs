namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi.UnitTest
{
    using System;
    using System.Linq.Expressions;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http;
    using System.Web.Http.ExceptionHandling;

    using Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi.Handlers;
    using Microsoft.PrivacyServices.Testing;

    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Xunit2;
    using Xunit;

    public class ServiceExceptionHandlerTest
    {
        [Theory(DisplayName = "When a generic exception is caught then convert to ServiceFault and store it on the RequestContextFactory"), AutoMoqData]
        public void VerifyGenericExceptionStoredOnContext([Frozen] Mock<IRequestContext> context, Mock<IRequestContextFactory> contextFactory, Exception exn)
        {
            // Arrange
            var handlerContext = this.CreateHandlerContext(exn);
            var exnHandler = new ServiceExceptionHandler(contextFactory.Object);

            // Act
            exnHandler.Handle(handlerContext);

            // Assert
            contextFactory.Verify(m => m.Create(handlerContext.Request), Times.Once());

            Expression<Func<ServiceException, bool>> verify = s => 
                s is ServiceFault && 
                s.ServiceError.InnerError.Code == "System.Exception";

            context.Verify(m => m.Set("ServiceException", It.Is<ServiceException>(verify)), Times.Once());
        }

        [Theory(DisplayName = "When a service exception is caught then store it on the RequestContextFactory"), AutoMoqData]
        public void VerifyServiceExceptionStoredOnContext([Frozen] Mock<IRequestContext> context, Mock<IRequestContextFactory> contextFactory, Fixture fixture)
        {
            // Arrange
            fixture.DisableRecursionCheck();
            var exn = fixture.Create<ServiceException>();

            var handlerContext = this.CreateHandlerContext(exn);
            var exnHandler = new ServiceExceptionHandler(contextFactory.Object);
            
            // Act
            exnHandler.Handle(handlerContext);

            // Assert
            contextFactory.Verify(m => m.Create(handlerContext.Request), Times.Once());
            context.Verify(m => m.Set("ServiceException", exn), Times.Once());
        }

        [Theory(DisplayName = "When an exception is caught then return its status code and service error"), AutoMoqData]
        public async Task VerifyResponseData(IRequestContextFactory contextFactory, Fixture fixture)
        {
            // Arrange
            fixture.DisableRecursionCheck();
            var exn = fixture.Create<ServiceException>();

            var handlerContext = this.CreateHandlerContext(exn);
            var exnHandler = new ServiceExceptionHandler(contextFactory);

            // Act
            exnHandler.Handle(handlerContext);
            var response = await handlerContext.Result.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Equal(exn.StatusCode, response.StatusCode);
            ResponseError result;
            response.TryGetContentValue(out result);
            Assert.Equal(exn.ServiceError, result.Error);
        }

        private ExceptionHandlerContext CreateHandlerContext(Exception exn)
        {
            // Bare minimum to create a request message.
            var request = new HttpRequestMessage();
            request.SetConfiguration(new HttpConfiguration());

            // Bare minimum to create an ExceptionHandlerContext.
            var catchBlock = new ExceptionContextCatchBlock(string.Empty, true, false);
            var exceptionContext = new ExceptionContext(exn, catchBlock, request);
            var handlerContext = new ExceptionHandlerContext(exceptionContext);

            return handlerContext;
        }
    }
}
