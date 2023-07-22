// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.Controllers
{
    using System;
    using System.Threading.Tasks;
    using System.Web.Http;

    using Microsoft.AspNet.OData;
    using Microsoft.AspNet.OData.Routing;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers;
    using Microsoft.Membership.MemberServices.Privacy.Core.PCF;
    using Microsoft.Membership.MemberServices.Privacy.Core.PrivacyCommand;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Controllers.Helpers;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    /// <summary>
    ///     Public class for Users Controller.
    /// </summary>
    [Authorize]
    public class UsersController : ODataPrivacyController
    {
        private readonly string cloudInstance;

        private readonly IPcfProxyService pcfProxyService;

        private readonly IRequestClassifier requestClassifier;

        private readonly IAppConfiguration appConfiguration;

        private readonly ILogger logger;

        /// <summary>
        ///     Constructor for UsersController class.
        /// </summary>
        /// <param name="logger">The logger interface.</param>
        /// <param name="pcfProxyService">The Pcf Proxy Service object.</param>
        /// <param name="configurationManager"></param>
        /// <param name="requestClassifier"></param>
        /// <param name="appConfiguration"></param>
        public UsersController(
            ILogger logger,
            IPcfProxyService pcfProxyService,
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
        ///     Export personal data for an account providing its id.
        /// </summary>
        /// <param name="key">The account Id.</param>
        /// <param name="parameters">
        ///     "storageLocation": the location to place all the data in.
        ///     "scope": the scope of the export, this will always be "default" for now.
        /// </param>
        /// <returns>An HttpActionResult.</returns>
        /// <group>Users</group>
        /// <verb>post</verb>
        /// <url>https://pxs.api.account.microsoft.com/users('{key}')/exportPersonalData</url>        
        /// <response code="202"><see cref="Guid"/></response>
        [HttpPost]
        [ODataRoute("users({key})/exportPersonalData")]
        public async Task<IHttpActionResult> ExportPersonalData([FromODataUri] string key, ODataActionParameters parameters)
        {
            var responseMessage = await ExportPersonalDataHelper.ExportPersonalData(
                nameof(UsersController), 
                key,
                parameters,
                this.logger,
                this.pcfProxyService,
                this.cloudInstance,
                this.requestClassifier.IsTestRequest(Portals.MsGraph, this.User.Identity),
                addLocationHeaders: true, // add location headers to response
                this.CurrentRequestContext,
                this.Request);

            return this.ResponseMessage(responseMessage);
        }
    }
}
