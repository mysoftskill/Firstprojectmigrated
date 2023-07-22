// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.ScheduleDbClient.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Thrown when there was a problem while performing an operation on Schedule Db
    /// </summary>
    [Serializable]
    public class ScheduleDbClientException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduleDbClientException"/> class.
        /// </summary>
        public ScheduleDbClientException()
            : this("There was a problem performing an operation with ScheduleDb.")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduleDbClientException"/> class.
        /// </summary>
        public ScheduleDbClientException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduleDbClientException"/> class.
        /// </summary>
        public ScheduleDbClientException(string component, string message, Exception innerException)
            : base($"The scheduleDb operation in ({component}) failed . {message})", innerException)
        {
        }
    }
}
