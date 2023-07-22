// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Interfaces
{
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Models;

    /// <summary>
    ///     Export Client interface
    /// </summary>
    public interface IExportClient
    {
        /// <summary>
        ///     List Export History
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        Task<ListExportHistoryResponse> ListExportHistoryAsync(PrivacyExperienceClientBaseArgs args);

        /// <summary>
        ///     Post Cancel of Export Request
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        Task<ExportStatus> PostExportCancelAsync(PostExportCancelArgs args);

        /// <summary>
        ///     Post Export Request
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        Task<PostExportResponse> PostExportRequestAsync(PostExportRequestArgs args);

        /// <summary>
        ///     Delete export archives
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        Task<HttpResponseMessage> DeleteExportArchivesAsync(DeleteExportArchivesArgs args);

    }
}
