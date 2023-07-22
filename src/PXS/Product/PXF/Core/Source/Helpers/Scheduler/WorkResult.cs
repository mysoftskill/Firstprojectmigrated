// -------------------------------------------------------------------------
// <copyright>
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Scheduler
{
    using Common.Logging;
    using System;

    /// <summary>
    /// Results of a 'run' from a worker
    /// </summary>
    public sealed class WorkResult
    {
        private static WorkResult successResult = new WorkResult { Success = true, WorkReady = true, };
        private static WorkResult successRunImmediateResult = new WorkResult { Success = true, WorkReady = true, RescheduleImmediate = true };
        private static WorkResult noWorkResult = new WorkResult { Success = true, WorkReady = false, };

        /// <summary>
        /// Pre-canned 'success' result.
        /// </summary>
        public static WorkResult Succeeded
        {
            get { return successResult; }
        }

        public static WorkResult SuccessRunImmediate
        {
            get { return successRunImmediateResult; }
        }

        /// <summary>
        /// Pre-canned 'not ready'. Use this response from CheckForWork, if there is no work to do.
        /// </summary>
        public static WorkResult NoWork
        {
            get { return noWorkResult; }
        }

        /// <summary>
        /// Creates a failure work result based on an exception.
        /// </summary>
        /// <param name="exception">Exception</param>
        /// <param name="workReady">work ready flag (defaults true)</param>
        /// <returns>WorkResult object</returns>
        public static WorkResult Failed(Exception exception, bool workReady = true)
        {
            var workResult = new WorkResult
            {
                Success = false,
                WorkReady = workReady,
                Exception = exception,
            };

            return workResult;
        }

        /// <summary>
        /// Creates a failure work result based on error details
        /// </summary>
        /// <param name="errorDetails">Error details</param>
        /// <param name="workReady">work ready flag (defaults true)</param>
        /// <returns>WorkResult object</returns>
        public static WorkResult Failed(ErrorDetails errorDetails, bool workReady = true)
        {
            var workResult = new WorkResult
            {
                Success = false,
                WorkReady = workReady,
                ErrorDetails = errorDetails,
            };

            return workResult;
        }

        /// <summary>
        /// Indicates if the work was a success or failure
        /// </summary>
        public bool Success { get; internal set; }

        public bool RescheduleImmediate { get; internal set; }

        /// <summary>
        /// Indicates if CheckForWork found available work or not
        /// </summary>
        public bool WorkReady { get; internal set; }

        /// <summary>
        /// Exception that stopped the work from completing
        /// </summary>
        public Exception Exception { get; internal set; }

        /// <summary>
        /// Error that stopped the work from completing
        /// </summary>
        public ErrorDetails ErrorDetails { get; internal set; }
    }
}
