// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Combined view of resources collected from multiple partners
    /// </summary>
    /// <typeparam name="T">Resource type</typeparam>
    public class AggregatedResponse<T>
    {
        /// <summary>
        /// Combined set of resources collected
        /// </summary>
        public IList<T> Items { get; internal set; }
    }
}
