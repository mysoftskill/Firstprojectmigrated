// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///     Trace manager.
    /// </summary>
    public class TraceManager
    {
        private static Func<string, IEnumerable<ITraceOperation>> getTraceOperations = input => new List<ITraceOperation>();

        /// <summary>
        ///     Get trace operations for create and dispose.
        /// </summary>
        /// <value><c>getTraceOperations</c>.</value>
        public static Func<string, IEnumerable<ITraceOperation>> GetTraceOperations
        {
            set { getTraceOperations = value ?? throw new ArgumentNullException(nameof(GetTraceOperations)); }

            internal get { return getTraceOperations; }
        }
    }
}
