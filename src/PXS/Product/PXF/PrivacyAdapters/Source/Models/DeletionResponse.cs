// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Models
{
    using System.Collections.Generic;

    /// <summary>
    ///     Combined view of resources deleted from multiple partners
    /// </summary>
    /// <typeparam name="T">Resource type</typeparam>
    public class DeletionResponse<T>
    {
        /// <summary>
        ///     Combined set of resources deleted
        /// </summary>
        public IEnumerable<T> Items { get; internal set; }

        public DeletionResponse(IEnumerable<T> items)
        {
            this.Items = items;
        }
    }
}
