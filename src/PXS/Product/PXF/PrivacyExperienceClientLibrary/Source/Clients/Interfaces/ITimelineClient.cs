// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Models;

    /// <summary>
    ///     Timeline client
    /// </summary>
    public interface ITimelineClient
    {
        /// <summary>
        ///     Delete timeline data by ids.
        /// </summary>
        /// <param name="args">The delete timeline arguments.</param>
        /// <returns>Task result.</returns>
        Task DeleteTimelineAsync(DeleteTimelineByIdsArgs args);

        /// <summary>
        ///     Delete timeline data by types (bulk)
        /// </summary>
        /// <param name="args">The delete timeline arguments.</param>
        /// <returns>Task result.</returns>
        Task DeleteTimelineAsync(DeleteTimelineByTypesArgs args);

        /// <summary>
        ///     Get aggregate count of resource types
        /// </summary>
        /// <param name="args">The aggregate count arguments.</param>
        /// <returns></returns>
        Task<AggregateCountResponse> GetAggregateCountAsync(GetTimelineAggregateCountArgs args);

        /// <summary>
        ///     Get timeline data
        /// </summary>
        /// <param name="args">The get timeline arguments.</param>
        /// <param name="method">Http method.</param>
        /// <returns>Task result.</returns>
        Task<PagedResponse<TimelineCard>> GetTimelineAsync(GetTimelineArgs args, HttpMethod method = default);

        /// <summary>
        ///     Get the next page of timeline data.
        /// </summary>
        /// <param name="args">The get timeline arguments.</param>
        /// <param name="nextLink">Request Uri for next page.</param>
        /// <returns>Task result.</returns>
        Task<PagedResponse<TimelineCard>> GetTimelineNextPageAsync(PrivacyExperienceClientBaseArgs args, Uri nextLink);

        /// <summary>
        ///     Gete the voice card audio stream for playback
        /// </summary>
        /// <param name="args">The get aggregate count arguments.</param>
        /// <returns>Task result.</returns>
        Task<VoiceCardAudio> GetVoiceCardAudioAsync(GetVoiceCardAudioArgs args);

        /// <summary>
        ///     Warmup the timeline cards. They sometimes are cold.
        /// </summary>
        /// <param name="args">The get voice card audio arguments.</param>
        /// <returns>Task result.</returns>
        Task WarmupTimelineAsync(PrivacyExperienceClientBaseArgs args);

        /// <summary>
        ///     Get recurring deletes info
        /// </summary>
        /// <param name="args">The get recurring deletes arguments.</param>
        /// <returns>All existing recurrent delete records for requested UserProxyTicket (puid) from Schedule DB.</returns>
        Task<IList<GetRecurringDeleteResponse>> GetRecurringDeletesAsync(PrivacyExperienceClientBaseArgs args);

        /// <summary>
        ///     Delete recurrent deletes schedule
        /// </summary>
        /// <param name="args">The get recurring deletes arguments.</param>
        /// <returns>Task result.</returns>
        Task DeleteRecurringDeletesAsync(DeleteRecurringDeletesArgs args);

        /// <summary>
        ///     Create/Update recurring deletes schedule.
        ///     If recurrent delete schedule does not exist it will be created otherwise it will be updated.
        /// </summary>
        /// <param name="args">The recurring deletes arguments.</param>
        /// <returns>Newly created or updated recurrent delete schedule.</returns>
        Task<GetRecurringDeleteResponse> CreateOrUpdateRecurringDeletesAsync(CreateOrUpdateRecurringDeletesArgs args);
    }
}
