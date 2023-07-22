// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyAdapters.PcfAdapter
{
    using Newtonsoft.Json;

    public class DeleteExportArchiveParameters
    {
        public DeleteExportArchiveParameters(string commandId, long puid)
        {
            this.CommandId = commandId;
            this.Puid = puid;
        }

        /// <summary>
        /// CommandId of the export archive user wants to delete
        /// </summary>
        [JsonProperty("commandId")]
        public string CommandId { get; set; }

        /// <summary>
        /// Puid of the user
        /// </summary>
        [JsonProperty("puid")]
        public long Puid { get; set; }

    }
}
