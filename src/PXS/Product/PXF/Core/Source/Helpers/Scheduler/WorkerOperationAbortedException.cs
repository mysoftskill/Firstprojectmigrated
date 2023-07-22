// -------------------------------------------------------------------------
// <copyright>
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Scheduler
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public sealed class WorkerOperationAbortedException : Exception
    {
        public WorkerOperationAbortedException()
        {
        }

        public WorkerOperationAbortedException(string message)
            : base(message)
        {
        }

        public WorkerOperationAbortedException(string message, Exception exception)
            : base(message, exception)
        {
        }

        private WorkerOperationAbortedException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        {
        }
    }
}
