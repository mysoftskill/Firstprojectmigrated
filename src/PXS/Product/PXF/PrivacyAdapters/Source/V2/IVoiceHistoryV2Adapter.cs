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
    ///     Delete predicate for <see cref="IVoiceHistoryV2Adapter" />
    /// </summary>
    public class VoiceV2Delete
    {
        /// <summary>
        ///     Timestamp of the delete
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        /// <summary>
        ///     VoiceId of the delete
        /// </summary>
        public string VoiceId { get; }

        /// <summary>
        ///     Construct a voice delete.
        /// </summary>
        public VoiceV2Delete(string voiceId, DateTimeOffset timestamp)
        {
            this.VoiceId = voiceId;
            this.Timestamp = timestamp;
        }
    }

    /// <summary>
    ///     Voice History V2 Adapter.
    /// </summary>
    public interface IVoiceHistoryV2Adapter
    {
        /// <summary>
        ///     Deletes a single voice history entry
        /// </summary>
        Task<DeleteResourceResponse> DeleteVoiceHistoryAsync(IPxfRequestContext requestContext, params VoiceV2Delete[] deletes);

        /// <summary>
        ///     Gets next page of voice history from partner
        /// </summary>
        Task<PagedResponse<VoiceResource>> GetNextVoiceHistoryPageAsync(
            IPxfRequestContext requestContext,
            Uri nextUri);

        /// <summary>
        ///     Gets voice history from configured partners
        /// </summary>
        Task<PagedResponse<VoiceResource>> GetVoiceHistoryAsync(
            IPxfRequestContext requestContext,
            DateTimeOffset startingAt,
            IList<string> sources,
            string search);

        /// <summary>
        ///     Gets voice history audio from configured partners
        /// </summary>
        Task<VoiceAudioResource> GetVoiceHistoryAudioAsync(
            IPxfRequestContext requestContext,
            string id,
            DateTimeOffset timestamp);


        /// <summary>
        ///     Gets the aggregate count of VoiceHistory
        /// </summary>
        Task<CountResourceResponse> GetVoiceHistoryAggregateCountAsync(IPxfRequestContext requestContext);
    }
}
