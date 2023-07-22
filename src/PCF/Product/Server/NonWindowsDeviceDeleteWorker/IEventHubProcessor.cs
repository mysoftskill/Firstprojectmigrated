// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Azure.ComplianceServices.NonWindowsDeviceDeleteWorker
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IEventHubProcessor
    {
        /// <summary>
        /// Send json event async.
        /// </summary>
        /// <param name="message">EventHub json event.</param>
        /// <returns></returns>
        Task SendAsync(string message);

        /// <summary>
        /// Run EventHub processor until canceled.
        /// </summary>
        /// <param name="eventHubProcessorHandler">EventHub processor handler</param>
        /// <param name="taskCancellationToken">Cancellation token</param>
        /// <returns></returns>
        Task RunAsync(IEventHubProcessorHandler eventHubProcessorHandler, CancellationToken taskCancellationToken);
    }
}
