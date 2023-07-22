namespace Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache
{
    using System;
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    using Newtonsoft.Json;

    /// <summary>
    /// PDMS AssetGroup JSON schema
    /// </summary>
    public sealed class AssetGroupInfoDocument
    {
        [JsonProperty]
        public AgentId AgentId { get; set; }

        [JsonProperty]
        public AssetGroupId AssetGroupId { get; set; }

        [JsonProperty]
        public string AssetGroupQualifier { get; set; }

        [JsonProperty]
        public string AuthenticationType { get; set; }

        [JsonProperty]
        public string[] Capabilities { get; set; }

        [JsonProperty]
        public string[] DataTypes { get; set; }

        [JsonProperty]
        public bool IsDeprecated { get; set; }

        [JsonProperty]
        public bool IsRealTimeStore { get; set; }

        [JsonProperty]
        public long? MsaSiteId { get; set; }

        [JsonProperty]
        public Guid? AadAppId { get; set; }

        [JsonProperty]
        public string[] SubjectTypes { get; set; }

        [JsonProperty]
        public string[] TenantIds { get; set; }

        [JsonProperty]
        public string AgentReadiness { get; set; }

        [JsonProperty]
        public IDictionary<string, string> ExtendedProps { get; set; }

        [JsonProperty]
        public IList<AssetGroupVariantInfoDocument> VariantInfosAppliedByPcf { get; set; }

        [JsonProperty]
        public IList<AssetGroupVariantInfoDocument> VariantInfosAppliedByAgents { get; set; }

        [JsonProperty]
        public string[] SupportedCloudInstances { get; set; }

        [JsonProperty]
        public string DeploymentLocation { get; set; }
    }
}
