// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks
{
    using System.Threading.Tasks;

    /// <summary>
    ///     contact for background task classes
    /// </summary>
    public interface IBackgroundTask
    {
        /// <summary>
        ///     Starts the task
        /// </summary>
        void Start();

        /// <summary>
        ///     Stops the task
        /// </summary>
        /// <returns>resulting value</returns>
        Task StopAsync();
    }
}
