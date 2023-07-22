// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Vortex
{
    using System;

    using Newtonsoft.Json;

    public class DeviceDeleteRequest
    {
        [JsonProperty("vortexEventData", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public byte[] Data { get; set; }

        [JsonProperty("requestInformation", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public VortexRequestInformation RequestInformation { get; set; }

        [JsonProperty("requestId")]
        public Guid RequestId { get; set; } = Guid.NewGuid();

        [JsonProperty("isSentToPCF")]
        public bool IsSentToPCF { get; set; } = false;
    }
}
