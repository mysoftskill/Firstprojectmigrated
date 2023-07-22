// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.Context
{
    using System;
    using System.Threading;

    /// <summary>
    ///     contract for contexts used in action execute operations
    /// </summary>
    public interface IExecuteContext : IContext
    {
        /// <summary>
        ///     Gets the cancellation token
        /// </summary>
        CancellationToken CancellationToken { get; }

        /// <summary>
        ///     Gets the time that the action started processing at
        /// </summary>
        DateTimeOffset OperationStartTime { get; }

        /// <summary>
        ///     Gets the current time (UTC)
        /// </summary>
        DateTimeOffset NowUtc { get; }

        /// <summary>
        ///     Gets the current duration of the operation
        /// </summary>
        TimeSpan Duration { get; }

        /// <summary>
        ///     Gets a value indicating whether this instance is a simulation
        /// </summary>
        /// <remarks>
        ///     Simulations do not perform actions that can trigger calls, send email, etc, but do perform read-only
        ///      actions (such as read only database queries)     
        /// </remarks>
        bool IsSimulation { get; }

        /// <summary>
        ///     Updates the most recent tag set by OnExecuting
        /// </summary>
        /// <param name="tag">updated tag</param>
        void OnActionUpdate(string tag);
    }
}
