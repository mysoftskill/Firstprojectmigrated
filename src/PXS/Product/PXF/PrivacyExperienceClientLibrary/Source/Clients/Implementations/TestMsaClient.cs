// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.PrivacySubject;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Clients.Interfaces;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Extensions;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Helpers;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Models;
    using Microsoft.PrivacyServices.PXS.Command.CommandStatus;

    /// <summary>
    ///     Privacy-Experience Test MSA Client
    /// </summary>
    public partial class PrivacyExperienceClient : ITestMsaClient
    {
        /// <inheritdoc />
        public async Task TestForceCommandCompletionAsync(TestForceCompletionArgs args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            HttpRequestMessage request = await PrivacyExperienceRequestHelper.CreateS2SRequestAsync(
                HttpMethod.Post,
                PrivacyExperienceUrlHelper.CreateTestForceCompleteRequest(args.CreateQueryStringCollection()),
                OperationNames.PostPrivacyRequestApiTestForceCommandComplete,
                args.RequestId,
                args.CorrelationVector,
                this.authClient,
                args.UserProxyTicket,
                args.FamilyTicket).ConfigureAwait(false);
            this.AddAcceptJsonHeader(request);

            HttpResponseMessage response = await this.httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);
            await HttpHelper.HandleHttpResponseAsync(response).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IList<AssetGroupQueueStatistics>> TestGetAgentStatisticsAsync(TestGetAgentStatisticsArgs args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            HttpRequestMessage request = await PrivacyExperienceRequestHelper.CreateS2SRequestAsync(
                HttpMethod.Get,
                PrivacyExperienceUrlHelper.CreateTestAgentQueueStatsRequest(args.CreateQueryStringCollection()),
                OperationNames.GetPrivacyRequestApiTestGetAgentStats,
                args.RequestId,
                args.CorrelationVector,
                this.authClient,
                args.UserProxyTicket,
                args.FamilyTicket).ConfigureAwait(false);
            this.AddAcceptJsonHeader(request);

            HttpResponseMessage response = await this.httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);
            return await HttpHelper.HandleHttpResponseAsync<IList<AssetGroupQueueStatistics>>(response).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<CommandStatusResponse> TestGetCommandStatusByIdAsync(TestGetCommandStatusByIdArgs args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            HttpRequestMessage request = await PrivacyExperienceRequestHelper.CreateS2SRequestAsync(
                HttpMethod.Get,
                PrivacyExperienceUrlHelper.CreateTestRequestByIdRequest(args.CreateQueryStringCollection()),
                OperationNames.GetPrivacyRequestApiTestCommandById,
                args.RequestId,
                args.CorrelationVector,
                this.authClient,
                args.UserProxyTicket,
                args.FamilyTicket).ConfigureAwait(false);
            this.AddAcceptJsonHeader(request);

            HttpResponseMessage response = await this.httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);
            return await HttpHelper.HandleHttpResponseAsync<CommandStatusResponse>(response).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IList<CommandStatusResponse>> TestGetCommandStatusesAsync(PrivacyExperienceClientBaseArgs args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            HttpRequestMessage request = await PrivacyExperienceRequestHelper.CreateS2SRequestAsync(
                HttpMethod.Get,
                PrivacyExperienceUrlHelper.CreateTestRequestsByUserRequest(),
                OperationNames.GetPrivacyRequestApiTestCommandsByUser,
                args.RequestId,
                args.CorrelationVector,
                this.authClient,
                args.UserProxyTicket,
                args.FamilyTicket).ConfigureAwait(false);
            this.AddAcceptJsonHeader(request);

            HttpResponseMessage response = await this.httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);
            return await HttpHelper.HandleHttpResponseAsync<IList<CommandStatusResponse>>(response).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<OperationResponse> TestMsaCloseAsync(TestMsaCloseArgs args)
        {
            args.ThrowOnNull(nameof(args));

            HttpRequestMessage request = await PrivacyExperienceRequestHelper.CreateS2SRequestAsync(
                HttpMethod.Post,
                PrivacyExperienceUrlHelper.CreatePrivacySubjectTestMsaCloseRequest(),
                OperationNames.PostPrivacyRequestApiTestMsaClose,
                args.RequestId,
                args.CorrelationVector,
                this.authClient,
                args.UserProxyTicket,
                args.FamilyTicket).ConfigureAwait(false);
            request = this.SetRequestBody(this.AddAcceptJsonHeader(request), new TestMsaCloseRequest());

            HttpResponseMessage response = await this.httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);
            return await HttpHelper.HandleHttpResponseAsync<OperationResponse>(response).ConfigureAwait(false);
        }
    }
}
