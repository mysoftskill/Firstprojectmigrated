namespace Microsoft.PrivacyServices.CommandFeed.Client.Test.PrivacyCommandValidator
{
    using System;
    using System.Collections.Generic;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    
    using Microsoft.PrivacyServices.CommandFeed.Validator.TokenValidation;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TokenExtensionTests
    {
        [TestMethod]
        public void VerifySecurityTokenExtensionMethods()
        {
            const string verifier =
                "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsImtpZCI6IjFIWDAzOXJEOHc1d1lmd01xSlRKVjRCSHJURSJ9.eyJ2ZXIiOjEsInRpZCI6IjQ5MjUzMDhjLWYxNjQtNGQyZC1iYzdlLTA2MzExMzJlOTM3NSIsImlzcyI6Imh0dHBzOi8vZ2Rwci5sb2dpbi5saXZlLWludC5jb20vNDkyNTMwOGMtZjE2NC00ZDJkLWJjN2UtMDYzMTEzMmU5Mzc1IiwiZXhwIjoxNTE3MzU2NDQ2LCJuYmYiOjE1MTIxNzI0NDYsIm9wIjoiRXhwb3J0IiwianRpIjoiZTQ5NDQxMDQtMGJmNS00M2M5LTI3M2YtZTA5MTA0MWE4YWQxIiwib3BfdGltZSI6MTUxMjE3MjQ0NiwicHVpZCI6IjAwMDMwMDAwOTgzRkVFQzQiLCJyZXAiOjAsImNpZCI6ImI1NGNjOTNlMDkwNDlmZGMiLCJhbmlkIjoiQzQzMTMwMEIyNDRGRDI3NkFCREYzNDUwRkZGRkZGRkYiLCJ4dWlkIjoidGhpcy1pcy1hLXh1aWQtaWQiLCJwcmVkIjoiQnJvd3Nlckhpc3Rvcnk7TG9jYXRpb25EYXRhO09wYXF1ZUlkIn0.pCP8K6MyLBnugmrQIKzuG1DDoYUzgaoIT4-NjVg5kvURcYM4bFdE8wk2zO2eSjN0ccHFuvZI299pzs-XdHzJmnRcUx3lHSEu7GVbHpo-NA1JQda9mbAyzSH-4ro3tpY-BfyMMLP02SDSTBPnBv8V7qtpQzkklFtMkS5oTWh7j97EnkFYuI27l7PP6w8LAcJ-uVwiuai0SRjXh585WbDgz_YkofIP6F_fte2AMgCP1ISuhd4relUVghni0eb365eC7zODk5C3nly-WyOWVNr6B4Lu2p08wYZGlCypFtYRvby8noZanjChG6PWaRj2szq_T2zDyzarUW02ha2xKo31AA";
            const string keyId = "1HX039rD8w5wYfwMqJTJV4BHrTE";
            const string algorithm = "RS256";
            const string tenantId = "4925308c-f164-4d2d-bc7e-0631132e9375";
            var jwtSecurityToken = new JwtSecurityToken(verifier);

            Assert.AreEqual(Guid.Parse(tenantId), jwtSecurityToken.GetTenantIdFromIssuerUrl());
            Assert.AreEqual(keyId, jwtSecurityToken.GetKeyId());
            Assert.AreEqual(algorithm, jwtSecurityToken.Header.Alg);
        }

        [TestMethod]
        public void VerifyClaimsExtensionMethods()
        {
            const long puid = 844427484821603;
            const long cid = 381901336696102;
            Guid objectId = Guid.NewGuid();
            Guid tenantId = Guid.NewGuid();
            const string anid = "someanid";
            const string xuid = "somexuid";
            const string rid = "request1";
            const string operation = "delete";
            const string version = "3.0";

            IEnumerable<Claim> claims = new List<Claim>
            {
                new Claim("puid", puid.ToString("X")),
                new Claim("cid", cid.ToString("X")),
                new Claim("oid", objectId.ToString()),
                new Claim("tid", tenantId.ToString()),
                new Claim("anid", anid),
                new Claim("xuid", xuid),
                new Claim("rid", rid),
                new Claim("op", operation),
                new Claim("pa", "true"),
                new Claim("ca", "false"),
                new Claim("ver", version),
            };

            Assert.AreEqual(claims.GetPuid(), puid);
            Assert.AreEqual(claims.GetCid(), cid);
            Assert.AreEqual(claims.GetObjectId(), objectId);
            Assert.AreEqual(claims.GetTenantIdFromClaims(), tenantId);
            Assert.AreEqual(claims.GetAnid(), anid);
            Assert.AreEqual(claims.GetXuid(), xuid);
            Assert.AreEqual(claims.GetOperation(), operation);
            Assert.AreEqual(claims.GetProcessorApplicable(), "true");
            Assert.AreEqual(claims.GetControllerApplicable(), "false");

            Assert.AreEqual(claims.GetRequestIds().Length, 1);
            Assert.AreEqual(claims.GetRequestIds()[0], rid);
            Assert.AreEqual(claims.GetVersion(), version);
        }

        [TestMethod]
        public void VerifyClaimsExtensionMethodsWithRequestIdList()
        {
            const long puid = 844427484821603;
            const long cid = 381901336696102;
            Guid objectId = Guid.NewGuid();
            Guid tenantId = Guid.NewGuid();
            const string anid = "someanid";
            const string xuid = "somexuid";
            const string operation = "delete";

            IEnumerable<Claim> claims = new List<Claim>
            {
                new Claim("puid", puid.ToString("X")),
                new Claim("cid", cid.ToString("X")),
                new Claim("oid", objectId.ToString()),
                new Claim("tid", tenantId.ToString()),
                new Claim("anid", anid),
                new Claim("xuid", xuid),
                new Claim("rids", "r1,r2,r3,r4"),
                new Claim("op", operation),
                new Claim("ca", "banana"),
            };

            Assert.AreEqual(claims.GetPuid(), puid);
            Assert.AreEqual(claims.GetCid(), cid);
            Assert.AreEqual(claims.GetObjectId(), objectId);
            Assert.AreEqual(claims.GetTenantIdFromClaims(), tenantId);
            Assert.AreEqual(claims.GetAnid(), anid);
            Assert.AreEqual(claims.GetXuid(), xuid);
            Assert.AreEqual(claims.GetOperation(), operation);
            Assert.AreEqual(claims.GetProcessorApplicable(), string.Empty);
            Assert.AreEqual(claims.GetControllerApplicable(), "banana");

            Assert.AreEqual(claims.GetRequestIds().Length, 4);
            Assert.AreEqual(claims.GetRequestIds()[0], "r1");
            Assert.AreEqual(claims.GetRequestIds()[1], "r2");
            Assert.AreEqual(claims.GetRequestIds()[2], "r3");
            Assert.AreEqual(claims.GetRequestIds()[3], "r4");
        }

        [TestMethod]
        public void VerifyClaimsExtensionMethodsWithHex()
        {
            const long puid = 0x00030000983FEEC4;
            const long cid = 0x054cc93e09049fdc;

            IEnumerable<Claim> claims = new List<Claim>
            {
                new Claim("puid", puid.ToString("X")),
                new Claim("cid", cid.ToString("X"))
            };

            Assert.AreEqual(claims.GetPuid(), puid);
            Assert.AreEqual(claims.GetCid(), cid);
            Assert.AreEqual(claims.GetObjectId(), Guid.Empty);
            Assert.AreEqual(claims.GetTenantIdFromClaims(), Guid.Empty);
        }
    }
}
