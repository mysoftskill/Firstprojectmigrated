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
    ///     Delete from <see cref="IAppUsageV2Adapter" />
    /// </summary>
    public struct AppUsageV2Delete
    {
        /// <summary>
        ///     Construct an app usage delete
        /// </summary>
        public AppUsageV2Delete(string id, DateTimeOffset timestamp, string aggregation)
        {
            this.Id = id;
            this.Timestamp = timestamp;
            this.Aggregation = aggregation;
        }

        /// <summary>
        ///     Id of the AppUsage record
        /// </summary>
        public string Id { get; }

        /// <summary>
        ///     Timestamp of the AppUsage record
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        /// <summary>
        ///     Aggregation type of the AppUsage record (needed just for PDOS)
        /// </summary>
        public string Aggregation { get; }
    }

    public interface IAppUsageV2Adapter
    {
        /// <summary>
        ///     Delete a specific AppUsage entry.
        /// </summary>
        /// <param name="requestContext">Request context</param>
        /// <param name="deletes">List of entries to delete, if null, delete all.</param>
        Task<DeleteResourceResponse> DeleteAppUsageAsync(IPxfRequestContext requestContext, params AppUsageV2Delete[] deletes);

        /// <summary>
        ///     Get AppUsage
        /// </summary>
        Task<PagedResponse<AppUsageResource>> GetAppUsageAsync(
            IPxfRequestContext requestContext,
            DateTimeOffset startingAt,
            IList<string> sources,
            string search);

        /// <summary>
        ///     Gets next page of AppUsage from partner
        /// </summary>
        Task<PagedResponse<AppUsageResource>> GetNextAppUsagePageAsync(
            IPxfRequestContext requestContext,
            Uri nextUri);

        /// <summary>
        ///     Gets the aggregate count of AppUsage
        /// </summary>
        Task<CountResourceResponse> GetAppUsageAggregateCountAsync(IPxfRequestContext requestContext);
    }
}
