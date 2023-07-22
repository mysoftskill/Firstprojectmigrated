// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.DataContracts.V2
{
    using System;

    using Newtonsoft.Json;

    public class ContentConsumptionResourceV2 : PrivacyResourceV2
    {
        /// <summary>
        ///     Application the content was consumed in.
        /// </summary>
        [JsonProperty("appName")]
        public string AppName { get; set; }

        /// <summary>
        ///     Artist of the content.
        /// </summary>
        [JsonProperty("artist")]
        public string Artist { get; set; }

        /// <summary>
        ///     For how long was the content consumed.
        /// </summary>
        [JsonProperty("consumptionTime")]
        public int ConsumptionTimeSeconds { get; set; }

        /// <summary>
        ///     The container fo the content. The show for an episode, album for a song, etc.
        /// </summary>
        [JsonProperty("containerName")]
        public string ContainerName { get; set; }

        /// <summary>
        ///     Url to the content consumed.
        /// </summary>
        [JsonProperty("contentUrl")]
        public Uri ContentUrl { get; set; }

        /// <summary>
        ///     Url of the icon for the content.
        /// </summary>
        [JsonProperty("iconUrl")]
        public Uri IconUrl { get; set; }

        /// <summary>
        ///     Id of the content.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        ///     The type of content.
        /// </summary>
        [JsonProperty("mediaType")]
        public string MediaType { get; set; }

        /// <summary>
        ///     The title of the content.
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }
    }
}
