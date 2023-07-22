// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Export
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;

    public interface IExportService
    {
        /// <summary>
        ///     list history of export requests
        /// </summary>
        /// <param name="requestContext"></param>
        /// <returns></returns>
        Task<ServiceResponse<ListExportHistoryResponse>> ListExportHistoryAsync(IRequestContext requestContext);

        /// <summary>
        ///     Post export cancel
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="exportId">the export request id of the status record</param>
        /// <returns>
        ///     <see cref="ExportStatus" />
        /// </returns>
        Task<ServiceResponse> PostExportCancelAsync(IRequestContext requestContext, string exportId);

        /// <summary>
        ///     Post export request
        /// </summary>
        Task<ServiceResponse<PostExportResponse>> PostExportRequestAsync(IRequestContext requestContext, IList<string> dataTypes, DateTimeOffset startTime, DateTimeOffset endTime);

        /// <summary>
        ///     Delete export archives
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="exportId">the export request id of the status record</param>
        /// <returns></returns>
        Task<ServiceResponse> DeleteExportArchivesAsync(RequestContext requestContext, string exportId, string exportType);
    }
}
