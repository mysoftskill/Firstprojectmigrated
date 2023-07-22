// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.V2
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;

    public class BrowseV2Delete
    {
        /// <summary>
        ///     Gets the timestamp.
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        /// <summary>
        ///     Gets the URI hash.
        /// </summary>
        public string UriHash { get; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="BrowseV2Delete" /> class.
        /// </summary>
        /// <param name="uriHash">The URI hash.</param>
        /// <param name="timestamp">The timestamp.</param>
        public BrowseV2Delete(string uriHash, DateTimeOffset timestamp)
        {
            this.UriHash = uriHash;
            this.Timestamp = timestamp;
        }
    }

    /// <summary>
    ///     IBrowseHistoryV2Adapter
    /// </summary>
    public interface IBrowseHistoryV2Adapter
    {
        /// <summary>
        ///     Delete a specific BrowseHistory entry.
        /// </summary>
        Task<DeleteResourceResponse> DeleteBrowseHistoryAsync(IPxfRequestContext requestContext, params BrowseV2Delete[] deletes);

        /// <summary>
        ///     Get BrowseHistory
        /// </summary>
        Task<PagedResponse<BrowseResource>> GetBrowseHistoryAsync(
            IPxfRequestContext requestContext,
            DateTimeOffset startingAt,
            IList<string> sources,
            string search);

        /// <summary>
        ///     Gets next page of BrowseHistory from partner
        /// </summary>
        Task<PagedResponse<BrowseResource>> GetNextBrowseHistoryPageAsync(
            IPxfRequestContext requestContext,
            Uri nextUri);

        /// <summary>
        ///     Gets the aggregate count of BrowseHistory from partner
        /// </summary>
       Task<CountResourceResponse> GetBrowseHistoryAggregateCountAsync(IPxfRequestContext requestContext);
    }
}
