//--------------------------------------------------------------------------------
// <copyright file="DeleteRequestV2.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Privacy.DataContracts.V2
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    /// <summary>
    /// Request body for a creating a delete request
    /// </summary>
    public class DeleteRequestV2
    {
        /// <summary>
        /// A list of filters that should be deleted, or if null, delete entire collection.
        /// </summary>
        [JsonProperty("filters")]
        public IList<string> Filters { get; set; }
    }
}
