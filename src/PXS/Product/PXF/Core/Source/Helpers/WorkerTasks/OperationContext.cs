// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks
{
    using System.Collections.Generic;

    /// <summary>
    ///     worker task context
    /// </summary>
    public class OperationContext : OperationContextBasic
    {
        /// <summary>
        ///    Initializes a new instance of the OperationContext class
        /// </summary>
        /// <param name="taskId">task id</param>
        /// <param name="workerIndex">worker index</param>
        public OperationContext(
            string taskId,
            int workerIndex) :
            base(taskId)
        {
            this.WorkerIndex = workerIndex;
            this.Item = string.Empty;
            this.Op = string.Empty;
        }

        /// <summary>
        ///     Gets or sets the item context
        /// </summary>
        public string Item { get; set; }

        /// <summary>
        ///     Gets or sets the worker index
        /// </summary>
        public int WorkerIndex { get; }

    }
}
