// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.Security
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http.Dependencies;
    using System.Web.Http.Filters;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.PrivacyAdapters;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.CustomerMasterAdapter;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.ProfileIdentityService;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.Windows.Services.AuthN.Server;

    using ErrorCode = Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.ErrorCode;
    using HeaderNames = Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.HeaderNames;

    /// <summary>
    ///     PrivacyExperience AuthenticationFilter supports:
    ///     1. MSA S2S Self Auth
    ///     2. MSA Site Auth for AllowedListed routes
    ///     3. Cert based Auth (Vortex)
    ///     4. AAD Auth
    /// </summary>
    public sealed class PrivacyExperienceAuthenticationFilter : IAuthenticationFilter
    {
        private const string ComponentName = nameof(PrivacyExperienceAuthenticationFilter);

        private readonly IAadAuthManager aadAuthManager;

        private readonly IRpsAuthServer authServer;

        private readonly ICertificateValidator certificateValidator;

        private readonly ICustomerMasterAdapter customerMasterAdapter;

        private readonly IFamilyClaimsParser familyClaimsParser;

        private readonly ILogger logger;

        private readonly IMsaIdentityServiceAdapter msaIdentityServiceAdapter;

        private readonly IAppConfiguration appConfiguration;

        /// <summary>
        ///     Gets or sets whether multiple instances of this filter type are allowed in the validation chain.
        ///     Set to false because we don't want this filter to run twice for any action.
        /// </summary>
        public bool AllowMultiple => false;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PrivacyExperienceAuthenticationFilter" /> class.
        /// </summary>
        /// <param name="authServer">The authentication server.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="msaIdentityServiceAdapter">The MSA Identity Service Adapter.</param>
        /// <param name="certificateValidator"> The certificate validator</param>
        /// <param name="aadAuthManager">The aad auth manager</param>
        /// <param name="familyClaimsParser">The family claims parser</param>
        /// <param name="customerMasterAdapter">The adapter to get user's privacy profile</param>
        /// <param name="appConfiguration"></param>
        public PrivacyExperienceAuthenticationFilter(
            IRpsAuthServer authServer,
            ILogger logger,
            IMsaIdentityServiceAdapter msaIdentityServiceAdapter,
            ICertificateValidator certificateValidator,
            IAadAuthManager aadAuthManager,
            IFamilyClaimsParser familyClaimsParser,
            ICustomerMasterAdapter customerMasterAdapter,
            IAppConfiguration appConfiguration)
        {
            this.authServer = authServer ?? throw new ArgumentNullException(nameof(authServer));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.msaIdentityServiceAdapter = msaIdentityServiceAdapter ?? throw new ArgumentNullException(nameof(msaIdentityServiceAdapter));
            this.certificateValidator = certificateValidator ?? throw new ArgumentNullException(nameof(certificateValidator));
            this.aadAuthManager = aadAuthManager ?? throw new ArgumentNullException(nameof(aadAuthManager));
            this.familyClaimsParser = familyClaimsParser ?? throw new ArgumentNullException(nameof(familyClaimsParser));
            this.customerMasterAdapter = customerMasterAdapter ?? throw new ArgumentNullException(nameof(customerMasterAdapter));
            this.appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));
        }

        /// <summary>
        ///     Authenticates the request.
        /// </summary>
        /// <param name="context">The authentication context.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>
        ///     A Task that will perform authentication.
        /// </returns>
        public async Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellationToken)
        {
            IPrivacyExperienceServiceConfiguration privacyExperienceServiceConfiguration = GetPrivacyExperienceServiceConfiguration(context.Request);

            HttpRequestMessage request = context.Request;
            X509Certificate2 certificate = request.GetClientCertificate();

            string proxyTicket = null;
            if (request.Headers.Contains(HeaderNames.ProxyTicket))
            {
                proxyTicket = request.Headers.GetValues(HeaderNames.ProxyTicket).FirstOrDefault();
            }

            if (certificate != null)
            {
                string accessToken = null;
                string familyToken = null;

                if (request.Headers.Contains(HeaderNames.AccessToken))
                {
                    accessToken = request.Headers.GetValues(HeaderNames.AccessToken).FirstOrDefault();
                }

                if (request.Headers.Contains(HeaderNames.FamilyTicket))
                {
                    familyToken = request.Headers.GetValues(HeaderNames.FamilyTicket).FirstOrDefault();
                }

                if (!string.IsNullOrEmpty(accessToken) ||
                    !string.IsNullOrEmpty(proxyTicket) ||
                    !string.IsNullOrEmpty(familyToken))
                {
                    await this.AuthenticateMsaRouteAsync(
                        context,
                        request,
                        privacyExperienceServiceConfiguration,
                        certificate,
                        accessToken,
                        proxyTicket,
                        familyToken).ConfigureAwait(false);
                }
                else
                {
                    this.AuthenticateVortexRoute(context, privacyExperienceServiceConfiguration, certificate);
                }
            }
            else
            {
                await this.AuthenticateAadRouteAsync(context, proxyTicket, privacyExperienceServiceConfiguration).ConfigureAwait(false);
            }
        }

        /// <summary>
        ///     Asynchronously challenges an authentication
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken)
        {
            // We're not going to add any challenges to the response
            return Task.CompletedTask;
        }

        private async Task AuthenticateAadRouteAsync(
            HttpAuthenticationContext context,
            string proxyTicket,
            IPrivacyExperienceServiceConfiguration privacyExperienceServiceConfiguration)
        {
            Error authenticationError = null; 
            if (context.Request.Headers.Authorization == null)
            {
                authenticationError = new Error(
                    ErrorCode.Unauthorized,
                    $"{HttpRequestHeader.Authorization.ToString()} is null and client certificate is missing. API RoutePath {context.Request.RequestUri.LocalPath}");
                context.ErrorResult = new ErrorHttpActionResult(authenticationError, context.Request);
                return;
            }

            string scheme = context.Request.Headers.Authorization.Scheme;
            string parameter = context.Request.Headers.Authorization.Parameter;

            // According to MS Graph documentation, here is the format of a header for PFT cases
            // Authorization: MSAuth1.0 actortoken="Bearer [B]", accesstoken="Bearer [A’]", type="PFAT"
            if (scheme.Equals("MSAuth1.0") && parameter.EndsWith("type=\"PFAT\""))
            {
                await this.AuthenticatePftAsync(context, privacyExperienceServiceConfiguration).ConfigureAwait(false);
            }
            else
            {
                await this.AuthenticateJwtAndProxyTicketAsync(context, proxyTicket, privacyExperienceServiceConfiguration).ConfigureAwait(false);
            }
        }

        private async Task AuthenticateJwtAndProxyTicketAsync(
            HttpAuthenticationContext context,
            string proxyTicket,
            IPrivacyExperienceServiceConfiguration privacyExperienceServiceConfiguration)
        {
            // first, validate the aad access token
            var headerValue = context.Request.Headers.Authorization.ToString();
            IAadS2SAuthResult aadS2SAuthResult = await this.aadAuthManager.ValidateInboundJwtAsync(headerValue).ConfigureAwait(false);

            Error authenticationError = HandleAadS2SAuthErrors(aadS2SAuthResult, context.Request.RequestUri.LocalPath, this.logger);

            string callerName = null;
            if (!string.IsNullOrEmpty(aadS2SAuthResult?.InboundAppId))
            {
                privacyExperienceServiceConfiguration.SiteIdToCallerName.TryGetValue(aadS2SAuthResult?.InboundAppId, out callerName);
            }

            // second, validate the proxy ticket if there is one
            if (authenticationError == null)
            {
                if (string.IsNullOrEmpty(proxyTicket))
                {
                    context.Principal = new CallerPrincipal(
                        new AadIdentity(
                            aadS2SAuthResult.InboundAppId,
                            aadS2SAuthResult.ObjectId,
                            aadS2SAuthResult.TenantId,
                            aadS2SAuthResult.AccessToken,
                            string.IsNullOrEmpty(callerName) ? aadS2SAuthResult.AppDisplayName : callerName));
                    this.logger.MethodExit(nameof(AadAuthManager), nameof(this.AuthenticateJwtAndProxyTicketAsync));
                    return;
                }

                long ticketPuid;
                long ticketCid;
                string userProxyTicket;
                string countryRegion;
                DateTimeOffset? birthDate;
                bool isChildInFamily;
                LegalAgeGroup? legalAgeGroupValue;

                // Validate the user proxy ticket
                // The long lived site name below is important. (S2SUserLongSiteName) This path is for AAD auth with a proxy ticket tagging
                // along, copy and pasted from PRC. These tickets need a long expire time, and the 'Long' site name has this auth policy.
                authenticationError = this.ValidateProxyTicket(
                    privacyExperienceServiceConfiguration.S2SUserLongSiteName,
                    privacyExperienceServiceConfiguration.AppAllowList,
                    aadS2SAuthResult?.TenantId.ToString(),
                    proxyTicket,
                    new RpsPropertyBag(),
                    out ticketPuid,
                    out ticketCid,
                    out userProxyTicket,
                    out countryRegion,
                    out birthDate,
                    out isChildInFamily,
                    out legalAgeGroupValue);

                if (authenticationError == null)
                {
                    context.Principal = new CallerPrincipal(
                        new AadIdentityWithMsaUserProxyTicket(
                            aadS2SAuthResult.InboundAppId,
                            aadS2SAuthResult.ObjectId,
                            aadS2SAuthResult.TenantId,
                            aadS2SAuthResult.AccessToken,
                            string.IsNullOrEmpty(callerName) ? aadS2SAuthResult.AppDisplayName : callerName,
                            ticketPuid,
                            userProxyTicket,
                            ticketCid));
                    this.logger.MethodExit(nameof(AadAuthManager), nameof(this.AuthenticateJwtAndProxyTicketAsync));
                    return;
                }
                else if (await appConfiguration.IsFeatureFlagEnabledAsync(FeatureNames.AuthenticationLogging).ConfigureAwait(false))
                {
                    this.logger.Error(nameof(PrivacyExperienceAuthenticationFilter), $"AuthenticateJwtAndProxyTicketAsync: Error validating access token: {proxyTicket}");
                };
            }
            else if (await appConfiguration.IsFeatureFlagEnabledAsync(FeatureNames.AuthenticationLogging).ConfigureAwait(false))
            {
                this.logger.Error(nameof(PrivacyExperienceAuthenticationFilter), $"AuthenticateJwtAndProxyTicketAsync: Error validating access token: {headerValue}");
            };

            this.logger.Error(nameof(PrivacyExperienceAuthenticationFilter), authenticationError.ToString());
            context.ErrorResult = new ErrorHttpActionResult(authenticationError, context.Request);
        }

        private async Task AuthenticateMsaRouteAsync(
            HttpAuthenticationContext context,
            HttpRequestMessage request,
            IPrivacyExperienceServiceConfiguration privacyExperienceServiceConfiguration,
            X509Certificate2 certificate,
            string accessToken,
            string proxyTicket,
            string familyToken)
        {
            Error authenticationError = null;
            MsaSelfIdentity identity = null;

            // Validate client certificate & access token (app ticket) for site auth
            RpsPropertyBag propertyBag;
            long applicationId;
            authenticationError = this.ValidateAccessToken(privacyExperienceServiceConfiguration.S2SAppSiteName, accessToken, certificate, out propertyBag, out applicationId);

            // Retrieve the caller name for the calling partner
            string callerName = null;
            if (authenticationError == null)
            {
                // This is the 'AllowedList' of allowed site ids.
                // If site id and caller name is invalid (not in the configuration), fail the request. 
                if (!TryValidateSiteIdToCallerName(privacyExperienceServiceConfiguration, applicationId.ToString(CultureInfo.InvariantCulture), out callerName))
                {
                    string errorMessage = string.Format(CultureInfo.InvariantCulture, "Invalid caller name. The site id: {0} is not AllowedList.", applicationId);
                    authenticationError = new Error(ErrorCode.InvalidClientCredentials, errorMessage);
                }
            }

            if (authenticationError == null)
            {
                string routeTemplate = request.RequestUri.AbsolutePath.TrimStart('/');

                if (ApiRouteMapping.IsSiteIdAuthenticatedRoute(routeTemplate))
                {
                    // If we don't have a proxy ticket - that's fine, validate just site auth, otherwise validate proxy ticket as well.
                    if (string.IsNullOrEmpty(proxyTicket))
                    {
                        // The route and caller only requires site id verification, create the principal and exit this filter.
                        this.logger.Verbose(ComponentName, "Request successfully authorized for MSA Site Auth.");
                        context.Principal = new CallerPrincipal(new MsaSiteIdentity(callerName, applicationId));

                        return;
                    }
                }

                // Prevent callers that are authorized by ONLY site id from accessing other API's, if they provide a proxy ticket.
                // Example: if caller is a delete-feed-client, and route is not delete feed route, prevent access.
                if (ApiRouteMapping.IsProxyTicketAuthenticatedRoute(routeTemplate) && !IsCallerProxyTicketAuthorized(privacyExperienceServiceConfiguration, applicationId))
                {
                    string errorMessage = $"Access to this route: {routeTemplate} is not authorized to site id: {applicationId}";
                    authenticationError = new Error(ErrorCode.InvalidClientCredentials, errorMessage);
                }
            }

            if (authenticationError == null)
            {
                long ticketPuid;
                long ticketCid;
                string userProxyTicket;
                string countryRegion;
                DateTimeOffset? birthDate;
                bool isChildInFamily;
                LegalAgeGroup? legalAgeGroupValue;
                bool? isFamilyConsentSet = null;

                // Validate the user proxy ticket
                authenticationError = this.ValidateProxyTicket(
                    privacyExperienceServiceConfiguration.S2SUserSiteName,
                    privacyExperienceServiceConfiguration.AppAllowList,
                    applicationId.ToString(),
                    proxyTicket,
                    propertyBag,
                    out ticketPuid,
                    out ticketCid,
                    out userProxyTicket,
                    out countryRegion,
                    out birthDate,
                    out isChildInFamily,
                    out legalAgeGroupValue);

                if (authenticationError == null)
                {
                    long targetPuid;
                    var authType = AuthType.None;

                    // Validate the family claims, if they exist
                    // targetCid is not part of the family claims, but it is in FMS.
                    long? targetCid;

                    authenticationError = this.ValidateFamilyClaims(familyToken, ticketPuid, ticketCid, out targetPuid, out targetCid, out authType, ref isChildInFamily);
                    if (authenticationError == null)
                    {
                        if (!string.IsNullOrEmpty(familyToken))
                        {
                            authType = AuthType.OnBehalfOf;

                            var ctx = new PxfRequestContext(proxyTicket, familyToken, ticketPuid, targetPuid, targetCid, null, false, null);

                            Task<AdapterResponse<ISigninNameInformation>> cidTask = this.msaIdentityServiceAdapter.GetSigninNameInformationAsync(targetPuid);
                            Task<AdapterResponse<IProfileAttributesUserData>> profileTask = this.msaIdentityServiceAdapter.GetProfileAttributesAsync(
                                ctx,
                                ProfileAttribute.AgeGroup,
                                ProfileAttribute.Birthdate,
                                ProfileAttribute.Country);
                            Task<AdapterResponse<bool?>> privacyProfileTask = this.customerMasterAdapter
                                .GetOboPrivacyConsentSettingAsync(ctx);

                            AdapterResponse<ISigninNameInformation> cid = await cidTask.ConfigureAwait(false);
                            AdapterResponse<IProfileAttributesUserData> profile = await profileTask.ConfigureAwait(false);
                            AdapterResponse<bool?> privacyProfile = await privacyProfileTask.ConfigureAwait(false);
                            if (cid.IsSuccess && profile.IsSuccess && privacyProfile.IsSuccess)
                            {
                                birthDate = profile.Result?.Birthdate;
                                countryRegion = profile.Result?.CountryCode;
                                legalAgeGroupValue = profile.Result?.AgeGroup;
                                targetCid = cid.Result?.Cid;
                                isFamilyConsentSet = privacyProfile.Result ?? true;
                            }
                            else
                            {
                                AdapterResponse[] responses =
                                {
                                    cid,
                                    profile,
                                    privacyProfile
                                };

                                string message = string.Join(", ", responses.Where(ar => !ar.IsSuccess).Select(ar => ar.Error.Message));
                                authenticationError = new Error(ErrorCode.PartnerError, message);
                            }
                        }

                        identity = new MsaSelfIdentity(
                            userProxyTicket,
                            familyToken,
                            ticketPuid,
                            targetPuid,
                            ticketCid,
                            callerName,
                            applicationId,
                            targetCid,
                            countryRegion,
                            birthDate,
                            isChildInFamilyInFamily: isChildInFamily,
                            authType: authType,
                            legalAgeGroup: legalAgeGroupValue,
                            isFamilyConsentSet: isFamilyConsentSet);
                    }
                }
            }

            // If there was an error, then finalize logging right away
            if (authenticationError != null)
            {
                this.logger.Information(ComponentName, "Request was not authorized. ErrorInfo={0}", authenticationError.ToString());
                context.ErrorResult = new ErrorHttpActionResult(authenticationError, context.Request);
                return;
            }

            // Authentication was successful. Logging will be finalized by controllers
            this.logger.Verbose(ComponentName, "Request successfully authorized for MSA Self Auth.");
            context.Principal = new CallerPrincipal(identity);
        }

        private async Task AuthenticatePftAsync(HttpAuthenticationContext context, IPrivacyExperienceServiceConfiguration privacyExperienceServiceConfiguration)
        {
            this.logger.Information(ComponentName, "AuthenticatePftAsync");
            IAadS2SAuthResult pftResult = await this.aadAuthManager
                .ValidateInboundPftAsync(context.Request.Headers.Authorization, LogicalWebOperationContext.ServerActivityId)
                .ConfigureAwait(false);

            Error authenticationError = HandleAadS2SAuthErrors(pftResult, context.Request.RequestUri.LocalPath, this.logger);

            string callerName = null;
            if (!string.IsNullOrEmpty(pftResult?.InboundAppId))
            {
                privacyExperienceServiceConfiguration.SiteIdToCallerName.TryGetValue(pftResult?.InboundAppId, out callerName);
            }

            if (authenticationError == null)
            {
                context.Request.Headers.TryGetValues(HeaderNames.TargetObjectId, out IEnumerable<string> targetObjectId);
                string targetOidStr = targetObjectId?.FirstOrDefault();
                Guid targetOid = pftResult.ObjectId;
                if (!string.IsNullOrEmpty(targetOidStr))
                {
                    if (!Guid.TryParse(targetOidStr, out targetOid))
                    {
                        throw new InvalidOperationException($"Invalid values for header {HeaderNames.TargetObjectId}.");
                    }
                }

                context.Principal = new CallerPrincipal(
                    new AadIdentity(
                        pftResult.InboundAppId,
                        pftResult.ObjectId,
                        targetOid,
                        pftResult.TenantId,
                        pftResult.AccessToken,
                        string.IsNullOrEmpty(callerName) ? pftResult.AppDisplayName : callerName));
                this.logger.Information(nameof(PrivacyExperienceAuthenticationFilter), $"Successfully authenticated S2S auth type {nameof(AadIdentity)}.");
            }
            else
            {
                this.logger.Error(nameof(PrivacyExperienceAuthenticationFilter), authenticationError.ToString());
                context.ErrorResult = new ErrorHttpActionResult(authenticationError, context.Request);
            }
        }

        private void AuthenticateVortexRoute(
            HttpAuthenticationContext context,
            IPrivacyExperienceServiceConfiguration privacyExperienceServiceConfiguration,
            X509Certificate2 certificate)
        {
            if (this.certificateValidator.IsAuthorized(certificate))
            {
                string callerName = privacyExperienceServiceConfiguration.VortexAllowedCertSubjects
                    .FirstOrDefault(info => string.Equals(info.Key, certificate.Subject, StringComparison.OrdinalIgnoreCase)).Value;

                context.Principal = new VortexPrincipal(callerName);
                return;
            }

            var authenticationError = new Error(ErrorCode.InvalidClientCredentials, "Request does not have a valid client certificate");
            this.logger.Information(ComponentName, "Request was not authorized. ErrorInfo={0}", authenticationError.ToString());
            context.ErrorResult = new ErrorHttpActionResult(authenticationError, context.Request);
        }

        private Error CreateErrorAndLog(ErrorCode code, string message, string errorDetails = null)
        {
            var error = new Error(code, message);
            this.logger.Error(ComponentName, $"Error Code: '{error.Code}', Error Message: '{error.Message}'");

            if (!string.IsNullOrWhiteSpace(errorDetails))
            {
                error.ErrorDetails = errorDetails;
                this.logger.Error(ComponentName, error.ErrorDetails);
            }

            return error;
        }

        private RpsAuthResult GetAuthResultWithAdditionalAllowedApps(string siteName, string proxyTicket, RpsPropertyBag propertyBag, long[] additionalAllowedApps)
        {
            int idx = 0;
            while (true)
            {
                try
                {
                    RpsAuthResult result = this.authServer.GetAuthResult(siteName, proxyTicket, RpsTicketType.Proxy, propertyBag);
                    return result;
                }
                catch (AuthNException ex) when (additionalAllowedApps != null && idx < additionalAllowedApps.Length)
                {
                    // Only caught when we have additional site ids left to check.
                    this.logger.Information(
                        ComponentName,
                        ex,
                        $"Got exception against app id {propertyBag["ValidatedHexAppId"]}, trying app id {additionalAllowedApps[idx]:X} instead");
                    propertyBag["ValidatedHexAppId"] = additionalAllowedApps[idx++].ToString("X");
                }
            }
        }

        /// <summary>
        ///     Validate access token and client certificate passed in an incoming request by calling RPS through a helper library.
        /// </summary>
        /// <param name="siteName">Name of the application site.</param>
        /// <param name="accessToken">The access token provided by caller</param>
        /// <param name="certificate">The certificate in the request</param>
        /// <param name="propertyBag">
        ///     The property bag that was created to validate the access token. This can be used
        ///     to validate a matching user proxy ticket
        /// </param>
        /// <param name="applicationId">Caller Application ID returned by RPS</param>
        /// <returns>
        ///     Null if request was successfully validated, otherwise <see cref="Error" /> instance containing error details
        /// </returns>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private Error ValidateAccessToken(string siteName, string accessToken, X509Certificate2 certificate, out RpsPropertyBag propertyBag, out long applicationId)
        {
            applicationId = 0;
            propertyBag = new RpsPropertyBag();

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return new Error(ErrorCode.MissingClientCredentials, "An access token is required to access the requested resource.");
            }

            Error error = this.ValidateAccessToken(accessToken, certificate, propertyBag, ref applicationId, siteName);

            if (error == null)
            {
                this.logger.Verbose(ComponentName, "Access token validation successful");
                return null;
            }

            return error;
        }

        private Error ValidateAccessToken(string accessToken, X509Certificate2 certificate, RpsPropertyBag propertyBag, ref long applicationId, string siteName)
        {
            Error error = null;

            try
            {
                using (RpsAuthResult result = this.authServer.GetS2SSiteAuthResult(siteName, accessToken, certificate.RawData, propertyBag))
                {
                    if (result == null)
                    {
                        error = this.CreateErrorAndLog(ErrorCode.Unknown, "Unknown error occurred when validating access token. Message: RpsAuthResult was null");
                    }
                    else if (result.AppId == null)
                    {
                        error = this.CreateErrorAndLog(ErrorCode.Unknown, "Error occurred when validating access token. Message: The AppID was null");
                    }
                    else
                    {
                        applicationId = result.AppId.Value;
                        return null;
                    }
                }
            }
            catch (AuthNException e)
            {
                // This is when the validation fails due to an invalid token passed by caller
                error = this.CreateErrorAndLog(
                    ErrorCode.InvalidClientCredentials,
                    $"Request contained an invalid or unauthorized access token for sitename: {siteName}. ErrorCode: {e.ErrorCode}, Message: {e.Message}");
                error.ErrorDetails = e.ToString();
            }
            catch (Exception e)
            {
                error = this.CreateErrorAndLog(ErrorCode.Unknown, $"Unknown error occurred when validating access token. Message: {e.Message}");
                error.ErrorDetails = e.ToString();
            }

            return error;
        }

        private Error ValidateFamilyClaims(
            string familyToken,
            long authorizingPuid,
            long authorizingCid,
            out long targetPuid,
            out long? targetCid,
            out AuthType authType,
            ref bool isChildInFamily)
        {
            // For family OBO scenarios, CID is not in the claim.
            targetCid = null;

            // If there is no family token, then targetPuid is the authorizing puid and targetCid is the authorizingCid
            if (familyToken == null)
            {
                targetPuid = authorizingPuid;
                targetCid = authorizingCid;
                authType = AuthType.MsaSelf;
                return null;
            }

            if (this.familyClaimsParser.TryParse(familyToken, out IFamilyClaims familyClaims))
            {
                if (!familyClaims.CheckIsValid())
                {
                    targetPuid = 0;
                    authType = AuthType.None;
                    return this.CreateErrorAndLog(ErrorCode.InvalidClientCredentials, "Family claims not valid.");
                }

                if (!familyClaims.ParentChildRelationshipIsClaimed(authorizingPuid, familyClaims.TargetPuid.Value))
                {
                    targetPuid = 0;
                    authType = AuthType.None;
                    return this.CreateErrorAndLog(ErrorCode.InvalidClientCredentials, "User and target do not have a parent-child relationship.");
                }

                targetPuid = familyClaims.TargetPuid.Value;

                // claims state the target is a child of the parent
                isChildInFamily = true;

                if (targetPuid == authorizingPuid)
                {
                    authType = AuthType.MsaSelf;
                }
                else
                {
                    authType = AuthType.OnBehalfOf;
                }

                return null;
            }

            targetPuid = 0;
            authType = AuthType.None;
            return this.CreateErrorAndLog(ErrorCode.InvalidClientCredentials, "Not able to parse family token.");
        }

        /// <summary>
        ///     Validates the user proxy ticket passed in an incoming request by calling RPS through a helper library.
        /// </summary>
        /// <param name="siteName">Name of the site.</param>
        /// <param name="appAllowList">Additional site id authorization map</param>
        /// <param name="applicationId">calling application id</param>
        /// <param name="proxyTicket">The user proxy ticket provided by caller</param>
        /// <param name="propertyBag">The property bag that was used to validate the access token</param>
        /// <param name="puid">The PUID retrieved from user proxy ticket</param>
        /// <param name="cid">The CID retrieved from the user proxy ticket</param>
        /// <param name="userProxyTicket">The user proxy ticket returned after validation. MSA recommends using this to forward on.</param>
        /// <param name="countryRegion">The authenticated user's country/region.</param>
        /// <param name="birthDate">The birth date of the user retrieved from user proxy ticket.</param>
        /// <param name="isChildInFamily">if set to <c>true</c>, indicates the user is a child in a family.</param>
        /// <param name="legalAgeGroupValue">The legal age group value of the user.</param>
        /// <returns>
        ///     Null if request was successfully validated, otherwise <see cref="Error" /> instance containing error details
        /// </returns>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private Error ValidateProxyTicket(
            string siteName,
            IDictionary<string, string> appAllowList,
            string applicationId,
            string proxyTicket,
            RpsPropertyBag propertyBag,
            out long puid,
            out long cid,
            out string userProxyTicket,
            out string countryRegion,
            out DateTimeOffset? birthDate,
            out bool isChildInFamily,
            out LegalAgeGroup? legalAgeGroupValue)
        {
            puid = 0;
            cid = 0;
            userProxyTicket = null;
            countryRegion = null;
            birthDate = null;
            isChildInFamily = false;
            legalAgeGroupValue = null;

            if (string.IsNullOrWhiteSpace(proxyTicket))
            {
                return this.CreateErrorAndLog(ErrorCode.MissingClientCredentials, "A user proxy ticket is required to access the requested resource.");
            }

            // When PCD is calling, they are providing an access token for the S2S portion, and optionally a proxy ticket here that is the MSA subject. Since the
            // proxy ticket doesn't match the access token (it was pulled from the PRC site (or for now, AMC dashboard) we need to authorize the proxy ticket
            // against a different site id.
            long[] additionalAllowedApps = null;
            if (appAllowList != null && appAllowList.TryGetValue(applicationId, out string additionalSiteIdsStr))
            {
                additionalAllowedApps = additionalSiteIdsStr.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries).Select(long.Parse).ToArray();
            }

            try
            {
                using (RpsAuthResult result = this.GetAuthResultWithAdditionalAllowedApps(siteName, proxyTicket, propertyBag, additionalAllowedApps))
                {
                    if (result == null)
                    {
                        return this.CreateErrorAndLog(ErrorCode.Unknown, "Unknown error occurred when validating proxy ticket. Message: RpsAuthResult was null");
                    }

                    if (!result.MemberId.HasValue)
                    {
                        return this.CreateErrorAndLog(ErrorCode.Unknown, "Error occurred when validating proxy ticket. Message: The MemberId was null");
                    }

                    if (!result.Cid.HasValue)
                    {
                        return this.CreateErrorAndLog(ErrorCode.Unknown, "Error occurred when validating proxy ticket. Message: The Cid was null");
                    }

                    puid = result.MemberId.Value;
                    cid = result.Cid.Value;
                    isChildInFamily = IsChildInFamily(result);

                    // Reference: https://microsoft.sharepoint.com/teams/liveid/docs/RPS6.7/Profile_Properties.html#country
                    // null countries/regions are possible (new profiles don't have them by default).
                    countryRegion = result[RpsTicketProfileField.Country] as string;
                    this.logger.Verbose(ComponentName, "Country/Region read from RPS Ticket: {0}", string.IsNullOrWhiteSpace(countryRegion) ? "null" : countryRegion);

                    // Reference: https://microsoft.sharepoint.com/teams/liveid/docs/RPS6.7/Profile_Properties.html#birthdate
                    var ticketBirthDate = result[RpsTicketProfileField.Birthdate] as DateTime?;

                    if (ticketBirthDate == null)
                    {
                        // may not require a birthdate (to calculate age) for all cases, so this is only a warning
                        this.logger.Warning(ComponentName, "birthDate value not found when reading from rps user proxy ticket");
                    }
                    else
                    {
                        // Assume birth date is UTC to be consistent with how other MEE services do this
                        birthDate = DateTime.SpecifyKind(ticketBirthDate.Value, DateTimeKind.Utc);
                    }

                    if (result.IsProxyTicket)
                    {
                        userProxyTicket = proxyTicket;
                    }
                    else
                    {
                        userProxyTicket = result[RpsTicketField.ProxyTicket] as string;
                    }

                    object rawAgeGroup = result[RpsTicketField.AgeGroup];
                    if (rawAgeGroup != null)
                    {
                        legalAgeGroupValue = (LegalAgeGroup)Convert.ToByte(rawAgeGroup);
                    }
                    else
                    {
                        this.logger.Error(nameof(PrivacyExperienceAuthenticationFilter), "Failed to get AgeGroup from the rps ticket");
                    }
                }
            }
            catch (AuthNException e)
            {
                // This is when the validation fails due to an invalid ticket passed by caller
                var error = new Error(
                    e.ErrorCode == AuthNErrorCode.ExpiredTicket ? ErrorCode.TimeWindowExpired : ErrorCode.InvalidClientCredentials,
                    string.Format(CultureInfo.InvariantCulture, "Request contained an invalid or unauthorized proxy ticket. ErrorCode: {0}", e.ErrorCode));
                this.logger.Information(ComponentName, e, error.Message);
                return error;
            }
            catch (Exception e)
            {
                return this.CreateErrorAndLog(
                    ErrorCode.Unknown,
                    string.Format(CultureInfo.InvariantCulture, "Unknown error occurred when validating proxy ticket. Message: {0}", e.Message),
                    e.ToString());
            }

            this.logger.Verbose(ComponentName, "Proxy ticket validation successful");
            return null;
        }

        private class ApiAndRoute
        {
            public string ApiName { get; }

            public string RoutePath { get; }

            public ApiAndRoute(string apiName, string routePath)
            {
                this.ApiName = apiName;
                this.RoutePath = routePath;
            }

            /// <inheritdoc />
            public override string ToString()
            {
                return $"Api Name: {this.ApiName}, Api RoutePath: {this.RoutePath}";
            }
        }

        private static IPrivacyConfigurationManager GetPrivacyConfigurationManager(HttpRequestMessage request)
        {
            IDependencyScope scope = request.GetDependencyScope();
            return scope.GetService(typeof(IPrivacyConfigurationManager)) as IPrivacyConfigurationManager;
        }

        private static IPrivacyExperienceServiceConfiguration GetPrivacyExperienceServiceConfiguration(HttpRequestMessage request)
        {
            return GetPrivacyConfigurationManager(request).PrivacyExperienceServiceConfiguration;
        }

        private static Error HandleAadS2SAuthErrors(IAadS2SAuthResult aadS2SAuthResult, string apiRoute, ILogger logger)
        {
            string requestIsNotAuthorized = $"Request is not authorized. Api RoutePath: {apiRoute}";
            Error authenticationError = null;
            if (aadS2SAuthResult == null)
            {
                authenticationError = new Error(ErrorCode.Unauthorized, requestIsNotAuthorized) { ErrorDetails = "PFT Result was null" };
            }
            else if (!aadS2SAuthResult.Succeeded)
            {
                Error innerError = null;

                if (aadS2SAuthResult.DiagnosticLogs?.Count > 0)
                {
                    string logs = string.Join(", ", aadS2SAuthResult.DiagnosticLogs);
                    logger.Error(nameof(PrivacyExperienceAuthenticationFilter), logs);

                    var errorEvent = new ErrorEvent
                    {
                        ComponentName = nameof(PrivacyExperienceAuthenticationFilter),
                        ErrorCode = ErrorCode.Unauthorized.ToString(),
                        ErrorName = "InvalidAadToken",
                        ErrorMessage = aadS2SAuthResult.Exception?.ToString(),
                        ErrorDetails = logs,
                        ErrorType = "Authorization",
                        ErrorMethod = nameof(HandleAadS2SAuthErrors)
                    };
                    errorEvent.LogError();

                    innerError = new Error(ErrorCode.Unauthorized, logs);
                }

                authenticationError = new Error(ErrorCode.Unauthorized, requestIsNotAuthorized) { ErrorDetails = aadS2SAuthResult?.Exception?.ToString(), InnerError = innerError };
            }

            return authenticationError;
        }

        private static bool IsCallerProxyTicketAuthorized(IPrivacyExperienceServiceConfiguration privacyExperienceServiceConfiguration, long applicationId)
        {
            return privacyExperienceServiceConfiguration.SiteIdToCallerName.ContainsKey(applicationId.ToString(CultureInfo.InvariantCulture));
        }

        private static bool IsChildInFamily(RpsAuthResult result)
        {
            switch (result.FamilyRole)
            {
                // The user is a child and in a family
                case RpsFamilyRole.Child:
                    return true;
            }

            // If the user is not in a family, this will always be false, even if they are actually a child.
            // This has been verified by doing the following:
            // 1. Add a user to a family as a child
            // 2. Verify the role from the ticket is 'Child'
            // 3. Remove the child user from the family
            // 4. Verify the role from the ticket is 'None'
            return false;
        }

        private static bool TryValidateSiteIdToCallerName(
            IPrivacyExperienceServiceConfiguration privacyExperienceServiceConfiguration,
            string siteId,
            out string callerName)
        {
            return privacyExperienceServiceConfiguration.SiteIdToCallerName.TryGetValue(siteId, out callerName);
        }
    }
}
