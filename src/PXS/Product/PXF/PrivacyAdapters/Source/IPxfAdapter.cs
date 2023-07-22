// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;

    /// <summary>
    ///     Specifies the field to order results by
    /// </summary>
    public enum OrderByType
    {
        /// <summary>
        ///     Sort by DateTime
        /// </summary>
        DateTime = 0,

        /// <summary>
        ///     Sort by Search terms
        /// </summary>
        SearchTerms = 1
    }

    /// <summary>
    ///     Indicates the date option
    /// </summary>
    public enum DateOption
    {
        /// <summary>
        ///     Single day (use date specified in 'startDate')
        /// </summary>
        SingleDay = 0,

        /// <summary>
        ///     Dates before specified dateTime (use 'startDate')
        /// </summary>
        [Obsolete("No longer supported.")]
        Before = 1,

        /// <summary>
        ///     Dates after specified dateTime (use 'startDate')
        /// </summary>
        [Obsolete("No longer supported.")]
        After = 2,

        /// <summary>
        ///     Dates in the specified range ('startDate' to 'endDate'). Only uses Date part, time is ignored.
        /// </summary>
        Between = 3
    }

    /// <summary>
    ///     PxfAdapter interface
    /// </summary>
    public interface IPxfAdapter
    {
        /// <summary>
        ///     Gets the custom headers.
        /// </summary>
        IDictionary<string, string> CustomHeaders { get; }

        /// <summary>
        ///     Deletes the AppUsage asynchronously.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <returns>A <see cref="DeleteResourceResponse" /> contains details about the partner and deletion status.</returns>
        Task<DeleteResourceResponse> DeleteAppUsageAsync(IPxfRequestContext requestContext);

        /// <summary>
        ///     Deletes the browse history asynchronously.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <returns>A <see cref="DeleteResourceResponse" /> contains details about the partner and deletion status.</returns>
        Task<DeleteResourceResponse> DeleteBrowseHistoryAsync(IPxfRequestContext requestContext);

        /// <summary>
        ///     Deletes the location history asynchronously.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <returns>A <see cref="DeleteResourceResponse" /> contains details about the partner and deletion status.</returns>
        Task<DeleteResourceResponse> DeleteLocationHistoryAsync(IPxfRequestContext requestContext);

        /// <summary>
        ///     Deletes the search history asynchronously.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="disableThrottling">if set to <c>true</c>, disable throttling for the request. Only test callerName is allowed to pass <c>true</c> for this.</param>
        /// <returns>A <see cref="DeleteResourceResponse" /> contains details about the partner and deletion status.</returns>
        Task<DeleteResourceResponse> DeleteSearchHistoryAsync(IPxfRequestContext requestContext, bool disableThrottling);

        /// <summary>
        ///     Deletes the voice history asynchronously.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <returns>A <see cref="DeleteResourceResponse" /> contains details about the partner and deletion status.</returns>
        Task<DeleteResourceResponse> DeleteVoiceHistoryAsync(IPxfRequestContext requestContext);

        /// <summary>
        ///     Get AppUsage
        /// </summary>
        /// <param name="requestContext">Context of the request</param>
        /// <param name="orderBy">Order by</param>
        /// <param name="dateOption">Date Option. Start/End date parameters may be required. (required)</param>
        /// <param name="startDate">State date/time (optional)</param>
        /// <param name="endDate">End date/time (optional)</param>
        /// <param name="search">Search query (optional)</param>
        /// <returns>Page of AppUsageResource results</returns>
        Task<PagedResponse<AppUsageResource>> GetAppUsageAsync(
            IPxfRequestContext requestContext,
            OrderByType orderBy,
            DateOption? dateOption = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string search = null);

        /// <summary>
        ///     Get browse history
        /// </summary>
        /// <param name="requestContext">Context of the request</param>
        /// <param name="orderBy">Order by</param>
        /// <param name="dateOption">Date Option. Start/End date parameters may be required. (required)</param>
        /// <param name="startDate">State date/time (optional)</param>
        /// <param name="endDate">End date/time (optional)</param>
        /// <param name="search">Search query (optional)</param>
        /// <returns>Page of BrowseResource results</returns>
        Task<PagedResponse<BrowseResource>> GetBrowseHistoryAsync(
            IPxfRequestContext requestContext,
            OrderByType orderBy,
            DateOption? dateOption = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string search = null);

        /// <summary>
        ///     Get location history
        /// </summary>
        /// <param name="requestContext">Context of the request</param>
        /// <param name="orderBy">Order by</param>
        /// <param name="dateOption">Date Option. Start/End date parameters may be required. (required)</param>
        /// <param name="startDate">State date/time (optional)</param>
        /// <param name="endDate">End date/time (optional)</param>
        /// <returns>Page of LocationResource results</returns>
        Task<PagedResponse<LocationResource>> GetLocationHistoryAsync(
            IPxfRequestContext requestContext,
            OrderByType orderBy,
            DateOption? dateOption = null,
            DateTime? startDate = null,
            DateTime? endDate = null);

        /// <summary>
        ///     Gets next page of AppUsage from partner
        /// </summary>
        /// <param name="requestContext">Context of the request</param>
        /// <param name="nextUri">Next link returned by previous page</param>
        /// <returns>Page of AppUsageResource results</returns>
        Task<PagedResponse<AppUsageResource>> GetNextAppUsagePageAsync(
            IPxfRequestContext requestContext,
            Uri nextUri);

        /// <summary>
        ///     Gets next page of browse history from partner
        /// </summary>
        /// <param name="requestContext">Context of the request</param>
        /// <param name="nextUri">Next link returned by previous page</param>
        /// <returns>Page of BrowseResource results</returns>
        Task<PagedResponse<BrowseResource>> GetNextBrowsePageAsync(
            IPxfRequestContext requestContext,
            Uri nextUri);

        /// <summary>
        ///     Gets next page of location history from partner
        /// </summary>
        /// <param name="requestContext">Context of the request</param>
        /// <param name="nextUri">Next link returned by previous page</param>
        /// <returns>Page of LocationResource results</returns>
        Task<PagedResponse<LocationResource>> GetNextLocationPageAsync(
            IPxfRequestContext requestContext,
            Uri nextUri);

        /// <summary>
        ///     Gets next page of search history from partner
        /// </summary>
        /// <param name="requestContext">Context of the request</param>
        /// <param name="nextUri">Next link returned by previous page</param>
        /// <returns>Page of SearchResource results</returns>
        Task<PagedResponse<SearchResource>> GetNextSearchPageAsync(
            IPxfRequestContext requestContext,
            Uri nextUri);

        /// <summary>
        ///     Gets next page of voice history from partner
        /// </summary>
        /// <param name="requestContext">Context of the request</param>
        /// <param name="nextUri">Next link returned by previous page</param>
        /// <returns>Page of VoiceResource results</returns>
        Task<PagedResponse<VoiceResource>> GetNextVoicePageAsync(
            IPxfRequestContext requestContext,
            Uri nextUri);

        /// <summary>
        ///     Get search history
        /// </summary>
        /// <param name="requestContext">Context of the request</param>
        /// <param name="orderBy">Order by</param>
        /// <param name="dateOption">Date Option. Start/End date parameters may be required. (required)</param>
        /// <param name="startDate">State date/time (optional)</param>
        /// <param name="endDate">End date/time (optional)</param>
        /// <param name="search">Search query (optional)</param>
        /// <param name="disableThrottling">if set to <c>true</c>, disable throttling for the request. Only test callerName is allowed to pass <c>true</c> for this.</param>
        /// <returns>Page of SearchResource results</returns>
        Task<PagedResponse<SearchResource>> GetSearchHistoryAsync(
            IPxfRequestContext requestContext,
            OrderByType orderBy,
            DateOption? dateOption = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string search = null,
            bool disableThrottling = false);

        /// <summary>
        ///     Get voice history
        /// </summary>
        /// <param name="requestContext">Context of the request</param>
        /// <param name="orderBy">Order by</param>
        /// <param name="dateOption">Date Option. Start/End date parameters may be required. (required)</param>
        /// <param name="startDate">State date/time (optional)</param>
        /// <param name="endDate">End date/time (optional)</param>
        /// <param name="search">Search query (optional)</param>
        /// <returns>Page of VoiceResource results</returns>
        Task<PagedResponse<VoiceResource>> GetVoiceHistoryAsync(
            IPxfRequestContext requestContext,
            OrderByType orderBy,
            DateOption? dateOption = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string search = null);

        /// <summary>
        ///     Get voice history audio
        /// </summary>
        /// <param name="requestContext">Context of the request</param>
        /// <param name="id">Id of the voice history audio to retrieve.</param>
        /// <returns>VoiceAudioResource result</returns>
        Task<VoiceAudioResource> GetVoiceHistoryAudioAsync(
            IPxfRequestContext requestContext,
            string id);

        /// <summary>
        ///  Gets the aggregate count of AppUsage
        /// </summary>
        /// <param name="requestContext">Context of the request</param>
        /// <returns>Count Resource Response</returns>
        Task<CountResourceResponse> GetAppUsageAggregateCountAsync(IPxfRequestContext requestContext);

        /// <summary>
        ///  Gets the aggregate count of BrowseHistory
        /// </summary>
        /// <param name="requestContext">Context of the request</param>
        /// <returns>Count Resource Response</returns>
        Task<CountResourceResponse> GetBrowseHistoryAggregateCountAsync(IPxfRequestContext requestContext);

        /// <summary>
        ///  Gets the aggregate count of ContentConsumption
        /// </summary>
        /// <param name="requestContext">Context of the request</param>
        /// <returns>Count Resource Response</returns>
        Task<CountResourceResponse> GetContentConsumptionAggregateCountAsync(IPxfRequestContext requestContext);

        /// <summary>
        ///  Gets the aggregate count of Location
        /// </summary>
        /// <param name="requestContext">Context of the request</param>
        /// <returns>Count Resource Response</returns>
        Task<CountResourceResponse> GetLocationAggregateCountAsync(IPxfRequestContext requestContext);

        /// <summary>
        ///  Gets the aggregate count of Search History
        /// </summary>
        /// <param name="requestContext">Context of the request</param>
        /// <returns>Count Resource Response</returns>
        Task<CountResourceResponse> GetSearchHistoryAggregateCountAsync(IPxfRequestContext requestContext);

        /// <summary>
        ///  Gets the aggregate count of Voice History
        /// </summary>
        /// <param name="requestContext">Context of the request</param>
        /// <returns>Count Resource Response</returns>
        Task<CountResourceResponse> GetVoiceHistoryAggregateCountAsync(IPxfRequestContext requestContext);

    }
}
