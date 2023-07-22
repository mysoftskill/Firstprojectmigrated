// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Timeline
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2;

    /// <summary>
    ///     Interface for TimelineService
    /// </summary>
    public interface ITimelineService
    {
        /// <summary>
        ///     Deletes timeline cards by ids
        /// </summary>
        Task<ServiceResponse> DeleteAsync(IRequestContext requestContext, IList<string> ids);

        /// <summary>
        ///     Deletes timeline cards by type for the last period of time
        /// </summary>
        Task<ServiceResponse<IList<Guid>>> DeleteAsync(IRequestContext requestContext, IList<string> types, TimeSpan period, string portal);

        /// <summary>
        ///     Gets timeline cards
        /// </summary>
        Task<ServiceResponse<PagedResponse<TimelineCard>>> GetAsync(
            IRequestContext requestContext,
            IList<string> cardTypes,
            int? count,
            IList<string> deviceIds,
            IList<string> sources,
            string search,
            TimeSpan timeZoneOffset,
            DateTimeOffset startingAt,
            string nextToken);

        /// <summary>
        ///     Gets voice card audio
        /// </summary>
        Task<ServiceResponse<VoiceCardAudio>> GetVoiceCardAudioAsync(IRequestContext requestContext, string id);

        /// <summary>
        ///     Gets aggregate count of card types
        /// </summary>
        /// <param name="requestContext"></param>
        /// <param name="cardTypes">card types to count</param>
        /// <returns>ServiceResponse containing the count of resources</returns>
        Task<ServiceResponse<AggregateCountResponse>> GetAggregateCountAsync(IRequestContext requestContext, IList<string> cardTypes);

        /// <summary>
        ///     Warms up the service.
        /// </summary>
        Task<ServiceResponse> WarmupAsync(IRequestContext requestContext);

        /// <summary>
        ///     Gets recurring deletes
        /// </summary>
        /// <param name="requestContext">Request context</param>
        /// <param name="maxNumberOfRetries">Max number of retries from config</param>
        /// <returns>Recurring delete records</returns>
        Task<ServiceResponse<IList<GetRecurringDeleteResponse>>> GetRecurringDeletesAsync(IRequestContext requestContext, int maxNumberOfRetries);

        /// <summary>
        ///     Delete recurring delete schedule
        /// </summary>
        /// <param name="requestContext">RequestContext</param>
        /// <param name="dataType">DataType</param>
        /// <returns>ServiceResponse</returns>
        Task<ServiceResponse> DeleteRecurringDeletesAsync(IRequestContext requestContext, string dataType);

        /// <summary>
        ///     Create or update recurring deletes
        /// </summary>
        /// <param name="requestContext">RequestContext</param>
        /// <param name="dataType">DataType</param>
        /// <param name="nextDeleteDate">Next delete date</param>
        /// <param name="recurringIntervalDays">Recurring delete interval</param>
        /// <param name="status">Recurring delete status</param>
        /// <param name="recurringDeleteWorkerConfiguration"></param>
        /// <returns>GetRecurringDeleteResponse</returns>
        Task<ServiceResponse<GetRecurringDeleteResponse>> CreateOrUpdateRecurringDeletesAsync(
            IRequestContext requestContext,
            string dataType,
            DateTimeOffset nextDeleteDate,
            RecurringIntervalDays recurringIntervalDays,
            RecurrentDeleteStatus status,
            IRecurringDeleteWorkerConfiguration recurringDeleteWorkerConfiguration);
    }
}
