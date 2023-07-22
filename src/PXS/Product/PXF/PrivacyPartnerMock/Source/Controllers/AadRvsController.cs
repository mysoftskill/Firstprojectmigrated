// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyMockService.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IdentityModel.Tokens.Jwt;
    using System.Net;
    using System.Net.Http;
    using System.Numerics;
    using System.Security.Claims;
    using System.Web.Http;

    using Microsoft.Azure.ComplianceServices.Common.UnitTests;
    using Microsoft.IdentityModel.Tokens;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.AadRequestVerificationService.Models;
    using Microsoft.Membership.MemberServices.Test.Common;

    public class AadRvsController : ApiController
    {
        private const string AccountCloseOperationName = "AccountClose";

        private const string AccountCleanupOperationName = "AccountCleanup";

        private const string DeleteOperationName = "Delete";

        private const string ExportOperationName = "Export";

        private const string VerifierHeaderName = "Verifier";

        [HttpPost]
        [OverrideAuthentication]
        [AllowAnonymous]
        [Route("api/ActorListAuthorization")]
        public HttpResponseMessage ActorListAuthorization([FromBody] AadRvsActorRequest _)
        {
            var scopeResponse = new AadRvsScopeResponse
            {
                Outcome = AadRvsOutcome.OperationSuccess.ToString(),
                Scopes = AadRvsScope.UserProcesscorExportAll
            };
            return this.Request.CreateResponse(HttpStatusCode.OK, scopeResponse);
        }

        [HttpPost]
        [OverrideAuthentication]
        [AllowAnonymous]
        [Route("api/ConstructAccountClose")]
        public HttpResponseMessage ConstructAccountClose([FromBody] AadRvsRequest request)
        {
            // Return a specific error based on detection of known object ids
            switch (request.ObjectId.ToLowerInvariant())
            {
                case TestData.ObjectId400:
                    return this.Request.CreateResponse(HttpStatusCode.BadRequest);
                case TestData.ObjectId401:
                    return this.Request.CreateResponse(HttpStatusCode.Unauthorized);
                case TestData.ObjectId403:
                    return this.Request.CreateResponse(HttpStatusCode.Forbidden);
                case TestData.ObjectId404:
                    return this.Request.CreateResponse(HttpStatusCode.NotFound);
                case TestData.ObjectId405:
                    return this.Request.CreateResponse(HttpStatusCode.MethodNotAllowed);
                case TestData.ObjectId409:
                    return this.Request.CreateResponse(HttpStatusCode.Conflict);
                case TestData.ObjectId429:
                    return this.Request.CreateResponse((HttpStatusCode)429);
                default:
                    break;
            }

            switch (request.TenantId.ToUpperInvariant())
            {
                case TestData.TenantId400:
                    return this.Request.CreateResponse(HttpStatusCode.BadRequest);
                case TestData.TenantId403:
                    return this.Request.CreateResponse(HttpStatusCode.Forbidden);
                case TestData.TenantId409:
                    return this.Request.CreateResponse(HttpStatusCode.Conflict);
                case TestData.HomeTenantId:
                {
                    // Home tenant account close, v2 verifier is in response header, v3 verifier is in response body
                    var response = this.Request.CreateResponse(HttpStatusCode.OK, CreateResponseBodyV3(request, AccountCloseOperationName, 1));
                    response.Headers.Add(VerifierHeaderName, CreateVerifierV2(request, AccountCloseOperationName));
                    return response;
                }
                default:
                {
                    // For non-NI cases, i.e. the MultiTenantCollaboration feature flag is turned off, return the V2 response - only one v2 verifier in header
                    // var response = this.Request.CreateResponse(HttpStatusCode.OK, CreateResponseBodyV2());
                    var response = this.Request.CreateResponse(HttpStatusCode.OK, CreateResponseBodyV3(request, AccountCloseOperationName, 1));
                    response.Headers.Add(VerifierHeaderName, CreateVerifierV2(request, AccountCloseOperationName));
                    return response;
                }
            }
        }

        [HttpPost]
        [OverrideAuthentication]
        [AllowAnonymous]
        [Route("api/ConstructAccountCleanup")]
        public HttpResponseMessage ConstructAccountCleanup([FromBody] AadRvsRequest request)
        {
            // Return a specific error based on detection of known object ids
            switch (request.ObjectId.ToLowerInvariant())
            {
                case TestData.ObjectId400:
                    return this.Request.CreateResponse(HttpStatusCode.BadRequest);
                case TestData.ObjectId401:
                    return this.Request.CreateResponse(HttpStatusCode.Unauthorized);
                case TestData.ObjectId403:
                    return this.Request.CreateResponse(HttpStatusCode.Forbidden);
                case TestData.ObjectId404:
                    return this.Request.CreateResponse(HttpStatusCode.NotFound);
                case TestData.ObjectId405:
                    return this.Request.CreateResponse(HttpStatusCode.MethodNotAllowed);
                case TestData.ObjectId409:
                    return this.Request.CreateResponse(HttpStatusCode.Conflict);
                case TestData.ObjectId429:
                    return this.Request.CreateResponse((HttpStatusCode)429);
                default:
                    break;
            }

            switch (request.TenantId.ToUpperInvariant())
            {
                case TestData.ResourceTenantId:
                {
                    // Used to test the case in which the resource tenant is calling this on a local user
                    if (request.ObjectId.ToUpperInvariant() == TestData.ResourceUserObjIdNonComplexOrg)
                    {
                        // TODO: What should this return?
                        return this.Request.CreateResponse(HttpStatusCode.Forbidden);
                    }

                    // Resource tenant account cleanup, only v3 verifier in response body, no V2 verifier
                    var response = this.Request.CreateResponse(HttpStatusCode.OK, CreateResponseBodyV3(request, AccountCleanupOperationName, 1));
                    return response;
                }
                case TestData.HomeTenantId:
                {
                    // Used to test the case in which a users is targeting their home tenant
                    if (request.ObjectId.ToUpperInvariant() == TestData.HomeUserObjIdNonComplexOrg)
                    {
                        // TODO: What should this return?
                        return this.Request.CreateResponse(HttpStatusCode.Forbidden);
                    }

                    // Resource tenant account cleanup, only v3 verifier in response body, no V2 verifier
                    var response = this.Request.CreateResponse(HttpStatusCode.OK, CreateResponseBodyV3(request, AccountCleanupOperationName, 1));
                    return response;
                }
                default:
                    return this.Request.CreateResponse(HttpStatusCode.BadRequest);
            }
        }

        [HttpPost]
        [OverrideAuthentication]
        [AllowAnonymous]
        [Route("api/ConstructDelete")]
        public HttpResponseMessage ConstructDelete([FromBody] AadRvsRequest request)
        {
            using (HttpResponseMessage response = this.Request.CreateResponse(HttpStatusCode.OK))
            {
                response.Headers.Add(VerifierHeaderName, CreateVerifierV2(request, DeleteOperationName));
                return response;
            }
        }

        [HttpPost]
        [OverrideAuthentication]
        [AllowAnonymous]
        [Route("api/ConstructExport")]
        public HttpResponseMessage ConstructExport([FromBody] AadRvsRequest request)
        {
            // Return a specific error based on detection of known object ids
            switch (request.ObjectId.ToLowerInvariant())
            {
                case TestData.ObjectId400:
                    return this.Request.CreateResponse(HttpStatusCode.BadRequest);
                case TestData.ObjectId401:
                    return this.Request.CreateResponse(HttpStatusCode.Unauthorized);
                case TestData.ObjectId403:
                    return this.Request.CreateResponse(HttpStatusCode.Forbidden);
                case TestData.ObjectId404:
                    return this.Request.CreateResponse(HttpStatusCode.NotFound);
                case TestData.ObjectId405:
                    return this.Request.CreateResponse(HttpStatusCode.MethodNotAllowed);
                case TestData.ObjectId409:
                    return this.Request.CreateResponse(HttpStatusCode.Conflict);
                case TestData.ObjectId429:
                    return this.Request.CreateResponse((HttpStatusCode)429);
                default:
                    break;
            }

            switch (request.TenantId.ToUpperInvariant())
            {
                case TestData.HomeTenantId:
                {
                    if (request.ObjectId.ToUpperInvariant() == TestData.ResourceUserObjIdNonComplexOrg)
                    {
                        // TODO: What should this return?
                        return this.Request.CreateResponse(HttpStatusCode.Forbidden);
                    }

                    // Home tenant export, v2 verifier is in response header, v3 verifier is in response body
                    var response = this.Request.CreateResponse(HttpStatusCode.OK, CreateResponseBodyV3(request, ExportOperationName, 1));
                    response.Headers.Add(VerifierHeaderName, CreateVerifierV2(request, ExportOperationName));
                    return response;
                }
                case TestData.ResourceTenantId:
                {
                    if (request.ObjectId.ToUpperInvariant() == TestData.HomeUserObjIdNonComplexOrg)
                    {
                        // TODO: Is this the right error?
                        return this.Request.CreateResponse(HttpStatusCode.Forbidden);
                    }

                    // Resource tenant export, v3 verifier is in response body
                    var response = this.Request.CreateResponse(HttpStatusCode.OK, CreateResponseBodyV3(request, ExportOperationName, 1));
                    return response;
                }
                default:
                {
                    // For non-NI cases, i.e. the MultiTenantCollaboration feature flag is turned off, return the V2 response - only one v2 verifier in header
                    // var response = this.Request.CreateResponse(HttpStatusCode.OK, CreateResponseBodyV2());
                    var response = this.Request.CreateResponse(HttpStatusCode.OK, CreateResponseBodyV3(request, ExportOperationName, 1));
                    response.Headers.Add(VerifierHeaderName, CreateVerifierV2(request, ExportOperationName));
                    return response;
                }
            }
        }

        private static string CreateVerifierV2(AadRvsRequest request, string operation)
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
                    new Claim("ver", "2.0"),
                    new Claim("op", operation),
                    new Claim("rids", request.CommandIds),
                    new Claim("pa", request.ProcessorApplicable ? true.ToString().ToLowerInvariant() : false.ToString().ToLowerInvariant()),
                    new Claim("ca", request.ControllerApplicable ? true.ToString().ToLowerInvariant() : false.ToString().ToLowerInvariant()),
                    new Claim("iss", "https://pxsmock.api.account.microsoft-int.com"),
                    new Claim("oid", request.ObjectId),
                    new Claim("tid", request.TenantId),

                    // The following logic is used to create a predicatble value based on the object id - it will have collissions, but for testing - that is OK
                    new Claim("puid", ((long)(BigInteger.Parse(new Guid(request.ObjectId).ToString("N"), NumberStyles.AllowHexSpecifier) % long.MaxValue)).ToString("X"))
                });
            var token = new JwtSecurityToken(head, payload);
            string encodedJwt = new JwtSecurityTokenHandler().WriteToken(token);
            return encodedJwt;
        }

        private static string CreateVerifierV3(AadRvsRequest request, string operation)
        {
            var head = new JwtHeader(
                new SigningCredentials(
                    new SymmetricSecurityKey(UnitTestData.UnitTestCertificate.RawData),
                    SecurityAlgorithms.HmacSha256Signature))
            {
                { "kid", UnitTestData.UnitTestCertificate.Thumbprint }
            };
            var claims = new List<Claim>
            {
                new Claim("ver", "3.0"),
                new Claim("op", operation),
                new Claim("rids", request.CommandIds),
                new Claim("pa", request.ProcessorApplicable ? true.ToString().ToLowerInvariant() : false.ToString().ToLowerInvariant()),
                new Claim("ca", request.ControllerApplicable ? true.ToString().ToLowerInvariant() : false.ToString().ToLowerInvariant()),
                new Claim("iss", "https://pxsmock.api.account.microsoft-int.com"),
                new Claim("tid", request.TenantId),
            };

            string puid = ((long)(BigInteger.Parse(new Guid(request.ObjectId).ToString("N"), NumberStyles.AllowHexSpecifier) % long.MaxValue)).ToString("X");

            if (string.Compare(request.TenantId, TestData.HomeTenantId, StringComparison.OrdinalIgnoreCase) == 0)
            {
                claims.Add(new Claim("dsr_tid_type", "home")); // TODO: add enum converter?
                claims.Add(new Claim("oid", request.ObjectId));
                claims.Add(new Claim("puid", puid));
            }
            else
            {
                claims.Add(new Claim("dsr_tid_type", "resource"));  // TODO: add enum converter?
                claims.Add(new Claim("home_tid", TestData.HomeTenantId));
                claims.Add(new Claim("home_oid", request.ObjectId));
                claims.Add(new Claim("home_puid", puid));
            }

            var payload = new JwtPayload(claims);
            var token = new JwtSecurityToken(head, payload);
            string encodedJwt = new JwtSecurityTokenHandler().WriteToken(token);
            return encodedJwt;
        }

        private static AadRvsResponseV3 CreateResponseBodyV3(AadRvsRequest request, string operation, int numVerifiers)
        {
            var response = new AadRvsResponseV3 { Outcome = "outcome", Message = "Ok" };

            response.Verifiers = new string[numVerifiers];
            for (int i = 0; i < numVerifiers; i++)
            {
                response.Verifiers[i] = CreateVerifierV3(request, operation);
            }

            return response;
        }
    }
}
