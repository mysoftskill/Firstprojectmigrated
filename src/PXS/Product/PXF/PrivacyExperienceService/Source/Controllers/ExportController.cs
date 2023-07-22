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

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Privacy.Core.Export;
    using Microsoft.Membership.MemberServices.Privacy.Core.PCF;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Security;
    using Microsoft.Azure.Storage.Blob;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     Export Controller
    /// </summary>
    [Authorize]
    public class ExportController : MsaOnlyPrivacyController
    {
        private readonly IExportService exportService;

        private readonly ILogger logger;

        private readonly IPcfProxyService pcfProxyService;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ExportController" /> class.
        /// </summary>
        /// <param name="logger">The logger interface.</param>
        /// <param name="exportService">The export service.</param>
        /// <param name="pcfProxyService">The PCF service.</param>
        public ExportController(ILogger logger, IExportService exportService, IPcfProxyService pcfProxyService)
        {
            this.logger = logger;
            this.exportService = exportService;
            this.pcfProxyService = pcfProxyService;
        }

        /// <summary>
        ///     list export history
        /// </summary>
        /// <returns>HttpResponseMessage</returns>
        /// <group>Export</group>
        /// <verb>get</verb>
        /// <url>https://pxs.api.account.microsoft.com/v1/exporthistory</url>        
        /// <header name="X-Family-Json-Web-Token" cref="string" required="false">The family JWT with information required for on behalf of.</header>
        /// <header name="client-request-id" cref="string" required="false">The client activity ID.</header>
        /// <header name="MS-CV" cref="string" required="false">The correlation vector for the request.</header>
        /// <header name="Correlation-Context" cref="string" required="false">The correlation context for the request.</header>
        /// <header name="X-Flights" cref="string" required="false">The flights to apply to the request applied.</header>
        /// <header name="server-machine-id" cref="string" required="false">The server machine ID.</header>
        /// <header name="X-S2S-Proxy-Ticket" cref="string" required="false">The MSA User Proxy Ticket.</header>
        /// <header name="X-S2S-Access-Token" cref="string" required="false">The MSA S2S Access Token.</header>
        /// <response code="200"><see cref="ListExportHistoryResponse"/></response>
        [HttpGet, PrivacyExperienceAgeAuthZAuthorization(typeof(AgeAuthZLegalAgeGroup), PrivacyAction.View)]
        [Route(RouteNames.ListExportHistory)]
        public async Task<IHttpActionResult> ListExportHistoryAsync()
        {
            Task<ServiceResponse<ListExportHistoryResponse>> quickExportsTask = this.exportService.ListExportHistoryAsync(this.CurrentRequestContext);

            //// TODO: When the call for getting by MSA actually works, this should be the code instead:
            ////Task<ServiceResponse<IList<PrivacyRequestStatus>>> pcfExportsTask = this.pcfProxyService.ListRequestsByCallerMsaAsync(this.CurrentRequestContext);
            Task<ServiceResponse<IList<PrivacyRequestStatus>>> pcfExportsTask =
                Task.FromResult(new ServiceResponse<IList<PrivacyRequestStatus>> { Result = null });

            ServiceResponse<ListExportHistoryResponse> quickExports = await quickExportsTask.ConfigureAwait(false);
            ServiceResponse<IList<PrivacyRequestStatus>> pcfExports = await pcfExportsTask.ConfigureAwait(false);

            if (!quickExports.IsSuccess)
                return this.CreateHttpActionResult(quickExports);
            if (!pcfExports.IsSuccess)
                return this.CreateHttpActionResult(pcfExports);

            // Find the relevant pcf exports
            IEnumerable<ExportStatus> pcfResults = pcfExports.Result?
                .Select(
                    pcfStatus =>
                    {
                        var container = new CloudBlobContainer(pcfStatus.DestinationUri);
                        CloudBlob blob = container.GetBlobReference($"Export-{pcfStatus.Id:n}.zip");

                        Task task = null;
                        try
                        {
                            task = blob.FetchAttributesAsync();
                        }
                        catch (Exception ex)
                        {
                            this.logger.Warning(nameof(ExportController), ex, $"Could not fetch blob properties for PCF export {pcfStatus.Id}");
                        }

                        return new
                        {
                            PcfStatus = pcfStatus,
                            Blob = blob,
                            FetchAttributesTask = task
                        };
                    })
                .ToList() // ToList here to materialize the previous Select since it kicks off all the tasks
                .Select(
                    status =>
                    {
                        long blobLength = 0;
                        try
                        {
                            if (status.FetchAttributesTask != null)
                            {
                                // This is blocking, but all the tasks have already kicked off. Not nicely
                                // awaited but to await here I'd have to structure this differently.
                                status.FetchAttributesTask.GetAwaiter().GetResult();

                                blobLength = status.Blob.Properties.Length;
                            }
                        }
                        catch (Exception ex)
                        {
                            // It's ok if we couldn't fetch the blob properties yet. It might not yet be finished.
                            this.logger.Warning(nameof(ExportController), ex, $"Could not fetch blob properties for PCF export {status.PcfStatus.Id}");
                        }

                        // This needs to be the same as PCF config. How to keep in sync properly? :(
                        TimeSpan expiresAfter = TimeSpan.FromDays(60);

                        return new ExportStatus
                        {
                            DataTypes = status.PcfStatus.DataTypes,
                            ExpiresAt = status.PcfStatus.CompletedTime.Add(expiresAfter),
                            LastError = null, // never any error status currently
                            IsComplete = status.PcfStatus.State == PrivacyRequestState.Completed,
                            ExportId = status.PcfStatus.Id.ToString(),
                            RequestedAt = status.PcfStatus.SubmittedTime,
                            ZipFileSize = blobLength,

                            // Cannot just use the blob.Uri or the SAS token is lost. Instead, we have to use the original
                            // Uri of the container, and fiddle with it's path to point to the blob instead, which will maintain
                            // the SAS token.
                            ZipFileUri = new UriBuilder(status.PcfStatus.DestinationUri) { Path = status.Blob.Uri.LocalPath }.Uri
                        };
                    });

            quickExports.Result.Exports = quickExports.Result.Exports.Concat(pcfResults ?? Enumerable.Empty<ExportStatus>()).ToList();
            return this.CreateHttpActionResult(quickExports);
        }

        /// <summary>
        ///     Post Cancel of export
        /// </summary>
        /// <param in="query" name="exportId" cref="string">the export request id of the status record</param>
        /// <returns>HttpResponseMessage</returns>
        /// <group>Export</group>
        /// <verb>post</verb>
        /// <url>https://pxs.api.account.microsoft.com/v1/exportcancel</url>        
        /// <header name="X-Family-Json-Web-Token" cref="string" required="false">The family JWT with information required for on behalf of.</header>
        /// <header name="client-request-id" cref="string" required="false">The client activity ID.</header>
        /// <header name="MS-CV" cref="string" required="false">The correlation vector for the request.</header>
        /// <header name="Correlation-Context" cref="string" required="false">The correlation context for the request.</header>
        /// <header name="X-Flights" cref="string" required="false">The flights to apply to the request applied.</header>
        /// <header name="server-machine-id" cref="string" required="false">The server machine ID.</header>
        /// <header name="X-S2S-Proxy-Ticket" cref="string" required="true">The MSA User Proxy Ticket.</header>
        /// <header name="X-S2S-Access-Token" cref="string" required="true">The MSA S2S Access Token.</header>
        /// <response code="200"></response>
        [HttpPost, PrivacyExperienceAgeAuthZAuthorization(typeof(AgeAuthZLegalAgeGroup), PrivacyAction.View)]
        [Route(RouteNames.PostExportCancel)]
        public async Task<IHttpActionResult> PostExportCancelAsync(string exportId)
        {
            ServiceResponse response = await this.exportService.PostExportCancelAsync(this.CurrentRequestContext, exportId).ConfigureAwait(false);
            return this.CreateHttpActionResult(response);
        }

        /// <summary>
        ///     make and export request
        /// </summary>
        /// <group>Export</group>
        /// <verb>post</verb>
        /// <url>https://pxs.api.account.microsoft.com/v1/export</url>        
        /// <header name="X-Family-Json-Web-Token" cref="string" required="false">The family JWT with information required for on behalf of.</header>
        /// <header name="client-request-id" cref="string" required="false">The client activity ID.</header>
        /// <header name="MS-CV" cref="string" required="false">The correlation vector for the request.</header>
        /// <header name="Correlation-Context" cref="string" required="false">The correlation context for the request.</header>
        /// <header name="X-Flights" cref="string" required="false">The flights to apply to the request applied.</header>
        /// <header name="server-machine-id" cref="string" required="false">The server machine ID.</header>
        /// <header name="X-S2S-Proxy-Ticket" cref="string" required="true">The MSA User Proxy Ticket.</header>
        /// <header name="X-S2S-Access-Token" cref="string" required="true">The MSA S2S Access Token.</header>
        /// <param in="query" name="dataTypes" cref="string">A comma separated list of data types.</param>
        /// <param in="query" name="startTime"><see cref="DateTimeOffset"/>The start time.</param>
        /// <param in="query" name="endTime"><see cref="DateTimeOffset"/>The end time.</param>
        /// <response code="200"><see cref="PostExportResponse"/></response>
        [HttpPost, PrivacyExperienceAgeAuthZAuthorization(typeof(AgeAuthZLegalAgeGroup), PrivacyAction.View)]
        [Route(RouteNames.PostExportRequest)]
        public async Task<IHttpActionResult> PostExportRequestAsync(string dataTypes, DateTimeOffset startTime, DateTimeOffset endTime)
        {
            List<string> dataTypesList = dataTypes?.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries).Distinct().ToList();
            ServiceResponse<PostExportResponse> response =
                await this.exportService.PostExportRequestAsync(this.CurrentRequestContext, dataTypesList, startTime, endTime).ConfigureAwait(false);
            return this.CreateHttpActionResult(response);
        }

        /// <summary>
        ///     Delete Export Archives
        /// </summary>
        /// <param in="query" name="exportId" cref="string">the export id to delete its archive</param>
        /// <param name="exportType"></param>
        /// <returns>HttpResponseMessage</returns>
        /// <group>Export</group>
        /// <verb>delete</verb>
        /// <url>https://pxs.api.account.microsoft.com/v1/deleteexport</url>        
        /// <header name="X-Family-Json-Web-Token" cref="string" required="false">The family JWT with information required for on behalf of.</header>
        /// <header name="client-request-id" cref="string" required="false">The client activity ID.</header>
        /// <header name="MS-CV" cref="string" required="false">The correlation vector for the request.</header>
        /// <header name="Correlation-Context" cref="string" required="false">The correlation context for the request.</header>
        /// <header name="X-Flights" cref="string" required="false">The flights to apply to the request applied.</header>
        /// <header name="server-machine-id" cref="string" required="false">The server machine ID.</header>
        /// <header name="X-S2S-Proxy-Ticket" cref="string" required="true">The MSA User Proxy Ticket.</header>
        /// <header name="X-S2S-Access-Token" cref="string" required="true">The MSA S2S Access Token.</header>
        /// <response code="200"></response>
        [HttpDelete, PrivacyExperienceAgeAuthZAuthorization(typeof(AgeAuthZLegalAgeGroup), PrivacyAction.Delete)]
        [Route(RouteNames.DeleteExportArchives)]
        public async Task<IHttpActionResult> DeleteExportArchivesAsync(string exportId, string exportType)
        {
            ServiceResponse response = await this.exportService.DeleteExportArchivesAsync(this.CurrentRequestContext, exportId, exportType).ConfigureAwait(false);
            return this.CreateHttpActionResult(response);
        }
    }
}
