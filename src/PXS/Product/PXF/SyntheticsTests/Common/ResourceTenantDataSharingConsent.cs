using Newtonsoft.Json;
using System;

namespace Microsoft.Membership.MemberServices.PrivacyExperience.SyntheticsTests.Common
{
    public class ResourceTenantDataSharingConsent
    {
        [JsonProperty("resourceTenantId")]
        public String ResourceTenantId { get; set; }
    }
}
