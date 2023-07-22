// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.CosmosExport.Worker.Common
{
    /// <summary>
    ///    represents a single result data file
    /// </summary>
    public class ResultFile
    {
        /// <summary>
        ///     Initializes a new instance of the ResultFile class
        /// </summary>
        /// <param name="dataFilePath">data file</param>
        /// <param name="agentId">data agent identifier</param>
        public ResultFile(
            string dataFilePath,
            string agentId)
        {
            this.DataFilePath = dataFilePath;
            this.AgentId = agentId;
        }
        
        /// <summary>
        ///     Gets or sets the data file path
        /// </summary>
        public string DataFilePath { get; }

        /// <summary>
        ///     Gets or sets data agent identifier
        /// </summary>
        public string AgentId { get; }
    }
}
