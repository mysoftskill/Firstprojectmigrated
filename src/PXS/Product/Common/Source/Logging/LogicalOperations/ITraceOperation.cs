// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations
{
    using System;

    /// <summary>
    ///     Operation for tracing.
    /// </summary>
    public interface ITraceOperation : IDisposable
    {
        /// <summary>
        ///     Set trace operation result.
        /// </summary>
        /// <param name="traceResult">Trace result.</param>
        /// <param name="resultSignature">Result signature.</param>
        /// <param name="resultDetails">Result details.</param>
        /// <param name="isIncomingCall">Indicates whether this is incoming or outgoing call.</param>
        void SetResult(TraceResult traceResult, string resultSignature, string resultDetails, bool isIncomingCall);
    }
}
