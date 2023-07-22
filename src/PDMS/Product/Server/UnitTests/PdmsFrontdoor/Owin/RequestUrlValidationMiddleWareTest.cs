namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Owin.UnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Owin;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.Testing;

    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Xunit2;
    using Xunit;

    public class RequestUrlValidationMiddleWareTest
    {
        [Theory(DisplayName = "When a request is made with different URLs, the next middle ware should be called or skipped depending on if the URL is supported or not.")]
        [InlineAutoMoqData("https://www.foo.com/api/v2/gooduri", true)]
        [InlineAutoMoqData("https://www.foo.com/probe", true)]
        [InlineAutoMoqData("https://www.foo.com/baduri", false)]
        [InlineAutoMoqData("https://www.foo.com/apinotsupported", false)]
        public async Task VerifyRequestWithDifferentUrls(
            string uri,
            bool expectedValue,
            [Frozen] Mock<IOwinConfiguration> owinConfig,
            Fixture fixture)
        {
            // Arrange.
            var mockedMiddleWare = new MockMiddleWare();
            fixture.Register<OwinMiddleware>(() => mockedMiddleWare);
            owinConfig.Setup(m => m.ValidRequestUrls).Returns(new List<string> { @"^\/api\/v[1-9]\/.*$", @"^\/probe$" });
            var middleWare = fixture.Create<RequestUrlValidationMiddleWare>();
            var context = this.CreateContext(new Uri(uri), 200);

            // Act.
            await middleWare.Invoke(context.Object).ConfigureAwait(false);

            // Assert.
            Assert.Equal(200, context.Object.Response.StatusCode);
            Assert.Equal(expectedValue, mockedMiddleWare.IsCalled);
        }

        [Theory(DisplayName = "When a non-https url is provided, then terminate the call early.")]
        [InlineAutoMoqData("http://www.foo.com/api", false)]
        [InlineAutoMoqData("http://www.foo.com/probe", true)]
        [InlineAutoMoqData("https://www.foo.com/api", true)]
        public async Task VerifyHstsHandler(
            string uri,
            bool expectedValue,
            Fixture fixture)
        {
            // Arrange.
            var mockedMiddleWare = new MockMiddleWare();
            fixture.Register<OwinMiddleware>(() => mockedMiddleWare);
            var middleWare = fixture.Create<HstsMiddleWare>();
            var context = this.CreateContext(new Uri(uri), 200);

            // Act.
            await middleWare.Invoke(context.Object).ConfigureAwait(false);

            // Assert.
            Assert.Equal(200, context.Object.Response.StatusCode);
            Assert.Equal(expectedValue, mockedMiddleWare.IsCalled);
        }

        private Mock<IOwinContext> CreateContext(Uri requestUri, int responseCode)
        {
            var response = new Mock<IOwinResponse>();
            response.SetupGet(m => m.StatusCode).Returns(responseCode);

            var request = new Mock<IOwinRequest>();
            request.SetupGet(m => m.Uri).Returns(requestUri);

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

            public bool IsCalled { get; set; }

            public override Task Invoke(IOwinContext context)
            {
                Action action = () =>
                {
                    this.IsCalled = true;
                };

                return Task.Run(action);
            }
        }
    }
}