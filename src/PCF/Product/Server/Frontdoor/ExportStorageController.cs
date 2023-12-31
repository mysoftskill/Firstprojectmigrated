// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web.Http;

    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     The Export Storage Controller, used for interfacing with the export storage manager.
    /// </summary>
    [RoutePrefix("exportstorage/v1")]
    [ExcludeFromCodeCoverage]
    public class ExportStorageController : ApiController
    {
        /// <summary>
        ///     Gets the storage accounts managed by this service.
        /// </summary>        
        /// <url>https://pcf.privacy.microsoft.com/exportstorage/v1/accounts</url>
        /// <verb>get</verb>
        /// <group>Export Storage</group>
        /// <response code="200"><see cref="PcfAuthenticationContext"/></response>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        [HttpGet]
        [Route("accounts")]
        [IncomingRequestActionFilter("ExportStorage", "GetAccounts", "1.0")]
        [DisallowPort80ActionFilter]
        public async Task<IHttpActionResult> GetAccounts()
        {
            await CommandFeedGlobals.ServiceAuthorizer.CheckAuthorizedAsync(
                this.Request,
                AuthenticationScope.ExportStorageGetAccounts);

            try
            {
                return this.Ok(ExportStorageManager.Instance.AccountUris.ToArray());
            }
            catch (Exception ex)
            {
                DualLogger.Instance.Error(nameof(ExportStorageController), ex, $"{nameof(ExportStorageController)}.{nameof(this.GetAccounts)} Exception");
                return this.InternalServerError(ex);
            }
        }
    }
}
