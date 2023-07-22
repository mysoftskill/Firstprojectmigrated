// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Data
{
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility;
    using Microsoft.Azure.Cosmos.Table;

    using Newtonsoft.Json;

    /// <summary>
    ///    represents a single result data file
    /// </summary>
    public class PendingDataFile
    {
        private string dataManifestTag;
        private string dataTag;

        /// <summary>
        ///     Initializes a new instance of the PendingDataFile class
        /// </summary>
        /// <param name="manifestPath">manifest file</param>
        /// <param name="dataFilePath">data file</param>
        /// <param name="exportFileName">file name to use in export package</param>
        /// <param name="cosmosTag">tag for the Cosmos instance containing the file</param>
        /// <param name="agentId">data agent identifier</param>
        public PendingDataFile(
            string manifestPath,
            string dataFilePath,
            string exportFileName,
            string cosmosTag,
            string agentId)
        {
            this.ExportFileName = exportFileName;
            this.ManifestPath = manifestPath;
            this.DataFilePath = dataFilePath;
            this.CosmosTag = cosmosTag;
            this.AgentId = agentId;
        }

        /// <summary>
        ///     Initializes a new instance of the PendingDataFile class
        /// </summary>
        public PendingDataFile()
        {
        }

        /// <summary>
        ///     Gets or sets the manifest path
        /// </summary>
        public string ManifestPath { get; set; }

        /// <summary>
        ///     Gets or sets the data file path
        /// </summary>
        public string DataFilePath { get; set; }

        /// <summary>
        ///     Gets or sets the name of the file in the export package
        /// </summary>
        public string ExportFileName { get; set; }

        /// <summary>
        ///     Gets or sets the tag for the Cosmos instance containing the file
        /// </summary>
        public string CosmosTag { get; set; }

        /// <summary>
        ///     Gets or sets data agent identifier
        /// </summary>
        public string AgentId { get; set; }
        
        /// <summary>
        ///     Gets the data manifest tag (usually for logging purposes)
        /// </summary>
        [IgnoreProperty]
        [JsonIgnore]
        public string ManifestTag =>
            this.dataManifestTag ??
            (this.dataManifestTag = Utility.GenerateFileTagFromUri(this.CosmosTag, this.AgentId, this.ManifestPath));

        /// <summary>
        ///     Gets the data file tag (usually for logging purposes)
        /// </summary>
        [IgnoreProperty]
        [JsonIgnore]
        public string DataFileTag =>
            this.dataTag ??
            (this.dataTag = Utility.GenerateFileTagFromUri(this.CosmosTag, this.AgentId, this.DataFilePath));
    }
}
