using Microsoft.PrivacyServices.DocumentDB.Models;
using Nest;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PrivacyServices.DataManagement.Worker.ServiceTreeMetadata
{

    /// <summary>
    /// Contains the data that is stored in the ServiceTree Metadata worker lock.
    /// </summary>
    public class ServiceTreeMetadataWorkerLockState
    {

        /// <summary>
        /// Gets or sets the time of the last full sync.
        /// </summary>
        [JsonProperty(PropertyName = "lastSyncTime")]
        [JsonConverter(typeof(DateTimeOffsetConverter))]
        public DateTimeOffset LastSyncTime { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the update process has started.
        /// </summary>
        [JsonProperty(PropertyName = "inProgress")]
        public bool InProgress { get; set; }
    }
}
