// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Tasks
{
    using Microsoft.PrivacyServices.CommandFeed.Client;

    /// <summary>
    ///     contract for objects used to create export pipelines
    /// </summary>
    public interface ICosmosExportPipelineFactory
    {
        /// <summary>
        ///      Creates the specified command id
        /// </summary>
        /// <param name="commandId">command id</param>
        /// <param name="command">export command</param>
        /// <returns>resulting value</returns>
        IExportPipeline Create(
            string commandId,
            IExportCommand command);
    }
}
