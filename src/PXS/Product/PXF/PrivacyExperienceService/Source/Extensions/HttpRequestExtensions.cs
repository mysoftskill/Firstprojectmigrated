// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Formatting;
    using System.Text.RegularExpressions;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.ODataConfigs;
    using Microsoft.OData;

    /// <summary>
    ///     HttpRequest Extensions
    /// </summary>
    public static class HttpRequestExtensions
    {
        private static readonly MediaTypeFormatter Formatter = new JsonMediaTypeFormatter();

        /// <summary>
        ///     Creates the error response.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="error">The error.</param>
        /// <returns>HttpResponseMessage</returns>
        public static HttpResponseMessage CreateErrorResponse(this HttpRequestMessage request, Error error)
        {
            var responseCode = HttpStatusCode.InternalServerError;
            var errorCode = ErrorCode.Unknown;

            if (error == null)
            {
                return request.CreateResponse(responseCode, new Error(errorCode, "No error response."));
            }

            // If the error code is a valid enumeration value and it's in the mapping dictionary, set the appropriate response code.
            if (Enum.TryParse(error.Code, ignoreCase: true, result: out errorCode) &&
                HttpStatusCodeMapping.Mapping.ContainsKey(errorCode))
            {
                responseCode = HttpStatusCodeMapping.Mapping[errorCode];
            }

            // The error code is known, so try and map to an http-status-code.
            return request.CreateResponse(responseCode, error, Formatter);
        }

        /// <summary>
        ///     Creates the error response for OData APIs.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="error">The error.</param>
        /// <param name="hideDetailErrorMessages">
        ///     Value determines if generic graph api error response messages are used instead, which hides the detailed error messages.
        ///     Generic error messages will prevent downstream error messages from being sent to potential 3rd party clients, which were not intended to be publically visible.
        /// </param>
        /// <returns>HttpResponseMessage</returns>
        public static HttpResponseMessage CreateODataErrorResponse(this HttpRequestMessage request, Error error, bool hideDetailErrorMessages = true)
        {
            var responseCode = HttpStatusCode.InternalServerError;
            var errorCode = ErrorCode.Unknown;

            if (error == null)
            {
                return request.CreateResponse(
                    responseCode,
                    new ODataError
                    {
                        ErrorCode = errorCode.ToString(),
                        Message = "No error response."
                    });
            }

            if (Enum.TryParse(error.Code, ignoreCase: true, result: out errorCode) &&
                HttpStatusCodeMapping.Mapping.ContainsKey(errorCode))
            {
                responseCode = HttpStatusCodeMapping.Mapping[errorCode];
            }

            if (hideDetailErrorMessages)
            {
                ModifyErrorMessageToHideDetails(error, errorCode);
            }

            return request.CreateResponse(responseCode, ConvertServiceResponseErrorToODataError(error));
        }

        /// <summary>
        ///     Gets the API name based on the request path and method
        /// </summary>
        /// <param name="request">Request message</param>
        /// <returns>API name</returns>
        public static string GetApiName(this HttpRequestMessage request)
        {
            string uri = request.RequestUri.AbsolutePath;
            const string StringToRemove = ModelBuilder.ODataNamespace + ".";
            string dataRoute = uri?.Replace(StringToRemove, string.Empty);

            string key = ApiRouteMapping.NormalizeRouteMethodKey(dataRoute, request.Method);
            KeyValuePair<string, string> apiName = ApiRouteMapping.PathToApiNameMapping.FirstOrDefault(
                kpv => new Regex(kpv.Key).IsMatch(key) ||
                       string.Equals(key, kpv.Key, StringComparison.OrdinalIgnoreCase));
            string resApiName = !string.IsNullOrWhiteSpace(apiName.Key) ? apiName.Value : ApiRouteMapping.DefaultApiName;
            return resApiName;
        }

        /// <summary>
        ///     Determines whether the API is Default or KeepAlive.
        /// </summary>
        /// <param name="apiName">Name of the API.</param>
        public static bool IsDefaultOrKeepAlive(string apiName)
        {
            return string.Equals(apiName, ApiRouteMapping.DefaultApiName) || string.Equals(apiName, ApiRouteMapping.KeepAliveApiName);
        }

        private static ODataError ConvertServiceResponseErrorToODataError(Error error)
        {
            return new ODataError
            {
                ErrorCode = error.Code,
                Message = error.Message
            };
        }

        private static void ModifyErrorMessageToHideDetails(Error error, ErrorCode errorCode)
        {
            // Error messages based on error code
            switch (errorCode)
            {
                case ErrorCode.PartnerError:
                    error.Message = GraphApiErrorMessage.InternalServerError;
                    break;

                case ErrorCode.InvalidInput:
                    error.Message = GraphApiErrorMessage.InvalidInputDefault;
                    break;

                case ErrorCode.MissingClientCredentials:
                case ErrorCode.InvalidClientCredentials:
                    error.Message = GraphApiErrorMessage.InvalidClientCredentials;
                    break;

                case ErrorCode.PartnerTimeout:
                    error.Message = GraphApiErrorMessage.PartnerTimeout;
                    break;

                case ErrorCode.TooManyRequests:
                    error.Message = GraphApiErrorMessage.TooManyRequests;
                    break;

                case ErrorCode.Unauthorized:
                    error.Message = GraphApiErrorMessage.Unauthorized;
                    break;

                case ErrorCode.ResourceNotFound:
                    error.Message = GraphApiErrorMessage.ResourceNotFound;
                    break;

                case ErrorCode.Forbidden:
                    error.Message = GraphApiErrorMessage.ForbiddenDefault;
                    break;

                case ErrorCode.SharedAccessSignatureTokenInvalid:
                    error.Message = GraphApiErrorMessage.SharedAccessSignatureTokenInvalid;
                    break;

                case ErrorCode.StorageLocationInvalid:
                    error.Message = GraphApiErrorMessage.StorageLocationInvalid;
                    break;

                case ErrorCode.StorageLocationNotAzureBlob:
                    error.Message = GraphApiErrorMessage.StorageLocationNotAzureBlob;
                    break;

                case ErrorCode.StorageLocationNotServiceSAS:
                    error.Message = GraphApiErrorMessage.StorageLocationNotServiceSAS;
                    break;

                case ErrorCode.StorageLocationAlreadyUsed:
                    error.Message = GraphApiErrorMessage.StorageLocationAlreadyUsed;
                    break;

                case ErrorCode.StorageLocationNeedsWriteAddPermissions:
                    error.Message = GraphApiErrorMessage.StorageLocationNeedsWriteAddPermissions;
                    break;

                case ErrorCode.StorageLocationShouldNotAllowListAccess:
                    error.Message = GraphApiErrorMessage.StorageLocationShouldNotAllowListAccess;
                    break;

                case ErrorCode.StorageLocationShouldNotAllowReadAccess:
                    error.Message = GraphApiErrorMessage.StorageLocationShouldNotAllowReadAccess;
                    break;

                case ErrorCode.StorageLocationShouldSupportAppendBlobs:
                    error.Message = GraphApiErrorMessage.StorageLocationShouldSupportAppendBlobs;
                    break;

                case ErrorCode.Unknown:
                default:
                    error.Message = GraphApiErrorMessage.UnknownDefault;
                    break;
            }
        }
    }
}
