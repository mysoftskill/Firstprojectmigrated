// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.Worker
{
    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;

    /// <summary>
    ///     An abstract class representing a background worker.
    /// </summary>
    public abstract class BackgroundWorker : IWorker
    {
        /// <summary>
        ///     A value indicating whether the background
        ///     worker is running.
        /// </summary>
        protected bool isRunning;

        /// <summary>
        ///     The worker task.
        /// </summary>
        protected ConfiguredTaskAwaitable workerTask;

        /// <summary>
        ///     Starts the worker in the background.
        /// </summary>
        public virtual void Start()
        {
            this.isRunning = true;

            // Starts the task in the background on a ThreadPool thread.
            this.workerTask = Task.Run(
                async () =>
                {
                    while (this.isRunning)
                    {
                        await this.DoWorkAsync().ConfigureAwait(false);
                    }
                }).ConfigureAwait(false);
        }

        /// <summary>
        ///     Starts the worker in the background.
        /// </summary>
        /// <param name="delay">
        ///     The time to wait before trying to complete more work
        ///     when there is currently no work to complete.
        /// </param>
        public virtual void Start(TimeSpan delay)
        {
            this.isRunning = true;

            // Starts the task in the background on a ThreadPool thread.
            this.workerTask = Task.Run(
                async () =>
                {
                    while (this.isRunning)
                    {
                        if (!await this.DoWorkAsync().ConfigureAwait(false))
                        {
                            await Task.Delay(delay).ConfigureAwait(false);
                        }
                    }
                }).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public virtual async Task StopAsync()
        {
            this.isRunning = false;
            await this.workerTask;
        }

        /// <summary>
        ///     Does the work.
        /// </summary>
        /// <returns>A task that does the work.</returns>
        public abstract Task<bool> DoWorkAsync();

        /// <summary>
        ///     
        /// </summary>
        /// <param name="apiEvent"></param>
        /// <param name="actionFunc"></param>
        /// <returns></returns>
        public virtual async Task<bool> DoWorkInstrumentedAsync(OutgoingApiEventWrapper apiEvent, Func<OutgoingApiEventWrapper, Task<bool>> actionFunc)
        {
            bool isWorkDone = false;
            try
            {
                apiEvent.Start();
                isWorkDone = await actionFunc(apiEvent).ConfigureAwait(false);
                apiEvent.Success = true;
            }
            catch (Exception ex)
            {
                apiEvent.Success = false;
                isWorkDone = false;
                apiEvent.ExceptionTypeName = ex.GetType().Name;
                apiEvent.ErrorMessage = ex.ToString();
                apiEvent.RequestStatus = Ms.Qos.ServiceRequestStatus.ServiceError;
            }
            finally
            {
                apiEvent.ExtraData["IsWorkDone"] = isWorkDone.ToString();
                apiEvent.Finish();
            }

            return isWorkDone;
        }
    }
}
