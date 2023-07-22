// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///     OperationContextBasic class
    /// </summary>
    public class OperationContextBasic
    {
        private Stack<string> opStack;

        /// <summary>
        ///    Initializes a new instance of the OperationContextBasic class
        /// </summary>
        /// <param name="taskId">task id</param>
        public OperationContextBasic(string taskId)
        {
            this.TaskId = taskId;
        }

        /// <summary>
        ///     Gets or sets the operation context
        /// </summary>
        public string TaskId { get; }

        /// <summary>
        ///     Gets or sets the operation context
        /// </summary>
        public string Op { get; set; }

        /// <summary>
        ///      Pushes the current Op value so it can be recovered later
        /// </summary>
        public void PushOp()
        {
            this.opStack = this.opStack ?? new Stack<string>();
            this.opStack.Push(this.Op);
        }

        /// <summary>
        ///      Replaces the current Op value with the last pushed value (if any)
        /// </summary>
        public void PopOp()
        {
            if (this.opStack != null && this.opStack.Count > 0)
            {
                try
                {
                    this.Op = this.opStack.Pop();
                }
                catch (InvalidOperationException)
                {
                    // don't care if we accidentally pop more times than we pushed.
                }
            }
        }
    }
}
