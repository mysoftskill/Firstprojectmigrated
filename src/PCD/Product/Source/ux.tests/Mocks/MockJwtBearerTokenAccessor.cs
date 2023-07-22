using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.PrivacyServices.UX.Core.Security;
using Moq;

namespace Microsoft.PrivacyServices.UX.Tests.Mocks
{
    /// <summary>
    /// Provides mock of <see cref="IJwtBearerTokenAccessor"/>.
    /// </summary>
    public static class MockJwtBearerTokenAccessor
    {
        /// <summary>
        /// Creates a new instance of <see cref="IJwtBearerTokenAccessor"/> mock.
        /// </summary>
        public static Mock<IJwtBearerTokenAccessor> Create()
        {
            var jwtTokenAccessor = new Mock<IJwtBearerTokenAccessor>(MockBehavior.Strict);
            jwtTokenAccessor.Setup(jta => jta.GetFromHttpContextAsync()).ReturnsAsync("token");

            return jwtTokenAccessor;
        }
    }
}
