// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations
{
    /// <summary>
    ///     Trace result.
    /// </summary>
    public enum TraceResult
    {
        /// <summary>
        ///     Success.
        /// </summary>
        Success,

        /// <summary>
        ///     Client error.
        /// </summary>
        ClientError,

        /// <summary>
        ///     Failure.
        /// </summary>
        Failure,

        /// <summary>
        ///     Timeout.
        /// </summary>
        Timeout
    }
}
