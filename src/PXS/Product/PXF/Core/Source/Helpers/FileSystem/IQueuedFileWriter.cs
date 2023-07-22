// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

using System.Threading.Tasks;

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem
{
    using System.Threading;

    /// <summary>
    ///     contract for module that allow for background writing to a store
    /// </summary>
    public interface IQueuedFileWriter
    {
        /// <summary>
        ///     queues data to be written to the store
        /// </summary>
        /// <param name="data">data to queue for writing</param>
        /// <returns>resulting value</returns>
        Task QueueWriteAsync(string data);

        /// <summary>
        ///     sends the pending data to the store
        /// </summary>
        /// <returns>resulting value</returns>
        Task FlushQueueAsync(CancellationToken cancelToken);
    }
}
