// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Aggregation;
    using Microsoft.Membership.MemberServices.Privacy.Core.PCF;
    using Microsoft.Membership.MemberServices.Privacy.Core.PrivacyCommand;
    using Microsoft.Membership.MemberServices.Privacy.Core.TestMsa;
    using Microsoft.Membership.MemberServices.Privacy.Core.Timeline;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.PrivacySubject;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Controllers.Helpers;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Security;
    using Microsoft.PrivacyServices.PXS.Command.CommandStatus;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    // This using should not be used: (Check Delete/Export V1 vs V2 endpoints, they use different namespaces with the same named types)
    // Instead make sure the using above (Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.PrivacySubject) is kept
    ////using Microsoft.PrivacyServices.PrivacyOperation.Contracts.PrivacySubject;

    /// <summary>
    ///     This controller is for privacy requests, such as from PCD.
    /// </summary>

    [Authorize]
    public class PrivacyRequestApiController : PrivacyController
    {
        private readonly string cloudInstance;

        private readonly IPrivacyConfigurationManager configurationManager;

        private readonly DataTypesClassifier dataTypesClassifier;

        private readonly IPcfProxyService pcfProxyService;

        private readonly IRequestClassifier requestClassifier;

        private readonly ITestMsaService testMsaService;

        private readonly ITimelineService timelineService;

        private readonly IAppConfiguration appConfiguration;

        /// <summary>
        ///     Creates a new <see cref="PrivacyRequestApiController"/>
        /// </summary>
        public PrivacyRequestApiController(
            IPcfProxyService pcfProxyService,
            ITestMsaService testMsaService,
            IPrivacyConfigurationManager configurationManager,
            ITimelineService timelineService,
            DataTypesClassifier dataTypesClassifier,
            IRequestClassifier requestClassifier,
            IAppConfiguration appConfiguration)
        {
            this.timelineService = timelineService ?? throw new ArgumentNullException(nameof(timelineService));
            this.dataTypesClassifier = dataTypesClassifier ?? throw new ArgumentNullException(nameof(dataTypesClassifier));
            this.pcfProxyService = pcfProxyService;
            this.testMsaService = testMsaService;
            this.configurationManager = configurationManager;
            this.cloudInstance = (configurationManager?.PrivacyExperienceServiceConfiguration?.CloudInstance).ToPcfCloudInstance();
            this.requestClassifier = requestClassifier ?? throw new ArgumentNullException(nameof(requestClassifier));
            this.appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));
        }

        /// <summary>
        ///     This allows a partner to execute a delete.
        /// </summary>
        /// <group>Privacy Request Api</group>
        /// <verb>post</verb>
        /// <url>https://pxs.api.account.microsoft.com/v1/privacyrequest/delete</url>        
        /// <header name="X-Family-Json-Web-Token" cref="string" required="false">The family JWT with information required for on behalf of.</header>
        /// <header name="client-request-id" cref="string" required="false">The client activity ID.</header>
        /// <header name="MS-CV" cref="string" required="false">The correlation vector for the request.</header>
        /// <header name="Correlation-Context" cref="string" required="false">The correlation context for the request.</header>
        /// <header name="X-Flights" cref="string" required="false">The flights to apply to the request applied.</header>
        /// <header name="server-machine-id" cref="string" required="false">The server machine ID.</header>
        /// <header name="X-S2S-Proxy-Ticket" cref="string" required="false">The MSA User Proxy Ticket.</header>
        /// <header name="X-S2S-Access-Token" cref="string" required="false">The MSA S2S Access Token.</header>
        /// <param in="query" name="dataTypes" required="true" cref="string">A comma separated list of privacy data types to perform delete on.</param>
        /// <param in="query" name="startTime" required="true" cref="DateTimeOffset">The start time of the time window to perform delete on.</param>
        /// <param in="query" name="endTime" required="true" cref="DateTimeOffset">The end time of the time window to perform delete on.</param>
        /// <param in="body" name="deleteOperationRequest" required="true"><see cref="DeleteOperationRequest"/>Request body</param>
        /// <response code="200"><see cref="OperationResponse"/></response>
        [HttpPost]
        [Route(RouteNames.PrivacyRequestApiDelete)]
        [PrivacyExperienceIdentityAuthorization(typeof(MsaSelfIdentity), typeof(AadIdentity), typeof(AadIdentityWithMsaUserProxyTicket))]
        public async Task<IHttpActionResult> DeleteAsync(
            string dataTypes,
            DateTimeOffset startTime,
            DateTimeOffset endTime,
            [FromBody] DeleteOperationRequest deleteOperationRequest)
        {
            if (deleteOperationRequest.Subject == null)
                return new ErrorHttpActionResult(new Error(ErrorCode.InvalidInput, "Subject cannot be null"), this.Request);

            if (deleteOperationRequest.Subject is MsaSelfAuthSubject
                && !(this.CurrentRequestContext.Identity is MsaSelfIdentity))
                return new ErrorHttpActionResult(new Error(ErrorCode.InvalidClientCredentials, "Must provide proxy ticket for MsaSelfAuth"), this.Request);

            this.SetSubjectTypev1(deleteOperationRequest.Subject);
            var useEmailOnlyManadatoryRule = await this.appConfiguration.IsFeatureFlagEnabledAsync(ConfigNames.PXS.PRCMakeEmailMandatory).ConfigureAwait(false);
            deleteOperationRequest.Subject.Validate(SubjectUseContext.Delete, useEmailOnlyManadatoryRule);

            List<string> dataTypesList = dataTypes?.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries).Distinct().ToList();

            string requestOriginPortal = PortalHelper.DeducePortal(this, this.User.Identity, this.configurationManager.PrivacyExperienceServiceConfiguration.SiteIdToCallerName);

            List<DeleteRequest> requests = PrivacyRequestConverter.CreatePcfDeleteRequests(
                PrivacyRequestConverter.ToSubject(deleteOperationRequest.Subject, this.CurrentRequestContext),
                this.CurrentRequestContext,
                LogicalWebOperationContext.ServerActivityId,
                Sll.Context.Vector.Value,
                deleteOperationRequest.Context,
                DateTimeOffset.UtcNow,
                dataTypesList,
                startTime,
                endTime,
                this.cloudInstance,
                requestOriginPortal,
                this.requestClassifier.IsTestRequest(requestOriginPortal, this.User.Identity, GetCorrelationContextRootOperationName())).ToList();

            ServiceResponse<IList<Guid>> pcfResponse = await this.pcfProxyService.PostDeleteRequestsAsync(
                this.CurrentRequestContext,
                requests).ConfigureAwait(false);

            ServiceResponse<OperationResponse> response = !pcfResponse.IsSuccess
                ? new ServiceResponse<OperationResponse> { Error = pcfResponse.Error }
                : new ServiceResponse<OperationResponse> { Result = new OperationResponse { Ids = pcfResponse.Result } };

            return this.CreateHttpActionResult(response);
        }

        /// <summary>
        ///     This allows a partner to execute a delete.
        /// </summary>
        /// <group>Privacy Request Api</group>
        /// <verb>post</verb>
        /// <url>https://pxs.api.account.microsoft.com/v2/privacyrequest/delete</url>        
        /// <header name="client-request-id" cref="string" required="false">The client activity ID.</header>
        /// <header name="MS-CV" cref="string" required="false">The correlation vector for the request.</header>
        /// <header name="Correlation-Context" cref="string" required="false">The correlation context for the request.</header>
        /// <header name="X-Flights" cref="string" required="false">The flights to apply to the request applied.</header>
        /// <header name="server-machine-id" cref="string" required="false">The server machine ID.</header>
        /// <header name="X-S2S-Proxy-Ticket" cref="string" required="false">The MSA User Proxy Ticket.</header>
        /// <header name="X-S2S-Access-Token" cref="string" required="false">The MSA S2S Access Token.</header>
        /// <param in="query" name="dataTypes" required="true" cref="string">A comma separated list of privacy data types to perform delete on.</param>
        /// <param in="query" name="startTime" required="true" cref="DateTimeOffset">The start time of the time window to perform delete on.</param>
        /// <param in="query" name="endTime" required="true" cref="DateTimeOffset">The end time of the time window to perform delete on.</param>
        /// <param in="body" name="deleteOperationRequest" required="true"><see cref="DeleteOperationRequest"/>Request body</param>
        /// <response code="200"><see cref="OperationResponse"/></response>
        [HttpPost]
        [Route(RouteNames.PrivacyRequestApiDeleteV2)]
        [PrivacyExperienceIdentityAuthorization(typeof(AadIdentity), typeof(AadIdentityWithMsaUserProxyTicket))]
        public async Task<IHttpActionResult> DeleteAsyncV2(
            string dataTypes,
            DateTimeOffset startTime,
            DateTimeOffset endTime,
            [FromBody] PrivacyServices.PrivacyOperation.Contracts.PrivacySubject.DeleteOperationRequest deleteOperationRequest)
        {
            if (deleteOperationRequest.Subject == null)
            {
                return new ErrorHttpActionResult(new Error(ErrorCode.InvalidInput, "Subject cannot be null"), this.Request);
            }

            if (deleteOperationRequest.Subject is PrivacyServices.PrivacyOperation.Contracts.PrivacySubject.MsaSelfAuthSubject
                && !(this.CurrentRequestContext.Identity is AadIdentityWithMsaUserProxyTicket))
            {
                return new ErrorHttpActionResult(new Error(ErrorCode.InvalidClientCredentials, "Must provide proxy ticket for MsaSelfAuth"), this.Request);
            }

            this.SetSubjectTypev2(deleteOperationRequest.Subject);
            var useEmailOnlyManadatoryRule = await this.appConfiguration.IsFeatureFlagEnabledAsync(ConfigNames.PXS.PRCMakeEmailMandatory).ConfigureAwait(false);
            deleteOperationRequest.Subject.Validate(PrivacyServices.PrivacyOperation.Contracts.PrivacySubject.SubjectUseContext.Delete, useEmailOnlyManadatoryRule);

            IList<string> dataTypesList = dataTypes?.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries).Distinct().ToList();
            List<Guid> pcfResponeGuids = new List<Guid>();

            // Portal where request was initiated
            string portal = PortalHelper.DeducePortal(
                this,
                this.User.Identity,
                this.configurationManager.PrivacyExperienceServiceConfiguration.SiteIdToCallerName);

            // Check for PCD requests with MSASubject and split the requests based on data type
            // Timeline supported DataTypes are sent to Timeline Controller (Which internally issues PCF Delete)
            // Else Data types are sent to PCF
            if (deleteOperationRequest.Subject is PrivacyServices.PrivacyOperation.Contracts.PrivacySubject.MsaSelfAuthSubject &&
                this.CurrentRequestContext.Identity is AadIdentityWithMsaUserProxyTicket && dataTypesList != null)
            {
                this.dataTypesClassifier.Classify(dataTypesList, out IList<string> dataTypesForTimeline, out IList<string> dataTypesForPcf);

                if (dataTypesForTimeline.Any())
                {
                    // Delete Timeline
                    ServiceResponse timelineResponse = await this.timelineService
                        .DeleteAsync(
                            this.CurrentRequestContext,
                            dataTypesForTimeline,
                            TimeSpan.MaxValue,
                            portal: portal)
                        .ConfigureAwait(false);

                    if (!timelineResponse.IsSuccess)
                    {
                        return this.CreateHttpActionResult(
                            new ServiceResponse<PrivacyServices.PrivacyOperation.Contracts.PrivacySubject.OperationResponse> { Error = timelineResponse.Error });
                    }

                    if (timelineResponse is ServiceResponse<IList<Guid>> timelineResponseGuids)
                    {
                        pcfResponeGuids.AddRange(timelineResponseGuids.Result);
                    }
                }

                // Check if nothing to process on PCF
                if (!dataTypesForPcf.Any())
                {
                    // Return empty list if no datatypes to process on PCF
                    return this.CreateHttpActionResult(
                        new ServiceResponse<PrivacyServices.PrivacyOperation.Contracts.PrivacySubject.OperationResponse>
                        {
                            Result = new PrivacyServices.PrivacyOperation.Contracts.PrivacySubject.OperationResponse { Ids = new List<Guid>() }
                        }
                    );
                }

                dataTypesList = dataTypesForPcf;
            }
            List<DeleteRequest> requests = PrivacyRequestConverter.CreatePcfDeleteRequests(
                PrivacyRequestConverter.ToSubject(deleteOperationRequest.Subject, this.CurrentRequestContext),
                this.CurrentRequestContext,
                LogicalWebOperationContext.ServerActivityId,
                Sll.Context.Vector.Value,
                deleteOperationRequest.Context,
                DateTimeOffset.UtcNow,
                dataTypesList,
                startTime,
                endTime,
                this.cloudInstance,
                portal,
                this.requestClassifier.IsTestRequest(portal, this.User.Identity, GetCorrelationContextRootOperationName())).ToList();

            ServiceResponse<IList<Guid>> pcfResponse = await this.pcfProxyService.PostDeleteRequestsAsync(
                this.CurrentRequestContext,
                requests).ConfigureAwait(false);

            ServiceResponse<PrivacyServices.PrivacyOperation.Contracts.PrivacySubject.OperationResponse> response;
            if (!pcfResponse.IsSuccess)
            {
                response = new ServiceResponse<PrivacyServices.PrivacyOperation.Contracts.PrivacySubject.OperationResponse> { Error = pcfResponse.Error };
            }
            else
            {
                pcfResponeGuids.AddRange(pcfResponse.Result);
                response = new ServiceResponse<PrivacyServices.PrivacyOperation.Contracts.PrivacySubject.OperationResponse>
                {
                    Result = new PrivacyServices.PrivacyOperation.Contracts.PrivacySubject.OperationResponse { Ids = pcfResponeGuids }
                };
            }

            return this.CreateHttpActionResult(response);
        }

        /// <summary>
        ///     This allows a partner to execute an export.
        /// </summary>
        /// <group>Privacy Request Api</group>
        /// <verb>post</verb>
        /// <url>https://pxs.api.account.microsoft.com/v1/privacyrequest/export</url>        
        /// <header name="X-Family-Json-Web-Token" cref="string" required="false">The family JWT with information required for on behalf of.</header>
        /// <header name="client-request-id" cref="string" required="false">The client activity ID.</header>
        /// <header name="MS-CV" cref="string" required="false">The correlation vector for the request.</header>
        /// <header name="Correlation-Context" cref="string" required="false">The correlation context for the request.</header>
        /// <header name="X-Flights" cref="string" required="false">The flights to apply to the request applied.</header>
        /// <header name="server-machine-id" cref="string" required="false">The server machine ID.</header>
        /// <header name="X-S2S-Proxy-Ticket" cref="string" required="false">The MSA User Proxy Ticket.</header>
        /// <header name="X-S2S-Access-Token" cref="string" required="false">The MSA S2S Access Token.</header>
        /// <param in="query" name="dataTypes" required="true" cref="string">A comma separated list of privacy data types to export.</param>
        /// <param in="query" name="startTime" required="false" cref="DateTimeOffset">The start time of the time window to export.</param>
        /// <param in="query" name="endTime" required="false" cref="DateTimeOffset">The end time of the time window to export.</param>
        /// <param in="body" name="exportOperationRequest" required="true"><see cref="ExportOperationRequest"/>Request body</param>
        /// <param in="query" name="isSynthetic" required="false" cref="bool">A flag indicating if the request is synthetic, by default <c>false</c>.</param>
        /// <response code="200"><see cref="OperationResponse"/></response>
        [HttpPost]
        [Route(RouteNames.PrivacyRequestApiExport)]
        [PrivacyExperienceIdentityAuthorization(typeof(MsaSelfIdentity), typeof(AadIdentity), typeof(AadIdentityWithMsaUserProxyTicket))]
        public async Task<IHttpActionResult> ExportAsync(
            string dataTypes,
            DateTimeOffset? startTime,
            DateTimeOffset? endTime,
            [FromBody] ExportOperationRequest exportOperationRequest,
            bool isSynthetic = false)
        {
            if (!startTime.HasValue)
                startTime = DateTimeOffset.MinValue.UtcDateTime;
            if (!endTime.HasValue)
                endTime = DateTimeOffset.MaxValue.UtcDateTime;

            PrivacyServices.CommandFeed.Contracts.Subjects.IPrivacySubject subject;
            if (exportOperationRequest.Subject != null)
            {
                if (exportOperationRequest.Subject is MsaSelfAuthSubject && !(this.CurrentRequestContext.Identity is MsaSelfIdentity))
                    return new ErrorHttpActionResult(new Error(ErrorCode.InvalidClientCredentials, "Must provide proxy ticket for MsaSelfAuth"), this.Request);
                this.SetSubjectTypev1(exportOperationRequest.Subject); 
                var useEmailOnlyManadatoryRule = await this.appConfiguration.IsFeatureFlagEnabledAsync(ConfigNames.PXS.PRCMakeEmailMandatory).ConfigureAwait(false);
                exportOperationRequest.Subject.Validate(SubjectUseContext.Export, useEmailOnlyManadatoryRule);
                subject = PrivacyRequestConverter.ToSubject(exportOperationRequest.Subject, this.CurrentRequestContext);
            }
            else
            {
                subject = PrivacyRequestConverter.CreateAadSubjectFromIdentity(this.CurrentRequestContext.RequireIdentity<AadIdentity>());

                // Data type parameter is ignored for AAD subjects. This is a curated list.
                // See PrivacyRequestConverter.defaultExportDataTypes
                dataTypes = null;
            }

            List<string> dataTypesList = dataTypes?.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries).Distinct().ToList();
            string requestOriginPortal = PortalHelper.DeducePortal(this, this.User.Identity, this.configurationManager.PrivacyExperienceServiceConfiguration.SiteIdToCallerName);

            ServiceResponse<Guid> pcfResponse = await this.pcfProxyService.PostExportRequestAsync(
                this.CurrentRequestContext,
                PrivacyRequestConverter.CreateExportRequest(
                    subject,
                    this.CurrentRequestContext,
                    LogicalWebOperationContext.ServerActivityId,
                    Sll.Context.Vector.Value,
                    DateTimeOffset.UtcNow,
                    exportOperationRequest.Context,
                    null,
                    exportOperationRequest.StorageLocationUri,
                    dataTypesList,
                    isSynthetic,
                    this.cloudInstance,
                    requestOriginPortal,
                    this.requestClassifier.IsTestRequest(requestOriginPortal, this.User.Identity, GetCorrelationContextRootOperationName()))).ConfigureAwait(false);

            ServiceResponse<OperationResponse> response = !pcfResponse.IsSuccess
                ? new ServiceResponse<OperationResponse> { Error = pcfResponse.Error }
                : new ServiceResponse<OperationResponse> { Result = new OperationResponse { Ids = new List<Guid> { pcfResponse.Result } } };

            return this.CreateHttpActionResult(response);
        }

        /// <summary>
        ///     This allows a partner to execute an export.
        /// </summary>
        /// <group>Privacy Request Api</group>
        /// <verb>post</verb>
        /// <url>https://pxs.api.account.microsoft.com/v2/privacyrequest/export</url>        
        /// <header name="client-request-id" cref="string" required="false">The client activity ID.</header>
        /// <header name="MS-CV" cref="string" required="false">The correlation vector for the request.</header>
        /// <header name="Correlation-Context" cref="string" required="false">The correlation context for the request.</header>
        /// <header name="X-Flights" cref="string" required="false">The flights to apply to the request applied.</header>
        /// <header name="server-machine-id" cref="string" required="false">The server machine ID.</header>
        /// <header name="X-S2S-Proxy-Ticket" cref="string" required="false">The MSA User Proxy Ticket.</header>
        /// <header name="X-S2S-Access-Token" cref="string" required="false">The MSA S2S Access Token.</header>
        /// <param in="query" name="dataTypes" required="true" cref="string">A comma separated list of privacy data types to export.</param>
        /// <param in="query" name="startTime" required="false" cref="DateTimeOffset">The start time of the time window to export.</param>
        /// <param in="query" name="endTime" required="false" cref="DateTimeOffset">The end time of the time window to export.</param>
        /// <param in="body" name="exportOperationRequest" required="true"><see cref="ExportOperationRequest"/>Request body</param>
        /// <param in="query" name="isSynthetic" required="false" cref="bool">A flag indicating if the request is synthetic, by default <c>false</c>.</param>
        /// <response code="200"><see cref="OperationResponse"/></response>
        [HttpPost]
        [Route(RouteNames.PrivacyRequestApiExportV2)]
        [PrivacyExperienceIdentityAuthorization(typeof(AadIdentity), typeof(AadIdentityWithMsaUserProxyTicket))]
        public async Task<IHttpActionResult> ExportAsyncV2(
            string dataTypes,
            DateTimeOffset? startTime,
            DateTimeOffset? endTime,
            [FromBody] PrivacyServices.PrivacyOperation.Contracts.PrivacySubject.ExportOperationRequest exportOperationRequest,
            bool isSynthetic = false)
        {
            if (!startTime.HasValue)
                startTime = DateTimeOffset.MinValue.UtcDateTime;
            if (!endTime.HasValue)
                endTime = DateTimeOffset.MaxValue.UtcDateTime;

            PrivacyServices.CommandFeed.Contracts.Subjects.IPrivacySubject subject;
            if (exportOperationRequest.Subject != null)
            {
                if (exportOperationRequest.Subject is PrivacyServices.PrivacyOperation.Contracts.PrivacySubject.MsaSelfAuthSubject &&
                    !(this.CurrentRequestContext.Identity is AadIdentityWithMsaUserProxyTicket))
                    return new ErrorHttpActionResult(new Error(ErrorCode.InvalidClientCredentials, "Must provide proxy ticket for MsaSelfAuth"), this.Request);

                this.SetSubjectTypev2(exportOperationRequest.Subject);
                var useEmailOnlyManadatoryRule = await this.appConfiguration.IsFeatureFlagEnabledAsync(ConfigNames.PXS.PRCMakeEmailMandatory).ConfigureAwait(false);
                exportOperationRequest.Subject.Validate(PrivacyServices.PrivacyOperation.Contracts.PrivacySubject.SubjectUseContext.Export, useEmailOnlyManadatoryRule);
                subject = PrivacyRequestConverter.ToSubject(exportOperationRequest.Subject, this.CurrentRequestContext);
            }
            else
            {
                subject = PrivacyRequestConverter.CreateAadSubjectFromIdentity(this.CurrentRequestContext.RequireIdentity<AadIdentity>());

                // Data type parameter is ignored for AAD subjects. This is a curated list.
                // See PrivacyRequestConverter.defaultExportDataTypes
                dataTypes = null;
            }

            List<string> dataTypesList = dataTypes?.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries).Distinct().ToList();
            string requestOriginPortal = PortalHelper.DeducePortal(this, this.User.Identity, this.configurationManager.PrivacyExperienceServiceConfiguration.SiteIdToCallerName);

            ServiceResponse<Guid> pcfResponse = await this.pcfProxyService.PostExportRequestAsync(
                this.CurrentRequestContext,
                PrivacyRequestConverter.CreateExportRequest(
                    subject,
                    this.CurrentRequestContext,
                    LogicalWebOperationContext.ServerActivityId,
                    Sll.Context.Vector.Value,
                    DateTimeOffset.UtcNow,
                    exportOperationRequest.Context,
                    null,
                    exportOperationRequest.StorageLocationUri,
                    dataTypesList,
                    isSynthetic,
                    this.cloudInstance,
                    requestOriginPortal,
                    this.requestClassifier.IsTestRequest(requestOriginPortal, this.User.Identity, GetCorrelationContextRootOperationName()))).ConfigureAwait(false);

            ServiceResponse<PrivacyServices.PrivacyOperation.Contracts.PrivacySubject.OperationResponse> response;
            if (!pcfResponse.IsSuccess)
            {
                response = new ServiceResponse<PrivacyServices.PrivacyOperation.Contracts.PrivacySubject.OperationResponse> { Error = pcfResponse.Error };
            }
            else
            {
                response = new ServiceResponse<PrivacyServices.PrivacyOperation.Contracts.PrivacySubject.OperationResponse>
                {
                    Result = new PrivacyServices.PrivacyOperation.Contracts.PrivacySubject.OperationResponse { Ids = new List<Guid> { pcfResponse.Result } }
                };
            }

            return this.CreateHttpActionResult(response);
        }

        /// <summary>
        ///     This allows a partner to list their requests. It is only site-id authenticated.
        /// </summary>
        /// <group>Privacy Request Api</group>
        /// <verb>get</verb>
        /// <url>https://pxs.api.account.microsoft.com/v1/privacyrequest/list</url>        
        /// <header name="X-Family-Json-Web-Token" cref="string" required="false">The family JWT with information required for on behalf of.</header>
        /// <header name="client-request-id" cref="string" required="false">The client activity ID.</header>
        /// <header name="MS-CV" cref="string" required="false">The correlation vector for the request.</header>
        /// <header name="Correlation-Context" cref="string" required="false">The correlation context for the request.</header>
        /// <header name="X-Flights" cref="string" required="false">The flights to apply to the request applied.</header>
        /// <header name="server-machine-id" cref="string" required="false">The server machine ID.</header>
        /// <header name="X-S2S-Proxy-Ticket" cref="string" required="false">The MSA User Proxy Ticket.</header>
        /// <header name="X-S2S-Access-Token" cref="string" required="false">The MSA S2S Access Token.</header>
        /// <param name="requestTypes" required="false" cref="string">A comma separated list of request types to list.</param>
        /// <response code="200">
        /// <see cref="List{T}"/>
        /// where T is <see cref="PrivacyRequestStatus"/>
        /// A collection of request statuses.
        /// </response>
        [HttpGet]
        [Route(RouteNames.PrivacyRequestApiList)]
        [PrivacyExperienceIdentityAuthorization(typeof(MsaSiteIdentity), typeof(MsaSelfIdentity), typeof(AadIdentity), typeof(AadIdentityWithMsaUserProxyTicket))]
        public async Task<IHttpActionResult> ListAsync(string requestTypes = null)
        {
            RequestType[] requestTypesList = requestTypes
                ?.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                .Distinct()
                .Select(s => !Enum.TryParse(s, out RequestType requestType) ? RequestType.None : requestType)
                .Where(r => r != RequestType.None)
                .ToArray();

            ServiceResponse<IList<PrivacyRequestStatus>> response =
                await this.pcfProxyService.ListRequestsByCallerSiteAsync(this.CurrentRequestContext, requestTypesList).ConfigureAwait(false);

            return this.CreateHttpActionResult(response);
        }

        /// <summary>
        ///     This allows caller to retrieve non-sensitive information for a given command.
        /// </summary>
        /// <group>Privacy Request Api</group>
        /// <verb>get</verb>
        /// <url>https://pxs.api.account.microsoft.com/v1/privacyrequest/listrequestbyid</url>        
        /// <header name="client-request-id" cref="string" required="false">The client activity ID.</header>
        /// <header name="MS-CV" cref="string" required="false">The correlation vector for the request.</header>
        /// <header name="Correlation-Context" cref="string" required="false">The correlation context for the request.</header>
        /// <header name="X-Flights" cref="string" required="false">The flights to apply to the request applied.</header>
        /// <header name="server-machine-id" cref="string" required="false">The server machine ID.</header>
        /// <header name="X-S2S-Proxy-Ticket" cref="string" required="false">The MSA User Proxy Ticket.</header>
        /// <header name="X-S2S-Access-Token" cref="string" required="false">The MSA S2S Access Token.</header>
        /// <param name="commandId" cref="Guid" required="true">The command id to pull information for.</param>
        /// <response code="200"><see cref="CommandStatusResponse"/></response>
        [HttpGet]
        [Route(RouteNames.PrivacyRequestApiListRequestById)]
        [PrivacyExperienceIdentityAuthorization(typeof(AadIdentity), typeof(AadIdentityWithMsaUserProxyTicket))]
        public async Task<IHttpActionResult> ListRequestByIdAsync(Guid commandId)
        {
            ServiceResponse<CommandStatusResponse> response = await this.pcfProxyService.ListRequestByIdAsync(this.CurrentRequestContext, commandId).ConfigureAwait(false);

            return this.CreateHttpActionResult(response);
        }

        /// <summary>
        ///     This allows a partner to list the requests by the Puid in the caller's request.
        /// </summary>
        /// <group>Privacy Request Api</group>
        /// <verb>get</verb>
        /// <url>https://pxs.api.account.microsoft.com/v1/privacyrequest/listmsa</url>        
        /// <header name="X-Family-Json-Web-Token" cref="string" required="false">The family JWT with information required for on behalf of.</header>
        /// <header name="client-request-id" cref="string" required="false">The client activity ID.</header>
        /// <header name="MS-CV" cref="string" required="false">The correlation vector for the request.</header>
        /// <header name="Correlation-Context" cref="string" required="false">The correlation context for the request.</header>
        /// <header name="X-Flights" cref="string" required="false">The flights to apply to the request applied.</header>
        /// <header name="server-machine-id" cref="string" required="false">The server machine ID.</header>
        /// <header name="X-S2S-Proxy-Ticket" cref="string" required="false">The MSA User Proxy Ticket.</header>
        /// <header name="X-S2S-Access-Token" cref="string" required="false">The MSA S2S Access Token.</header>
        /// <response code="200">
        /// <see cref="List{T}"/>
        /// where T is <see cref="PrivacyRequestStatus"/>
        /// A collection of request statuses.
        /// </response>
        [HttpGet]
        [Route(RouteNames.PrivacyRequestApiListByCallerMsa)]
        [PrivacyExperienceIdentityAuthorization(typeof(MsaSelfIdentity), typeof(AadIdentityWithMsaUserProxyTicket))]
        public async Task<IHttpActionResult> ListRequestsByCallerMsaAsync()
        {
            ServiceResponse<IList<PrivacyRequestStatus>> response = await this.pcfProxyService.ListRequestsByCallerMsaAsync(this.CurrentRequestContext).ConfigureAwait(false);

            return this.CreateHttpActionResult(response);
        }

        /// <summary>
        ///     This allows the test page to retrieve all information for a given agent.
        /// </summary>
        /// <group>Privacy Request Api</group>
        /// <verb>get</verb>
        /// <url>https://pxs.api.account.microsoft.com/v1/privacyrequest/agentqueuestats</url>        
        /// <header name="X-Family-Json-Web-Token" cref="string" required="false">The family JWT with information required for on behalf of.</header>
        /// <header name="client-request-id" cref="string" required="false">The client activity ID.</header>
        /// <header name="MS-CV" cref="string" required="false">The correlation vector for the request.</header>
        /// <header name="Correlation-Context" cref="string" required="false">The correlation context for the request.</header>
        /// <header name="X-Flights" cref="string" required="false">The flights to apply to the request applied.</header>
        /// <header name="server-machine-id" cref="string" required="false">The server machine ID.</header>
        /// <header name="X-S2S-Proxy-Ticket" cref="string" required="false">The MSA User Proxy Ticket.</header>
        /// <header name="X-S2S-Access-Token" cref="string" required="false">The MSA S2S Access Token.</header>
        /// <param in="query" name="agentId" cref="Guid" required="true">The target agent id</param>
        /// <response code="200">
        /// <see cref="List{T}"/>
        /// where T is <see cref="AssetGroupQueueStatistics"/>
        /// A collection of queue statistics.
        /// </response>
        [HttpGet]
        [Route(RouteNames.PrivacyRequestApiTestAgentQueueStats)]
        [PrivacyExperienceIdentityAuthorization(typeof(MsaSelfIdentity))]
        public async Task<IHttpActionResult> TestAgentQueueStatsAsync(Guid agentId)
        {
            ServiceResponse<IList<AssetGroupQueueStatistics>>
                response = await this.pcfProxyService.TestAgentQueueStatsAsync(this.CurrentRequestContext, agentId).ConfigureAwait(false);

            return this.CreateHttpActionResult(response);
        }

        /// <summary>
        ///     This allows the test page to force a given command to complete even if not all agents have completed.
        /// </summary>
        /// <group>Privacy Request Api</group>
        /// <verb>post</verb>
        /// <url>https://pxs.api.account.microsoft.com/v1/privacyrequest/forcecomplete</url>        
        /// <header name="X-Family-Json-Web-Token" cref="string" required="false">The family JWT with information required for on behalf of.</header>
        /// <header name="client-request-id" cref="string" required="false">The client activity ID.</header>
        /// <header name="MS-CV" cref="string" required="false">The correlation vector for the request.</header>
        /// <header name="Correlation-Context" cref="string" required="false">The correlation context for the request.</header>
        /// <header name="X-Flights" cref="string" required="false">The flights to apply to the request applied.</header>
        /// <header name="server-machine-id" cref="string" required="false">The server machine ID.</header>
        /// <header name="X-S2S-Proxy-Ticket" cref="string" required="false">The MSA User Proxy Ticket.</header>
        /// <header name="X-S2S-Access-Token" cref="string" required="false">The MSA S2S Access Token.</header>
        /// <param in="query" name="commandId" cref="Guid" required="true">The command id to force complete.</param>
        /// <response code="200"></response>
        [HttpPost]
        [Route(RouteNames.PrivacyRequestApiTestForceComplete)]
        [PrivacyExperienceIdentityAuthorization(typeof(MsaSelfIdentity), typeof(AadIdentity), typeof(AadIdentityWithMsaUserProxyTicket))]
        public async Task<IHttpActionResult> TestForceCompleteAsync(Guid commandId)
        {
            ServiceResponse response = await this.pcfProxyService.TestForceCommandCompletionAsync(this.CurrentRequestContext, commandId).ConfigureAwait(false);

            return this.CreateHttpActionResult(response);
        }

        /// <summary>
        ///     This allows a partner to send MSA close signal.
        /// </summary>
        /// <group>Privacy Request Api</group>
        /// <verb>post</verb>
        /// <url>https://pxs.api.account.microsoft.com/v1/privacyrequest/testmsaclose</url>        
        /// <header name="X-Family-Json-Web-Token" cref="string" required="false">The family JWT with information required for on behalf of.</header>
        /// <header name="client-request-id" cref="string" required="false">The client activity ID.</header>
        /// <header name="MS-CV" cref="string" required="false">The correlation vector for the request.</header>
        /// <header name="Correlation-Context" cref="string" required="false">The correlation context for the request.</header>
        /// <header name="X-Flights" cref="string" required="false">The flights to apply to the request applied.</header>
        /// <header name="server-machine-id" cref="string" required="false">The server machine ID.</header>
        /// <header name="X-S2S-Proxy-Ticket" cref="string" required="false">The MSA User Proxy Ticket.</header>
        /// <header name="X-S2S-Access-Token" cref="string" required="false">The MSA S2S Access Token.</header>
        /// <response code="200"><see cref="OperationResponse"/></response>
        [HttpPost]
        [Route(RouteNames.PrivacyRequestApiTestMsaClose)]
        [PrivacyExperienceIdentityAuthorization(typeof(MsaSelfIdentity))]
        public async Task<IHttpActionResult> TestMsaCloseAsync()
        {
            ServiceResponse<Guid> testMsaServiceResponse = await this.testMsaService.PostTestMsaCloseAsync(this.CurrentRequestContext).ConfigureAwait(false);

            ServiceResponse<OperationResponse> response = testMsaServiceResponse.IsSuccess
                ? new ServiceResponse<OperationResponse> { Result = new OperationResponse { Ids = new List<Guid> { testMsaServiceResponse.Result } } }
                : new ServiceResponse<OperationResponse> { Error = testMsaServiceResponse.Error };

            return this.CreateHttpActionResult(response);
        }

        /// <summary>
        ///     This allows the test page to retrieve all information for a given command.
        /// </summary>
        /// <group>Privacy Request Api</group>
        /// <verb>get</verb>
        /// <url>https://pxs.api.account.microsoft.com/v1/privacyrequest/testrequestbyid</url>        
        /// <header name="X-Family-Json-Web-Token" cref="string" required="false">The family JWT with information required for on behalf of.</header>
        /// <header name="client-request-id" cref="string" required="false">The client activity ID.</header>
        /// <header name="MS-CV" cref="string" required="false">The correlation vector for the request.</header>
        /// <header name="Correlation-Context" cref="string" required="false">The correlation context for the request.</header>
        /// <header name="X-Flights" cref="string" required="false">The flights to apply to the request applied.</header>
        /// <header name="server-machine-id" cref="string" required="false">The server machine ID.</header>
        /// <header name="X-S2S-Proxy-Ticket" cref="string" required="false">The MSA User Proxy Ticket.</header>
        /// <header name="X-S2S-Access-Token" cref="string" required="false">The MSA S2S Access Token.</header>
        /// <param in="query" name="commandId" cref="Guid" required="true">The command id.</param>
        /// <response code="200"><see cref="CommandStatusResponse"/></response>
        [HttpGet]
        [Route(RouteNames.PrivacyRequestApiTestRequestById)]
        [PrivacyExperienceIdentityAuthorization(typeof(MsaSelfIdentity))]
        public async Task<IHttpActionResult> TestRequestByIdAsync(Guid commandId)
        {
            ServiceResponse<CommandStatusResponse> response = await this.pcfProxyService.TestRequestByIdAsync(this.CurrentRequestContext, commandId).ConfigureAwait(false);

            return this.CreateHttpActionResult(response);
        }

        /// <summary>
        ///     This allows the test page to retrieve all information for all commands this partner has produced with the caller's subject
        /// </summary>
        /// <group>Privacy Request Api</group>
        /// <verb>get</verb>
        /// <url>https://pxs.api.account.microsoft.com/v1/privacyrequest/testrequestsbyuser</url>        
        /// <header name="X-Family-Json-Web-Token" cref="string" required="false">The family JWT with information required for on behalf of.</header>
        /// <header name="client-request-id" cref="string" required="false">The client activity ID.</header>
        /// <header name="MS-CV" cref="string" required="false">The correlation vector for the request.</header>
        /// <header name="Correlation-Context" cref="string" required="false">The correlation context for the request.</header>
        /// <header name="X-Flights" cref="string" required="false">The flights to apply to the request applied.</header>
        /// <header name="server-machine-id" cref="string" required="false">The server machine ID.</header>
        /// <header name="X-S2S-Proxy-Ticket" cref="string" required="false">The MSA User Proxy Ticket.</header>
        /// <header name="X-S2S-Access-Token" cref="string" required="false">The MSA S2S Access Token.</header>
        /// <response code="200">
        /// <see cref="List{T}"/>
        /// where T is <see cref="CommandStatusResponse"/>
        /// A collection of command statuses.
        /// </response>
        [HttpGet]
        [Route(RouteNames.PrivacyRequestApiTestRequestsByUser)]
        [PrivacyExperienceIdentityAuthorization(typeof(MsaSelfIdentity))]
        public async Task<IHttpActionResult> TestRequestsByUserAsync()
        {
            ServiceResponse<IList<CommandStatusResponse>> response = await this.pcfProxyService.TestRequestByUserAsync(this.CurrentRequestContext).ConfigureAwait(false);

            return this.CreateHttpActionResult(response);
        }

        private void SetSubjectTypev1(IPrivacySubject subject)
        {
            Sll.Context.Incoming.baseData.operationName = $"{Sll.Context.Incoming.baseData.operationName}_{subject.GetType().Name}";
        }

        private void SetSubjectTypev2(PrivacyServices.PrivacyOperation.Contracts.PrivacySubject.IPrivacySubject subject)
        {
            Sll.Context.Incoming.baseData.operationName = $"{Sll.Context.Incoming.baseData.operationName}_{subject.GetType().Name}";
        }

        private static string GetCorrelationContextRootOperationName()
        {
            string correlationContextRootOperationName = null;

            // Refer to link for where this special key is defined
            // https://osgwiki.com/wiki/CorrelationContext#Part_B
            Sll.Context.CorrelationContext?.TryGetValue("ms.b.qos.rootOperationName", out correlationContextRootOperationName);
            return correlationContextRootOperationName;
        }
    }
}
