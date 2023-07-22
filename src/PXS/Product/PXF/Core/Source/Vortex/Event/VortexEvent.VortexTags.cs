// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Vortex.Event
{
    using Newtonsoft.Json;

    /// <summary>
    /// VortexEvent
    /// </summary>
    public partial class VortexEvent
    {
        /// <summary>
        /// VortexTags
        /// </summary>
        public class VortexTags
        {
            /// <summary>
            ///     Gets or sets the correlation vector in the legacy location
            /// </summary>
            [JsonProperty("cV", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string CorrelationVector { get; set; }
        }
    }
}
