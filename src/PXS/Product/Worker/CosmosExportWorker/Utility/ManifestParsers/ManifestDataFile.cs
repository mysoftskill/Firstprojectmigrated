// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility.ManifestParsers
{
    /// <summary>
    ///     an enumeration of possible file sources
    /// </summary>
    public enum DataFileSource
    {
        /// <summary>
        ///     invalid option
        /// </summary>
        Invalid = 0,

        /// <summary>
        ///     the name was parsed from a data file manifest
        /// </summary>
        ManifestFile,

        /// <summary>
        ///     the name was extracted from manifest state
        /// </summary>
        ManifestState,
    }

    /// <summary>
    ///     single data file name entry to look for in Cosmos
    /// </summary>
    public class ManifestDataFile
    {
        /// <summary>
        ///     Initializes a new instance of the ManifestDataFile class
        /// </summary>
        /// <param name="rawName">raw name as found in the data file manifest</param>
        /// <param name="cosmosName">processed name that will be looked for in Cosmos</param>
        /// <param name="packageName">processed name that will be created in </param>
        public ManifestDataFile(
            string rawName, 
            string cosmosName, 
            string packageName)
        {
            this.CountFound = 1;

            this.PackageName = packageName;
            this.CosmosName = cosmosName;
            this.RawName = rawName;

            this.Source = DataFileSource.ManifestFile;
        }

        /// <summary>
        ///     Initializes a new instance of the ManifestDataFile class
        /// </summary>
        /// <param name="rawName">raw name as found in the data file manifest</param>
        /// <param name="cosmosName">processed name that will be looked for in Cosmos</param>
        /// <param name="packageName">processed name that will be created in</param>
        /// <param name="tag">data file tag</param>
        public ManifestDataFile(
            string rawName,
            string cosmosName,
            string packageName,
            string tag)
        {
            this.CountFound = 1;

            this.PackageName = packageName;
            this.CosmosName = cosmosName;
            this.RawName = rawName;

            this.Tag = tag;

            this.Source = DataFileSource.ManifestState;
        }

        /// <summary>
        ///     Gets the raw name as it appears in the manifest file
        /// </summary>
        public string RawName { get; }

        /// <summary>
        ///     Gets the name of the file as it would appear in Cosmos
        /// </summary>
        public string CosmosName { get; }

        /// <summary>
        ///     Gets the name of the file as it should appear in the command output
        /// </summary>
        public string PackageName { get; }

        /// <summary>
        ///     Gets the data file tag
        /// </summary>
        public string Tag { get; private set; }

        /// <summary>
        ///     Gets or sets the count of instaces of the same raw name found in the manifest
        /// </summary>
        public int CountFound { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether the file has invalid characters in the name
        /// </summary>
        public bool Invalid { get; set; }

        /// <summary>
        ///     Gets or sets the source of the information that resulted in the creation of this object
        /// </summary>
        public DataFileSource Source { get; set; }

        /// <summary>
        ///     Populates the tag field
        /// </summary>
        /// <param name="cosmosTag">cosmos tag</param>
        /// <param name="agentId">agent id</param>
        public void PopulateTag(
            string cosmosTag,
            string agentId)
        {
            this.Tag = Utility.GenerateFileTag(cosmosTag, agentId, this.CosmosName);
        }
    }
}
