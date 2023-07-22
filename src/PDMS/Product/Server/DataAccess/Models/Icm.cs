namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// Info needed for creating incidents.
    /// </summary>
    public class Icm
    {
        /// <summary>
        /// Gets or sets the ICM connector Id.
        /// </summary>
        [JsonProperty(PropertyName = "connectorId")]
        public Guid ConnectorId { get; set; }

        /// <summary>
        /// Gets or sets the ICM data source.
        /// </summary>
        [JsonProperty(PropertyName = "source")]
        public IcmSource Source { get; set; }

        /// <summary>
        /// Gets or sets the ICM tenant Id.
        /// </summary>
        [JsonProperty(PropertyName = "tenantId")]
        public long TenantId { get; set; }
    }
}
