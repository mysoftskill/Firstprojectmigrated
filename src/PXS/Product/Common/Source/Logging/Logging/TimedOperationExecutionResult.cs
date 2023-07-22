//--------------------------------------------------------------------------------
// <copyright file="TimedOperationExecutionResult.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Common.Logging
{
    using System;

    /// <summary>
    /// Stores latency and result of a delegate execution.
    /// </summary>
    public class TimedOperationExecutionResult
    {
        /// <summary>
        /// Gets or sets the value of any exception that happend during execution.
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// Gets or sets the execution duration.
        /// </summary>
        public ulong LatencyInMilliseconds { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether <see cref="TimedOperationExecutionResult.Exception"/> is null.
        /// </summary>
        public virtual bool IsSuccess
        {
            get { return this.Exception == null; }
        }
    }

    /// <summary>
    /// The timed-operation execution result.
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    //// REVISIT(nnaemeka): is IDisposable needed?
    public class TimedOperationExecutionResult<TResult> : TimedOperationExecutionResult, IDisposable
    {
        /// <summary>
        /// Determines <see cref="Dispose(bool)"/> has already been called.
        /// </summary>
        private bool disposed = false;

        /// <summary>
        /// Gets or sets the response.
        /// </summary>
        public TResult Response { get; set; }

        /// <summary>
        /// Implement <see cref="IDisposable"/>; dispose resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose underlying AP resources.
        /// </summary>
        /// <param name="disposing">true if called from <see cref="System.IDisposable"/>; otherwise false.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            if (disposing)
            {
                //// REVISIT(nnaemeka): comment needed -- why is it the responsibility of TimedOperationExecutionResult to dispose a get/set property that it didn't create?
                var disposableResponse = this.Response as IDisposable;
                if (disposableResponse != null)
                {
                    disposableResponse.Dispose();
                }

                this.Response = default(TResult);
            }

            this.disposed = true;
        }
    }
}
