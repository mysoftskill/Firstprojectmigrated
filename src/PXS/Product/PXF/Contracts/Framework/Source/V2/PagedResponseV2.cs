//--------------------------------------------------------------------------------
// <copyright file="PagedResponseV2.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Privacy.DataContracts.V2
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// Paged Response of type T V1
    /// </summary>
    public class PagedResponseV2<T>
    {
        /// <summary>
        /// Gets or sets the list of items in the current response page
        /// </summary>
        [JsonProperty("items")]
        public IEnumerable<T> Items { get; set; }

        [JsonProperty("@nextLink")]
        public Uri NextLink { get; set; }
    }
}