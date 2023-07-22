// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1
{
    using System.Collections.Generic;

    using Newtonsoft.Json;

    /// <summary>
    /// Browse History V1
    /// </summary>
    public class BrowseHistoryV1 : WebActivityV1
    {
        [JsonIgnore]
        private Dictionary<string, int> aggregateCountByPartner;

        /// <summary>
        /// Gets or sets the navigated to URL.
        /// </summary>
        [JsonProperty("navigatedToUrl")]
        public string NavigatedToUrl { get; set; }

        /// <summary>
        /// Gets or sets the page title.
        /// </summary>
        [JsonProperty("pageTitle")]
        public string PageTitle { get; set; }

        /// <summary>
        /// Gets or sets the count of de-duped aggregate count of items.
        /// </summary>
        /// <remarks>This value is a de-duped aggregate count across all partners for this browse event.</remarks>
        [JsonProperty("aggregateCount")]
        public int AggregateCount { get; set; }

        [JsonIgnore]
        public Dictionary<string, int> AggregateCountByPartner => this.aggregateCountByPartner ?? (this.aggregateCountByPartner = new Dictionary<string, int>());
    }
}