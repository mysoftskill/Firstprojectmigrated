// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary
{
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.PrivacySubject;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Extensions;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Helpers;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Models.PrivacySubject;

    /// <summary>
    ///     Privacy-Experience Client
    /// </summary>
    public partial class PrivacyExperienceClient : IPrivacySubjectClient
    {
        /// <inheritdoc cref="IPrivacySubjectClient.DeleteByTypesAsync"/>.
        public async Task<OperationResponse> DeleteByTypesAsync(DeleteByTypesArgs args)
        {
            args.ThrowOnNull("args");
            args.Subject.Validate(SubjectUseContext.Delete);

            var requestData = this.PopulateCommonPrivacySubjectOperationRequestOptions(args, new DeleteOperationRequest());

            HttpRequestMessage request = await PrivacyExperienceRequestHelper.CreateS2SRequestAsync(
                HttpMethod.Post,
                PrivacyExperienceUrlHelper.CreatePrivacySubjectDeleteRequest(args.CreateQueryStringCollection()),
                OperationNames.PostPrivacyRequestApiDelete,
                args.RequestId,
                args.CorrelationVector,
                this.authClient,
                args.UserProxyTicket,
                args.FamilyTicket).ConfigureAwait(false);
            request = this.SetRequestBody(this.AddAcceptJsonHeader(request), requestData);

            HttpResponseMessage response = await this.httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);
            return await HttpHelper.HandleHttpResponseAsync<OperationResponse>(response).ConfigureAwait(false);
        }

        /// <inheritdoc cref="IPrivacySubjectClient.ExportByTypesAsync"/>.
        public async Task<OperationResponse> ExportByTypesAsync(ExportByTypesArgs args)
        {
            args.ThrowOnNull("args");
            args.Subject.Validate(SubjectUseContext.Export);

            var requestData = this.PopulateCommonPrivacySubjectOperationRequestOptions(
                args,
                new ExportOperationRequest
                {
                    StorageLocationUri = args.StorageLocationUri
                });

            HttpRequestMessage request = await PrivacyExperienceRequestHelper.CreateS2SRequestAsync(
                HttpMethod.Post,
                PrivacyExperienceUrlHelper.CreatePrivacySubjectExportRequest(args.CreateQueryStringCollection()),
                OperationNames.PostPrivacyRequestApiExport,
                args.RequestId,
                args.CorrelationVector,
                this.authClient,
                args.UserProxyTicket,
                args.FamilyTicket).ConfigureAwait(false);
            request = this.SetRequestBody(this.AddAcceptJsonHeader(request), requestData);

            HttpResponseMessage response = await this.httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);
            return await HttpHelper.HandleHttpResponseAsync<OperationResponse>(response).ConfigureAwait(false);
        }

        private T PopulateCommonPrivacySubjectOperationRequestOptions<T>(PrivacySubjectClientBaseArgs args, T request)
            where T : OperationRequestBase
        {
            request.Subject = args.Subject;
            request.Context = args.Context;

            return request;
        }
    }
}
