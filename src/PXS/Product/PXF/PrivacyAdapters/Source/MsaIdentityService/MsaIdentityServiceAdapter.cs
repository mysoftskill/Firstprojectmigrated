// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IdentityModel.Tokens.Jwt;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Serialization;

    using Microsoft.Membership.MemberServices.Adapters.Common;
    using Microsoft.Membership.MemberServices.Adapters.Common.DelegatingExecutors;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.ProfileIdentityService;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.PrivacyOperation.Contracts.PrivacySubject;

    /// <summary>
    ///     Claim identifier is additional information to claim in the verifier token.
    /// </summary>
    /// <remarks>
    ///     Claim identifiers must match what is white-listed for us to send. This is defined in the spec @
    ///     https://microsoft.sharepoint.com/teams/ngphome/ngpx/execution/Official%20Documents/NGPX%20Technical%20Specifications/Data%20Subject%20Request%20Command%20Hardening.docx?web=1
    /// </remarks>
    public enum ClaimIdentifier
    {
        /// <summary>
        ///     The xuid
        /// </summary>
        Xuid,

        /// <summary>
        ///     The Predicate
        /// </summary>
        Pred,

        /// <summary>
        ///     The request id (aka command id)
        /// </summary>
        Rid,

        /// <summary>
        ///     The azure storage location
        /// </summary>
        Azsp,

        /// <summary>
        ///     The request ids (aka command ids)
        /// </summary>
        Rids,

        /// <summary>
        ///     The data types associated with a scoped delete
        /// </summary>
        Dts,
    }

    /// <inheritdoc cref="IMsaIdentityServiceAdapter" />
    public class MsaIdentityServiceAdapter : IMsaIdentityServiceAdapter
    {
        /// <summary>
        ///     The verifier claim is used in the request to MSA to claim additional information about the request
        /// </summary>
        public class VerifierClaim
        {
            /// <summary>
            ///     The identifier of the claim, such as Xuid or Pred
            /// </summary>
            public ClaimIdentifier Identifier { get; }

            /// <summary>
            ///     The value of the claim. May be null or empty if there is nothing to claim
            /// </summary>
            public string Value { get; }

            /// <summary>
            ///     The verifier claim for the request
            /// </summary>
            /// <param name="claimIdentifier">Claim identifier maps to the enum <see cref="ClaimIdentifier" /></param>
            /// <param name="value">The value of the claim, may be null or empty if not present</param>
            public VerifierClaim(ClaimIdentifier claimIdentifier, string value)
            {
                this.Identifier = claimIdentifier;
                this.Value = value;
            }
        }

        // Per spec, max # of chars per claim. MSA rejects anything bigger.
        public const int MaxValuePerClaim = 2048;

        private const string PuidHexFormatter = "X16";

        private static readonly Models.AdapterResponse<string> partnerAdapterDisabledError = new Models.AdapterResponse<string>
        {
            Error = new AdapterError(AdapterErrorCode.PartnerDisabled, "Partner Adapter is disabled.", (int)HttpStatusCode.MethodNotAllowed)
        };

        private readonly IMsaIdentityServiceAdapterConfiguration adapterConfig;

        private readonly ICounterFactory counterFactory;

        private readonly ICredentialServiceClient credentialServiceClient;

        private readonly ILogger logger;

        private readonly IProfileServiceClient profileServiceClient;

        /// <summary>
        ///     Initialize a new instance of the Msa IdentityService Adapter
        /// </summary>
        /// <param name="certProvider">The cert provider</param>
        /// <param name="pxsConfig">The pxs configuration</param>
        /// <param name="logger">The logger</param>
        /// <param name="counterFactory">The counter factory</param>
        /// <param name="msaIdentityServiceClientFactory">msa identity service client factory</param>
        public MsaIdentityServiceAdapter(
            IPrivacyConfigurationManager pxsConfig,
            ICertificateProvider certProvider,
            ILogger logger,
            ICounterFactory counterFactory,
            IMsaIdentityServiceClientFactory msaIdentityServiceClientFactory)
        {
            if (pxsConfig == null)
            {
                throw new ArgumentNullException(nameof(pxsConfig));
            }

            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.counterFactory = counterFactory ?? throw new ArgumentNullException(nameof(counterFactory));

            this.adapterConfig = pxsConfig.AdaptersConfiguration.MsaIdentityServiceAdapterConfiguration;
            IMsaIdentityServiceConfiguration msaSiteConfig = pxsConfig.MsaIdentityServiceConfiguration;

            this.credentialServiceClient = msaIdentityServiceClientFactory.CreateCredentialServiceClient(this.adapterConfig, msaSiteConfig, certProvider);
            this.profileServiceClient = msaIdentityServiceClientFactory.CreateProfileServiceClient(this.adapterConfig, msaSiteConfig, certProvider);
        }

        /// <inheritdoc />
        public Task<Models.AdapterResponse<string>> GetGdprAccountCloseVerifierAsync(Guid commandId, long puid, string preVerifierToken, string xuid)
        {
            Task<Models.AdapterResponse<string>> adapterEnabledError = this.CheckAdapterConfiguration();
            if (adapterEnabledError != null)
            {
                return adapterEnabledError;
            }

            const string ApiName = "GetGdprAccountCloseVerifier";
            var operation = eGDPR_VERIFIER_OPERATION.AccountClose;
            var targetIdentifier = new tagPASSID { pit = PASSIDTYPE.PASSID_PUID, bstrID = puid.ToString(PuidHexFormatter) };
            IDictionary<string, string> additionalClaims = CreateAdditionalClaims(
                new VerifierClaim(ClaimIdentifier.Xuid, xuid),
                new VerifierClaim(ClaimIdentifier.Rid, commandId.ToString()));

            // For account close to transmit preverifier tokens in optional params. Spec:
            // https://microsoft.sharepoint.com/teams/ngphome/ngpx/execution/Official%20Documents/NGPX%20Technical%20Specifications/Data%20Subject%20Request%20Command%20Hardening.docx?web=1
            string optionalParams = $"<RequestPreVerifier>{preVerifierToken}</RequestPreVerifier>";

            OutgoingApiEventWrapper outgoingApiEvent = OutgoingApiEventWrapper.CreateSoapEvent(
                this.adapterConfig.PartnerId,
                ApiName,
                this.credentialServiceClient.TargetUri,
                puid);
            var unauthSessionID = Guid.NewGuid().ToString("N");
            outgoingApiEvent.ExtraData["UnauthSessionID"] = unauthSessionID;

            IWcfRequestHandler executor = this.CreateExecutor(ApiName, outgoingApiEvent);
            Task<string> task = executor.ExecuteAsync(
                () => this.credentialServiceClient.GetGdprVerifierAsync(targetIdentifier, operation, additionalClaims, unauthSessionID, optionalParams));
            return this.HandleErrorsAsync(() => task, ApiName);
        }

        /// <inheritdoc />
        public Task<Models.AdapterResponse<string>> GetGdprDeviceDeleteVerifierAsync(Guid commandId, long globalDeviceId, string predicate)
        {
            Task<Models.AdapterResponse<string>> adapterEnabledError = this.CheckAdapterConfiguration();
            if (adapterEnabledError != null)
            {
                return adapterEnabledError;
            }

            const string ApiName = "GetGdprDeviceDeleteVerifier";

            OutgoingApiEventWrapper outgoingApiEvent = OutgoingApiEventWrapper.CreateSoapEvent(
                this.adapterConfig.PartnerId,
                ApiName,
                this.credentialServiceClient.TargetUri,
                userPuid: null);
            var unauthSessionID = Guid.NewGuid().ToString("N");
            outgoingApiEvent.ExtraData["UnauthSessionID"] = unauthSessionID;

            var operation = eGDPR_VERIFIER_OPERATION.Delete;

            // Even though this isn't a user puid, this is how global device id is transmitted
            var targetIdentifier = new tagPASSID { pit = PASSIDTYPE.PASSID_PUID, bstrID = globalDeviceId.ToString(PuidHexFormatter) };
            IDictionary<string, string> additionalClaims = CreateAdditionalClaims(
                new VerifierClaim(ClaimIdentifier.Pred, predicate),
                new VerifierClaim(ClaimIdentifier.Rid, commandId.ToString()));

            IWcfRequestHandler executor = this.CreateExecutor(ApiName, outgoingApiEvent);
            Task<string> task = executor.ExecuteAsync(
                () => this.credentialServiceClient.GetGdprVerifierAsync(targetIdentifier, operation, additionalClaims, unauthSessionID, null));
            return this.HandleErrorsAsync(() => task, ApiName);
        }

        /// <inheritdoc />
        public Task<Models.AdapterResponse<string>> GetGdprExportVerifierAsync(Guid commandId, IPxfRequestContext requestContext, Uri storageDestination, string xuid)
        {
            Task<Models.AdapterResponse<string>> adapterEnabledError = this.CheckAdapterConfiguration();
            if (adapterEnabledError != null)
            {
                return adapterEnabledError;
            }

            const string ApiName = "GetGdprExportVerifier";

            OutgoingApiEventWrapper outgoingApiEvent = OutgoingApiEventWrapper.CreateSoapEvent(
                this.adapterConfig.PartnerId,
                ApiName,
                this.credentialServiceClient.TargetUri,
                requestContext.TargetPuid);
            var unauthSessionID = Guid.NewGuid().ToString("N");
            outgoingApiEvent.ExtraData["UnauthSessionID"] = unauthSessionID;

            var operation = eGDPR_VERIFIER_OPERATION.Export;
            var targetIdentifier = new tagPASSID { pit = PASSIDTYPE.PASSID_PUID, bstrID = requestContext.TargetPuid.ToString(PuidHexFormatter) };
            IDictionary<string, string> additionalClaims = CreateAdditionalClaims(
                new VerifierClaim(ClaimIdentifier.Xuid, xuid),
                new VerifierClaim(ClaimIdentifier.Rid, commandId.ToString()),
                new VerifierClaim(ClaimIdentifier.Azsp, storageDestination.ToString()));

            string optionalParams = !string.IsNullOrEmpty(requestContext.FamilyJsonWebToken) ? "<FamilyAuth>true</FamilyAuth>" : null;

            IWcfRequestHandler executor = this.CreateExecutor(ApiName, outgoingApiEvent);
            Task<string> task = executor.ExecuteAsync(
                () => this.credentialServiceClient.GetGdprVerifierAsync(targetIdentifier, operation, additionalClaims, unauthSessionID, optionalParams, requestContext.UserProxyTicket));
            return this.HandleErrorsAsync(() => task, ApiName);
        }

        /// <inheritdoc />
        public Task<Models.AdapterResponse<string>> GetGdprUserDeleteVerifierAsync(IList<Guid> commandIds, IPxfRequestContext requestContext, string xuid, string predicate, string dataType)
        {
            Task<Models.AdapterResponse<string>> adapterEnabledError = this.CheckAdapterConfiguration();
            if (adapterEnabledError != null)
            {
                return adapterEnabledError;
            }

            const string ApiName = "GetGdprUserDeleteVerifier";

            OutgoingApiEventWrapper outgoingApiEvent = OutgoingApiEventWrapper.CreateSoapEvent(
                this.adapterConfig.PartnerId,
                ApiName,
                this.credentialServiceClient.TargetUri,
                requestContext.TargetPuid);
            var unauthSessionID = Guid.NewGuid().ToString("N");
            outgoingApiEvent.ExtraData["UnauthSessionID"] = unauthSessionID;

            eGDPR_VERIFIER_OPERATION operation;
            if (string.IsNullOrEmpty(dataType))
            {
                operation = eGDPR_VERIFIER_OPERATION.Delete;
            }
            else
            {
                operation = eGDPR_VERIFIER_OPERATION.ScopedDelete;
            }

            var targetIdentifier = new tagPASSID { pit = PASSIDTYPE.PASSID_PUID, bstrID = requestContext.TargetPuid.ToString(PuidHexFormatter) };

            VerifierClaim commandClaim;
            if (commandIds.Count == 1)
            {
                commandClaim = new VerifierClaim(ClaimIdentifier.Rid, commandIds.First().ToString());
            }
            else
            {
                commandClaim = new VerifierClaim(ClaimIdentifier.Rids, string.Join(",", commandIds));
            }

            IDictionary<string, string> additionalClaims = CreateAdditionalClaims(
                new VerifierClaim(ClaimIdentifier.Xuid, xuid),
                new VerifierClaim(ClaimIdentifier.Pred, predicate),
                new VerifierClaim(ClaimIdentifier.Dts, dataType),
                commandClaim);

            string optionalParams = !string.IsNullOrEmpty(requestContext.FamilyJsonWebToken) ? "<FamilyAuth>true</FamilyAuth>" : null;

            IWcfRequestHandler executor = this.CreateExecutor(ApiName, outgoingApiEvent);

            Task<string> task = executor.ExecuteAsync(
                () => this.credentialServiceClient.GetGdprVerifierAsync(targetIdentifier, operation, additionalClaims, unauthSessionID, optionalParams, requestContext.UserProxyTicket));
            return this.HandleErrorsAsync(() => task, ApiName);
        }

        /// <inheritdoc />
        public Task<Models.AdapterResponse<string>> GetGdprUserDeleteVerifierWithPreverifierAsync(
            Guid commandId,
            IPxfRequestContext requestContext,
            string preVerifier,
            string xuid,
            string predicate,
            string dataType)
        {
            Task<Models.AdapterResponse<string>> adapterEnabledError = this.CheckAdapterConfiguration();
            if (adapterEnabledError != null)
            {
                return adapterEnabledError;
            }

            if (string.IsNullOrEmpty(preVerifier))
            {
                throw new ArgumentException("PreVerifier cannot be null or empty");
            }

            string ApiName = "GetGdprUserDeleteVerifierWithPreverifier";

            OutgoingApiEventWrapper outgoingApiEvent = OutgoingApiEventWrapper.CreateSoapEvent(
                this.adapterConfig.PartnerId,
                ApiName,
                this.credentialServiceClient.TargetUri,
                requestContext.TargetPuid);
            var unauthSessionID = Guid.NewGuid().ToString("N");
            outgoingApiEvent.ExtraData["UnauthSessionID"] = unauthSessionID;

            var operation = eGDPR_VERIFIER_OPERATION.Delete;
            var targetIdentifier = new tagPASSID { pit = PASSIDTYPE.PASSID_PUID, bstrID = null };
            var commandClaim = new VerifierClaim(ClaimIdentifier.Rid, commandId.ToString());

            IDictionary<string, string> additionalClaims = CreateAdditionalClaims(
                new VerifierClaim(ClaimIdentifier.Xuid, xuid),
                new VerifierClaim(ClaimIdentifier.Pred, predicate),
                new VerifierClaim(ClaimIdentifier.Dts, dataType),
                commandClaim);

            StringBuilder sb = new StringBuilder();
            sb.Append("<Options><RequestPreVerifier>");
            sb.Append(preVerifier);
            sb.Append("</RequestPreVerifier><SupportRefresh>True</SupportRefresh></Options>");
            string optionalParams = sb.ToString();

            IWcfRequestHandler executor = this.CreateExecutor(ApiName, outgoingApiEvent);

            Task<string> task = executor.ExecuteAsync(
                () => this.credentialServiceClient.GetGdprVerifierAsync(targetIdentifier, operation, additionalClaims, unauthSessionID, optionalParams, requestContext.UserProxyTicket, true));
            return this.HandleErrorsAsync(() => task, ApiName);
        }

        /// <inheritdoc />
        public Task<Models.AdapterResponse<string>> GetGdprUserDeleteVerifierWithRefreshClaimAsync(IPxfRequestContext requestContext)
        {
            const string ApiName = "GetGdprUserDeleteVerifierWithFreshClaim";
            
            var targetIdentifier = new tagPASSID {
                pit = PASSIDTYPE.PASSID_PUID,
                bstrID = requestContext.TargetPuid.ToString(PuidHexFormatter)
            };
            
            IDictionary<string, string> additionalClaims = null;
            string optionalParams = "<Options><SupportRefresh>True</SupportRefresh></Options>";

            return this.GetGdprVerifierWithPreverifierAsync(ApiName, requestContext, targetIdentifier, additionalClaims, optionalParams);
        }

        /// <inheritdoc />
        public Task<Models.AdapterResponse<string>> RenewGdprUserDeleteVerifierUsingPreverifierAsync(IPxfRequestContext requestContext, string preVerifier)
        {
            if (string.IsNullOrEmpty(preVerifier))
            {
                throw new ArgumentException("PreVerifier cannot be null or empty");
            }

            const string ApiName = "RenewGdprUserDeleteVerifierUsingPreverifier";

            var targetIdentifier = new tagPASSID {
                pit = PASSIDTYPE.PASSID_PUID,
                bstrID = null
            };

            IDictionary<string, string> additionalClaims = null;

            StringBuilder sb = new StringBuilder();
            sb.Append("<Options><RequestPreVerifier>");
            sb.Append(preVerifier);
            sb.Append("</RequestPreVerifier><SupportRefresh>True</SupportRefresh></Options>");
            string optionalParams = sb.ToString();

            return this.GetGdprVerifierWithPreverifierAsync(ApiName, requestContext, targetIdentifier, additionalClaims, optionalParams);
        }

        public async Task<Models.AdapterResponse<IProfileAttributesUserData>> GetProfileAttributesAsync(
            IPxfRequestContext requestContext,
            params ProfileAttribute[] attributes)
        {
            Task<Models.AdapterResponse<IProfileAttributesUserData>> adapterEnabledError = this.CheckAdapterConfiguration<IProfileAttributesUserData>();
            if (adapterEnabledError != null)
            {
                return await adapterEnabledError.ConfigureAwait(false);
            }

            const string ApiName = "GetProfileAttributes";
            OutgoingApiEventWrapper outgoingApiEvent = OutgoingApiEventWrapper.CreateSoapEvent(
                this.adapterConfig.PartnerId,
                ApiName,
                this.credentialServiceClient.TargetUri,
                null);

            try
            {
                IWcfRequestHandler executor = this.CreateExecutor(ApiName, outgoingApiEvent);
                string response = await executor.ExecuteAsync(
                        () => this.profileServiceClient.GetProfileByAttributesAsync(requestContext.TargetPuid.ToString(PuidHexFormatter), GetAttributeStringParameter()))
                    .ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(response))
                {
                    return new Models.AdapterResponse<IProfileAttributesUserData>
                    {
                        Error = new AdapterError(AdapterErrorCode.EmptyResponse, "Empty response from partner.", (int)HttpStatusCode.InternalServerError)
                    };
                }

                var serializer = new XmlSerializer(typeof(userData));
                userData res;
                using (var reader = new StringReader(response))
                {
                    res = (userData)serializer.Deserialize(reader);
                }

                var data = new Dictionary<ProfileAttribute, string>();
                foreach (userDataPropertyCollection propertyCollection in res.propertyCollection)
                {
                    foreach (userDataPropertyCollectionProperty property in propertyCollection.property)
                    {
                        data[ProfileAttributesExtension.ToAttributeEnum(propertyCollection.name, property.name)] = property.Value;
                    }
                }

                return new Models.AdapterResponse<IProfileAttributesUserData>
                {
                    Result = new ProfileAttributesUserData(data)
                };
            }
            catch (Exception e)
            {
                this.logger.Error(nameof(MsaIdentityServiceAdapter), e, "Exception occurred in API: '{0}'.", ApiName);
                AdapterError error;

                if (e is PrivacySubjectInvalidException)
                {
                    error = new AdapterError(AdapterErrorCode.BadRequest, e.ToString(), (int)HttpStatusCode.NotFound);
                }
                else
                {
                    error = new AdapterError(AdapterErrorCode.Unknown, e.ToString(), (int)HttpStatusCode.InternalServerError);
                }

                return new Models.AdapterResponse<IProfileAttributesUserData>
                {
                    Error = error
                };
            }

            string GetAttributeStringParameter()
            {
                return attributes.Any(att => att == ProfileAttribute.All) ? "*" : attributes.ToAttributeListString();
            }
        }

        public async Task<Models.AdapterResponse<ISigninNameInformation>> GetSigninNameInformationAsync(long puid)
        {
            Task<Models.AdapterResponse<ISigninNameInformation>> adapterEnabledError = this.CheckAdapterConfiguration<ISigninNameInformation>();
            if (adapterEnabledError != null)
            {
                return await adapterEnabledError.ConfigureAwait(false);
            }

            Models.AdapterResponse<IEnumerable<ISigninNameInformation>> response = await this.GetSigninNameInformationsAsync(new[] { puid }).ConfigureAwait(false);
            if (!response.IsSuccess)
            {
                return new Models.AdapterResponse<ISigninNameInformation>
                {
                    Error = response.Error
                };
            }

            return new Models.AdapterResponse<ISigninNameInformation>
            {
                Result = response.Result?.FirstOrDefault()
            };
        }

        public async Task<Models.AdapterResponse<IEnumerable<ISigninNameInformation>>> GetSigninNameInformationsAsync(IEnumerable<long> puids)
        {
            Task<Models.AdapterResponse<IEnumerable<ISigninNameInformation>>> adapterEnabledError = this.CheckAdapterConfiguration<IEnumerable<ISigninNameInformation>>();
            if (adapterEnabledError != null)
            {
                return await adapterEnabledError.ConfigureAwait(false);
            }

            const string ApiName = "GetSigninNamesAndCidsForNetId";

            string netIds = string.Join(",", puids.Select(p => p.ToString(PuidHexFormatter)));

            OutgoingApiEventWrapper outgoingApiEvent = OutgoingApiEventWrapper.CreateSoapEvent(
                this.adapterConfig.PartnerId,
                ApiName,
                this.credentialServiceClient.TargetUri,
                null);
            var unauthSessionID = Guid.NewGuid().ToString("N");
            outgoingApiEvent.ExtraData["UnauthSessionID"] = unauthSessionID;

            try
            {
                IWcfRequestHandler executor = this.CreateExecutor(ApiName, outgoingApiEvent);
                string response = await executor.ExecuteAsync(() => this.credentialServiceClient.GetSigninNamesAndCidsForNetIdAsync(netIds, unauthSessionID)).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(response))
                {
                    return new Models.AdapterResponse<IEnumerable<ISigninNameInformation>>
                    {
                        Error = new AdapterError(AdapterErrorCode.EmptyResponse, "Empty response from partner.", (int)HttpStatusCode.InternalServerError)
                    };
                }

                var serializer = new XmlSerializer(typeof(NETID2Name));
                NETID2Name res;
                using (var reader = new StringReader(response))
                {
                    res = (NETID2Name)serializer.Deserialize(reader);
                }

                IEnumerable<SigninNameInformation> results = res?.Items?.Select(
                    name => new SigninNameInformation
                    {
                        Cid = !string.IsNullOrWhiteSpace(name.CID) ? long.Parse(name.CID, NumberStyles.AllowHexSpecifier) : (long?)null,
                        Puid = !string.IsNullOrWhiteSpace(name.NetID) ? long.Parse(name.NetID, NumberStyles.AllowHexSpecifier) : (long?)null,
                        CredFlags = !string.IsNullOrWhiteSpace(name.CredFlags) ? int.Parse(name.CredFlags, NumberStyles.AllowHexSpecifier) : (int?)null,
                        SigninName = name.Value
                    }).ToList();

                return new Models.AdapterResponse<IEnumerable<ISigninNameInformation>>
                {
                    Result = results
                };
            }
            catch (Exception ex)
            {
                this.logger.Error(nameof(MsaIdentityServiceAdapter), ex, "Exception occurred in API: '{0}'.", ApiName);

                return new Models.AdapterResponse<IEnumerable<ISigninNameInformation>>
                {
                    Error = new AdapterError(AdapterErrorCode.Unknown, ex.ToString(), (int)HttpStatusCode.InternalServerError)
                };
            }
        }

        /// <summary>
        /// GetExpiryTimeFromVerifier
        /// </summary>
        /// <param name="verifier">PreVerifier with refresh claim.</param>
        /// <returns>An object that represents the date and time of Coordinated Universal Time (UTC).</returns>
        public static DateTimeOffset GetExpiryTimeFromVerifier(string verifier)
        {
            DateTimeOffset expiryTime = new DateTimeOffset();

            if (!string.IsNullOrEmpty(verifier))
            {
                try
                {
                    var token = new JwtSecurityToken(verifier);
                    var claim = token.Claims.Where(n => n.Type.Equals("refresh_token_expiry")).FirstOrDefault();
                    string expiryDate = claim?.Value;
                    if (!string.IsNullOrEmpty(expiryDate))
                    {
                        long expiry;
                        if (long.TryParse(expiryDate, out expiry))
                        {
                            expiryTime = DateTimeOffset.FromUnixTimeSeconds(expiry);
                        }
                    }
                }
                catch (Exception ex)
                {
                    IfxTraceLogger.Instance.Error(nameof(GetExpiryTimeFromVerifier), ex, "An exception occurred while trying to get expiry date from verifier.");
                    throw;
                }
            }

            return expiryTime.ToUniversalTime();
        }

        /// <summary>
        /// GetCidFromVerifier
        /// </summary>
        /// <param name="verifier">verifier</param>
        /// <returns></returns>
        public static long GetCidFromVerifier(string verifier)
        {
            long cidValue = 0;

            var token = new JwtSecurityToken(verifier);
            var claim = token.Claims.Where(n => n.Type.Equals("cid")).FirstOrDefault();

            string cidString = claim?.Value;

            if (!string.IsNullOrEmpty(cidString))
            {
                try
                {
                    return Convert.ToInt64(value: cidString, fromBase: 16);
                }
                catch (Exception ex)
                {
                    IfxTraceLogger.Instance.Error(nameof(GetCidFromVerifier), ex, $"Fail to parse '{cidString}' to Int64.");
                    throw;
                }
            }

            return cidValue;
        }

        private Task<Models.AdapterResponse<string>> CheckAdapterConfiguration()
        {
            return this.CheckAdapterConfiguration<string>();
        }

        private Task<Models.AdapterResponse<T>> CheckAdapterConfiguration<T>()
        {
            if (!this.adapterConfig.EnableAdapter)
            {
                if (this.adapterConfig.IgnoreErrors)
                {
                    this.logger.Warning(nameof(MsaIdentityServiceAdapter), "Adapter disabled and errors are ignored.");
                    return Task.FromResult(new Models.AdapterResponse<T>());
                }

                this.logger.Warning(nameof(MsaIdentityServiceAdapter), partnerAdapterDisabledError?.Error?.ToString() ?? "Adapter disabled");
                return Task.FromResult(
                    new Models.AdapterResponse<T>
                    {
                        Error = partnerAdapterDisabledError?.Error
                    });
            }

            return null;
        }

        /// <summary>
        ///     Creates a WCF executor pipeline which handles perf counters, logical operations, and retries.
        /// </summary>
        /// <param name="methodName">The name of the method to call.</param>
        /// <param name="outgoingApiEvent">The outgoing event to track logical operation execution.</param>
        /// <returns>A pipelined WCF executor.</returns>
        private IWcfRequestHandler CreateExecutor(string methodName, OutgoingApiEventWrapper outgoingApiEvent)
        {
            DelegatingWcfHandler retryHandler = new RetryWcfHandler(
                errorDetectionStrategy: WcfServiceClientDetectionStrategy.Instance,
                retryStrategyConfiguration: this.adapterConfig.RetryStrategyConfiguration,
                logger: this.logger,
                componentName: this.adapterConfig.CounterCategoryName,
                methodName: methodName);

            DelegatingWcfHandler logicalOperationHandler = new LogicalOperationMsaIdentityServiceHandler(apiEvent: outgoingApiEvent);

            DelegatingWcfHandler perfCounterHandler = new PerfCounterWcfHandler(
                counterFactory: this.counterFactory,
                componentName: this.adapterConfig.CounterCategoryName,
                methodName: methodName);

            DelegatingWcfHandler servicePointHandler = new ServicePointWcfHandler(
                configuration: this.adapterConfig.ServicePointConfiguration,
                endpointAddress: new Uri(outgoingApiEvent.TargetUri),
                counterFactory: this.counterFactory);

            return WcfClientFactory.Create(retryHandler, logicalOperationHandler, perfCounterHandler, servicePointHandler);
        }

        private async Task<Models.AdapterResponse<string>> HandleErrorsAsync(Func<Task<string>> request, string apiName)
        {
            try
            {
                string response = await request().ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(response))
                {
                    return new Models.AdapterResponse<string> { Result = response };
                }

                if (this.adapterConfig.IgnoreErrors)
                {
                    return new Models.AdapterResponse<string>();
                }

                return new Models.AdapterResponse<string>
                {
                    Error = new AdapterError(AdapterErrorCode.EmptyResponse, "Empty response from partner.", (int)HttpStatusCode.InternalServerError)
                };
            }
            catch (MsaIdentityServiceException idsapiException)
            {
                // https://microsoft.sharepoint.com/teams/liveid/docs/idsapi/SAPI_Error_Codes.html
                string innerXml = ((XmlElement)idsapiException.Detail)?.InnerXml;

                string errorMessage = $"SOAP InnerXml Error: {innerXml}" +
                                      $"Code: 0x{idsapiException.ErrorCode:X}, " +
                                      $"Description: {idsapiException.Description}, " +
                                      $"InternalErrorCode: 0x{idsapiException.InternalErrorCode:X}, " +
                                      $"InternalErrorText: {idsapiException.InternalErrorText}";
                this.logger.Error(nameof(MsaIdentityServiceAdapter), errorMessage);

                if (this.adapterConfig.IgnoreErrors)
                {
                    return new Models.AdapterResponse<string>();
                }

                switch (idsapiException.ErrorCode)
                {
                    // TODO: Task 14967748: Error handling for all MSA RVS error codes
                    // Error codes (hex values) can be searched for @ https://errors

                    // Caller not authorized usually indicates the proxy site id, or our own site id, is not authorized. Ex: MSA didn't add our site id (ie. PXS), or proxy site id (ie. AMC) to its allowed list.
                    case 0x80048105:
                        return new Models.AdapterResponse<string>
                        {
                            Error = new AdapterError(AdapterErrorCode.MsaCallerNotAuthorized, errorMessage, (int)HttpStatusCode.Unauthorized)
                        };

                    // User is not authorized
                    // Could happen if the request did not originate from TFA and auth policy wasn't MBI_SSL_SA (strong authenticated)
                    case 0x80045024:
                        return new Models.AdapterResponse<string>
                        {
                            Error = new AdapterError(AdapterErrorCode.MsaUserNotAuthorized, errorMessage, (int)HttpStatusCode.Unauthorized)
                        };

                    // The header in the SOAP request is not valid.
                    // This happens if our Test Site id originates the request. The full error description by MSA is:
                    // "The proxy site id isn't allowed because the id doesn't match the validated app id, or the proxy site id property on the app allow list doesn't list it."
                    case 0x80048101:
                        if (idsapiException.InternalErrorCode == 0x80049228)
                        {
                            return new Models.AdapterResponse<string>
                            {
                                Error = new AdapterError(AdapterErrorCode.TimeWindowExpired, errorMessage, (int)HttpStatusCode.Unauthorized)
                            };
                        }

                        return new Models.AdapterResponse<string>
                        {
                            Error = new AdapterError(AdapterErrorCode.MsaCallerNotAuthorized, errorMessage, (int)HttpStatusCode.Unauthorized)
                        };

                    default:

                        return new Models.AdapterResponse<string>
                        {
                            Error = new AdapterError(AdapterErrorCode.Unknown, !string.IsNullOrWhiteSpace(innerXml) ? innerXml : idsapiException.ToString(), (int)HttpStatusCode.InternalServerError)
                        };
                }
            }
            catch (Exception ex)
            {
                this.logger.Error(nameof(MsaIdentityServiceAdapter), ex, "Exception occurred in API: '{0}'.", apiName);

                if (this.adapterConfig.IgnoreErrors)
                {
                    return new Models.AdapterResponse<string>();
                }

                return new Models.AdapterResponse<string>
                {
                    Error = new AdapterError(AdapterErrorCode.Unknown, ex.ToString(), (int)HttpStatusCode.InternalServerError)
                };
            }
        }

        private static IDictionary<string, string> CreateAdditionalClaims(params VerifierClaim[] verifierClaims)
        {
            IDictionary<string, string> additionalClaimsDictionary = new Dictionary<string, string>();

            foreach (VerifierClaim claim in verifierClaims)
            {
                if (!string.IsNullOrWhiteSpace(claim.Value))
                {
                    if (claim.Value.Length > MaxValuePerClaim)
                    {
                        throw new NotSupportedException($"Claim for {claim.Identifier} must not exceed the claim limit of {MaxValuePerClaim}");
                    }

                    // make key lower case to match what partner expects per spec
                    additionalClaimsDictionary.Add(claim.Identifier.ToString().ToLowerInvariant(), claim.Value);
                }
            }

            return additionalClaimsDictionary;
        }

        private Task<Models.AdapterResponse<string>> GetGdprVerifierWithPreverifierAsync(
            string ApiName,
            IPxfRequestContext requestContext,
            tagPASSID targetIdentifier,
            IDictionary<string, string> additionalClaims,
            string optionalParams)
        {
            Task<Models.AdapterResponse<string>> adapterEnabledError = this.CheckAdapterConfiguration();
            if (adapterEnabledError != null)
            {
                return adapterEnabledError;
            }

            OutgoingApiEventWrapper outgoingApiEvent = OutgoingApiEventWrapper.CreateSoapEvent(
                this.adapterConfig.PartnerId,
                ApiName,
                this.credentialServiceClient.TargetUri,
                requestContext.TargetPuid == 0 ? (long?)null : requestContext.TargetPuid);
            var unauthSessionID = Guid.NewGuid().ToString("N");
            outgoingApiEvent.ExtraData["UnauthSessionID"] = unauthSessionID;

            eGDPR_VERIFIER_OPERATION operation = eGDPR_VERIFIER_OPERATION.Delete;

            IWcfRequestHandler executor = this.CreateExecutor(ApiName, outgoingApiEvent);

            Task<string> task = executor.ExecuteAsync(
                () => this.credentialServiceClient.GetGdprVerifierAsync(targetIdentifier, operation, additionalClaims, unauthSessionID, optionalParams, requestContext.UserProxyTicket, true));
           
            return this.HandleErrorsAsync(() => task, ApiName);
        }
    }
}
