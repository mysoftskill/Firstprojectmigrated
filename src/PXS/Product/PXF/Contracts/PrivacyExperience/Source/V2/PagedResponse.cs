// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    /// <summary>
    ///     A page of timeline cards
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PagedResponse<T>
        where T : TimelineCard
    {
        /// <summary>
        ///     The timeline cards in this page
        /// </summary>
        /// <remarks>
        ///     Make sure when deserializing this class to use <see cref="TimelineCardBinder" /> since this has <see cref="TypeNameHandling.Auto" />.
        /// </remarks>
        [JsonProperty(ItemTypeNameHandling = TypeNameHandling.Auto)]
        public IList<T> Items { get; set; }

        /// <summary>
        ///     The next link if there is one, or null
        /// </summary>
        public Uri NextLink { get; set; }
    }
}
