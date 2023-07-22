// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.Controllers
{
    using System;
    using System.Globalization;
    using System.Net;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Microsoft.AspNet.OData;
    using Microsoft.AspNet.OData.Routing;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers;
    using Microsoft.Membership.MemberServices.Privacy.Core.PCF;
    using Microsoft.Membership.MemberServices.Privacy.Core.PrivacyCommand;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Controllers.Helpers;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Extensions;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    /// <summary>
    ///     Directory Controller.
    /// </summary>
    [Authorize]
    [ODataRoutePrefix("directory")]
    public class DirectoryController : ODataPrivacyController
    {
        private readonly string cloudInstance;

        private readonly IPcfProxyService pcfProxyService;

        private readonly IRequestClassifier requestClassifier;

        private readonly IAppConfiguration appConfiguration;

        private readonly ILogger logger;

        /// <summary>
        ///     Constructor for <see cref="DirectoryController"/>.
        /// </summary>
        /// <param name="logger">The logger interface.</param>
        /// <param name="pcfProxyService">The pcfProxyService.</param>
        /// <param name="configurationManager"></param>
        /// <param name="requestClassifier"></param>
        /// <param name="appConfiguration"></param>
        public DirectoryController(
            ILogger logger,
            PcfProxyService pcfProxyService,
            IPrivacyConfigurationManager configurationManager,
            IRequestClassifier requestClassifier,
            IAppConfiguration appConfiguration)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.pcfProxyService = pcfProxyService ?? throw new ArgumentNullException(nameof(pcfProxyService));
            this.cloudInstance = (configurationManager?.PrivacyExperienceServiceConfiguration?.CloudInstance).ToPcfCloudInstance();
            this.requestClassifier = requestClassifier ?? throw new ArgumentNullException(nameof(requestClassifier));
            this.appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));
        }

        /// <summary>
        ///     Export personal data for a native identity when called by a tenant admin.
        /// </summary>
        /// <param in="path" name="id" cref="string">The native identity object Id.</param>
        /// <param name="parameters">
        ///     "storageLocation": the location to place all the data in.
        ///     "scope": the scope of the export, this will always be "default" for now.
        /// </param>
        /// <returns>An HttpActionResult.</returns>
        /// <group>Directory</group>
        /// <verb>post</verb>
        /// <url>https://pxs.api.account.microsoft.com/directory/inboundSharedUserProfiles({id})/exportPersonalData</url>        
        /// <response code="202"><see cref="Guid"/></response>
        [HttpPost]
        [ODataRoute("inboundSharedUserProfiles({id})/exportPersonalData")]
        public async Task<IHttpActionResult> InboundSharedUserProfilesExportPersonalData([FromODataUri] string id, ODataActionParameters parameters)
        {
            var serviceResponse = await ExportPersonalDataHelper.ExportPersonalData(
                nameof(DirectoryController),
                id,
                parameters,
                this.logger,
                this.pcfProxyService,
                this.cloudInstance,
                this.requestClassifier.IsTestRequest(Portals.MsGraph, this.User.Identity),
                addLocationHeaders: true,
                this.CurrentRequestContext,
                this.Request);

            return this.ResponseMessage(serviceResponse);
        }

        /// <summary>
        ///     AccountClose in a resource tenant. This call is used by a tenant admin to 
        ///     remove any data associated with a user that has traversed into their tenant.
        ///     The target tenant id is inferred from the caller's identity.
        /// </summary>
        /// <param name="objectId">The user account object Id.</param>
        /// <returns>An HttpActionResult.</returns>
        /// <group>Directory</group>
        /// <verb>post</verb>
        /// <url>https://pxs.api.account.microsoft.com/directory/inboundSharedUserProfiles('{objectId}')/removePersonalData</url>        
        /// <response code="204"><see cref="Guid"/></response>
        [HttpPost]
        [ODataRoute("inboundSharedUserProfiles({objectId})/removePersonalData")]
        public async Task<IHttpActionResult> InboundSharedUserProfilesRemovePersonalData([FromODataUri] string objectId)
        {
            bool isMultiTenantCollaborationEnabled = await appConfiguration.IsFeatureFlagEnabledAsync(FeatureNames.MultiTenantCollaboration).ConfigureAwait(false);
            
            var targetTenandId = this.CurrentRequestContext.RequireIdentity<AadIdentity>().TenantId;
            var serviceResponse = await RemovePersonalDataHelper.RemovePersonalData(
                    nameof(DirectoryController),
                    objectId,
                    targetTenandId,
                    this.logger,
                    this.pcfProxyService,
                    this.cloudInstance,
                    this.requestClassifier.IsTestRequest(Portals.MsGraph, this.User.Identity),
                    isMultiTenantCollaborationEnabled,
                    this.CurrentRequestContext,
                    this.Request);

            return this.ResponseMessage(serviceResponse);
        }

        /// <summary>
        ///     AccountClose called from a home tenant to delete data in a resource tenant. 
        ///     This call is invoked by the user to remove any data associated with them in a resource tenant.
        ///     The target tenant id is included in the url.
        /// </summary>
        /// <param name="objectId">The user account object Id.</param>
        /// <param name="tenantId">The target tenant Id.</param>
        /// <returns>An HttpActionResult.</returns>
        /// <group>Directory</group>
        /// <verb>post</verb>
        /// <url>https://pxs.api.account.microsoft.com/directory/outboundSharedUserProfiles('{objectId}')/tenants('{tenantId}')/removePersonalData</url>        
        /// <response code="204"><see cref="Guid"/></response>
        [HttpPost]
        [ODataRoute("outboundSharedUserProfiles({objectId})/tenants({tenantId})/removePersonalData")]
        public async Task<IHttpActionResult> OutboundSharedUserProfilesRemovePersonalData([FromODataUri] string objectId, [FromODataUri] string tenantId)
        {
            bool isMultiTenantCollaborationEnabled = await appConfiguration.IsFeatureFlagEnabledAsync(FeatureNames.MultiTenantCollaboration).ConfigureAwait(false);

            if (!Guid.TryParse(tenantId, out Guid targetTenantId))
            {
                return this.ResponseMessage(
                        this.Request.CreateODataErrorResponse(
                        new Error(ErrorCode.InvalidInput, string.Format(CultureInfo.InvariantCulture, GraphApiErrorMessage.InvalidTenantIdFormat, tenantId)),
                        hideDetailErrorMessages: false));
            }

            var serviceResponse = await RemovePersonalDataHelper.RemovePersonalData(
                    nameof(DirectoryController),
                    objectId,
                    targetTenantId,
                    this.logger,
                    this.pcfProxyService,
                    this.cloudInstance,
                    this.requestClassifier.IsTestRequest(Portals.MsGraph, this.User.Identity),
                    isMultiTenantCollaborationEnabled,
                    this.CurrentRequestContext,
                    this.Request);

            return this.ResponseMessage(serviceResponse);
        }
    }
}
