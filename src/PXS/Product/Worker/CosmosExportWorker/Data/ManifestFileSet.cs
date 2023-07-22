// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Data
{
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility;
    using Microsoft.Azure.Cosmos.Table;

    using Newtonsoft.Json;

    /// <summary>
    ///    represents a single result file set
    /// </summary>
    public class ManifestFileSet
    {
        private string dataManifestTag;
        private string reqManifestTag;

        /// <summary>
        ///     Initializes a new instance of the ManifestFileSet class
        /// </summary>
        /// <param name="agentId">data agent id</param>
        /// <param name="cosmosTag">tag for the Cosmos instance containing the file</param>
        /// <param name="requestManifestPath">request manifest</param>
        /// <param name="dataManifestPath">data manifest</param>
        public ManifestFileSet(
            string agentId,
            string cosmosTag,
            string requestManifestPath,
            string dataManifestPath)
        {
            this.RequestManifestPath = requestManifestPath;
            this.DataManifestPath = dataManifestPath;
            this.CosmosTag = cosmosTag;
            this.AgentId = agentId;
        }

        /// <summary>
        ///     Initializes a new instance of the ManifestFileSet class
        /// </summary>
        public ManifestFileSet()
        {
        }

        /// <summary>
        ///     Gets or sets the tag for the Cosmos instance containing the file
        /// </summary>
        public string CosmosTag { get; set; }

        /// <summary>
        ///     Gets or sets request manifest
        /// </summary>
        public string RequestManifestPath { get; set; }

        /// <summary>
        ///     Gets or sets data manifest
        /// </summary>
        public string DataManifestPath { get; set; }

        /// <summary>
        ///     Gets or sets data agent identifier
        /// </summary>
        public string AgentId { get; set; }

        /// <summary>
        ///     Gets the request manifest tag (usually for logging purposes)
        /// </summary>
        [IgnoreProperty]
        [JsonIgnore]
        public string RequestManifestTag => 
            this.reqManifestTag ?? 
            (this.reqManifestTag = Utility.GenerateFileTagFromUri(this.CosmosTag, this.AgentId, this.RequestManifestPath));

        /// <summary>
        ///     Gets the data manifest tag (usually for logging purposes)
        /// </summary>
        [IgnoreProperty]
        [JsonIgnore]
        public string DataManifestTag => 
            this.dataManifestTag ?? 
            (this.dataManifestTag = Utility.GenerateFileTagFromUri(this.CosmosTag, this.AgentId, this.DataManifestPath));
    }
}
