// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.CustomerMasterAdapter
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Formatting;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Converters;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.CustomerMasterAdapter.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Helpers;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.OSGS.HttpClientCommon;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.Windows.Services.AuthN.Client.S2S;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    ///     Customer-Master Adapter
    /// </summary>
    public class CustomerMasterAdapter : ICustomerMasterAdapter
    {
        public const string PrivacyProfileGetType = "msa_privacy";

        private const string AuthHeaderFormat = "apptoken=\"{0}\",usertoken=\"{1}\"";

        private const string ComponentName = nameof(CustomerMasterAdapter);

        private const string ErrorMessageDefault = "An unknown error occurred.";

        private const string ErrorMessagerNullResponse = "Null response from partner adapter.";

        // Reference Jarvis API documentation @ https://jarvisapi/v2/
        private const string GetProfilesRelativePath = "JarvisCM/me/profiles";

        private const string GetFamilyProfilesRelativePath = "JarvisCM/my-family/profiles";

        private const string PartnerId = "CustomerMaster";

        private const string PostPrivacyProfilesRelativePath = "JarvisCM/me/profiles";

        private const string PostFamilyPrivacyProfilesRelativePath = "JarvisCM/my-family/profiles";

        private const string PutPrivacyProfilesRelativePath = "JarvisCM/me/profiles/{0}"; // format {0} with the profile id

        private const string PutFamilyPrivacyProfilesRelativePath = "JarvisCM/my-family/profiles/{0}"; // format {0} with the profile id

        private const string QueryMsaPrivacy = "type=msa_privacy";

        private readonly IHttpClient httpClient;

        private readonly ILogger logger;

        private readonly IPrivacyPartnerAdapterConfiguration partnerConfiguration;

        private readonly IS2SAuthClient s2sAuthClient;

        /// <summary>
        ///     Initializes a new instance of the <see cref="CustomerMasterAdapter" /> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="adapterPartnerConfiguration">The adapter configuration.</param>
        /// <param name="s2sAuthClient">The S2S authentication client.</param>
        /// <param name="logger">The logger.</param>
        public CustomerMasterAdapter(IHttpClient httpClient, IPrivacyPartnerAdapterConfiguration adapterPartnerConfiguration, IS2SAuthClient s2sAuthClient, ILogger logger)
        {
            httpClient.ThrowOnNull(nameof(httpClient));
            adapterPartnerConfiguration.ThrowOnNull(nameof(adapterPartnerConfiguration));
            s2sAuthClient.ThrowOnNull(nameof(s2sAuthClient));
            logger.ThrowOnNull(nameof(logger));

            this.httpClient = httpClient;
            this.partnerConfiguration = adapterPartnerConfiguration;
            this.s2sAuthClient = s2sAuthClient;
            this.logger = logger;
        }

        public async Task<AdapterResponse<PrivacyProfile>> CreatePrivacyProfileAsync(IPxfRequestContext requestContext, PrivacyProfile content)
        {
            const string OperationName = "CreatePrivacyProfile";
            requestContext.ThrowOnNull(nameof(requestContext));
            content.ThrowOnNull(nameof(content));

            Uri requestUri = new UriBuilder(this.partnerConfiguration.BaseUrl)
            {
                Path = string.IsNullOrEmpty(requestContext.FamilyJsonWebToken) ? PostPrivacyProfilesRelativePath : PostFamilyPrivacyProfilesRelativePath
            }.Uri;

            OutgoingApiEventWrapper outgoingApiEvent = OutgoingApiEventWrapper.CreateHttpEventWithPuid(
                PartnerId,
                OperationName,
                operationVersion: string.Empty,
                requestUri: requestUri,
                requestMethod: HttpMethod.Post,
                userPuid: requestContext.AuthorizingPuid);

            HttpRequestMessage requestMessage = await CreateHttpRequestMessageAsync(
                requestUri,
                requestContext,
                this.s2sAuthClient,
                this.partnerConfiguration.MsaS2STargetSite,
                HttpMethod.Post,
                outgoingApiEvent,
                this.logger).ConfigureAwait(false);
            requestMessage.Content = new ObjectContent<PrivacyProfile>(content, new JsonMediaTypeFormatter());
            this.LogVerboseRequestInformation(requestMessage);

            HttpResponseMessage responseMessage = await this.httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);

            return await DeserializePrivacyProfileResponseAsync(responseMessage, this.logger, DeserializeToPrivacyProfile).ConfigureAwait(false);
        }

        /// <summary>
        ///     Gets the privacy profile.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="operationName">The operation name used in logging SLL outgoing operations.</param>
        /// <returns>The <see cref="AdapterResponse{PrivacyProfile}" /> of the user</returns>
        public Task<AdapterResponse<PrivacyProfile>> GetPrivacyProfileAsync(IPxfRequestContext requestContext, string operationName = "GetPrivacyProfile")
        {
            return this.GetPrivacyProfileAsync(requestContext, operationName, DeserializeFromGetProfilesResponseToPrivacyProfile);
        }

        /// <inheritdoc />
        public async Task<AdapterResponse<bool?>> GetOboPrivacyConsentSettingAsync(IPxfRequestContext requestContext)
        {
            AdapterResponse<PrivacyProfile> response = await this.GetPrivacyProfileAsync(requestContext, "GetOboPrivacyConsentSetting").ConfigureAwait(false);
            return new AdapterResponse<bool?> { Error = response.Error, Result = response?.Result?.OnBehalfOfPrivacy };
        }

        /// <summary>
        ///     Gets the privacy profile as <see cref="JObject" />.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <returns>The <see cref="AdapterResponse{JObject}" /> of the user</returns>
        public Task<AdapterResponse<JObject>> GetPrivacyProfileJObjectAsync(IPxfRequestContext requestContext)
        {
            const string OperationName = "GetPrivacyProfile";
            return this.GetPrivacyProfileAsync(requestContext, OperationName, LoadPrivacyProfileJObject);
        }

        /// <summary>
        ///     Update the privacy profile
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="updatedContent">The content to update in the profile</param>
        /// <param name="existingProfile">The existing profile.</param>
        /// <returns><see cref="AdapterError" /> if any occurred; else null</returns>
        public async Task<AdapterResponse<PrivacyProfile>> UpdatePrivacyProfileAsync(IPxfRequestContext requestContext, PrivacyProfile updatedContent, JObject existingProfile)
        {
            const string OperationName = "UpdatePrivacyProfile";
            if (requestContext == null)
            {
                throw new ArgumentNullException(nameof(requestContext));
            }
            if (existingProfile == null)
            {
                throw new ArgumentNullException(nameof(existingProfile));
            }
            if (updatedContent == null)
            {
                this.logger.Information(ComponentName, "Update content was null. No update action required.");
                return new AdapterResponse<PrivacyProfile>
                {
                    Error = new AdapterError(AdapterErrorCode.ResourceNotModified, "Resource not modified", (int)HttpStatusCode.NotModified)
                };
            }

            AdapterError error = ValidateETag(updatedContent.ETag, existingProfile);

            if (error != null)
            {
                return new AdapterResponse<PrivacyProfile> { Error = error };
            }

            string profileId = existingProfile["id"].ValueOrDefault<string>();
            if (string.IsNullOrWhiteSpace(profileId))
            {
                return new AdapterResponse<PrivacyProfile>
                {
                    Error = new AdapterError(AdapterErrorCode.InvalidInput, "Id is required for updating a resource.", (int)HttpStatusCode.BadRequest)
                };
            }

            Uri requestUri = new UriBuilder(this.partnerConfiguration.BaseUrl)
            {
                Path = string.Format(CultureInfo.InvariantCulture, string.IsNullOrEmpty(requestContext.FamilyJsonWebToken) ? PutPrivacyProfilesRelativePath : PutFamilyPrivacyProfilesRelativePath, profileId)
            }.Uri;

            OutgoingApiEventWrapper outgoingApiEvent = OutgoingApiEventWrapper.CreateHttpEventWithPuid(
                PartnerId,
                OperationName,
                operationVersion: string.Empty,
                requestUri: requestUri,
                requestMethod: HttpMethod.Put,
                userPuid: requestContext.AuthorizingPuid);

            HttpRequestMessage requestMessage = await CreateHttpRequestMessageAsync(
                requestUri,
                requestContext,
                this.s2sAuthClient,
                this.partnerConfiguration.MsaS2STargetSite,
                HttpMethod.Put,
                outgoingApiEvent,
                this.logger).ConfigureAwait(false);
            requestMessage.Headers.TryAddWithoutValidation(CustomerMasterHeaders.IfMatch, updatedContent.ETag);

            JObject updateProfileRequest = CreateUpdateProfileRequest(updatedContent, existingProfile);

            requestMessage.Content = new ObjectContent<JObject>(updateProfileRequest, new JsonMediaTypeFormatter());
            this.LogVerboseRequestInformation(requestMessage);

            HttpResponseMessage responseMessage = await this.httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);

            return await DeserializePrivacyProfileResponseAsync(responseMessage, this.logger, DeserializeToPrivacyProfile).ConfigureAwait(false);
        }

        private async Task<AdapterResponse<T>> GetPrivacyProfileAsync<T>(
            IPxfRequestContext requestContext,
            string operationName,
            Func<string, ILogger, int, AdapterResponse<T>> deserializationFunc)
        {
            if (requestContext == null)
            {
                throw new ArgumentNullException(nameof(requestContext));
            }

            Uri requestUri = new UriBuilder(this.partnerConfiguration.BaseUrl)
            {
                Path = string.IsNullOrEmpty(requestContext.FamilyJsonWebToken) ? GetProfilesRelativePath : GetFamilyProfilesRelativePath,
                Query = QueryMsaPrivacy
            }.Uri;

            OutgoingApiEventWrapper outgoingApiEvent = OutgoingApiEventWrapper.CreateHttpEventWithPuid(
                PartnerId,
                operationName,
                operationVersion: string.Empty,
                requestUri: requestUri,
                requestMethod: HttpMethod.Get,
                userPuid: requestContext.AuthorizingPuid);

            HttpRequestMessage requestMessage =
                await
                    CreateHttpRequestMessageAsync(
                        requestUri,
                        requestContext,
                        this.s2sAuthClient,
                        this.partnerConfiguration.MsaS2STargetSite,
                        HttpMethod.Get,
                        outgoingApiEvent,
                        this.logger).ConfigureAwait(false);
            this.LogVerboseRequestInformation(requestMessage);

            HttpResponseMessage responseMessage = await this.httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);

            return await DeserializePrivacyProfileResponseAsync(responseMessage, this.logger, deserializationFunc).ConfigureAwait(false);
        }

        private void LogVerboseRequestInformation(HttpRequestMessage requestMessage)
        {
            this.logger.Verbose(ComponentName, $"[Target Uri]='{requestMessage.RequestUri}'");
            this.logger.Verbose(ComponentName, "[Request Headers]'");
            foreach (KeyValuePair<string, IEnumerable<string>> header in requestMessage.Headers)
            {
                this.logger.Verbose(ComponentName, $"{header.Key}, {string.Join(",", header.Value)}");
            }
            this.logger.Verbose(ComponentName, $"[Request Message]='{requestMessage}'");
        }

        private static async Task<HttpRequestMessage> CreateHttpRequestMessageAsync(
            Uri requestUri,
            IPxfRequestContext requestContext,
            IS2SAuthClient s2sAuthClient,
            string targetSite,
            HttpMethod httpMethod,
            OutgoingApiEventWrapper outgoingApiEvent,
            ILogger logger)
        {
            s2sAuthClient.ThrowOnNull(nameof(s2sAuthClient));
            requestContext.ThrowOnNull(nameof(requestContext));
            requestContext.UserProxyTicket.ThrowOnNull(nameof(requestContext.UserProxyTicket));

            // Custom Jarvis CM headers
            IDictionary<string, string> customHeaders = new Dictionary<string, string>();
            customHeaders.Add(CustomerMasterHeaders.TrackingId, Guid.NewGuid().ToString());
            customHeaders.Add(CustomerMasterHeaders.CorrelationId, LogicalWebOperationContext.ServerActivityId.ToString());
            customHeaders.Add(CustomerMasterHeaders.ApiVersion, "2015-03-31");

            if (requestContext.FamilyJsonWebToken != null)
            {
                customHeaders.Add(CustomerMasterHeaders.FamilyService, requestContext.FamilyJsonWebToken);
            }

            HttpRequestMessage requestMessage = HttpExtensions.CreateHttpRequestMessage(requestUri, httpMethod, outgoingApiEvent, customHeaders);

            logger.Verbose(ComponentName, $"target site: {targetSite}");
            string s2sToken = await s2sAuthClient.GetAccessTokenAsync(targetSite, CancellationToken.None).ConfigureAwait(false);

            // Authorization header is customized to what Jarvis FD expects
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue(
                CustomerMasterHeaders.AuthHeaderMSAAuth1,
                string.Format(AuthHeaderFormat, s2sToken, requestContext.UserProxyTicket));

            return requestMessage;
        }

        private static JObject CreateUpdateProfileRequest(PrivacyProfile updatedContent, JObject existingProfile)
        {
            // Must change values in the existing profile based on what changed in the updatedContent
            // This is done so that any values in the profile we don't know about don't accidentally get dropped/overwritten

            if (existingProfile == null)
            {
                throw new InvalidOperationException("Privacy profile did not exist so it cannot be updated.");
            }

            if (updatedContent.Advertising.HasValue)
            {
                existingProfile["advertising"] = updatedContent.Advertising;
            }

            if (updatedContent.TailoredExperiencesOffers.HasValue)
            {
                existingProfile["tailored_experiences_offers"] = updatedContent.TailoredExperiencesOffers;
            }

            if (updatedContent.OnBehalfOfPrivacy.HasValue)
            {
                existingProfile["OBOPrivacy"] = updatedContent.OnBehalfOfPrivacy;
            }

            if (updatedContent.LocationPrivacy.HasValue)
            {
                existingProfile["OBOPrivacyLocation"] = updatedContent.LocationPrivacy;
            }

            if (updatedContent.SharingState.HasValue)
            {
                existingProfile["sharing_state"] = updatedContent.SharingState;
            }


            // remove deprecated properties
            existingProfile.Remove("diagnostics");
            existingProfile.Remove("browse_activity");

            // remove etag from request body, it goes in the header
            existingProfile["etag"] = null;

            return existingProfile;
        }

        internal static AdapterResponse<PrivacyProfile> DeserializeFromGetProfilesResponseToPrivacyProfile(string content, ILogger logger, int httpStatusCode)
        {
            var adapterResponse = new AdapterResponse<PrivacyProfile>();

            if (string.IsNullOrWhiteSpace(content))
            {
                adapterResponse.Error = new AdapterError(AdapterErrorCode.EmptyResponse, ErrorMessagerNullResponse, httpStatusCode);
                return adapterResponse;
            }

            try
            {
                logger.Verbose(ComponentName, content);
                var profilesResult = JsonConvert.DeserializeObject<Profiles>(content);

                if (profilesResult == null)
                {
                    logger.Verbose(ComponentName, "ProfilesResult is null.");
                    return adapterResponse;
                }

                if (profilesResult.Items == null || profilesResult.Items.Count == 0)
                {
                    logger.Verbose(ComponentName, "Items in profile is null or 0.");
                    return adapterResponse;
                }

                JToken privacyProfileJson = profilesResult.Items.SingleOrDefault(IsPrivacyProfile());

                if (privacyProfileJson == null)
                {
                    logger.Verbose(ComponentName, $"No profile of type '{PrivacyProfileGetType}' was found.");
                    return adapterResponse;
                }

                var result = JsonConvert.DeserializeObject<PrivacyProfile>(privacyProfileJson.ToString());

                if (result == null)
                {
                    logger.Verbose(ComponentName, "Privacy profile is null after deserialization.");
                    return adapterResponse;
                }

                adapterResponse.Result = result;
                return adapterResponse;
            }
            catch (JsonSerializationException e)
            {
                adapterResponse.Error = new AdapterError(AdapterErrorCode.JsonDeserializationFailure, e.ToString(), httpStatusCode);
                return adapterResponse;
            }
            catch (Exception e)
            {
                adapterResponse.Error = new AdapterError(AdapterErrorCode.Unknown, e.ToString(), httpStatusCode);
                return adapterResponse;
            }
        }

        private static async Task<AdapterResponse<T>> DeserializePrivacyProfileResponseAsync<T>(
            HttpResponseMessage responseMessage,
            ILogger logger,
            Func<string, ILogger, int, AdapterResponse<T>> deserializationFunc)
        {
            logger.Verbose(ComponentName, "[Response]");
            logger.Verbose(ComponentName, responseMessage.ToString());

            if (!responseMessage.IsSuccessStatusCode)
            {
                return await HandleErrorAdapterResponseAsync<T>(responseMessage, logger).ConfigureAwait(false);
            }

            if (responseMessage.Content == null)
            {
                return new AdapterResponse<T>
                {
                    Error = new AdapterError(AdapterErrorCode.EmptyResponse, ErrorMessagerNullResponse, (int)responseMessage.StatusCode)
                };
            }

            string responseContent = await (responseMessage.Content?.ReadAsStringAsync()).ConfigureAwait(false);

            return deserializationFunc(responseContent, logger, (int)responseMessage.StatusCode);
        }

        internal static AdapterResponse<PrivacyProfile> DeserializeToPrivacyProfile(string content, ILogger logger, int httpStatusCode)
        {
            var adapterResponse = new AdapterResponse<PrivacyProfile>();

            if (string.IsNullOrWhiteSpace(content))
            {
                adapterResponse.Error = new AdapterError(AdapterErrorCode.EmptyResponse, ErrorMessagerNullResponse, httpStatusCode);
                return adapterResponse;
            }

            try
            {
                logger.Verbose(ComponentName, content);
                var result = JsonConvert.DeserializeObject<PrivacyProfile>(content);

                if (result == null)
                {
                    logger.Verbose(ComponentName, "Privacy profile is null after deserialization.");
                    return adapterResponse;
                }

                adapterResponse.Result = result;
                return adapterResponse;
            }
            catch (JsonSerializationException e)
            {
                adapterResponse.Error = new AdapterError(AdapterErrorCode.JsonDeserializationFailure, e.ToString(), httpStatusCode);
                return adapterResponse;
            }
            catch (Exception e)
            {
                adapterResponse.Error = new AdapterError(AdapterErrorCode.Unknown, e.ToString(), httpStatusCode);
                return adapterResponse;
            }
        }

        private static async Task<AdapterResponse<T>> HandleErrorAdapterResponseAsync<T>(HttpResponseMessage responseMessage, ILogger logger)
        {
            var adapterError = new AdapterError(AdapterErrorCode.Unknown, ErrorMessageDefault, (int)responseMessage.StatusCode);

            if (responseMessage.Content == null)
            {
                adapterError.Code = AdapterErrorCode.EmptyResponse;
                return new AdapterResponse<T> { Error = adapterError };
            }

            string errorMessage = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);

            switch (responseMessage.StatusCode)
            {
                case HttpStatusCode.Unauthorized:

                    // errors that fail authorization may fail at Jarvis FD, and that means they don't follow the same error contract as other error codes
                    logger.Error(ComponentName, $"Http StatusCode: {responseMessage.StatusCode}");
                    adapterError = new AdapterError(AdapterErrorCode.Unauthorized, errorMessage, (int)responseMessage.StatusCode);
                    break;

                default:

                    adapterError = HandlePartnerErrorCode(errorMessage, (int)responseMessage.StatusCode);
                    string errorMessageLog = $"Customer Master response: {errorMessage}";

                    // Treat Unknown as an error and known error codes as warnings
                    if (adapterError.Code != AdapterErrorCode.Unknown)
                    {
                        logger.Warning(ComponentName, errorMessageLog);
                    }
                    else
                    {
                        logger.Error(ComponentName, $"Http StatusCode: {responseMessage.StatusCode}");
                        logger.Error(ComponentName, errorMessageLog);
                    }

                    break;
            }

            return new AdapterResponse<T> { Error = adapterError };
        }

        /// <summary>
        ///     Handles the partner error code by parsing the error message into dynamic JSON to read the error_code value,
        ///     and map it to an <see cref="AdapterErrorCode" /> if the error is a known error code.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="httpStatusCode">The HTTP status code.</param>
        /// <returns>
        ///     An <see cref="AdapterError" /> to correspond to the partner error code.
        /// </returns>
        internal static AdapterError HandlePartnerErrorCode(string errorMessage, int httpStatusCode)
        {
            try
            {
                JObject errorResponse = JObject.Parse(errorMessage);

                string errorCode = errorResponse?["error_code"].ValueOrDefault<string>();

                if (!string.IsNullOrWhiteSpace(errorCode))
                {
                    CustomerMasterErrorCode partnerErrorCode;
                    if (!Enum.TryParse(errorCode, out partnerErrorCode))
                    {
                        partnerErrorCode = CustomerMasterErrorCode.Unknown;
                    }

                    switch (partnerErrorCode)
                    {
                        case CustomerMasterErrorCode.ConcurrencyFailure:

                            // May happen in PUT request where ETag doesn't match
                            return new AdapterError(AdapterErrorCode.ConcurrencyConflict, errorMessage, httpStatusCode);

                        case CustomerMasterErrorCode.ResourceAlreadyExists:

                            // May happen in POST request where resource already exists (such as creating a new profile that already exists)
                            return new AdapterError(AdapterErrorCode.ResourceAlreadyExists, errorMessage, httpStatusCode);
                    }
                }
            }
            catch (JsonReaderException)
            {
                IfxTraceLogger.Instance.Error(ComponentName, "Error Message was invalid JSON.");
                IfxTraceLogger.Instance.Error(ComponentName, errorMessage);
            }

            return new AdapterError(AdapterErrorCode.Unknown, $"Unknown error: {errorMessage}", httpStatusCode);
        }

        private static Func<JToken, bool> IsPrivacyProfile()
        {
            return p => p["type"] != null && string.Equals(p["type"].Value<string>(), PrivacyProfileGetType, StringComparison.OrdinalIgnoreCase);
        }

        private static AdapterResponse<JObject> LoadPrivacyProfileJObject(string content, ILogger logger, int httpStatusCode)
        {
            var adapterResponse = new AdapterResponse<JObject>();

            if (string.IsNullOrWhiteSpace(content))
            {
                adapterResponse.Error = new AdapterError(AdapterErrorCode.EmptyResponse, ErrorMessagerNullResponse, httpStatusCode);
                return adapterResponse;
            }

            logger.Verbose(ComponentName, content);
            JObject result = JObject.Parse(content);

            var response = new AdapterResponse<JObject>();

            JToken privacyProfile = result?["items"].SingleOrDefault(IsPrivacyProfile());

            if (privacyProfile != null)
            {
                response.Result = (JObject)privacyProfile;
            }
            else
            {
                response.Result = new JObject();
            }

            return response;
        }

        private static AdapterError ValidateETag(string eTag, JObject existingProfile)
        {
            if (string.IsNullOrWhiteSpace(eTag))
            {
                return new AdapterError(AdapterErrorCode.InvalidInput, "ETag is required, but was missing as input on the request.", (int)HttpStatusCode.PreconditionFailed);
            }

            string existingProfileEtag = existingProfile["etag"].ValueOrDefault<string>();

            if (string.IsNullOrWhiteSpace(existingProfileEtag))
            {
                return new AdapterError(AdapterErrorCode.Unknown, "ETag should exist on the existing profile. It was Null or WhiteSpace.", (int)HttpStatusCode.PreconditionFailed);
            }

            if (!string.Equals(eTag, existingProfileEtag, StringComparison.OrdinalIgnoreCase))
            {
                return new AdapterError(
                    AdapterErrorCode.ConcurrencyConflict,
                    "ETag did not match. The resource has been changed. Refresh the existing profile setting and re-submit the request.",
                    (int)HttpStatusCode.Conflict);
            }

            // Success
            return null;
        }
    }
}
