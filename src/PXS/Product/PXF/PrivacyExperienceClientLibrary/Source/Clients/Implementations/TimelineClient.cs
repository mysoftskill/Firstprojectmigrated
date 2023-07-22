// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Formatting;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Helpers;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Models;

    /// <summary>
    ///     Privacy-Experience Client
    /// </summary>
    public partial class PrivacyExperienceClient : ITimelineClient
    {
        /// <summary>
        ///     Delete timeline data by ids.
        /// </summary>
        /// <param name="args">The delete timeline arguments.</param>
        /// <returns>Task result.</returns>
        public async Task DeleteTimelineAsync(DeleteTimelineByIdsArgs args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            HttpRequestMessage request = await PrivacyExperienceRequestHelper.CreateS2SRequestAsync(
                HttpMethod.Post,
                PrivacyExperienceUrlHelper.CreateDeleteTimelinePathV2(args.CreateQueryStringCollection()),
                OperationNames.DeleteTimeline,
                args.RequestId,
                args.CorrelationVector,
                this.authClient,
                args.UserProxyTicket,
                args.FamilyTicket).ConfigureAwait(false);

            request.Content = new ObjectContent<IList<string>>(args.Ids, new JsonMediaTypeFormatter());

            HttpResponseMessage response = await this.httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);

            await HttpHelper.HandleHttpResponseAsync(response).ConfigureAwait(false);
        }

        /// <summary>
        ///     Delete timeline data by types (bulk)
        /// </summary>
        /// <param name="args">The delete timeline arguments.</param>
        /// <returns>Task result.</returns>
        public async Task DeleteTimelineAsync(DeleteTimelineByTypesArgs args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            HttpRequestMessage request = await PrivacyExperienceRequestHelper.CreateS2SRequestAsync(
                HttpMethod.Delete,
                PrivacyExperienceUrlHelper.CreateDeleteTimelinePathV2(args.CreateQueryStringCollection()),
                OperationNames.DeleteTimeline,
                args.RequestId,
                args.CorrelationVector,
                this.authClient,
                args.UserProxyTicket,
                args.FamilyTicket).ConfigureAwait(false);

            HttpResponseMessage response = await this.httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);

            await HttpHelper.HandleHttpResponseAsync(response).ConfigureAwait(false);
        }

        /// <summary>
        ///     Get timeline data
        /// </summary>
        /// <param name="args">The get timeline arguments.</param>
        /// <param name="method">Http method.</param>
        /// <returns>Task result.</returns>
        public async Task<PagedResponse<TimelineCard>> GetTimelineAsync(GetTimelineArgs args, HttpMethod method = default)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            HttpRequestMessage request = await PrivacyExperienceRequestHelper.CreateS2SRequestAsync(
                method == default ? HttpMethod.Get : method,
                PrivacyExperienceUrlHelper.CreateGetTimelinePathV3(args.CreateQueryStringCollection()),
                OperationNames.GetTimeline,
                args.RequestId,
                args.CorrelationVector,
                this.authClient,
                args.UserProxyTicket,
                args.FamilyTicket).ConfigureAwait(false);

            HttpResponseMessage response = await this.httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);

            return await HttpHelper.HandleHttpResponseAsync<PagedResponse<TimelineCard>>(response).ConfigureAwait(false);
        }

        /// <summary>
        ///     Get the next page of timeline data.
        /// </summary>
        /// <param name="args">The get timeline arguments.</param>
        /// <param name="nextLink">Request Uri for next page.</param>
        /// <returns>Task result.</returns>
        public async Task<PagedResponse<TimelineCard>> GetTimelineNextPageAsync(PrivacyExperienceClientBaseArgs args, Uri nextLink)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));
            if (nextLink == null)
                throw new ArgumentNullException(nameof(nextLink));

            HttpRequestMessage request = await PrivacyExperienceRequestHelper.CreateS2SRequestAsync(
                HttpMethod.Get,
                nextLink,
                OperationNames.GetTimeline,
                args.RequestId,
                args.CorrelationVector,
                this.authClient,
                args.UserProxyTicket,
                args.FamilyTicket).ConfigureAwait(false);

            HttpResponseMessage response = await this.httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);

            return await HttpHelper.HandleHttpResponseAsync<PagedResponse<TimelineCard>>(response).ConfigureAwait(false);
        }

        /// <summary>
        ///     Get the aggregate count of timeline card types THIS IS NOT IMPLEMENTED YET
        /// </summary>
        /// <param name="args">The get aggregate count arguments.</param>
        /// <returns>Task result.</returns>
        public async Task<AggregateCountResponse> GetAggregateCountAsync(GetTimelineAggregateCountArgs args)
        {
            
            if (args == null)
                throw new ArgumentNullException(nameof(args));
            HttpRequestMessage request = await PrivacyExperienceRequestHelper.CreateS2SRequestAsync(
                HttpMethod.Get,
                PrivacyExperienceUrlHelper.CreateAggregateCountTimelinePathV1(args.CreateQueryStringCollection()),
                OperationNames.GetTimelineAggregateCount,
                args.RequestId,
                args.CorrelationVector,
                this.authClient,
                args.UserProxyTicket,
                args.FamilyTicket).ConfigureAwait(false);

            HttpResponseMessage response = await this.httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);

            return await HttpHelper.HandleHttpResponseAsync<AggregateCountResponse>(response).ConfigureAwait(false);
        }

        /// <summary>
        ///     Gete the voice card audio stream for playback
        /// </summary>
        /// <param name="args">The get voice card audio arguments.</param>
        /// <returns>Task result.</returns>
        public async Task<VoiceCardAudio> GetVoiceCardAudioAsync(GetVoiceCardAudioArgs args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            HttpRequestMessage request = await PrivacyExperienceRequestHelper.CreateS2SRequestAsync(
                HttpMethod.Get,
                PrivacyExperienceUrlHelper.CreateGetVoiceCardAudioPathV2(args.CreateQueryStringCollection()),
                OperationNames.GetVoiceCardAudio,
                args.RequestId,
                args.CorrelationVector,
                this.authClient,
                args.UserProxyTicket,
                args.FamilyTicket).ConfigureAwait(false);

            HttpResponseMessage response = await this.httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);

            return await HttpHelper.HandleHttpResponseAsync<VoiceCardAudio>(response).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task WarmupTimelineAsync(PrivacyExperienceClientBaseArgs args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            HttpRequestMessage request = await PrivacyExperienceRequestHelper.CreateS2SRequestAsync(
                HttpMethod.Get,
                new Uri(RouteNames.WarmupTimelineV1, UriKind.Relative),
                OperationNames.WarmupTimeline,
                args.RequestId,
                args.CorrelationVector,
                this.authClient,
                args.UserProxyTicket,
                args.FamilyTicket).ConfigureAwait(false);

            HttpResponseMessage response = await this.httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);

            await HttpHelper.HandleHttpResponseAsync(response).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IList<GetRecurringDeleteResponse>> GetRecurringDeletesAsync(PrivacyExperienceClientBaseArgs args)
        {
            CheckPrivacyExperienceClientBaseArgs(args);

            HttpRequestMessage request = await PrivacyExperienceRequestHelper.CreateS2SRequestAsync(
                httpMethod: HttpMethod.Get,
                requestUri: PrivacyExperienceUrlHelper.CreateGetRecurringDeletesPathV1(),
                OperationNames.GetRecurringDeletes,
                args.RequestId,
                args.CorrelationVector,
                this.authClient,
                args.UserProxyTicket,
                args.FamilyTicket).ConfigureAwait(false);

            HttpResponseMessage response = await this.httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);

            return await HttpHelper.HandleHttpResponseAsync<IList<GetRecurringDeleteResponse>>(response).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteRecurringDeletesAsync(DeleteRecurringDeletesArgs args)
        {
            CheckPrivacyExperienceClientBaseArgs(args);

            HttpRequestMessage request = await PrivacyExperienceRequestHelper.CreateS2SRequestAsync(
                httpMethod: HttpMethod.Delete,
                PrivacyExperienceUrlHelper.CreateDeleteRecurringDeletesPathV1(args.CreateQueryStringCollection()),
                OperationNames.DeleteRecurringDeletes,
                args.RequestId,
                args.CorrelationVector,
                this.authClient,
                args.UserProxyTicket,
                args.FamilyTicket).ConfigureAwait(false);

            HttpResponseMessage response = await this.httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);

            await HttpHelper.HandleHttpResponseAsync(response).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<GetRecurringDeleteResponse> CreateOrUpdateRecurringDeletesAsync(CreateOrUpdateRecurringDeletesArgs args)
        {
            CheckPrivacyExperienceClientBaseArgs(args);

            HttpRequestMessage request = await PrivacyExperienceRequestHelper.CreateS2SRequestAsync(
                httpMethod: HttpMethod.Post,
                PrivacyExperienceUrlHelper.CreateOrUpdateRecurringDeletesPathV1(args.CreateQueryStringCollection()),
                OperationNames.CreateOrUpdateRecurringDeletes,
                args.RequestId,
                args.CorrelationVector,
                this.authClient,
                args.UserProxyTicket,
                args.FamilyTicket).ConfigureAwait(false);

            HttpResponseMessage response = await this.httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);
            return await HttpHelper.HandleHttpResponseAsync<GetRecurringDeleteResponse>(response).ConfigureAwait(false);
        }

        private static void CheckPrivacyExperienceClientBaseArgs(PrivacyExperienceClientBaseArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            if (string.IsNullOrEmpty(args.UserProxyTicket))
            {
                throw new ArgumentNullException(nameof(args.UserProxyTicket));
            }
        }
    }
}
