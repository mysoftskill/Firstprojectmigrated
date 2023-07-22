// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyMockService.Controllers
{
    using Newtonsoft.Json;

    public class CustomerMasterError
    {
        [JsonProperty("error_code")]
        public string ErrorCode { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
        
        [JsonProperty("object_type")]
        public string ObjectType { get; set; }
    }
}