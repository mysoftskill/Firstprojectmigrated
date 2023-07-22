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
    public class CompleteDataFile
    {
        private string dataManifestTag;
        private string dataTag;

        /// <summary>
        ///     Initializes a new instance of the CompleteDataFile class
        /// </summary>
        /// <param name="file">newly completed file</param>
        public CompleteDataFile(PendingDataFile file)
        {
            this.ManifestPath = file.ManifestPath;
            this.DataFilePath = file.DataFilePath;
            this.CosmosTag = file.CosmosTag;
            this.AgentId = file.AgentId;
        }

        /// <summary>
        ///     Initializes a new instance of the ManifestFileSet class
        /// </summary>
        public CompleteDataFile()
        {
        }

        /// <summary>
        ///     Gets or sets the tag for the Cosmos instance containing the file
        /// </summary>
        public string CosmosTag { get; set; }

        /// <summary>
        ///     Gets or sets the manifest path
        /// </summary>
        public string ManifestPath { get; set; }

        /// <summary>
        ///     Gets or sets the data file path
        /// </summary>
        public string DataFilePath { get; set; }

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
