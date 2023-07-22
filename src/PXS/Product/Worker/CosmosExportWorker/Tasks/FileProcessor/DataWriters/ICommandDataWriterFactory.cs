// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Tasks
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    ///     contract for objects that create ExportWriters 
    /// </summary>
    public interface ICommandDataWriterFactory
    {
        /// <summary>
        ///     Creates the specified agent id
        /// </summary>
        /// <param name="canceler">cancel token</param>
        /// <param name="agentId">agent id</param>
        /// <param name="commandId">command id</param>
        /// <param name="fileName">file name to write to</param>
        /// <returns>resulting value</returns>
        Task<ICommandDataWriter> CreateAsync(
            CancellationToken canceler,
            string agentId,
            string commandId,
            string fileName);
    }
}
