// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.AadAuthentication.UnitTests.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;

    using Microsoft.Azure.ComplianceServices.Common.UnitTests;
    using Microsoft.IdentityModel.Tokens;

    public static class AadAuthTestHelper
    {
        public static string CreateUnitTestJwt(
            string issuer = "https://default.com", 
            string objectId = "009bc08f-305b-49ea-a01a-25bd4a67c0b3", 
            string tenantId = "eeaed2e8-db46-4b95-bc5f-85523c38266e")
        {
            var head = new JwtHeader(
                new SigningCredentials(
                    new SymmetricSecurityKey(UnitTestData.UnitTestCertificate.RawData),
                    SecurityAlgorithms.HmacSha256Signature))
            {
                { "kid", UnitTestData.UnitTestCertificate.Thumbprint }
            };
            var payload = new JwtPayload(
                new List<Claim>
                {
                    new Claim("foo", "bar"),
                    new Claim("iss", issuer),
                    new Claim("oid", objectId),
                    new Claim("tid", tenantId)
                });
            var token = new JwtSecurityToken(head, payload);
            string encodedJwt = new JwtSecurityTokenHandler().WriteToken(token);
            return encodedJwt;
        }

        public static JwtSecurityToken CreateUnitTestJwtSecurityToken(string issuer = null)
        {
            return new JwtSecurityToken(CreateUnitTestJwt(issuer));
        }
    }
}
