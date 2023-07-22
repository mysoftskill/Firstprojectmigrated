using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Authentication;

namespace Microsoft.PrivacyServices.UX.Core.Security
{
    /// <summary>
    /// Provides access to JWT bearer token within current HTTP context.
    /// </summary>
    /// <remarks>
    /// The token name in <see cref="AuthenticationProperties"/> is always assumed to be "access_token", which is saved 
    /// there by Microsoft.AspNetCore.Authentication.JwtBearer at the time of the acquisition.
    /// </remarks>
    public interface IJwtBearerTokenAccessor
    {
        /// <summary>
        /// Gets JWT bearer token from HTTP context.
        /// </summary>
        Task<string> GetFromHttpContextAsync();

        /// <summary>
        /// Gets JWT bearer token from HTTP context.
        /// </summary>
        /// <param name="authenticationScheme">Name of the authentication scheme that acquired the token.</param>
        Task<string> GetFromHttpContextAsync(string authenticationScheme);
    }
}
