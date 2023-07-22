// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.Worker
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    ///     An interface representing a worker.
    /// </summary>
    public interface IWorker
    {
        /// <summary>
        ///     Starts the worker.
        /// </summary>
        void Start();

        /// <summary>
        ///     Starts the worker.
        /// </summary>
        /// <param name="delay">
        ///     The time to wait before trying to complete more work
        ///     when there is currently no work to complete.
        /// </param>
        void Start(TimeSpan delay);

        /// <summary>
        ///     Stops the worker.
        /// </summary>
        /// <returns>A task that stops the worker.</returns>
        Task StopAsync();
    }
}
