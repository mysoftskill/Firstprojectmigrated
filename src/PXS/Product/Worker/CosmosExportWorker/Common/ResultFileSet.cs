// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.CosmosExport.Worker.Common
{
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem;

    /// <summary>
    ///    represents a single result file set
    /// </summary>
    public class ResultFileSet
    {
        /// <summary>
        ///     Initializes a new instance of the ResultFileSet class
        /// </summary>
        /// <param name="agentId">data agent id</param>
        /// <param name="requestManifest">request manifest</param>
        /// <param name="dataManifest">data manifest</param>
        public ResultFileSet(
            string agentId,
            IFile requestManifest,
            IFile dataManifest)
        {
            this.RequestManifest = requestManifest;
            this.DataManifest = dataManifest;
            this.AgentId = agentId;
        }

        /// <summary>
        ///     Gets or sets data agent identifier
        /// </summary>
        public string AgentId { get; set; }

        /// <summary>
        ///     Gets or sets request manifest
        /// </summary>
        public IFile RequestManifest { get; set; }

        /// <summary>
        ///     Gets or sets data manifest
        /// </summary>
        public IFile DataManifest { get; set; }
    }
}
