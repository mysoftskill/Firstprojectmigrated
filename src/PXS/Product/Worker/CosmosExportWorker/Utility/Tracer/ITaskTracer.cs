// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility
{
    /// <summary>
    ///     contract of objects that provide tracing for worker tasks
    /// </summary>
    public interface ITaskTracer
    {
        /// <summary>
        ///     Emits an error trace
        /// </summary>
        /// <param name="format">format string</param>
        /// <param name="args">replacement arguments</param>
        void TraceError(
            string format,
            params object[] args);

        /// <summary>
        ///     Emits a error trace
        /// </summary>
        /// <param name="format">format string</param>
        /// <param name="args">replacement arguments</param>
        void TraceWarning(
            string format,
            params object[] args);

        /// <summary>
        ///     Emits an information trace
        /// </summary>
        /// <param name="format">format string</param>
        /// <param name="args">replacement arguments</param>
        void TraceInfo(
            string format,
            params object[] args);

        /// <summary>
        ///     Emits a warning trace
        /// </summary>
        /// <param name="format">format string</param>
        /// <param name="args">replacement arguments</param>
        void TraceVerbose(
            string format,
            params object[] args);
    }
}
