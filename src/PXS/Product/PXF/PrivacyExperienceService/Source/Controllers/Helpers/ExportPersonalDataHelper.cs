// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.Controllers.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.AspNet.OData;
    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Privacy.Core.PCF;
    using Microsoft.Membership.MemberServices.Privacy.Core.PrivacyCommand;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Extensions;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    using HeaderNames = Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.HeaderNames;

    /// <summary>
    ///     Helper class for ExportPersonalData APIs.
    /// </summary>
    public class ExportPersonalDataHelper
    {
        /// <summary>
        ///     Export personal data for an account providing its id.
        /// </summary>
        /// <param name="objectId">The object Id.</param>
        /// <param name="parameters">
        ///     "storageLocation": the location to place all the data in.
        ///     "scope": the scope of the export, this will always be "default" for now.
        /// </param>
        /// <param name="logger">The logger interface.</param>
        /// <param name="pcfProxyService">The Pcf Proxy Service object.</param>
        /// <param name="cloudInstance"></param>
        /// <param name="isTestRequest"></param>
        /// <param name="requestContext"></param>
        /// <param name="request"></param>
        /// <param name="controller">Which controller is calling us.</param>
        /// <param name="addLocationHeaders">Boolean indicating whether to return status location header info</param>
        /// <returns>An HttpResponseMessage.</returns>
        public static async Task<HttpResponseMessage> ExportPersonalData(
            string controller,
            string objectId, 
            ODataActionParameters parameters, 
            ILogger logger,
            IPcfProxyService pcfProxyService,
            string cloudInstance,
            bool isTestRequest,
            bool addLocationHeaders,
            RequestContext requestContext,
            HttpRequestMessage request
            )
        {
            // Check for valid object id
            if (!Guid.TryParse(objectId, out Guid objectIdGuid))
            {
                return request.CreateODataErrorResponse(
                        new Error(ErrorCode.InvalidInput, string.Format(CultureInfo.InvariantCulture, GraphApiErrorMessage.InvalidObjectIdFormat, objectId)),
                        hideDetailErrorMessages: false);
            }

            // Check that the service root header value is set and is a valid uri
            if (!request.Headers.TryGetValues(HeaderNames.MsGraphServiceRoot, out IEnumerable<string> values) ||
                values.Count() != 1 ||
                !Uri.IsWellFormedUriString(values.FirstOrDefault(), UriKind.Absolute))
            {
                return request.CreateODataErrorResponse(
                        new Error(ErrorCode.InvalidInput, string.Format(CultureInfo.InvariantCulture, GraphApiErrorMessage.MissingHeaderFormat, HeaderNames.MsGraphServiceRoot)),
                        hideDetailErrorMessages: false);
            }

            // check that we got a storage location in the body
            if (parameters == null || !parameters.TryGetValue("storageLocation", out object exportDestinationObject) || exportDestinationObject == null)
            {
                return request.CreateODataErrorResponse(new Error(ErrorCode.StorageLocationInvalid, GraphApiErrorMessage.StorageLocationInvalid));
            }

            // Check that the storage location is a properly formatted uri
            if (!Uri.TryCreate((string)exportDestinationObject, UriKind.Absolute, out Uri exportDestination))
            {
                return request.CreateODataErrorResponse(new Error(ErrorCode.StorageLocationInvalid, GraphApiErrorMessage.StorageLocationInvalid));
            }

            if (Sll.Context.Vector == null || string.IsNullOrWhiteSpace(Sll.Context.Vector.Value))
            {
                logger.Warning(controller, $"cv = {Sll.Context.Vector} cv.Value = {Sll.Context.Vector?.Value}");
                Sll.Context.Vector = new CorrelationVector();
            }

            requestContext.RequireIdentity<AadIdentity>().TargetObjectId = objectIdGuid;

            ServiceResponse<Guid> serviceResponse = await pcfProxyService.PostExportRequestAsync(
                requestContext,
                PrivacyRequestConverter.CreateAadExportRequest(
                    requestContext,
                    LogicalWebOperationContext.ServerActivityId,
                    Sll.Context.Vector.Value,
                    DateTimeOffset.UtcNow,
                    exportDestination,
                    false,
                    cloudInstance,
                    Portals.MsGraph,
                    isTestRequest));

            if (serviceResponse == null)
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }

            if (!serviceResponse.IsSuccess)
            {
                return request.CreateODataErrorResponse(serviceResponse.Error, true);
            }

            var responseMessage = new HttpResponseMessage();
            if (addLocationHeaders)
            {
                responseMessage.StatusCode = HttpStatusCode.Accepted;
                AddCommonHeaders(responseMessage, values.FirstOrDefault(), serviceResponse.Result.ToString());
            }
            else
            {
                responseMessage.StatusCode = HttpStatusCode.NoContent;
            }

            return responseMessage;
        }

        private static void AddCommonHeaders(HttpResponseMessage message, string serviceRoot, string operationId)
        {
            message.Headers.Add(HeaderNames.RetryAfter, "60");
            message.Headers.Add(HeaderNames.RequestId, operationId);

            var operationLocationUri = new Uri($"{serviceRoot.TrimEnd('/')}/{RouteNames.DataPolicyOperations}/{operationId}");
            message.Headers.Add(HeaderNames.OperationLocation, operationLocationUri.ToString());

            // This is a stop gap header when MS Graph works on enabling the Operation-Location header
            message.Headers.Add(HeaderNames.Location, operationLocationUri.ToString());
        }
    }
}
