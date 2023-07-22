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
    ///     Delete for <see cref="IContentConsumptionV2Adapter" />
    /// </summary>
    public class ContentConsumptionV2Delete
    {
        /// <summary>
        ///     Id of the content consumption record
        /// </summary>
        public string Id { get; }

        /// <summary>
        ///     Timestamp of the content consumption record
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        /// <summary>
        ///     Constructs a content consumption delete.
        /// </summary>
        public ContentConsumptionV2Delete(string id, DateTimeOffset timestamp)
        {
            this.Id = id;
            this.Timestamp = timestamp;
        }
    }

    public interface IContentConsumptionV2Adapter
    {
        /// <summary>
        ///     Delete a specific ContentConsumption entry.
        /// </summary>
        Task<DeleteResourceResponse> DeleteContentConsumptionAsync(IPxfRequestContext requestContext, params ContentConsumptionV2Delete[] deletes);

        /// <summary>
        ///     Get ContentConsumption
        /// </summary>
        Task<PagedResponse<ContentConsumptionResource>> GetContentConsumptionAsync(
            IPxfRequestContext requestContext,
            DateTimeOffset startingAt,
            IList<string> sources,
            string search);

        /// <summary>
        ///     Gets next page of ContentConsumption from partner
        /// </summary>
        Task<PagedResponse<ContentConsumptionResource>> GetNextContentConsumptionPageAsync(
            IPxfRequestContext requestContext,
            Uri nextUri);


        /// <summary>
        ///     Gets the aggregate count of ContentConsumption
        /// </summary>
        Task<CountResourceResponse> GetContentConsumptionAggregateCountAsync(IPxfRequestContext requestContext);
    }
}
