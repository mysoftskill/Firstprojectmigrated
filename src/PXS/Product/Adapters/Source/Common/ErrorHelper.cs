// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Adapters.Common
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Contracts.Exposed;

    public static class ErrorHelper
    {
        public const string EmptyResponseContentMessage = "Empty response content.";
        public const string EmptyResponseMessage = "Empty response.";
        public const string InvalidContentTypeMessage = "Unrecognized response content type: {0}.";
        public const string DeserializeErrorMessage = "Could not deserialize content. Exception: {0}.";
        public const string UnknownExceptionMessage = "An unknown exception occurred. Exception: {0}.";
        public const string WebExceptionMessage = "A WebException occurred. Status: {0}, Exception: {1}.";
        public const string UnknownWebExceptionMessage = "An unknown WebException occurred. Status: {0}, Exception: {1}.";
        public const string UnauthorizedErrorMessage = "Unable to authorize with partner.";
        public const string UnknownUser = "Unknown user.";
        public const string PartnerIsCurrentlyDisabledErrorMessage = "Partner is currently disabled.";

        public const string EmptyServiceResponseMessage = "Empty response from service.";
        public const string EmptyContentServiceResponseMessage = "Empty content in response from service.";
        public const string ServiceDeserializeErrorMessage = "Could not deserialize response. Exception: {0}.";

        public static readonly IDictionary<HttpStatusCode, ErrorCode> ResponseCodeMapping =
            new Dictionary<HttpStatusCode, ErrorCode>
            {
                { HttpStatusCode.ServiceUnavailable, ErrorCode.PartnerError },
                { HttpStatusCode.InternalServerError, ErrorCode.PartnerError },
                { HttpStatusCode.BadRequest, ErrorCode.PartnerErrorInternal },
                { HttpStatusCode.RequestTimeout, ErrorCode.PartnerTimeout },
                { HttpStatusCode.Unauthorized, ErrorCode.PartnerAuthorizationFailure },
                { HttpStatusCode.Forbidden, ErrorCode.PartnerAuthorizationFailure }
            };

        public static readonly IDictionary<WebExceptionStatus, ErrorCode> WebExceptionMapping =
            new Dictionary<WebExceptionStatus, ErrorCode>
            {
                { WebExceptionStatus.ConnectFailure, ErrorCode.PartnerUnreachable },
                { WebExceptionStatus.NameResolutionFailure, ErrorCode.PartnerUnreachable },
                { WebExceptionStatus.ProxyNameResolutionFailure, ErrorCode.PartnerUnreachable },
                { WebExceptionStatus.Timeout, ErrorCode.PartnerTimeout },
                { WebExceptionStatus.TrustFailure, ErrorCode.PartnerCertificateInvalid },
            };

        public static ErrorInfo CreateEmptyResponseError()
        {
            return new ErrorInfo
            {
                ErrorCode = ErrorCode.PartnerError,
                ErrorMessage = EmptyResponseMessage
            };
        }

        public static ErrorInfo CreateInvalidInputError(string message)
        {
            return new ErrorInfo
            {
                ErrorCode = ErrorCode.InvalidInput,
                ErrorMessage = message,
            };
        }

        public static ErrorInfo CreateErrorInfo(ErrorCode errorCode, string message)
        {
            return new ErrorInfo
            {
                ErrorCode = errorCode,
                ErrorMessage = message
            };
        }

        public static ErrorInfo CreateEmptyServiceResponseError()
        {
            return new ErrorInfo
            {
                ErrorCode = ErrorCode.ServiceError,
                ErrorMessage = EmptyServiceResponseMessage
            };
        }

        public static ErrorInfo CreateNullContentServiceResponseError()
        {
            return new ErrorInfo
            {
                ErrorCode = ErrorCode.ServiceError,
                ErrorMessage = EmptyContentServiceResponseMessage
            };
        }

        /// <summary>
        /// Creates an ErrorInfo for an exception.
        /// </summary>
        /// <remarks>
        /// If the exception is a WebException or has an inner WebException, 
        /// then this will attempt to map the WebException status code to an ErrorCode. 
        /// It will also include the WebException status in the error message.
        /// </remarks>
        /// <param name="ex">The exception.</param>
        /// <returns>The ErrorInfo.</returns>
        public static ErrorInfo CreateExceptionError(Exception ex)
        {
            string errorMessage;
            ErrorCode errorCode;

            WebException webException;
            WebExceptionHelper.TryGetWebException(ex, out webException);
            PartnerException partnerException = ex as PartnerException;

            if (webException != null)
            {
                // Map the web exception status to an ErrorCode, or Unknown
                if (!WebExceptionMapping.TryGetValue(webException.Status, out errorCode))
                {
                    errorMessage = string.Format(
                        CultureInfo.InvariantCulture, UnknownWebExceptionMessage, webException.Status, ex);
                    errorCode = ErrorCode.Unknown;
                }
                else
                {
                    errorMessage = string.Format(
                        CultureInfo.InvariantCulture, WebExceptionMessage, webException.Status, ex);
                }
            }
            else if (partnerException != null)
            {
                errorCode = (ErrorCode)partnerException.ErrorCode;
                errorMessage = string.Format(CultureInfo.InvariantCulture, "Partner exception: {0}", partnerException);
            }
            else
            {
                errorCode = ErrorCode.Unknown;
                errorMessage = string.Format(CultureInfo.InvariantCulture, UnknownExceptionMessage, ex);
            }

            return new ErrorInfo
            {
                ErrorCode = errorCode,
                ErrorMessage = errorMessage
            };
        }
        
        /// <summary>
        /// Creates an ErrorInfo for an HttpResponseMessage.
        /// </summary>
        /// <remarks>
        /// If the response's status code is unsuccessful, then this will attempt to map the status to an ErrorCode.
        /// Otherwise, returns null.
        /// </remarks>
        /// <param name="response">HTTP response message.</param>
        /// <param name="innerError">The inner error details.</param>
        /// <returns>The ErrorInfo.</returns>
        public static async Task<ErrorInfo> CreateResponseMessageErrorAsync(HttpResponseMessage response, InnerErrorDetails innerError = null)
        {
            if (response.IsSuccessStatusCode)
            {
                return null;
            }

            // Map the status code to an ErrorCode, or Unknown
            ErrorCode errorCode;
            if (!ResponseCodeMapping.TryGetValue(response.StatusCode, out errorCode))
            {
                errorCode = ErrorCode.Unknown;
            }

            return new ErrorInfo(errorCode, await ConvertToString(response), innerError);
        }

        public static ErrorInfo CreateUnauthorizedError()
        {
            return new ErrorInfo
            {
                ErrorCode = ErrorCode.PartnerAuthorizationFailure,
                ErrorMessage = UnauthorizedErrorMessage
            };
        }

        public static ErrorInfo CreateUnknownUserError()
        {
            return new ErrorInfo
            {
                ErrorCode = ErrorCode.UnknownUser,
                ErrorMessage = UnknownUser
            };
        }

        public static ErrorInfo CreateInvalidMsaTokenError()
        {
            return new ErrorInfo()
            {
                ErrorCode = ErrorCode.PartnerAuthorizationFailureMsaToken,
                ErrorMessage = "The MSA token was invalid."
            };
        }

        public static ErrorInfo CreateResponseDeserializationError(Exception ex)
        {
            return new ErrorInfo
            {
                ErrorCode = ErrorCode.ServiceError,
                ErrorMessage = string.Format(CultureInfo.InvariantCulture, DeserializeErrorMessage, ex)
            };
        }

        public static ErrorInfo CreateInvalidContentTypeError(MediaTypeHeaderValue contentType)
        {
            return new ErrorInfo
            {
                ErrorCode = ErrorCode.ServiceError,
                ErrorMessage = string.Format(CultureInfo.InvariantCulture, InvalidContentTypeMessage, contentType)
            };
        }

        public static ErrorInfo CreateAggregateError(ErrorCode errorCode, params Tuple<string, ErrorInfo>[] errors)
        {
            StringBuilder errorMessage = new StringBuilder();
            string errorFormat = "{0}. ErrorInfo: {1}";
            InnerErrorDetails innerError = null;

            foreach(Tuple<string, ErrorInfo> taskError in errors)
            {
                string description = taskError.Item1;
                ErrorInfo errorInfo = taskError.Item2;

                if (errorInfo != null)
                {
                    errorMessage.AppendLine(string.Format(CultureInfo.InvariantCulture,
                        errorFormat, description, errorInfo.ToString()));

                    var innerErrorCode = "{0}({1})".FormatInvariant(description, errorInfo.FlattenedErrorCode);
                    innerError = new InnerErrorDetails(innerErrorCode, string.Empty, innerError);
                }
            }

            return new ErrorInfo(errorCode, errorMessage.ToString().Trim(), innerError);
        }

        public static ErrorInfo CreatePartnerError(string details)
        {
            return new ErrorInfo()
            {
                ErrorCode = ErrorCode.PartnerError,
                ErrorMessage = details
            };
        }

        public static bool HasItems(this IEnumerable<ErrorInfo> errorInfos)
        {
            return (errorInfos != null && errorInfos.Any());
        }

        private static async Task<string> ConvertToString(HttpResponseMessage response)
        {
            // Create error message using HTTP response content and headers
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(response.StatusCode);

            if (response.Headers != null)
            {
                foreach (KeyValuePair<string, IEnumerable<string>> header in response.Headers)
                {
                    stringBuilder.AppendLine().AppendFormat(CultureInfo.InvariantCulture, "{0}: {1}", header.Key, string.Join(", ", header.Value));
                }
            }

            if (response.Content != null)
            {
                stringBuilder.AppendLine().Append(await response.Content.ReadAsStringAsync());
            }

            return stringBuilder.ToString();
        }
    }
}
