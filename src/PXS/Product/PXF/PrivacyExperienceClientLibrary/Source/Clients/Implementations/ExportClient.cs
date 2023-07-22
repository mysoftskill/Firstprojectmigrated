// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary
{
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Extensions;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Helpers;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Interfaces;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Models;

    /// <summary>
    ///     Privacy-Experience Client
    /// </summary>
    public partial class PrivacyExperienceClient : IExportClient
    {
        /// <summary>
        ///     List Export History
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public async Task<ListExportHistoryResponse> ListExportHistoryAsync(PrivacyExperienceClientBaseArgs args)
        {
            args.ThrowOnNull("args");
            args.UserProxyTicket.ThrowOnNull("userProxyTicket");

            HttpRequestMessage request = await PrivacyExperienceRequestHelper.CreateS2SRequestAsync(
                HttpMethod.Get,
                PrivacyExperienceUrlHelper.CreateListExportHistoryRequestPathV1(),
                OperationNames.ListExportHistory,
                args.RequestId,
                args.CorrelationVector,
                this.authClient,
                args.UserProxyTicket,
                args.FamilyTicket).ConfigureAwait(false);
            HttpResponseMessage response = await this.httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);
            return await HttpHelper.HandleHttpResponseAsync<ListExportHistoryResponse>(response).ConfigureAwait(false);
        }

        /// <summary>
        ///     Post Export Cancel
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public async Task<ExportStatus> PostExportCancelAsync(PostExportCancelArgs args)
        {
            args.ThrowOnNull("args");
            args.UserProxyTicket.ThrowOnNull("userProxyTicket");

            HttpRequestMessage request = await PrivacyExperienceRequestHelper.CreateS2SRequestAsync(
                HttpMethod.Post,
                PrivacyExperienceUrlHelper.CreatePostExportCancelPathV1(args.CreateQueryStringCollection()),
                OperationNames.PostExportCancel,
                args.RequestId,
                args.CorrelationVector,
                this.authClient,
                args.UserProxyTicket,
                args.FamilyTicket).ConfigureAwait(false);
            HttpResponseMessage response = await this.httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);
            return await HttpHelper.HandleHttpResponseAsync<ExportStatus>(response).ConfigureAwait(false);
        }

        /// <summary>
        ///     Delete export archives
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> DeleteExportArchivesAsync(DeleteExportArchivesArgs args)
        {
            args.ThrowOnNull("args");
            args.UserProxyTicket.ThrowOnNull("userProxyTicket");

            HttpRequestMessage request = await PrivacyExperienceRequestHelper.CreateS2SRequestAsync(
                HttpMethod.Delete,
                PrivacyExperienceUrlHelper.CreateDeleteExportArchivesPathV1(args.CreateQueryStringCollection()),
                OperationNames.DeleteExportArchives,
                args.RequestId,
                args.CorrelationVector,
                this.authClient,
                args.UserProxyTicket,
                args.FamilyTicket).ConfigureAwait(false);
            HttpResponseMessage response = await this.httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);
            return response;
        }

        /// <summary>
        ///     Post Export Request
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public async Task<PostExportResponse> PostExportRequestAsync(PostExportRequestArgs args)
        {
            args.ThrowOnNull("args");
            args.UserProxyTicket.ThrowOnNull("userProxyTicket");
            args.DataTypes.ThrowOnNull("DataTypes");

            HttpRequestMessage request = await PrivacyExperienceRequestHelper.CreateS2SRequestAsync(
                HttpMethod.Post,
                PrivacyExperienceUrlHelper.CreatePostExportRequestPathV1(args.CreateQueryStringCollection()),
                OperationNames.PostExportRequest,
                args.RequestId, 
                args.CorrelationVector,
                this.authClient,
                args.UserProxyTicket,
                args.FamilyTicket).ConfigureAwait(false);
            HttpResponseMessage response = await this.httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);
            return await HttpHelper.HandleHttpResponseAsync<PostExportResponse>(response).ConfigureAwait(false);
        }
    }
}
