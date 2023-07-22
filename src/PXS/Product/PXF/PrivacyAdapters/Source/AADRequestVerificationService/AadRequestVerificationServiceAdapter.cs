// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.AadRequestVerificationService
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IdentityModel.Tokens.Jwt;
    using System.Linq;
    using System.Net.Http;
    using System.Security.Claims;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.AadRequestVerificationService.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Helpers;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.OSGS.HttpClientCommon;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Newtonsoft.Json;

    /// <summary>
    ///     Public class for AadRequestVerificationServiceAdapter.
    /// </summary>
    public class AadRequestVerificationServiceAdapter : IAadRequestVerificationServiceAdapter
    {
        /// <summary>
        ///     Aad Rvs operation types.
        /// </summary>
        public enum AadRvsOperationType
        {
            /// <summary>
            ///     None.
            /// </summary>
            None,

            /// <summary>
            ///     Export.
            /// </summary>
            Export,

            /// <summary>
            ///     AccountClose.
            /// </summary>
            AccountClose,

            /// <summary>
            ///     AccountCleanup.
            /// </summary>
            AccountCleanup
        }

        // Per spec, max # of chars per claim.
        public const int MaxValuePerClaim = 1024;

        private const string ActorListAuthorization = @"api/ActorListAuthorization";

        private const string ConstructAccountCloseRelativePath = @"api/ConstructAccountClose";

        private const string ConstructAccountCleanupRelativePath = @"api/ConstructAccountCleanup";

        private const string ConstructDeleteRelativePath = @"api/ConstructDelete";

        private const string ConstructExportRelativePath = @"api/ConstructExport";

        private const string NullVerifierErrorMessage = "Verifier header or value does not exist in the response.";

        private const string VerifierHeader = "Verifier";

        private readonly IAadAuthManager aadAuthManager;

        private readonly IAadRequestVerificationServiceAdapterConfiguration configuration;

        private readonly ICounterFactory counterFactory;

        private readonly IHttpClient httpClient;

        public AadRequestVerificationServiceAdapter(
            IHttpClient httpClient,
            IAadRequestVerificationServiceAdapterConfiguration configuration,
            IAadAuthManager aadAuthManager,
            ICounterFactory counterFactory)
        {
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.aadAuthManager = aadAuthManager ?? throw new ArgumentNullException(nameof(aadAuthManager));
            this.counterFactory = counterFactory;
        }

        /// <inheritdoc />
        public async Task<AdapterResponse<AadRvsScopeResponse>> ActorListAuthorizationAsync(AadRvsActorRequest request, IRequestContext requestContext)
        {
            AadIdentity aadIdentity = requestContext.RequireIdentity<AadIdentity>();

            var actorListAuthorization = new Uri(new Uri(this.configuration.BaseUrl), ActorListAuthorization);
            var jwtOutboundPolicy = new JwtOutboundPolicy(
                this.configuration.AadAppId,
                request.TargetTenantId,
                this.aadAuthManager.AadLoginEndpoint,
                this.aadAuthManager.StsAuthorityEndpoint);

            OutgoingApiEventWrapper apiEvent = OutgoingApiEventWrapper.CreateBasicOutgoingEvent(
                this.configuration.PartnerId,
                "ActorListAuthorization",
                targetUri: actorListAuthorization.ToString(),
                operationVersion: null,
                requestMethod: HttpMethod.Post,
                dependencyType: "WebService");
            apiEvent.SetAadUserId(request.TargetObjectId);
            apiEvent.ExtraData["tid"] = request.TargetTenantId;

            var headerDictionary = new Dictionary<string, string>
            {
                { HeaderNames.ClientRequestId, LogicalWebOperationContext.ServerActivityId.ToString() }
            };

            HttpRequestMessage requestMessage = HttpExtensions.CreateHttpRequestMessage(actorListAuthorization, HttpMethod.Post, apiEvent, headerDictionary, request);

            await this.aadAuthManager.SetAuthorizationHeaderProtectedForwardedTokenAsync(requestMessage.Headers, aadIdentity.AccessToken, jwtOutboundPolicy, true)
                .ConfigureAwait(false);

            HttpResponseMessage responseMessage = await this.httpClient.SendAsync(requestMessage).ConfigureAwait(false);

            return await HandleJsonResponseAsync<AadRvsScopeResponse>(responseMessage).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<AdapterResponse<AadRvsVerifiers>> ConstructAccountCloseAsync(AadRvsRequest request)
        {
            return await ConstructAccountCloseImplAsync(request, ConstructAccountCloseRelativePath, "ConstructAccountClose", null);
        }

        /// <inheritdoc />
        public async Task<AdapterResponse<AadRvsVerifiers>> ConstructAccountCleanupAsync(AadRvsRequest request, IRequestContext requestContext)
        {
            return await ConstructAccountCloseImplAsync(request, ConstructAccountCleanupRelativePath, "ConstructAccountCleanup", requestContext);
        }

        /// <inheritdoc />
        public async Task<AdapterResponse<string>> ConstructDeleteAsync(AadRvsRequest request, IRequestContext requestContext)
        {
            if (!(requestContext.Identity is AadIdentity aadIdentity))
                throw new ArgumentOutOfRangeException(nameof(requestContext.Identity));

            var targetUri = new Uri(new Uri(this.configuration.BaseUrl), ConstructDeleteRelativePath);
            var jwtOutboundPolicy = new JwtOutboundPolicy(
                this.configuration.AadAppId,
                request.TenantId,
                this.aadAuthManager.AadLoginEndpoint,
                this.aadAuthManager.StsAuthorityEndpoint);

            OutgoingApiEventWrapper apiEvent = OutgoingApiEventWrapper.CreateBasicOutgoingEvent(
                this.configuration.PartnerId,
                "ConstructDelete",
                targetUri: targetUri.ToString(),
                operationVersion: null,
                requestMethod: HttpMethod.Post,
                dependencyType: "WebService");
            PopulateAadSubjectInfo(request, apiEvent);
            PopulateAadExtraDataRequestId(request, apiEvent);

            var headerDictionary = new Dictionary<string, string>
            {
                { HeaderNames.ClientRequestId, LogicalWebOperationContext.ServerActivityId.ToString() }
            };

            HttpRequestMessage requestMessage = HttpExtensions.CreateHttpRequestMessage(targetUri, HttpMethod.Post, apiEvent, headerDictionary, request);
            await this.aadAuthManager.SetAuthorizationHeaderProtectedForwardedTokenAsync(requestMessage.Headers, aadIdentity.AccessToken, jwtOutboundPolicy, true)
                .ConfigureAwait(false);

            HttpResponseMessage responseMessage = await this.httpClient.SendAsync(requestMessage).ConfigureAwait(false);

            var response = await GetAadRvsVerifiersAsync(responseMessage).ConfigureAwait(false);
            return new AdapterResponse<string>
            {
                Error = response.Error,
                Result = response.Result?.V2
            };
        }

        /// <inheritdoc />
        public async Task<AdapterResponse<AadRvsVerifiers>> ConstructExportAsync(AadRvsRequest request, IRequestContext requestContext)
        {
            if (!(requestContext.Identity is AadIdentity aadIdentity))
                throw new ArgumentOutOfRangeException(nameof(requestContext.Identity));

            request.Operation = AadRvsOperationType.Export.ToString();
            var constructExportUri = new Uri(new Uri(this.configuration.BaseUrl), ConstructExportRelativePath);

            var jwtOutboundPolicy = new JwtOutboundPolicy(
                this.configuration.AadAppId,
                request.TenantId,
                this.aadAuthManager.AadLoginEndpoint,
                this.aadAuthManager.StsAuthorityEndpoint);

            OutgoingApiEventWrapper apiEvent = OutgoingApiEventWrapper.CreateBasicOutgoingEvent(
                this.configuration.PartnerId,
                "ConstructExport",
                targetUri: constructExportUri.ToString(),
                operationVersion: null,
                requestMethod: HttpMethod.Post,
                dependencyType: "WebService");
            PopulateAadSubjectInfo(request, apiEvent);
            PopulateAadExtraDataRequestId(request, apiEvent);

            var headerDictionary = new Dictionary<string, string>
            {
                { HeaderNames.ClientRequestId, LogicalWebOperationContext.ServerActivityId.ToString() }
            };

            HttpRequestMessage requestMessage = HttpExtensions.CreateHttpRequestMessage(constructExportUri, HttpMethod.Post, apiEvent, headerDictionary, request);

            await this.aadAuthManager.SetAuthorizationHeaderProtectedForwardedTokenAsync(requestMessage.Headers, aadIdentity.AccessToken, jwtOutboundPolicy, true)
                    .ConfigureAwait(false);

            HttpResponseMessage responseMessage = await this.httpClient.SendAsync(requestMessage).ConfigureAwait(false);

            return await GetAadRvsVerifiersAsync(responseMessage).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public AdapterError UpdatePrivacyRequestWithVerifiers(PrivacyRequest request, AadRvsVerifiers verifiers)
        {
            if (verifiers.V3 == null)
            {
                return new AdapterError(AdapterErrorCode.NullVerifierV3, NullVerifierErrorMessage, 0);
            }
            else if (verifiers.V3.Length != 1)
            {
                // TODO: handle multiple v3 verifiers in the future.
                // Use a temporary error code here. This won't be an error once we support multiple V3 verifiers
                return new AdapterError(AdapterErrorCode.UnexpectedVerifier, $"Unexpected number of v3 verifiers: {verifiers.V3.Length}", 0);
            }
            else
            {
                if (string.IsNullOrEmpty(verifiers.V3[0]))
                {
                    return new AdapterError(AdapterErrorCode.NullVerifierV3, NullVerifierErrorMessage, 0);
                }

                var verifierV3 = verifiers.V3[0];
                request.VerificationToken = verifiers.V2;

                if (! (request.Subject is AadSubject aadSubject))
                {
                    throw new InvalidOperationException("This method can only be called with AadSubject");
                }

                if (TryGetOrgIdPuid(verifierV3, out long orgIdPuid))
                {
                    aadSubject.OrgIdPUID = orgIdPuid;
                }

                if (TryGetHomeTenantId(verifierV3, out Guid homeTenantId))
                {
                    // If a home_tid claim is found, this is a request from a Resource tenant
                    if (!string.IsNullOrEmpty(verifiers.V2))
                    {
                        return new AdapterError(AdapterErrorCode.UnexpectedVerifier, NullVerifierErrorMessage, 0);
                    }

                    if (!(request.Subject is AadSubject2))
                    {
                        // If we don't already have an AadSubject2, try to convert AadSubject to AadSubject2, since we got enough information from AadRvs
                        var aadSubject2 = new AadSubject2
                        {
                            TenantId = aadSubject.TenantId,
                            OrgIdPUID = aadSubject.OrgIdPUID,
                            ObjectId = aadSubject.ObjectId,
                            HomeTenantId = homeTenantId,
                            TenantIdType = TenantIdType.Resource
                        };

                        request.Subject = aadSubject2;
                    }

                    // Verifier string is long and takes up a lot spaces. Only populate v3 verifier for AadSubject2
                    request.VerificationTokenV3 = verifierV3;
                }
                else if (string.IsNullOrEmpty(verifiers.V2))
                {
                    // Otherwise, this is a request from a Home tenant, V2 verifier is required
                    return new AdapterError(AdapterErrorCode.NullVerifier, NullVerifierErrorMessage, 0);
                }
            }

            return null;
        }

        /// <inheritdoc />
        public bool TryGetOrgIdPuid(string token, out long orgIdPuid)
        {
            orgIdPuid = 0;
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    return false;
                }
    
                var jwtToken = new JwtSecurityToken(token);
                Claim puidClaim = jwtToken.Claims?.FirstOrDefault(c => string.Equals(c.Type, "puid"));
                if (puidClaim == null)
                {
                    // puid is in "home_puid" claim for users in Resource Tenant
                    puidClaim = jwtToken.Claims?.FirstOrDefault(c => string.Equals(c.Type, "home_puid"));
                    if (puidClaim == null)
                    {
                        return false;
                    }
                }

                return long.TryParse(puidClaim.Value, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out orgIdPuid);
            }
            catch
            {
                    return false;
            }
        }

        /// <inheritdoc />
        public bool TryGetHomeTenantId(string token, out Guid homeTenantId)
        {
            homeTenantId = default;
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    return false;
                }
        
                var jwtToken = new JwtSecurityToken(token);
                Claim homeTidClaim = jwtToken.Claims?.FirstOrDefault(c => string.Equals(c.Type, "home_tid"));
        
                if (homeTidClaim == null)
                {
                    return false;
                }
        
                return Guid.TryParse(homeTidClaim.Value, out homeTenantId);
             }
             catch
             {
                 return false;
             }
         }

         /// <inheritdoc />
         public bool TryGetTenantIdType(string token, out TenantIdType tenantIdType)
         {
            tenantIdType = 0;
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    return false;
                }

                var jwtToken = new JwtSecurityToken(token);
                Claim dsrTidType = jwtToken.Claims?.FirstOrDefault(c => string.Equals(c.Type, "dsr_tid_type"));
                if (dsrTidType == null)
                {
                    return false;
                }

                return Enum.TryParse(dsrTidType.Value, true, out tenantIdType);
            }
            catch
            {
                return false;
            }
        }
        
        private async Task<AdapterResponse<AadRvsVerifiers>> ConstructAccountCloseImplAsync(AadRvsRequest request, string apiPath, string operationName, IRequestContext requestContext = null)
        {
            var aadrvsUri = new Uri(new Uri(this.configuration.BaseUrl), apiPath);

            OutgoingApiEventWrapper apiEvent = OutgoingApiEventWrapper.CreateBasicOutgoingEvent(
                this.configuration.PartnerId,
                operationName,
                targetUri: aadrvsUri.ToString(),
                operationVersion: null,
                requestMethod: HttpMethod.Post,
                dependencyType: "WebService");
            PopulateAadSubjectInfo(request, apiEvent);
            PopulateAadExtraDataRequestId(request, apiEvent);
            
            // It's a known scenario that org id puid can be missing. So to help track how often this happens, log it.
            ICounter counter = this.counterFactory.GetCounter(CounterCategoryNames.AadAccountClose, "Requests:PerSec", CounterType.Rate);
            counter.Increment();
            if (string.IsNullOrWhiteSpace(request.OrgIdPuid))
            {
                counter.Increment("MissingOrgIdPuid");
                apiEvent.ExtraData["HasOrgIdPuid"] = false.ToString();
            }
            else
            {
                counter.Increment("HasOrgIdPuid");
            }
            
            var headerDictionary = new Dictionary<string, string>
            {
                { HeaderNames.ClientRequestId, LogicalWebOperationContext.ServerActivityId.ToString() }
            };
            
            HttpRequestMessage requestMessage = HttpExtensions.CreateHttpRequestMessage(aadrvsUri, HttpMethod.Post, apiEvent, headerDictionary, request);

            // If we have a request content, then we assume it comes from Graph;
            // in this case we must use a PFT when we call RVS.
            if (requestContext != null)
            {
                if (!(requestContext.Identity is AadIdentity aadIdentity))
                    throw new ArgumentOutOfRangeException(nameof(requestContext.Identity));

                var jwtOutboundPolicy = new JwtOutboundPolicy(
                    this.configuration.AadAppId,
                    aadIdentity.TenantId.ToString(), // TenantId of Actor token must match the TenantId in AccessToken
                    this.aadAuthManager.AadLoginEndpoint,
                    this.aadAuthManager.StsAuthorityEndpoint);

                await this.aadAuthManager.SetAuthorizationHeaderProtectedForwardedTokenAsync(requestMessage.Headers, aadIdentity.AccessToken, jwtOutboundPolicy, true)
                    .ConfigureAwait(false);
            }
            else 
            {
                if (await this.aadAuthManager.IsTenantIdValidAsync(request.TenantId))
                {
                    var jwtOutboundPolicy = new JwtOutboundPolicy(
                    this.configuration.AadAppId,
                    request.TenantId,
                    this.aadAuthManager.AadLoginEndpoint,
                    this.aadAuthManager.StsAuthorityEndpoint);

                    // account close/cleanup can share the same outbound policy name
                    await this.aadAuthManager.SetAuthorizationHeaderAppTokenAsync(requestMessage.Headers, jwtOutboundPolicy, !string.IsNullOrEmpty(request.PreVerifier), true).ConfigureAwait(false);
                }
                else
                {
                    await this.aadAuthManager.SetAuthorizationHeaderAppTokenAsync(requestMessage.Headers, OutboundPolicyName.AadRvsConstructAccountClose, !string.IsNullOrEmpty(request.PreVerifier), true).ConfigureAwait(false);
                }

            }

            HttpResponseMessage responseMessage = await this.httpClient.SendAsync(requestMessage).ConfigureAwait(false);
            
            return await GetAadRvsVerifiersAsync(responseMessage).ConfigureAwait(false);
        }
        
        private static async Task<AdapterResponse<T>> HandleErrorAsync<T>(HttpResponseMessage responseMessage)
        {
            if (!responseMessage.IsSuccessStatusCode)
            {
                int statusCode = (int)responseMessage.StatusCode;
                AdapterErrorCode adapterErrorCode;
                switch (statusCode)
                {
                    case 400:
                        adapterErrorCode = AdapterErrorCode.InvalidInput;
                        break;
                    case 401:
                        adapterErrorCode = AdapterErrorCode.Unauthorized;
                        break;
                    case 403:
                        adapterErrorCode = AdapterErrorCode.Forbidden;
                        break;
                    case 404:
                        adapterErrorCode = AdapterErrorCode.ResourceNotFound;
                        break;
                    case 405:
                        adapterErrorCode = AdapterErrorCode.MethodNotAllowed;
                        break;
                    case 409:
                        adapterErrorCode = AdapterErrorCode.ConcurrencyConflict;
                        break;
                    case 429:
                        adapterErrorCode = AdapterErrorCode.TooManyRequests;
                        break;
                    default:
                        adapterErrorCode = AdapterErrorCode.Unknown;
                        break;
                }
                return new AdapterResponse<T>
                {
                    Error = new AdapterError(adapterErrorCode, await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false), statusCode)
                };
            }
            return null;
        }
    
        private static async Task<AdapterResponse<T>> HandleJsonResponseAsync<T>(HttpResponseMessage responseMessage)
        {
            AdapterResponse<T> error = await HandleErrorAsync<T>(responseMessage).ConfigureAwait(false);
            if (error != null)
                return error;
        
            return new AdapterResponse<T>
            {
                Result = JsonConvert.DeserializeObject<T>(await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false))
            };
        }
    
        private static void PopulateAadExtraDataRequestId(AadRvsRequest request, OutgoingApiEventWrapper apiEvent)
        {
            // This is aka the command id.
            apiEvent.ExtraData["commandIds"] = request.CommandIds;
        }
    
        private static void PopulateAadSubjectInfo(AadRvsRequest request, OutgoingApiEventWrapper apiEvent)
        {
            apiEvent.SetAadUserId(request.ObjectId);
            apiEvent.ExtraData["tenantId"] = request.TenantId;
        }
    
        private static async Task<AdapterResponse<AadRvsVerifiers>> GetAadRvsVerifiersAsync(HttpResponseMessage responseMessage)
        {
            AdapterResponse<AadRvsVerifiers> error = await HandleErrorAsync<AadRvsVerifiers>(responseMessage).ConfigureAwait(false);
            if (error != null)
            {
                return error;
            }
            
            // V2 verifier is in the header
            responseMessage.Headers.TryGetValues(VerifierHeader, out IEnumerable<string> verifier);
        
            AadRvsResponseV3 response = null;
        
            // V3 verifiers (if any) are in the response body
            if (responseMessage.Content != null)
            {
                string responseAsString = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                response = JsonConvert.DeserializeObject<AadRvsResponseV3>(responseAsString);
            
                if (!string.IsNullOrWhiteSpace(response?.Continuation))
                {
                    // TODO (1140726): handle continuation token.
                    throw new InvalidOperationException("Continuation token from AADRVS is unexpected.");
                }
            }
        
            return new AdapterResponse<AadRvsVerifiers>
            {
                Result = new AadRvsVerifiers
                {
                    V2 = verifier?.First(),
                    V3 = response?.Verifiers
                }
            };
        }
    }
}
