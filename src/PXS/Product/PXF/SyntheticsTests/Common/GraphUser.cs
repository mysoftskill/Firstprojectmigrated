namespace Microsoft.Membership.MemberServices.PrivacyExperience.SyntheticsTests.Common
{
    using Newtonsoft.Json;

    public class GraphUser
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("userPrincipalName")]
        public string UserPrincipalName { get; set; }
    }
}
