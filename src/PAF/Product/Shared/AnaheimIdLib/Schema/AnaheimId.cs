namespace Microsoft.ComplianceServices.AnaheimIdLib.Schema
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Newtonsoft.Json;

    /// <summary>
    /// AnaheimId schema.
    /// </summary>
    public class AnaheimId
    {
        /// <summary>
        /// Delete DeviceId Request.
        /// </summary>
        [JsonProperty(PropertyName = "deleteDeviceIdRequest")]
        public DeleteDeviceIdRequest DeleteDeviceIdRequest { get; set; }

        /// <summary>
        /// List of Anaheim Ids associated with DeviceId.
        /// </summary>
        [JsonProperty(PropertyName = "anaheimIds")]
        public IEnumerable<long> AnaheimIds { get; set; }
    }
}
