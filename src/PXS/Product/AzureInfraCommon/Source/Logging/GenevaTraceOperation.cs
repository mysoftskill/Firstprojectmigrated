// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.AzureInfraCommon.Logging
{
    using System;

    using Microsoft.Cloud.InstrumentationFramework;
    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;

    /// <inheritdoc />
    /// <summary>
    ///     CMA web trace operation.
    /// </summary>
    public class GenevaTraceOperation : ITraceOperation
    {
        private readonly Operation operation;

        /// <summary>
        ///     Initializes a new instance of the <see cref="GenevaTraceOperation" /> class.
        /// </summary>
        /// <param name="eventName">Event name.</param>
        public GenevaTraceOperation(string eventName)
        {
            this.operation = new Operation(eventName);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public void SetResult(TraceResult traceResult, string resultSignature, string resultDetails, bool isIncomingCall)
        {
            // This will set the operation api type to incoming or outgoing.
            this.operation.ApiType = isIncomingCall ? OperationApiType.ServiceApi : OperationApiType.InternalCall;

            // This will set the result to the Geneva operation. But the logging will be done when the this.operation is disposed.
            this.operation.SetResult(traceResult.MapToOperationResult(), resultSignature, resultDetails);
        }

        /// <summary>
        ///     Dispose.
        /// </summary>
        /// <param name="disposing">Disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            // This is the place to do the actual logging.
            this.operation.Dispose();
        }
    }
}
