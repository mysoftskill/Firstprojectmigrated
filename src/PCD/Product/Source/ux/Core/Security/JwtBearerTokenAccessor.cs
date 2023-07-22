using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Osgs.Core.Helpers;

namespace Microsoft.PrivacyServices.UX.Core.Security
{
    /// <summary>
    /// Provides access to JWT bearer token within current HTTP context.
    /// Implementation of <see cref="IJwtBearerTokenAccessor"/>.
    /// </summary>
    public class JwtBearerTokenAccessor : IJwtBearerTokenAccessor
    {
        /// <summary>
        /// Name of the token. Used for unit tests.
        /// </summary>
        /// <remarks>
        /// This is a well-known bearer token name, do not change it.
        /// </remarks>
        internal const string BearerTokenName = "access_token";

        private readonly IHttpContextAccessor httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtBearerTokenAccessor"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">HTTP context accessor.</param>
        public JwtBearerTokenAccessor(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        #region IBearerTokenAccessor Members

        public Task<string> GetFromHttpContextAsync()
        {
            return httpContextAccessor.HttpContext.GetTokenAsync(BearerTokenName);
        }

        public Task<string> GetFromHttpContextAsync(string authenticationScheme)
        {
            EnsureArgument.NotNullOrEmpty(authenticationScheme, nameof(authenticationScheme));

            return httpContextAccessor.HttpContext.GetTokenAsync(authenticationScheme, BearerTokenName);
        }

        #endregion
    }
}
