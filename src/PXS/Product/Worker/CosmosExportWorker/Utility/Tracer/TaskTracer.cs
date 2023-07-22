// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility.Tracer
{
    using System;

    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks;

    /// <summary>
    ///     provides tracing for worker tasks
    /// </summary>
    public class TaskTracer : ITaskTracer
    {
        private readonly Action<string, object[]> traceVerbose;
        private readonly Action<string, object[]> traceWarn;
        private readonly Action<string, object[]> traceInfo;
        private readonly Action<string, object[]> traceErr;

        /// <summary>
        ///     Initializes a new instance of the TaskTracer class
        /// </summary>
        /// <param name="traceVerbose">trace verbose</param>
        /// <param name="traceWarn">trace warn</param>
        /// <param name="traceInfo">trace information</param>
        /// <param name="traceErr">trace error</param>
        public TaskTracer(
            Action<string, object[]> traceVerbose = null, 
            Action<string, object[]> traceWarn = null, 
            Action<string, object[]> traceInfo = null, 
            Action<string, object[]> traceErr = null)
        {
            this.traceVerbose = traceVerbose;
            this.traceWarn = traceWarn;
            this.traceInfo = traceInfo;
            this.traceErr = traceErr;
        }

        /// <summary>
        ///     Emits an error trace
        /// </summary>
        /// <param name="format">format string</param>
        /// <param name="args">replacement arguments</param>
        public void TraceError(
            string format, 
            params object[] args)
        {
            this.traceErr?.Invoke(format, args);
        }

        /// <summary>
        ///     Emits a error trace
        /// </summary>
        /// <param name="format">format string</param>
        /// <param name="args">replacement arguments</param>
        public void TraceWarning(
            string format, 
            params object[] args)
        {
            this.traceWarn?.Invoke(format, args);
        }

        /// <summary>
        ///     Emits an information trace
        /// </summary>
        /// <param name="format">format string</param>
        /// <param name="args">replacement arguments</param>
        public void TraceInfo(
            string format, 
            params object[] args)
        {
            this.traceInfo?.Invoke(format, args);
        }

        /// <summary>
        ///     Emits a warning trace
        /// </summary>
        /// <param name="format">format string</param>
        /// <param name="args">replacement arguments</param>
        public void TraceVerbose(
            string format, 
            params object[] args)
        {
            this.traceVerbose?.Invoke(format, args);
        }
    }
}
