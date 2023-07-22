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
    ///     Delete predicate for <see cref="ILocationV2Adapter" />
    /// </summary>
    public class LocationV2Delete
    {
        /// <summary>
        ///     Timestamp of the delete
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        /// <summary>
        ///     Construct a location delete.
        /// </summary>
        public LocationV2Delete(DateTimeOffset timestamp)
        {
            this.Timestamp = timestamp;
        }
    }

    /// <summary>
    ///     Location History V2 Adapter.
    /// </summary>
    public interface ILocationV2Adapter
    {
        /// <summary>
        ///     Deletes a single location history entry
        /// </summary>
        Task<DeleteResourceResponse> DeleteLocationAsync(IPxfRequestContext requestContext, params LocationV2Delete[] deletes);

        /// <summary>
        ///     Gets location history from configured partners
        /// </summary>
        Task<PagedResponse<LocationResource>> GetLocationAsync(
            IPxfRequestContext requestContext,
            DateTimeOffset startingAt,
            IList<string> sources,
            string search);

        /// <summary>
        ///     Gets next page of location history from partner
        /// </summary>
        Task<PagedResponse<LocationResource>> GetNextLocationPageAsync(
            IPxfRequestContext requestContext,
            Uri nextUri);


        /// <summary>
        ///     Gets the aggregate count of Location
        /// </summary>
        Task<CountResourceResponse> GetLocationAggregateCountAsync(IPxfRequestContext requestContext);
    }
}
