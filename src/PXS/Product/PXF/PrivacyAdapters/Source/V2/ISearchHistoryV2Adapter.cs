// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.V2
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;

    /// <summary>
    ///     Delete for <see cref="ISearchHistoryV2Adapter" />
    /// </summary>
    public class SearchV2Delete
    {
        /// <summary>
        ///     Impression Id of the search
        /// </summary>
        public string ImpressionId { get; }

        /// <summary>
        ///     Constructs a search delete
        /// </summary>
        public SearchV2Delete(string impressionId)
        {
            this.ImpressionId = impressionId;
        }
    }

    public interface ISearchHistoryV2Adapter
    {
        /// <summary>
        ///     Delete a specific SearchHistory entry.
        /// </summary>
        Task<DeleteResourceResponse> DeleteSearchHistoryAsync(IPxfRequestContext requestContext, params SearchV2Delete[] deletes);

        /// <summary>
        ///     Gets next page of SearchHistory from partner
        /// </summary>
        Task<PagedResponse<SearchResource>> GetNextSearchHistoryPageAsync(
            IPxfRequestContext requestContext,
            Uri nextUri);

        /// <summary>
        ///     Get SearchHistory
        /// </summary>
        Task<PagedResponse<SearchResource>> GetSearchHistoryAsync(
            IPxfRequestContext requestContext,
            DateTimeOffset startingAt,
            IList<string> sources,
            string search);


        /// <summary>
        ///     Gets the aggregate count of SearchHistory
        /// </summary>
        Task<CountResourceResponse> GetSearchHistoryAggregateCountAsync(IPxfRequestContext requestContext);
    }
}
