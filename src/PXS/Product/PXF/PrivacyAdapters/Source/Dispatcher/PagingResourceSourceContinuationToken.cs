// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Dispatcher
{
    using System;

    using Newtonsoft.Json;

    /// <summary>
    ///     The continuation token for a <see cref="PagingResourceSource{T}" />
    /// </summary>
    [ResourceSourceContinuationTokenName("p")]
    public class PagingResourceSourceContinuationToken : ResourceSourceContinuationToken
    {
        /// <summary>
        ///     The next link where to continue the source.
        /// </summary>
        [JsonProperty("n")]
        public Uri NextLink { get; set; }

        /// <summary>
        ///     The offset into the current <see cref="NextLink" />.
        /// </summary>
        [JsonProperty("o")]
        public int Offset { get; set; }
    }
}
