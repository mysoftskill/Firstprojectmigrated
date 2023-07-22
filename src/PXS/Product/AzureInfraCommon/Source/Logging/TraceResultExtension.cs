// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.AzureInfraCommon.Logging
{
    using System;

    using Microsoft.Cloud.InstrumentationFramework;
    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;

    /// <summary>
    ///     Trace result extension.
    /// </summary>
    public static class TraceResultExtension
    {
        /// <summary>
        ///     Map to operation result.
        /// </summary>
        /// <param name="traceResult">Trace result.</param>
        /// <returns>Operation result.</returns>
        public static OperationResult MapToOperationResult(this TraceResult traceResult)
        {
            switch (traceResult)
            {
                case TraceResult.Success:
                    return OperationResult.Success;
                case TraceResult.ClientError:
                    return OperationResult.ClientError;
                case TraceResult.Failure:
                    return OperationResult.Failure;
                case TraceResult.Timeout:
                    return OperationResult.Timeout;
                default:
                    throw new ArgumentOutOfRangeException(nameof(traceResult), traceResult, null);
            }
        }
    }
}
