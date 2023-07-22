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
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    using HeaderNames = Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.HeaderNames;

    /// <summary>
    ///     Helper class for RemovePersonalData APIs.
    /// </summary>
    public class RemovePersonalDataHelper
    {
        /// <summary>
        ///     Remove personal data for an account providing its object id and target tenant id.
        /// </summary>
        /// <param name="objectId">The object Id.</param>
        /// <param name="targetTenantId">The target tenant Id.</param>
        /// <param name="logger">The logger interface.</param>
        /// <param name="pcfProxyService">The Pcf Proxy Service object.</param>
        /// <param name="cloudInstance"></param>
        /// <param name="isTestRequest"></param>
        /// <param name="requestContext"></param>
        /// <param name="request"></param>
        /// <param name="controller">Which controller is calling us.</param>
        /// <param name="isMultiTenantCollaborationEnabled">Boolean indicating whether multi-tenant collaboration feature is enabled</param>
        /// <returns>An HttpResponseMessage.</returns>
        public static async Task<HttpResponseMessage> RemovePersonalData(
            string controller,
            string objectId,
            Guid targetTenantId, 
            ILogger logger,
            IPcfProxyService pcfProxyService,
            string cloudInstance,
            bool isTestRequest,
            bool isMultiTenantCollaborationEnabled,
            RequestContext requestContext,
            HttpRequestMessage request
            )
        {
            if (!isMultiTenantCollaborationEnabled)
            {
                return request.CreateODataErrorResponse(
                        new Error(ErrorCode.InvalidInput, $"Operation is not supported."),
                        hideDetailErrorMessages: false);
            }

            if (!Guid.TryParse(objectId, out Guid targetObjectId))
            {
                return request.CreateODataErrorResponse(
                        new Error(ErrorCode.InvalidInput, string.Format(CultureInfo.InvariantCulture, GraphApiErrorMessage.InvalidObjectIdFormat, objectId)),
                        hideDetailErrorMessages: false);
            }

            if (!request.Headers.TryGetValues(HeaderNames.MsGraphServiceRoot, out IEnumerable<string> values) ||
                values.Count() != 1 ||
                !Uri.IsWellFormedUriString(values.FirstOrDefault(), UriKind.Absolute))
            {
                return request.CreateODataErrorResponse(
                        new Error(ErrorCode.InvalidInput, string.Format(CultureInfo.InvariantCulture, GraphApiErrorMessage.MissingHeaderFormat, HeaderNames.MsGraphServiceRoot)),
                        hideDetailErrorMessages: false);
            }

            if (Sll.Context.Vector == null || string.IsNullOrWhiteSpace(Sll.Context.Vector.Value))
            {
                logger.Warning(controller, $"cv = {Sll.Context.Vector} cv.Value = {Sll.Context.Vector?.Value}");
                Sll.Context.Vector = new CorrelationVector();
            }

            // Create subject based on the targeted objectId/tenantId; 
            // this may not be the same identity as the caller
            AadSubject subject = PrivacyRequestConverter.CreateAadSubjectFromIdentity(new AadIdentity(targetObjectId, targetTenantId, 0));

            requestContext.RequireIdentity<AadIdentity>().TargetObjectId = targetObjectId;
            var requestGuid = LogicalWebOperationContext.ServerActivityId;
            ServiceResponse<Guid> serviceResponse = await pcfProxyService.PostAccountCleanupRequestAsync(
                requestContext,
                PrivacyRequestConverter.CreatePcfAccountCloseRequest(
                    subject,
                    requestContext,
                    requestGuid,
                    Sll.Context.Vector.Value,
                    DateTimeOffset.UtcNow,
                    cloudInstance,
                    Portals.MsGraph,
                    null, // no pre-verifier token
                    isTestRequest));

            if (serviceResponse == null)
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }

            if (!serviceResponse.IsSuccess)
            {
                return request.CreateODataErrorResponse(serviceResponse.Error, true);
            }

            var responseMessage = new HttpResponseMessage(HttpStatusCode.NoContent);

            return responseMessage;
        }
    }
}
