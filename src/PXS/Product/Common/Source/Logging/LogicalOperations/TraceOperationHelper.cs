// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations
{
    /// <summary>
    ///     Trace operation helper.
    /// </summary>
    public static class TraceOperationHelper
    {
        /// <summary>
        ///     Pop up operation result.
        /// </summary>
        /// <param name="isSuccess">Is success.</param>
        /// <param name="protocolStatusCode">Protocol status code.</param>
        /// <param name="resultSignature">Error code.</param>
        /// <param name="resultDetails">Error message.</param>
        /// <param name="traceOperation">Trace operation.</param>
        /// <param name="isIncomingCall">Indicates whether this is an incoming or outgoing call.</param>
        public static void PopupOperationResult(bool isSuccess, string protocolStatusCode, string resultSignature, string resultDetails, ITraceOperation traceOperation, bool isIncomingCall = false)
        {
            if (traceOperation == null)
            {
                return;
            }

            if (isSuccess)
            {
                traceOperation.SetResult(TraceResult.Success, resultSignature, resultDetails, isIncomingCall);
            }
            else
            {
                if (int.TryParse(protocolStatusCode, out int protocolStatusCodeInInt))
                {
                    traceOperation.SetResult(
                        protocolStatusCodeInInt > 499 ? TraceResult.Failure : TraceResult.ClientError,
                        resultSignature,
                        resultDetails,
                        isIncomingCall);
                }
                else
                {
                    traceOperation.SetResult(TraceResult.Failure, resultSignature, resultDetails, isIncomingCall);
                }
            }
        }
    }
}
