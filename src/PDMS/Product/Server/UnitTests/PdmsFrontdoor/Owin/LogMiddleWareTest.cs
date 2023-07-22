namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Owin.UnitTest
{
    using System;
    using System.Linq.Expressions;
    using System.Threading.Tasks;

    using Microsoft.Owin;
    using Microsoft.PrivacyServices.DataManagement.Common.Authentication;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.WebApi;
    using Microsoft.PrivacyServices.Testing;

    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Xunit2;
    using Xunit;

    public class LogMiddleWareTest
    {
        [Theory(DisplayName = "When the response is successful, then log success."), AutoMoqData]
        public async Task VerifySuccess([Frozen] Mock<ISession> session, Fixture fixture)
        {
            // Arrange.
            fixture.Register<OwinMiddleware>(() => new MockMiddleWare());
            var middleWare = fixture.Create<LogMiddleWare>();

            var context = this.CreateContext(fixture.Create<Uri>(), 200);

            // Act.
            await middleWare.Invoke(context.Object).ConfigureAwait(false);

            // Assert.
            session.Verify(m => m.Done(SessionStatus.Success, It.IsAny<OperationMetadata>()), Times.Once);
        }

        [Theory(DisplayName = "When the response is a user error without a ServiceException, then log a simple error."), AutoMoqData]
        public async Task VerifyErrorNoExceptionData([Frozen] Mock<ISession> session, Fixture fixture)
        {
            // Arrange.
            fixture.Register<OwinMiddleware>(() => new MockMiddleWare());
            var middleWare = fixture.Create<LogMiddleWare>();

            var context = this.CreateContext(fixture.Create<Uri>(), 404);
            context.Setup(m => m.Get<ServiceException>("ServiceException")).Returns<ServiceException>(null);

            // Act.
            await middleWare.Invoke(context.Object).ConfigureAwait(false);

            // Assert.
            session.Verify(m => m.Done(SessionStatus.Error, It.IsAny<OperationMetadata>()), Times.Once);
        }

        [Theory(DisplayName = "When the response is a user error with a ServiceException, then log a complex error."), AutoMoqData]
        public async Task VerifyErrorWithExceptionData([Frozen] Mock<ISession> session, Fixture fixture)
        {
            // Arrange.
            fixture.Register<OwinMiddleware>(() => new MockMiddleWare());
            var middleWare = fixture.Create<LogMiddleWare>();

            var exception = new NullArgumentError("param");
            var context = this.CreateContext(fixture.Create<Uri>(), 404);
            context.Setup(m => m.Get<ServiceException>("ServiceException")).Returns(exception);

            // Act.
            await middleWare.Invoke(context.Object).ConfigureAwait(false);

            // Assert.
            Expression<Func<Tuple<OperationMetadata, ServiceError>, bool>> verify = value => value.Item2 == exception.ServiceError;
            session.Verify(m => m.Done(SessionStatus.Error, It.Is(verify)), Times.Once);
        }

        [Theory(DisplayName = "When the response is a service error without a ServiceException, then log a simple fault."), AutoMoqData]
        public async Task VerifyFaultNoExceptionData([Frozen] Mock<ISession> session, Fixture fixture)
        {
            // Arrange.
            fixture.Register<OwinMiddleware>(() => new MockMiddleWare());
            var middleWare = fixture.Create<LogMiddleWare>();

            var context = this.CreateContext(fixture.Create<Uri>(), 500);
            context.Setup(m => m.Get<ServiceException>("ServiceException")).Returns<ServiceException>(null);

            // Act.
            await middleWare.Invoke(context.Object).ConfigureAwait(false);

            // Assert.
            session.Verify(m => m.Done(SessionStatus.Fault, It.IsAny<OperationMetadata>()), Times.Once);
        }

        [Theory(DisplayName = "When the response is a service error with a ServiceException, then log a complex fault."), AutoMoqData]
        public async Task VerifyFaultWithExceptionData([Frozen] Mock<ISession> session, Fixture fixture)
        {
            // Arrange.
            fixture.Register<OwinMiddleware>(() => new MockMiddleWare());
            var middleWare = fixture.Create<LogMiddleWare>();

            var exception = new NullArgumentError("param");
            var context = this.CreateContext(fixture.Create<Uri>(), 500);
            context.Setup(m => m.Get<ServiceException>("ServiceException")).Returns(exception);

            // Act.
            await middleWare.Invoke(context.Object).ConfigureAwait(false);

            // Assert.
            Expression<Func<Tuple<OperationMetadata, ServiceError>, bool>> verify = value => value.Item2 == exception.ServiceError;
            session.Verify(m => m.Done(SessionStatus.Fault, It.Is(verify)), Times.Once);
        }

        [Theory(DisplayName = "When an unhandled exception occurs in the handler, then log a complex fault."), AutoMoqData]
        public async Task VerifyUnhandledException([Frozen] Mock<ISession> session, Fixture fixture)
        {
            // Arrange.
            var innerMiddleWare = new MockMiddleWare();
            innerMiddleWare.Exception = new InvalidOperationException();
            fixture.Register<OwinMiddleware>(() => innerMiddleWare);
            var middleWare = fixture.Create<LogMiddleWare>();

            var context = this.CreateContext(fixture.Create<Uri>(), 200);

            // Act.
            await Assert.ThrowsAsync<InvalidOperationException>(() => middleWare.Invoke(context.Object)).ConfigureAwait(false);

            // Assert.                        
            Expression<Func<Tuple<OperationMetadata, ServiceError>, bool>> verify = value => value.Item2.InnerError.Code == "System.InvalidOperationException";
            session.Verify(m => m.Done(SessionStatus.Fault, It.Is(verify)), Times.Once);
        }

        [Theory(DisplayName = "When request is canceled, then treat as caller error and swallow exception.")]
        [InlineAutoMoqData(typeof(TaskCanceledException), 10000, SessionStatus.Error, "RequestCanceled")]
        [InlineAutoMoqData(typeof(OperationCanceledException), 10000, SessionStatus.Error, "RequestCanceled")]
        [InlineAutoMoqData(typeof(TaskCanceledException), 1, SessionStatus.Fault, "RequestCanceled:LatencyThresholdExceeded")]
        [InlineAutoMoqData(typeof(OperationCanceledException), 1, SessionStatus.Fault, "RequestCanceled:LatencyThresholdExceeded")]
        [InlineAutoMoqData(typeof(ObjectDisposedException), 10000, SessionStatus.Error, "RequestCanceled:Disposed")]
        [InlineAutoMoqData(typeof(ObjectDisposedException), 1, SessionStatus.Fault, "RequestCanceled:LatencyThresholdExceeded:Disposed")]
        public async Task VerifyTaskCanceledException(
            Type exceptionType, 
            long maxDuration, 
            SessionStatus expectedStatus, 
            string expectedErrorCode,
            [Frozen] Mock<IOwinConfiguration> config,
            [Frozen] Mock<ISession> session, 
            IFixture fixture)
        {
            // Arrange.
            config.SetupGet(m => m.MaximumCancelationThresholdMilliseconds).Returns(maxDuration);

            var innerMiddleWare = new MockMiddleWare();
            innerMiddleWare.Exception = Activator.CreateInstance(exceptionType, string.Empty) as Exception;
            fixture.Register<OwinMiddleware>(() => innerMiddleWare);
            var middleWare = fixture.Create<LogMiddleWare>();

            var context = this.CreateContext(fixture.Create<Uri>(), 200);

            // Act.
            await middleWare.Invoke(context.Object).ConfigureAwait(false);

            // Assert.                        
            Expression<Func<Tuple<OperationMetadata, ServiceError>, bool>> verify = value => value.Item2.Code == expectedErrorCode;
            session.Verify(m => m.Done(expectedStatus, It.Is(verify)), Times.Once);
        }

        [Theory(DisplayName = "When a request is made, then use operation name from the provider."), AutoMoqData]
        public async Task VerifyOperationName(
            [Frozen] Mock<ISessionFactory> sessionFactory, 
            [Frozen] AuthenticatedPrincipal authenticatedPrincipal,           
            Fixture fixture)
        {
            // Arrange.
            var operationName = 
                fixture
                .Build<OperationName>()
                .With(m => m.IncludeInTelemetry, true)
                .Create();

            fixture.Inject(operationName); // So that it is returned by the provider.
            fixture.Register<OwinMiddleware>(() => new MockMiddleWare());
            var middleWare = fixture.Create<LogMiddleWare>();

            var context = this.CreateContext(fixture.Create<Uri>(), 200);

            // Act.
            await middleWare.Invoke(context.Object).ConfigureAwait(false);

            // Assert.
            sessionFactory.Verify(m => m.StartSession(operationName.FriendlyName, SessionType.Incoming), Times.Once);

            Assert.Equal(operationName.FriendlyName, authenticatedPrincipal.OperationName);
        }
        
        [Theory(DisplayName = "When a request is made and it should not be included in telemetry, then do not start a session."), AutoMoqData]
        public async Task VerifyExcludeFromTelemetry(
            [Frozen] Mock<ISessionFactory> sessionFactory,
            Fixture fixture)
        {
            // Arrange.
            var operationName =
                fixture
                .Build<OperationName>()
                .With(m => m.IncludeInTelemetry, false)
                .Create();

            fixture.Inject(operationName); // So that it is returned by the provider.
            fixture.Register<OwinMiddleware>(() => new MockMiddleWare());
            var middleWare = fixture.Create<LogMiddleWare>();

            var context = this.CreateContext(fixture.Create<Uri>(), 200);

            // Act.
            await middleWare.Invoke(context.Object).ConfigureAwait(false);

            // Assert.
            sessionFactory.Verify(m => m.StartSession(It.IsAny<string>(), SessionType.Incoming), Times.Never);
        }

        [Theory(DisplayName = "When a request is made with a cv header, then use the provided cv value."), AutoMoqData]
        public async Task VerifyCV(
            [Frozen] Mock<ICorrelationVector> correlationVector,
            Mock<IHeaderDictionary> headers,
            Fixture fixture)
        {
            // Arrange.
            fixture.Register<OwinMiddleware>(() => new MockMiddleWare());
            headers.Setup(m => m.Get("MS-CV")).Returns("cv");
            
            var middleWare = fixture.Create<LogMiddleWare>();
            var context = this.CreateContext(fixture.Create<Uri>(), 200, headers.Object);

            // Act.
            await middleWare.Invoke(context.Object).ConfigureAwait(false);

            // Assert.
            correlationVector.Verify(m => m.Set("cv"), Times.Once);
        }

        private Mock<IOwinContext> CreateContext(Uri requestUri, int responseCode, IHeaderDictionary headers = null)
        {
            var response = new Mock<IOwinResponse>();
            response.SetupGet(m => m.StatusCode).Returns(responseCode);

            var request = new Mock<IOwinRequest>();
            request.SetupGet(m => m.Uri).Returns(requestUri);
            request.SetupGet(m => m.Headers).Returns(headers);

            var context = new Mock<IOwinContext>();
            context.SetupGet(m => m.Response).Returns(response.Object);
            context.SetupGet(m => m.Request).Returns(request.Object);

            return context;
        }

        public class MockMiddleWare : OwinMiddleware
        {
            public MockMiddleWare() : base(null)
            {
            }

            public Exception Exception { get; set; }

            public override Task Invoke(IOwinContext context)
            {
                Action action = () =>
                {
                    if (this.Exception != null)
                    {
                        throw Exception;
                    }
                };
                return Task.Run(action);
            }
        }
    }
}