namespace Microsoft.PrivacyServices.CommandFeed.Client.Test.PrivacyCommandValidator
{
    using System;

    using System.IdentityModel.Tokens.Jwt;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Validator;
    using Microsoft.PrivacyServices.CommandFeed.Validator.Configuration;
    using Microsoft.PrivacyServices.CommandFeed.Validator.TokenValidation;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TokenValidatorTests
    {
        private static readonly LoggableInformation LoggableInformation = new LoggableInformation("some command", "someSubject", "someId");

        [TestMethod]
        public void VerifyRunPrechecksOnTokenSucceedsForMsa()
        {
            const string verifier =
                "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsImtpZCI6IjFIWDAzOXJEOHc1d1lmd01xSlRKVjRCSHJURSJ9.eyJ2ZXIiOjEsInRpZCI6IjQ5MjUzMDhjLWYxNjQtNGQyZC1iYzdlLTA2MzExMzJlOTM3NSIsImlzcyI6Imh0dHBzOi8vZ2Rwci5sb2dpbi5saXZlLWludC5jb20vNDkyNTMwOGMtZjE2NC00ZDJkLWJjN2UtMDYzMTEzMmU5Mzc1IiwiZXhwIjoxNTE3MzU2NDQ2LCJuYmYiOjE1MTIxNzI0NDYsIm9wIjoiRXhwb3J0IiwianRpIjoiZTQ5NDQxMDQtMGJmNS00M2M5LTI3M2YtZTA5MTA0MWE4YWQxIiwib3BfdGltZSI6MTUxMjE3MjQ0NiwicHVpZCI6IjAwMDMwMDAwOTgzRkVFQzQiLCJyZXAiOjAsImNpZCI6ImI1NGNjOTNlMDkwNDlmZGMiLCJhbmlkIjoiQzQzMTMwMEIyNDRGRDI3NkFCREYzNDUwRkZGRkZGRkYiLCJ4dWlkIjoidGhpcy1pcy1hLXh1aWQtaWQiLCJwcmVkIjoiQnJvd3Nlckhpc3Rvcnk7TG9jYXRpb25EYXRhO09wYXF1ZUlkIn0.pCP8K6MyLBnugmrQIKzuG1DDoYUzgaoIT4-NjVg5kvURcYM4bFdE8wk2zO2eSjN0ccHFuvZI299pzs-XdHzJmnRcUx3lHSEu7GVbHpo-NA1JQda9mbAyzSH-4ro3tpY-BfyMMLP02SDSTBPnBv8V7qtpQzkklFtMkS5oTWh7j97EnkFYuI27l7PP6w8LAcJ-uVwiuai0SRjXh585WbDgz_YkofIP6F_fte2AMgCP1ISuhd4relUVghni0eb365eC7zODk5C3nly-WyOWVNr6B4Lu2p08wYZGlCypFtYRvby8noZanjChG6PWaRj2szq_T2zDyzarUW02ha2xKo31AA";
            var jwtSecurityToken = new JwtSecurityToken(verifier);

            var tokenValidator = new TokenValidator();
            try
            {
                tokenValidator.RunPrechecksOnToken(jwtSecurityToken, new MsaSubject(), LoggableInformation, EnvironmentConfiguration.MsaPreproduction);
            }
            catch (InvalidPrivacyCommandException ex)
            {
                Assert.IsTrue(ex.Message.Contains("Verifier has expired."));
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPrivacyCommandException))]
        public void VerifyRunPrechecksOnTokenFailsForMsaIfTenantIsInvalid()
        {
            const string verifier =
                "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsImtpZCI6IjFIWDAzOXJEOHc1d1lmd01xSlRKVjRCSHJURSJ9.eyJ2ZXIiOjEsInRpZCI6IjQ5MjUzMDhjLWYxNjQtNGQyZC1iYzdlLTA2MzExMzJlOTM3NSIsImlzcyI6Imh0dHBzOi8vZ2Rwci5sb2dpbi5saXZlLWludC5jb20vNDkyNTMwOGMtZjE2NC00ZDJkLWJjN2UtMDYzMTEzMmU5Mzc1IiwiZXhwIjoxNTE3MzU2NDQ2LCJuYmYiOjE1MTIxNzI0NDYsIm9wIjoiRXhwb3J0IiwianRpIjoiZTQ5NDQxMDQtMGJmNS00M2M5LTI3M2YtZTA5MTA0MWE4YWQxIiwib3BfdGltZSI6MTUxMjE3MjQ0NiwicHVpZCI6IjAwMDMwMDAwOTgzRkVFQzQiLCJyZXAiOjAsImNpZCI6ImI1NGNjOTNlMDkwNDlmZGMiLCJhbmlkIjoiQzQzMTMwMEIyNDRGRDI3NkFCREYzNDUwRkZGRkZGRkYiLCJ4dWlkIjoidGhpcy1pcy1hLXh1aWQtaWQiLCJwcmVkIjoiQnJvd3Nlckhpc3Rvcnk7TG9jYXRpb25EYXRhO09wYXF1ZUlkIn0.pCP8K6MyLBnugmrQIKzuG1DDoYUzgaoIT4-NjVg5kvURcYM4bFdE8wk2zO2eSjN0ccHFuvZI299pzs-XdHzJmnRcUx3lHSEu7GVbHpo-NA1JQda9mbAyzSH-4ro3tpY-BfyMMLP02SDSTBPnBv8V7qtpQzkklFtMkS5oTWh7j97EnkFYuI27l7PP6w8LAcJ-uVwiuai0SRjXh585WbDgz_YkofIP6F_fte2AMgCP1ISuhd4relUVghni0eb365eC7zODk5C3nly-WyOWVNr6B4Lu2p08wYZGlCypFtYRvby8noZanjChG6PWaRj2szq_T2zDyzarUW02ha2xKo31AA";
            var jwtSecurityToken = new JwtSecurityToken(verifier);
            var msaPreproduction = new EnvironmentConfiguration(
                PcvEnvironment.Preproduction,
                Issuer.Msa,
                new Uri("https://nexus.passport-int.com/public/partner/discovery/gdpr/key"),
                @"^https:\/\/gdpr.login.live-int.com\/[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$",
                false,
                "12345678-f164-4d2d-bc7e-0631132e9375");

            var tokenValidator = new TokenValidator();
            try
            {
                tokenValidator.RunPrechecksOnToken(jwtSecurityToken, new MsaSubject(), LoggableInformation, msaPreproduction);
            }
            catch (InvalidPrivacyCommandException ex)
            {
                Assert.IsTrue(ex.Message.Contains("Verifier doesn't contain an valid tenantId in issuer"));
                throw;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidPrivacyCommandException))]
        public void VerifyRunPrechecksOnTokenFailsForMsaIfIssuerIsInvalid()
        {
            const string verifier =
                "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsImtpZCI6IjFIWDAzOXJEOHc1d1lmd01xSlRKVjRCSHJURSJ9.eyJ2ZXIiOjEsInRpZCI6IjQ5MjUzMDhjLWYxNjQtNGQyZC1iYzdlLTA2MzExMzJlOTM3NSIsImlzcyI6Imh0dHBzOi8vZ2Rwci5sb2dpbi5saXZlLWludC5jb20vNDkyNTMwOGMtZjE2NC00ZDJkLWJjN2UtMDYzMTEzMmU5Mzc1IiwiZXhwIjoxNTE3MzU2NDQ2LCJuYmYiOjE1MTIxNzI0NDYsIm9wIjoiRXhwb3J0IiwianRpIjoiZTQ5NDQxMDQtMGJmNS00M2M5LTI3M2YtZTA5MTA0MWE4YWQxIiwib3BfdGltZSI6MTUxMjE3MjQ0NiwicHVpZCI6IjAwMDMwMDAwOTgzRkVFQzQiLCJyZXAiOjAsImNpZCI6ImI1NGNjOTNlMDkwNDlmZGMiLCJhbmlkIjoiQzQzMTMwMEIyNDRGRDI3NkFCREYzNDUwRkZGRkZGRkYiLCJ4dWlkIjoidGhpcy1pcy1hLXh1aWQtaWQiLCJwcmVkIjoiQnJvd3Nlckhpc3Rvcnk7TG9jYXRpb25EYXRhO09wYXF1ZUlkIn0.pCP8K6MyLBnugmrQIKzuG1DDoYUzgaoIT4-NjVg5kvURcYM4bFdE8wk2zO2eSjN0ccHFuvZI299pzs-XdHzJmnRcUx3lHSEu7GVbHpo-NA1JQda9mbAyzSH-4ro3tpY-BfyMMLP02SDSTBPnBv8V7qtpQzkklFtMkS5oTWh7j97EnkFYuI27l7PP6w8LAcJ-uVwiuai0SRjXh585WbDgz_YkofIP6F_fte2AMgCP1ISuhd4relUVghni0eb365eC7zODk5C3nly-WyOWVNr6B4Lu2p08wYZGlCypFtYRvby8noZanjChG6PWaRj2szq_T2zDyzarUW02ha2xKo31AA";
            var jwtSecurityToken = new JwtSecurityToken(verifier);
            var msaPreproduction = new EnvironmentConfiguration(
                PcvEnvironment.Preproduction,
                Issuer.Msa,
                new Uri("https://nexus.passport-int.com/public/partner/discovery/gdpr/key"),
                @"^https:\/\/somethingelse.com\/[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$",
                false,
                "4925308c-f164-4d2d-bc7e-0631132e9375");

            var tokenValidator = new TokenValidator();
            try
            {
                tokenValidator.RunPrechecksOnToken(jwtSecurityToken, new MsaSubject(), LoggableInformation, msaPreproduction);
            }
            catch (InvalidPrivacyCommandException ex)
            {
                Assert.IsTrue(ex.Message.Contains("Verifier doesn't contain an valid issuer"));
                throw;
            }
        }
    }
}
