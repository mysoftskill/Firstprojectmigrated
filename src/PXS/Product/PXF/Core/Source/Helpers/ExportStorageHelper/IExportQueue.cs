// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.ExportStorageHelper
{
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.DataContracts.ExportTypes;

    /// <summary>
    ///     Queuing mechanism which schedules export data tasks and cosmos reader tasks and retries them until they expire or are complete
    ///     This class is a singleton shared across thread and can be acquired via the Export Storage Provider
    /// </summary>
    public interface IExportQueue
    {
        /// <summary>
        ///     Add a Message to the Export Queue
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="visibilityDelay"></param>
        /// <returns></returns>
        Task AddMessageAsync(BaseQueueMessage msg, System.TimeSpan? visibilityDelay = null);

        /// <summary>
        ///     Complete the message
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        Task CompleteMessageAsync(BaseQueueMessage msg);

        /// <summary>
        ///     Get a Message from the Queue
        /// </summary>
        /// <returns></returns>
        Task<BaseQueueMessage> GetMessageAsync();

        /// <summary>
        ///     See the next message from the queue without changing its state
        /// </summary>
        /// <returns></returns>
        Task<BaseQueueMessage> PeekMessageAsync();
    }
}
