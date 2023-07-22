// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.AadAuthentication.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;

    using Microsoft.IdentityModel.S2S;
    using Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class AadS2SAuthResultTest
    {
        [TestMethod]
        public void AadS2SAuthResultConstructorNullS2SResult()
        {
            //Act
            AadS2SAuthResult result = new AadS2SAuthResult(null, "accessToken");

            //Assert
            Assert.IsNotNull(result.Exception);
            Assert.IsTrue(result.Exception is ArgumentNullException);
        }

        [TestMethod]
        public void CheckAccessTokenIsProperlyDecoded()
        {
            var tenantId = Guid.NewGuid();
            var objectId = Guid.NewGuid();
            var appDisplayName = "appDisplayName";
            var upn = "username@microsoft.com";

            var claims = new List<Claim>
            {
                new Claim("tid", tenantId.ToString()),
                new Claim("oid", objectId.ToString()),
                new Claim("app_displayname", appDisplayName),
                new Claim("upn", upn)
            };

            // Create a new JWTSecurityToken with the claims
            var jwtToken = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddHours(1)
            );
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var tokenString = jwtTokenHandler.WriteToken(jwtToken);

            AadS2SAuthResult result = new AadS2SAuthResult(new S2SAuthenticationResult(), tokenString);
            
            //Assert the token string was successfully deciphered and all the the claims were set correctly
            Assert.AreEqual(tenantId, result.TenantId);
            Assert.AreEqual(objectId, result.ObjectId);
            Assert.AreEqual(appDisplayName, result.AppDisplayName);
            Assert.AreEqual(upn, result.UserPrincipalName);
        }

        [TestMethod]
        public void AadS2SAuthResultConstructorSuccess()
        {
            //Act
            var result = new AadS2SAuthResult(this.GenerateS2SResult("62e90394-69f5-4237-9190-012177145e10"), "accessToken");

            //Assert
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Exception);
            Assert.AreEqual("accessToken", result.AccessToken);
        }

        [TestMethod]
        public void AadS2SAuthResultConstructorSuccess_NotAuthorizedForPrivacyOperations()
        {
            //Act
            var result = new AadS2SAuthResult(this.GenerateS2SResult(Guid.NewGuid().ToString()), "accessToken");

            //Assert
            Assert.IsTrue(result.Succeeded);
            Assert.IsNull(result.Exception);
            Assert.AreEqual("accessToken", result.AccessToken);
        }

        [TestMethod]
        [DataRow("wids")]
        [DataRow("oid")]
        [DataRow("upn")]
        [DataRow("appid")]
        [DataRow("tid")]
        [DataRow("app_displayname")]
        public void CoverProperty(string claimType)
        {
            //Act
            var result = new AadS2SAuthResult(this.GenerateS2SResult("62e90394-69f5-4237-9190-012177145e10", claimType), "accessToken");

            //Assert
            Assert.AreEqual("accessToken", result.AccessToken);
            if (!claimType.Equals("app_displayname"))
                Assert.IsNull(result.AppDisplayName);
            Assert.IsNull(result.SubjectTicket);
            if (!claimType.Equals("upn"))
                Assert.IsNull(result.UserPrincipalName);
        }

        private S2SAuthenticationResult GenerateS2SResult(string guid, string claimType = "wids")
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(claimType, guid)
            };

            JwtSecurityToken accessToken = new JwtSecurityToken(
                issuer: null,
                claims: claims,
                audience: null,
                expires: null,
                notBefore: null,
                signingCredentials: null);

            return new S2SAuthenticationResult
            {
                Exception = null,
                Ticket = new S2SAuthenticationTicket(TokenType.AccessToken, AuthenticationScheme.Unknown),
                Succeeded = true
            };
        }
    }
}
