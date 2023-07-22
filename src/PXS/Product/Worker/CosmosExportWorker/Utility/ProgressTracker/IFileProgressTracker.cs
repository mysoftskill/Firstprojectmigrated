// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility
{
    using System.Threading.Tasks;

    /// <summary>
    ///     contract for objects implementing a file progress tracker
    /// </summary>
    public interface IFileProgressTracker
    {
        /// <summary>
        ///     Adds the message to the internal message buffer
        /// </summary>
        /// <param name="type">message type</param>
        /// <param name="format">message format string</param>
        /// <param name="args">message replacement parameters</param>
        void AddMessage(
            string type,
            string format,
            params object[] args);

        /// <summary>
        ///     Persists the internal message buffer to storage
        /// </summary>
        /// <returns>resulting value</returns>
        Task PersistAsync();
    }
}
