namespace Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.GraphApis
{
    using System;
    using Newtonsoft.Json;

    public class ExportPersonalDataBody
    {
            [JsonProperty("storageLocation")]
            public Uri StorageLocation { get; set; }
    }
}
