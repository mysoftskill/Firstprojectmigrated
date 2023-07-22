// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <inheritdoc />
    /// <summary>
    ///     Tracer.
    /// </summary>
    public class Tracer : ITraceOperation
    {
        private readonly List<ITraceOperation> traceOperations;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Tracer" /> class.
        /// </summary>
        /// <param name="eventName">Event name.</param>
        public Tracer(string eventName)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                throw new ArgumentNullException(nameof(eventName));
            }

            this.traceOperations = TraceManager.GetTraceOperations(eventName).ToList();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public void SetResult(TraceResult traceResult, string resultSignature, string resultDetails, bool isIncomingCall = false)
        {
            this.traceOperations.ForEach(o => o.SetResult(traceResult, resultSignature, resultDetails, isIncomingCall));
        }

        /// <summary>
        ///     Dispose.
        /// </summary>
        /// <param name="disposing">Disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing ||
                this.traceOperations == null)
            {
                return;
            }

            foreach (ITraceOperation traceOperation in this.traceOperations)
            {
                traceOperation.Dispose();
            }
        }
    }
}
