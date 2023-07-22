// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Microsoft.AspNet.OData;
    using Microsoft.AspNet.OData.Routing;
    using Microsoft.AspNet.OData.Extensions;
    using Microsoft.Membership.MemberServices.Privacy.Core.PCF;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Extensions;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.DataSubjectRight.Contracts.V1;
    using Microsoft.OData;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    /// <summary>
    ///     DataPolicyOperationsController.
    /// </summary>
    [Authorize]
    public class DataPolicyOperationsController : ODataPrivacyController
    {
        private readonly IPcfProxyService pcfProxyService;

        /// <summary>
        ///     Constructor for OperationsController class.
        /// </summary>
        /// <param name="pcfProxyService">The Pcf Proxy Service object.</param>
        public DataPolicyOperationsController(IPcfProxyService pcfProxyService)
        {
            this.pcfProxyService = pcfProxyService ?? throw new ArgumentNullException(nameof(pcfProxyService));
        }

        /// <summary>
        ///     Get a data policy operation by its ID.
        /// </summary>
        /// <param in="path" name="key" cref="string">The data policy operation specified by ID.</param>
        /// <returns>All data policy operations.</returns>
        /// <group>Data Policy Operations</group>
        /// <verb>get</verb>
        /// <url>https://pxs.api.account.microsoft.com/dataPolicyOperations({key})</url>        
        /// <response code="200"><see cref="PrivacyRequestStatus"/></response>
        [HttpGet]
        [ODataRoute("dataPolicyOperations({key})")]
        public async Task<IHttpActionResult> GetDataPolicyOperation([FromODataUri] string key)
        {
            if (!Guid.TryParse(key, out Guid keyGuid))
                return this.BadRequest(string.Format(CultureInfo.InvariantCulture, GraphApiErrorMessage.InvalidObjectIdFormat, key));

            ServiceResponse<PrivacyRequestStatus> serviceResponse =
                await this.pcfProxyService.ListMyRequestByIdAsync(this.CurrentRequestContext, keyGuid).ConfigureAwait(false);

            if (serviceResponse == null)
            {
                return this.InternalServerError(new Exception("Null response from PcfProxyService"));
            }

            if (serviceResponse.IsSuccess)
            {
                if (serviceResponse.Result == null)
                    throw new HttpResponseException(
                        this.Request.CreateErrorResponse(
                            HttpStatusCode.BadRequest,
                            new ODataError
                            {
                                ErrorCode = "OperationNotFound",
                                Message = GraphApiErrorMessage.OperationNotFound
                            }));
                return this.Ok(ConvertPrivacyRequest(serviceResponse.Result));
            }

            return this.ResponseMessage(this.Request.CreateODataErrorResponse(serviceResponse.Error, true));
        }

        /// <summary>
        ///     Get all data policy operations.
        /// </summary>
        /// <returns>All data policy operations.</returns>
        /// <group>Data Policy Operations</group>
        /// <verb>get</verb>
        /// <url>https://pxs.api.account.microsoft.com/dataPolicyOperations</url>            
        /// <response code="200">
        /// <see cref="List{T}"/>
        /// where T is <see cref="PrivacyRequestStatus"/>
        /// A collection of PrivacyRequest statuses.
        /// </response>
        [HttpGet]
        [ODataRoute("dataPolicyOperations")]
        public async Task<IHttpActionResult> GetDataPolicyOperations()
        {
            // If we don't restrict on request type, this can easily exceed the internal limit of 5000 results.
            // Since Export is the only request type we exposed to MS Graph, it is logical to set it to Export.
            ServiceResponse<IList<PrivacyRequestStatus>> serviceResponse =
                await this.pcfProxyService.ListRequestsByCallerSiteAsync(this.CurrentRequestContext, RequestType.Export).ConfigureAwait(false);

            if (serviceResponse == null)
            {
                return this.InternalServerError();
            }

            if (serviceResponse.IsSuccess)
            {
                return this.Ok(serviceResponse.Result.Select(ConvertPrivacyRequest));
            }

            return this.ResponseMessage(this.Request.CreateODataErrorResponse(serviceResponse.Error, true));
        }

        internal static DataPolicyOperation ConvertPrivacyRequest(PrivacyRequestStatus privacyRequestStatus)
        {
            var aadSubject = privacyRequestStatus.Subject as AadSubject;
            return new DataPolicyOperation
            {
                Id = privacyRequestStatus.Id.ToString(),
                Status = ConvertPrivacyRequestState(privacyRequestStatus.State),
                StorageLocation = privacyRequestStatus.DestinationUri?.ToString(), // this is a temp fix for a PCF bug where they return null URI
                SubmittedDateTime = privacyRequestStatus.SubmittedTime,
                UserId = aadSubject?.ObjectId.ToString() ?? string.Empty, // Should this be an exception rather than null coalesced?
                CompletedDateTime = privacyRequestStatus.CompletedTime,
                Progress = privacyRequestStatus.CompletionSuccessRate
            };
        }

        private static DataPolicyOperationStatus ConvertPrivacyRequestState(PrivacyRequestState state)
        {
            switch (state)
            {
                case PrivacyRequestState.Submitted:
                    return DataPolicyOperationStatus.Running;
                case PrivacyRequestState.Completed:
                    return DataPolicyOperationStatus.Complete;
                default:
                    throw new InvalidDataException($"Invalid PrivacyRequestState: {state}");
            }
        }
    }
}
