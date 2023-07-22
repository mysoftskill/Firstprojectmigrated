// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.EventHubProcessor
{
    using System;

    using Newtonsoft.Json;

    /// <summary>
    ///     A class representing the resource data.
    /// </summary>
    public class ResourceData
    {
        /// <summary>
        ///     Gets or sets the UTC date and time of the event.
        /// </summary>
        [JsonProperty(PropertyName = "eventTime")]
        public DateTimeOffset EventTime { get; set; }


        [JsonIgnore]
        public Guid Id
        {
            get => !string.IsNullOrWhiteSpace(this.IdStr) ? Guid.Parse(this.IdStr) : default;
            set => this.IdStr = value.ToString();
        }

        /// <summary>
        ///     Gets or sets the ID of the resource.
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        private string IdStr { get; set; }

        /// <summary>
        ///     Gets or sets the OData etag property.
        /// </summary>
        [JsonProperty(PropertyName = "@odata.etag")]
        public string ODataEtag { get; set; }

        /// <summary>
        ///     Gets or sets the OData ID of the resource. This is the same value as the resource property.
        /// </summary>
        [JsonProperty(PropertyName = "@odata.id")]
        public string ODataId { get; set; }

        /// <summary>
        ///     Gets or sets the OData type of the resource: "#Microsoft.Graph.Message", "#Microsoft.Graph.Event", or "#Microsoft.Graph.Contact".
        /// </summary>
        [JsonProperty(PropertyName = "@odata.type")]
        public string ODataType { get; set; }

        [JsonIgnore]
        public long? OrgPuid
        {
            get => !string.IsNullOrWhiteSpace(this.OrgIdPuidStr) ? long.Parse(this.OrgIdPuidStr, System.Globalization.NumberStyles.AllowHexSpecifier) : (long?)null;
            set => this.OrgIdPuidStr = value?.ToString("X16");
        }

        /// <summary>
        ///     Gets or sets the org id puid.
        /// </summary>
        [JsonProperty(PropertyName = "netId")]
        private string OrgIdPuidStr { get; set; }

        [JsonIgnore]
        public Guid TenantId
        {
            get => !string.IsNullOrWhiteSpace(this.TenantIdStr) ? Guid.Parse(this.TenantIdStr) : default;
            set => this.TenantIdStr = value.ToString();
        }

        /// <summary>
        ///     Gets or sets the tenant id.
        /// </summary>
        [JsonProperty(PropertyName = "organizationId")]
        private string TenantIdStr { get; set; }

        [JsonIgnore]
        public Guid HomeTenantId
        {
            get => !string.IsNullOrWhiteSpace(this.HomeTenantIdStr) ? Guid.Parse(this.HomeTenantIdStr) : default;
            set => this.HomeTenantIdStr = value.ToString();
        }

        /// <summary>
        ///     Gets or sets the home tenant id. This value only exists for traversed user.
        /// </summary>
        [JsonProperty(PropertyName = "home_organizationId")]
        private string HomeTenantIdStr { get; set; }
    }
}
