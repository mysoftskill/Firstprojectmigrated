// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1
{
    using Newtonsoft.Json;

    /// <summary>
    /// Web Activity V1
    /// </summary>
    public abstract class WebActivityV1 : ResourceV1
    {
        /// <summary>
        /// Gets or sets the search terms.
        /// </summary>
        /// <remarks>TODO: This should be moved to <see cref="SearchHistoryV1"/> in the future.</remarks>
        [JsonProperty("searchTerms")]
        public string SearchTerms { get; set; }

        /// <summary>
        /// Gets or sets the location.
        /// </summary>
        [JsonProperty("location")]
        public WebLocationV1 Location { get; set; }
    }

    /// <summary>
    /// Web Location V1
    /// </summary>
    public class WebLocationV1
    {
        /// <summary>
        /// Gets or sets the latitude.
        /// </summary>
        [JsonProperty("latitude")]
        public double Latitude { get; set; }

        /// <summary>
        /// Gets or sets the longitude.
        /// </summary>
        [JsonProperty("longitude")]
        public double Longitude { get; set; }

        /// <summary>
        /// Gets or sets the accuracy radius in meters.
        /// </summary>
        [JsonProperty("accuracyRadius")]
        public int? AccuracyRadius { get; set; }
    }
}