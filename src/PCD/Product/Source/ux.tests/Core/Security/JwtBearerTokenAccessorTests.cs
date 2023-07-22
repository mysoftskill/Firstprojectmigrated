using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.PrivacyServices.UX.Core.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.PrivacyServices.UX.Tests.Core.Security
{
    [TestClass]
    public class JwtBearerTokenAccessorTests
    {
        private Mock<IHttpContextAccessor> mockHttpContextAccessor;

        private Mock<IAuthenticationService> mockAuthService;

        [TestInitialize]
        public void Initialize()
        {
            mockAuthService = new Mock<IAuthenticationService>(MockBehavior.Strict);

            var serviceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
            serviceProvider
                .Setup(sp => sp.GetService(It.Is<Type>(t => t == typeof(IAuthenticationService))))
                .Returns(mockAuthService.Object);

            var mockHttpContext = new Mock<HttpContext>(MockBehavior.Strict);
            mockHttpContext
                .SetupGet(ctx => ctx.RequestServices)
                .Returns(serviceProvider.Object);

            mockHttpContextAccessor = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
            mockHttpContextAccessor
                .SetupGet(ctx => ctx.HttpContext)
                .Returns(mockHttpContext.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Ctor_Throws_ArgumentNull()
        {
            new JwtBearerTokenAccessor(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetFromHttpContextAsync_Throws_ArgumentNull()
        {
            var accessor = new JwtBearerTokenAccessor(mockHttpContextAccessor.Object);
            accessor.GetFromHttpContextAsync(null);
        }

        [TestMethod]
        public void JwtBearerTokenAccessor_UsesWellKnownTokenName()
        {
            //  This is a well-known bearer token name, do not change it.
            Assert.AreEqual("access_token", JwtBearerTokenAccessor.BearerTokenName);
        }

        [TestMethod]
        public async Task GetFromHttpContextAsync_DefaultAuthenticationScheme()
        {
            var token = "{40EE6AF4-9426-4848-BF4D-92EBE2715002}";

            mockAuthService
                .Setup(svc => svc.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
                .Returns(AuthenticateResultForToken(token, string.Empty));

            var accessor = new JwtBearerTokenAccessor(mockHttpContextAccessor.Object);

            var valueFromContext = await accessor.GetFromHttpContextAsync();
            Assert.AreEqual(token, valueFromContext);
        }

        [TestMethod]
        public async Task GetFromHttpContextAsync_CustomAuthenticationScheme()
        {
            var authScheme = "{34E27EEB-9EE6-482F-BB95-9157E7832BF9}";
            var token = "{506BA9BE-6D47-4FAE-96AD-2DB9E739F8E2}";

            mockAuthService
                .Setup(svc => svc.AuthenticateAsync(It.IsAny<HttpContext>(), It.Is<string>(scheme => scheme == authScheme)))
                .Returns(AuthenticateResultForToken(token, authScheme));

            var accessor = new JwtBearerTokenAccessor(mockHttpContextAccessor.Object);

            var valueFromContext = await accessor.GetFromHttpContextAsync(authScheme);
            Assert.AreEqual(token, valueFromContext);
        }

        /// <summary>
        /// Creates an instance of <see cref="AuthenticateResult"/> for access token.
        /// </summary>
        /// <param name="accessToken">Access token to store in result.</param>
        /// <param name="authScheme">Authentication scheme that was requested.</param>
        private static Task<AuthenticateResult> AuthenticateResultForToken(string accessToken, string authScheme)
        {
            var props = new Dictionary<string, string>
            {
                { ".Token.access_token", accessToken }
            };

            var authenticationTiket = new AuthenticationTicket(
                new ClaimsPrincipal(),
                new AuthenticationProperties(props),
                authScheme);

            return Task.FromResult(AuthenticateResult.Success(authenticationTiket));
        }
    }
}
