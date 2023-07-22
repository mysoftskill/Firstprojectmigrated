// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Dispatcher
{
    using System.Collections.Generic;

    using Newtonsoft.Json;

    [ResourceSourceContinuationTokenName("m")]
    public class MergingResourceSourceContinuationToken : ResourceSourceContinuationToken
    {
        [JsonProperty("s")]
        public Dictionary<string, ResourceSourceContinuationToken> SubTokens { get; set; }
    }
}
