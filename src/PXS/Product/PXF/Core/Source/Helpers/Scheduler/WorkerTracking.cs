// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Scheduler
{
    using System;

    /// <summary>
    ///     WorkerTracking keeps track if work is started or completed.
    ///     This keeps a single status record to allow for worker machines to only allow a single machine to do work at a time.
    /// </summary>
    public class WorkerTracking : TableEntityBase
    {
        public const string RowQualifier = "WorkItemName";

        public const string WorkCompletedProperty = "WorkCompleted";

        public const string WorkerMachineNameProperty = "WorkerMachineName";

        public const string WorkStartedTimeProperty = "WorkStartedTime";

        public static readonly string EnvironmentMachineName = Environment.MachineName;

        /// <summary>
        ///     True if the work has been completed
        /// </summary>
        public bool? WorkCompleted
        {
            get { return this.Entity.GetBool(WorkCompletedProperty); }
            set { this.Entity.Set(WorkCompletedProperty, value); }
        }

        /// <summary>
        ///     The worker machine name, used to know who updated the worker tracking table.
        /// </summary>
        public string WorkerMachineName
        {
            get { return this.Entity.GetString(WorkerMachineNameProperty); }
            set { this.Entity.Set(WorkerMachineNameProperty, value); }
        }

        /// <summary>
        ///     Work item name
        /// </summary>
        public string WorkItemName
        {
            get { return this.Entity.GetPartitionKeyString(); }
            set
            {
                this.Entity.SetPartitionKey(value);
                this.Entity.SetQualifiedRowKey(RowQualifier, value);
            }
        }

        /// <summary>
        ///     Time the last worker started the work
        /// </summary>
        public DateTimeOffset? WorkStartedTime
        {
            get { return this.Entity.GetDateTimeOffset(WorkStartedTimeProperty); }
            set { this.Entity.Set(WorkStartedTimeProperty, value); }
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="workItemName">Work item name</param>
        public WorkerTracking(string workItemName)
            : this()
        {
            if (string.IsNullOrWhiteSpace(workItemName))
            {
                throw new ArgumentNullException(nameof(workItemName));
            }

            this.WorkItemName = workItemName;
        }

        /// <summary>
        ///     Creates a new instance of <see cref="WorkerTracking" />
        /// </summary>
        public WorkerTracking()
        {
            this.WorkerMachineName = EnvironmentMachineName;
        }
    }
}
