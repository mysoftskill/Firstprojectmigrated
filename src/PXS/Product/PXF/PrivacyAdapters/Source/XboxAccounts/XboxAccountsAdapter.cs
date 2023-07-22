// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Formatting;
    using System.Security.Cryptography;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Converters;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Helpers;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts.Exceptions;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts.Models;
    using Microsoft.OSGS.HttpClientCommon;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.Windows.Services.AuthN.Client.S2S;
    using Microsoft.XboxLive.Auth.Json;
    using Microsoft.XboxLive.Auth.ProofKeys;

    /// <summary>
    ///     Public class for XboxAccountsAdapter
    /// </summary>
    public class XboxAccountsAdapter : IXboxAccountsAdapter
    {
        private const string ComponentName = nameof(XboxAccountsAdapter);

        private const string GetUserLookupInfoApiRelativePath = @"/users/{0}({1})/lookup";

        private const string GetUserLookupInfoMethodName = "GetXboxLiveUserLookupInfoAsync";

        private const string GetUserLookupInfoOperationName = "GetXboxLiveUserLookupInfo";

        private const string GetUsersLookupInfoApiRelativePath = @"/users/puid/lookup";

        private const string GetUsersLookupInfoMethodName = "GetXboxLiveUsersLookupInfoAsync";

        private const string GetUsersLookupInfoOperationName = "GetXboxLiveUsersLookupInfo";

        private const string GetXassTokenOperationName = "GetXassToken";

        private const string GetXstsTokenMethodName = "GetXstsTokenAsync";

        private const string GetXstsTokenOperationName = "GetXstsToken";

        private const string PartnerId = "XboxLive";

        private const string PuidOptionName = "puid";

        private const string XassRelyingParty = @"http://auth.xboxlive.com"; // lgtm[cs/non-https-url]

        private const string XassTokenRelativeUrl = @"/service/authenticate";

        private const string XstsRelyingParty = @"http://xboxlive.com"; // lgtm[cs/non-https-url]

        private const string XstsTokenRelativeUrl = @"/xsts/authorize";

        private const int xboxBatchLimit = 200;

        private static readonly CngKey privateSigningKey = CngKey.Create(
            algorithm: CngAlgorithm.ECDsaP256,
            keyName: null,
            creationParameters: new CngKeyCreationParameters { KeyUsage = CngKeyUsages.Signing });

        private static readonly SignaturePolicy xboxSignaturePolicy = new SignaturePolicy
        {
            Version = 1,
            ExtraHeaders = new string[] { },
            MaxBodyBytes = long.MaxValue,
            SupportedAlgorithms = new[] { JsonWebSigningAlgorithms.ECDSASHA256 }
        };

        private readonly IClock clock;

        private readonly IHttpClient httpClient;

        private readonly ILogger logger;

        private readonly IXboxAccountsAdapterConfiguration partnerConfiguration;

        private readonly IS2SAuthClient s2sAuthClient;

        private XassToken xassTokenCached;

        /// <summary>
        ///     Initializes a new instance of the <see cref="XboxAccountsAdapter" /> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="adapterPartnerConfiguration">The adapter configuration.</param>
        /// <param name="s2sAuthClient">The S2S authentication client.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="clock">The clock</param>
        public XboxAccountsAdapter(
            IHttpClient httpClient,
            IXboxAccountsAdapterConfiguration adapterPartnerConfiguration,
            IS2SAuthClient s2sAuthClient,
            ILogger logger,
            IClock clock)
        {
            httpClient.ThrowOnNull(nameof(httpClient));
            adapterPartnerConfiguration.ThrowOnNull(nameof(adapterPartnerConfiguration));
            s2sAuthClient.ThrowOnNull(nameof(s2sAuthClient));
            logger.ThrowOnNull(nameof(logger));
            clock.ThrowOnNull(nameof(clock));

            this.httpClient = httpClient;
            this.partnerConfiguration = adapterPartnerConfiguration;
            this.s2sAuthClient = s2sAuthClient;
            this.logger = logger;
            this.clock = clock;
        }

        /// <summary>
        ///     Gets Xbox Live user lookup info by the requestContext.
        /// </summary>
        /// <param name="requestContext">The request context</param>
        /// <returns>Xbox Live user lookup info.</returns>
        public async Task<AdapterResponse<string>> GetXuidAsync(IPxfRequestContext requestContext)
        {
            // Bug 15886417: Xbox calls temporarily disabled in PPE while we wait on access
            if (!this.partnerConfiguration.EnableAdapter)
            {
                return new AdapterResponse<string>();
            }

            requestContext.ThrowOnNull(nameof(requestContext));
            long puid = requestContext.TargetPuid;

            var adapterResponse = new AdapterResponse<string>();
            try
            {
                XassToken xassToken = await this.GetXassTokenAsync().ConfigureAwait(false);
                XstsToken xstsToken = await this.GetXstsTokenAsync(xassToken.Token, XstsRelyingParty).ConfigureAwait(false);
                XboxLiveUserLookupInfo lookupInfo = await this.GetXboxLiveUserLookupInfoAsync(PuidOptionName, puid.ToString(), xstsToken).ConfigureAwait(false);
                adapterResponse.Result = lookupInfo.Xuid;
            }
            catch (XboxUserAccountException e)
            {
                if (e.XErr == 2148916233)
                {
                    adapterResponse.Result = null;
                }
                else
                {
                    adapterResponse.Error = new AdapterError(AdapterErrorCode.Unknown, e.ToString(), 500);
                }
            }
            catch (XboxRequestException e)
            {
                adapterResponse.Error = new AdapterError(AdapterErrorCode.BadRequest, e.ToString(), 400);
            }
            catch (Exception ex)
            {
                adapterResponse.Error = new AdapterError(AdapterErrorCode.Unknown, ex.ToString(), 500);
            }

            return adapterResponse;
        }

        /// <inheritdoc />
        public async Task<AdapterResponse<Dictionary<long, string>>> GetXuidsAsync(IEnumerable<long> puids)
        {
            var adapterResponse = new AdapterResponse<Dictionary<long, string>>();
            var xuidByPuidDictionary = new Dictionary<long, string>();

            try
            {
                XassToken xassToken = await this.GetXassTokenAsync().ConfigureAwait(false);
                XstsToken xstsToken = await this.GetXstsTokenAsync(xassToken.Token, XstsRelyingParty).ConfigureAwait(false);

                var tasks = new List<Task<XboxLiveUsersLookupInfo>>();
                var batchedPuids = new List<long>(xboxBatchLimit);
                foreach (var puid in puids)
                {
                    batchedPuids.Add(puid);

                    if (batchedPuids.Count == xboxBatchLimit)
                    {
                        tasks.Add(this.GetXboxLiveUsersLookupInfoAsync(batchedPuids, xstsToken));
                        batchedPuids = new List<long>(xboxBatchLimit);
                    }
                }

                if (batchedPuids.Count > 0)
                {
                    tasks.Add(this.GetXboxLiveUsersLookupInfoAsync(batchedPuids, xstsToken));
                }

                await Task.WhenAll(tasks);
                foreach (var task in tasks)
                {
                    foreach (XboxLiveUserLookupInfo user in task.Result.Users)
                    {
                        xuidByPuidDictionary[user.Puid] = user.Xuid;
                    }
                }

                adapterResponse.Result = xuidByPuidDictionary;
            }
            catch (XboxUserAccountException e)
            {
                adapterResponse.Error = new AdapterError(AdapterErrorCode.Unknown, e.ToString(), 500);
            }
            catch (XboxRequestException e)
            {
                adapterResponse.Error = new AdapterError(AdapterErrorCode.InvalidInput, e.ToString(), 400);
            }

            return adapterResponse;
        }

        internal async Task<string> CreateXboxSignature(HttpRequestMessage request)
        {
            return await XboxAuthUtilities.CreateSignature(request, xboxSignaturePolicy, privateSigningKey).ConfigureAwait(false);
        }

        /// <summary>
        ///     Retrieves a XASS token using S2S authentication.
        /// </summary>
        /// <returns>XASS token.</returns>
        internal async Task<XassToken> GetXassTokenAsync()
        {
            if (this.xassTokenCached == null)
            {
                this.logger.Information(
                    nameof(XboxAccountsAdapter),
                    "Fetching new Xass token for the first time.");
                this.xassTokenCached = await this.FetchNewXassTokenAsync().ConfigureAwait(false);
            }
            else if (this.clock.UtcNow >= this.xassTokenCached.NotAfter.AddMinutes(-1 * this.partnerConfiguration.RefreshXassTokenBeforeExpiryMinutes))
            {
                this.logger.Information(
                    nameof(XboxAccountsAdapter),
                    $"Refreshing cached Xass token. Token expiry in {(this.xassTokenCached.NotAfter - this.clock.UtcNow).TotalSeconds} seconds.");
                this.xassTokenCached = await this.FetchNewXassTokenAsync().ConfigureAwait(false);
            }
            else if (this.clock.UtcNow >= this.xassTokenCached.IssueInstant.AddMinutes(this.partnerConfiguration.MaxXassTokenCacheAgeMinutes))
            {
                string logMessage =
                    $"Refreshing cached Xass token due to max cache age reached: {this.partnerConfiguration.MaxXassTokenCacheAgeMinutes} minutes. " +
                    $"Token issued at: {this.xassTokenCached.IssueInstant}. Current time: {this.clock.UtcNow.DateTime}";
                this.logger.Information(
                    nameof(XboxAccountsAdapter),
                    logMessage);
                this.xassTokenCached = await this.FetchNewXassTokenAsync().ConfigureAwait(false);
            }
            else
            {
                this.logger.Information(
                    nameof(XboxAccountsAdapter),
                    $"Using cached Xass token. Token expiry in {(this.xassTokenCached.NotAfter - this.clock.UtcNow).TotalSeconds} seconds.");
            }

            return this.xassTokenCached;
        }

        /// <summary>
        ///     Looks up the Xbox Live user info via Xbox Accounts lookup API.
        /// </summary>
        /// <param name="lookupType">Lookup type. See <see href="https://xboxlivetools/APIDocs/Files/usersgamertagGamertaglookupGET.html" /> for details.</param>
        /// <param name="id">lookup ID value.</param>
        /// <param name="token">XSTS token to access lookup endpoint.</param>
        /// <returns>Xbox Live User Lookup Info.</returns>
        internal async Task<XboxLiveUserLookupInfo> GetXboxLiveUserLookupInfoAsync(string lookupType, string id, XstsToken token)
        {
            this.logger.MethodEnter(ComponentName, GetUserLookupInfoMethodName);

            id.ThrowOnNull(nameof(id));
            lookupType.ThrowOnNull(nameof(lookupType));

            XboxLiveUserLookupInfo serviceResponse = null;

            // Create the HTTP request
            string relativePath = string.Format(CultureInfo.InvariantCulture, GetUserLookupInfoApiRelativePath, lookupType, id);
            var requestUri = new Uri(string.Format(CultureInfo.InvariantCulture, "{0}{1}", this.partnerConfiguration.BaseUrl, relativePath));

            OutgoingApiEventWrapper apiEvent = OutgoingApiEventWrapper.CreateBasicOutgoingEvent(
                partnerId: PartnerId,
                operationName: GetUserLookupInfoOperationName,
                targetUri: requestUri.ToString(),
                requestMethod: HttpMethod.Get,
                operationVersion: null,
                dependencyType: "WebService");

            IDictionary<string, string> headers = new Dictionary<string, string>
            {
                { XboxAuthConstants.ContractVersionHeaderKey, "4" },
                { XboxAuthConstants.AuthorizationHeaderKey, XboxAuthConstants.AuthorizationHeaderValue + token.Token }
            };

            HttpRequestMessage requestMessage = HttpExtensions.CreateHttpRequestMessage(requestUri, HttpMethod.Get, apiEvent, headers);

            using (HttpResponseMessage httpResponse =
                await this.httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false))
            {
                if (!httpResponse.IsSuccessStatusCode)
                {
                    string bodyContent = (httpResponse.Content == null) ? "null" : await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                    string errorMessage = $"Non-success status code. Response={httpResponse}, Content={bodyContent}";
                    if (httpResponse?.StatusCode == HttpStatusCode.BadRequest)
                    {
                        throw new XboxRequestException(errorMessage);
                    }

                    throw new HttpRequestException(errorMessage);
                }

                if (httpResponse.Content == null)
                {
                    throw new HttpRequestException($"No content in response to deserialize. Response={httpResponse}");
                }

                serviceResponse = await httpResponse.Content.ReadAsAsync<XboxLiveUserLookupInfo>().ConfigureAwait(false);
            }

            this.logger.MethodExit(ComponentName, GetUserLookupInfoMethodName);
            return serviceResponse;
        }

        internal async Task<XboxLiveUsersLookupInfo> GetXboxLiveUsersLookupInfoAsync(IEnumerable<long> puids, XstsToken token)
        {
            this.logger.MethodEnter(ComponentName, GetUsersLookupInfoMethodName);

            puids.ThrowOnNull(nameof(puids));
            GetXboxLiveUsersLookupInfoRequest requestBody = new GetXboxLiveUsersLookupInfoRequest
            {
                Puids = puids
            };

            XboxLiveUsersLookupInfo serviceResponse = null;

            // Create the HTTP request
            var requestUri = new Uri(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}{1}",
                    this.partnerConfiguration.BaseUrl,
                    GetUsersLookupInfoApiRelativePath));

            OutgoingApiEventWrapper apiEvent = OutgoingApiEventWrapper.CreateBasicOutgoingEvent(
                partnerId: PartnerId,
                operationName: GetUsersLookupInfoOperationName,
                targetUri: requestUri.ToString(),
                requestMethod: HttpMethod.Post,
                operationVersion: null,
                dependencyType: "WebService");

            IDictionary<string, string> headers = new Dictionary<string, string>
            {
                { XboxAuthConstants.ContractVersionHeaderKey, "1" },
                { XboxAuthConstants.AuthorizationHeaderKey, XboxAuthConstants.AuthorizationHeaderValue + token.Token }
            };

            HttpRequestMessage requestMessage = HttpExtensions.CreateHttpRequestMessage(requestUri, HttpMethod.Post, apiEvent, headers, requestBody);

            using (HttpResponseMessage httpResponse =
                await this.httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false))
            {
                if (!httpResponse.IsSuccessStatusCode)
                {
                    string bodyContent = (httpResponse.Content == null) ? "null" : await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                    string errorMessage = $"Non-success status code. Response={httpResponse}, Content={bodyContent}";
                    if (httpResponse?.StatusCode == HttpStatusCode.BadRequest)
                    {
                        throw new XboxRequestException(errorMessage);
                    }

                    throw new HttpRequestException(errorMessage);
                }

                if (httpResponse.Content == null)
                {
                    throw new HttpRequestException($"No content in response to deserialize. Response={httpResponse}");
                }

                serviceResponse = await httpResponse.Content.ReadAsAsync<XboxLiveUsersLookupInfo>().ConfigureAwait(false);
            }

            this.logger.MethodExit(ComponentName, GetUsersLookupInfoMethodName);
            return serviceResponse;
        }

        /// <summary>
        ///     Retrieves an Xbox security token by calling XSTS with a service tokens.
        /// </summary>
        /// <param name="xassToken">The Xbox service token to convert.</param>
        /// <param name="relyingParty">The Xbox defined name of the target Xbox service.</param>
        /// <returns>The retrieved Xbox security token.</returns>
        internal async Task<XstsToken> GetXstsTokenAsync(string xassToken, string relyingParty)
        {
            relyingParty.ThrowOnNull(nameof(relyingParty));
            if (string.IsNullOrWhiteSpace(xassToken))
            {
                throw new ArgumentException("XassToken must not be null");
            }

            this.logger.MethodEnter(ComponentName, GetXstsTokenMethodName);

            XstsToken xstsToken = null;

            string relativePath = XstsTokenRelativeUrl;
            var requestUri = new Uri(new Uri(this.partnerConfiguration.XstsServiceEndpoint), relativePath);

            try
            {
                var xstsRequest = new XstsRequest();
                xstsRequest.TokenType = "JWT";
                xstsRequest.RelyingParty = relyingParty;
                xstsRequest.Properties = new XstsProperties
                {
                    SandboxId = "RETAIL",
                    ServiceToken = !string.IsNullOrWhiteSpace(xassToken) ? xassToken : null,
                    UserTokens = null
                };

                HttpContent content = new ObjectContent<XstsRequest>(xstsRequest, new JsonMediaTypeFormatter());

                IDictionary<string, string> headers = new Dictionary<string, string>
                {
                    { XboxAuthConstants.ContractVersionHeaderKey, XboxAuthConstants.ContractVersion }
                };

                OutgoingApiEventWrapper apiEvent = OutgoingApiEventWrapper.CreateBasicOutgoingEvent(
                    partnerId: PartnerId,
                    operationName: GetXstsTokenOperationName,
                    targetUri: requestUri.ToString(),
                    requestMethod: HttpMethod.Post,
                    operationVersion: null,
                    dependencyType: "WebService");

                HttpRequestMessage requestMessage = await CreateRequestMessageWithHeaderSignature(
                    HttpMethod.Post,
                    requestUri,
                    content,
                    apiEvent,
                    XboxAuthConstants.SignatureHeaderKey,
                    request => this.CreateXboxSignature(request),
                    headers).ConfigureAwait(false);

                using (HttpResponseMessage httpResponse = await this.httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false))
                {
                    if (httpResponse.Content == null)
                    {
                        throw new HttpRequestException($"No content in response to deserialize. Response={httpResponse}");
                    }

                    if (!httpResponse.IsSuccessStatusCode)
                    {
                        XstsError xboxError = await httpResponse.Content.ReadAsAsync<XstsError>().ConfigureAwait(false);

                        // https://microsoft.sharepoint.com/teams/osg_xboxtv/xboxlive/_layouts/15/WopiFrame.aspx?sourcedoc={9cc8b53a-0eeb-4a6c-832b-4832a1f6bbc4}&action=view&wdparaid=4FAD4BA4
                        switch (xboxError?.XErr)
                        {
                            case 2148916227: // 0x8015DC03 
                            case 2148916229: // 0x8015DC05 
                            case 2148916233: // 0x8015DC09 
                            case 2148916234: // 0x8015DC0A 
                            case 2148916235: // 0x8015DC0B 
                            case 2148916236: // 0x8015DC0C 
                            case 2148916237: // 0x8015DC0D 
                            case 2148916238: // 0x8015DC0E 
                            case 2148916239: // 0x8015DC0F 
                            case 2148916240: // 0x8015DC10 
                            case 2148916243: // 0x8015DC13 
                                throw new XboxUserAccountException(xboxError.XErr);
                            case 2148916242: // 0x8015DC12
                                throw new XboxAccessException();
                            case 2148916255: // 0x8015DC1F
                                throw new ExpiredXboxServiceTokenException();
                            case 2148916258: // 0x8015DC22
                                throw new ExpiredXboxUserTokenException();
                            case 2148916263: // 0x8015DC27
                                throw new InvalidXboxServiceTokenException();
                            case 2148916262: // 0x8015DC26
                                throw new InvalidXboxUserTokenException();
                            case 2148916273: // 0x8015DC31
                            case 2148916274: // 0x8015DC32
                                throw new XboxOutageException();
                            default:
                                string bodyContent = (httpResponse.Content == null) ? "null" : await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                                string errorMessage = $"Non-success status code. Response={httpResponse}, Content={bodyContent}";
                                if (httpResponse?.StatusCode == HttpStatusCode.BadRequest)
                                {
                                    throw new XboxRequestException(errorMessage);
                                }

                                throw new XboxAuthenticationException(errorMessage);
                        }
                    }

                    XstsResponse xstsResponse = await httpResponse.Content.ReadAsAsync<XstsResponse>().ConfigureAwait(false);

                    if (xstsResponse == null)
                    {
                        throw new XboxResponseException("Null response in HTTP XSTS response body.");
                    }

                    if (!xstsResponse.IsValid)
                    {
                        throw new XboxResponseException($"XSTS token returned is not valid. Response={xstsResponse}");
                    }

                    xstsToken = XstsResponse.ToToken(xstsResponse);
                }
            }
            catch (HttpRequestException e)
            {
                throw new XboxAuthenticationException("Error calling XSTS service.", e);
            }
            finally
            {
                this.logger.MethodExit(ComponentName, GetXstsTokenMethodName);
            }

            return xstsToken;
        }

        private async Task<XassToken> FetchNewXassTokenAsync()
        {
            this.logger.MethodEnter(ComponentName, nameof(this.FetchNewXassTokenAsync));

            XassToken xassToken;
            string relativePath = XassTokenRelativeUrl;
            var requestUri = new Uri(new Uri(this.partnerConfiguration.XassServiceEndpoint), relativePath);

            try
            {
                string s2sToken = await this.s2sAuthClient.GetAccessTokenAsync(
                    targetSite: this.partnerConfiguration.MsaS2STargetSite,
                    cancellationToken: CancellationToken.None).ConfigureAwait(false);

                var xassRequest = new XassRequest();
                xassRequest.TokenType = "JWT";
                xassRequest.RelyingParty = XassRelyingParty;
                xassRequest.Properties = new XassProperties
                {
                    AuthMethod = "RPS",
                    AppSiteName = this.partnerConfiguration.XtokenMsaS2STargetSite,
                    AppTicket = $"a={s2sToken}",
                    ProofKey = privateSigningKey.ToProofKey().ToDictionary()
                };

                HttpContent content = new ObjectContent<XassRequest>(xassRequest, new JsonMediaTypeFormatter());
                IDictionary<string, string> headers = new Dictionary<string, string>
                {
                    { XboxAuthConstants.ContractVersionHeaderKey, XboxAuthConstants.ContractVersion }
                };

                OutgoingApiEventWrapper apiEvent = OutgoingApiEventWrapper.CreateBasicOutgoingEvent(
                    partnerId: PartnerId,
                    operationName: GetXassTokenOperationName,
                    targetUri: requestUri.ToString(),
                    requestMethod: HttpMethod.Post,
                    operationVersion: null,
                    dependencyType: "WebService");

                HttpRequestMessage requestMessage = await CreateRequestMessageWithHeaderSignature(
                    HttpMethod.Post,
                    requestUri,
                    content,
                    apiEvent,
                    XboxAuthConstants.SignatureHeaderKey,
                    request => this.CreateXboxSignature(request),
                    headers).ConfigureAwait(false);

                using (HttpResponseMessage httpResponse =
                    await this.httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false))
                {
                    if (!httpResponse.IsSuccessStatusCode)
                    {
                        string bodyContent = (httpResponse.Content == null) ? "null" : await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                        string errorMessage = $"Non-success status code. Response={httpResponse}, Content={bodyContent}";

                        if (httpResponse.StatusCode == HttpStatusCode.BadRequest)
                        {
                            throw new XboxRequestException(errorMessage);
                        }

                        throw new HttpRequestException(errorMessage);
                    }

                    if (httpResponse.Content == null)
                    {
                        throw new HttpRequestException($"No content in response to deserialize. Response={httpResponse}");
                    }

                    XassResponse xassResponse = await httpResponse.Content.ReadAsAsync<XassResponse>().ConfigureAwait(false);

                    if (xassResponse == null)
                    {
                        throw new XboxResponseException("Null response in HTTP XASS response body.");
                    }

                    if (!xassResponse.IsValid)
                    {
                        throw new XboxResponseException($"XASS token returned is not valid. Response={xassResponse}");
                    }

                    xassToken = XassResponse.ToToken(xassResponse);
                }
            }
            catch (XboxRequestException e)
            {
                throw new XboxRequestException("Error calling XASS service.", e);
            }
            catch (HttpRequestException e)
            {
                throw new XboxAuthenticationException("Error calling XASS service.", e);
            }
            finally
            {
                this.logger.MethodExit(ComponentName, nameof(this.FetchNewXassTokenAsync));
            }

            return xassToken;
        }

        internal static async Task<HttpRequestMessage> CreateRequestMessageWithHeaderSignature(
            HttpMethod httpMethod,
            Uri requestUri,
            HttpContent content,
            OutgoingApiEventWrapper outgoingApiEvent,
            string signatureHeaderKey,
            Func<HttpRequestMessage, Task<string>> createSignature,
            IDictionary<string, string> headers = null)
        {
            HttpRequestMessage requestMessage = HttpExtensions.CreateHttpRequestMessage(requestUri, httpMethod, outgoingApiEvent, headers);
            requestMessage.Content = content;
            requestMessage.Headers.Add(signatureHeaderKey, await createSignature.Invoke(requestMessage).ConfigureAwait(false));
            return requestMessage;
        }
    }
}
