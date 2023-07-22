// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyMockService.Controllers.AadLogin
{
    using System;
    using System.Collections.Generic;
    using System.IdentityModel.Tokens;
    using System.IdentityModel.Tokens.Jwt;
    using System.Net.Http.Formatting;
    using System.Security.Claims;
    using System.Web.Http;
    using System.Web.Http.Description;

    /// <summary>
    ///     controller for mock AAD login endpoint
    /// </summary>
    public class AadLoginController : ApiController
    {
        /// <summary>
        ///     Accepts the login request and returns a token
        /// </summary>
        /// <param name="body">request body</param>
        /// <returns>result containing the requested token</returns>
        [HttpPost]
        [ResponseType(typeof(LoginResponse))]
        [Route("aadtoken/login")]
        public IHttpActionResult PostAsync(FormDataCollection body)
        {
            JwtSecurityToken input = this.GetInputToken(body);
            DateTime now = DateTime.UtcNow;

            List<Claim> claims = new List<Claim>
            {
                // issuer
                new Claim("iss", "PXS-FAKE-TOKEN-SOURCE"),

                // tenantId
                new Claim("tid", Guid.NewGuid().ToString("D")),

                // appId
                new Claim("appid", input?.Issuer ?? "UNKNOWN"),

                // not valid before
                new Claim(
                    "nbf", input?.Payload?.Iat?.ToString() ?? AadLoginController.ToTokenEpochTime(now.AddMinutes(-1)).ToString()),

                // expiry
                new Claim(
                    "exp", input?.Payload?.Exp?.ToString() ?? AadLoginController.ToTokenEpochTime(now.AddDays(1)).ToString()),
            };

            JwtSecurityToken output = new JwtSecurityToken(claims: claims);

            return this.Ok(
                new LoginResponse { AccessToken = output.EncodedHeader + "." + output.EncodedPayload + "." });
        }

        /// <summary>
        ///     Gets the input token from a form-encoded request body
        /// </summary>
        /// <param name="body">request body</param>
        /// <returns>resulting value</returns>
        private JwtSecurityToken GetInputToken(FormDataCollection body)
        {
            string token = body.Get("request");
            return token != null ? new JwtSecurityToken(token) : null;
        }

        /// <summary>
        ///     Gets the unix time from a given date
        /// </summary>
        /// <param name="date">date to get unix time for</param>
        /// <returns>unix time</returns>
        public static long ToTokenEpochTime(DateTime date)
        {
            return Convert.ToInt64((date - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds);
        }
    }
}
