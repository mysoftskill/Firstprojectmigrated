// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.Runner.Data
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Data;
    using Microsoft.PrivacyServices.DataMonitor.Runner.Storage;

    /// <summary>
    ///     job work item
    /// </summary>
    public class JobWorkItem
    {
        private readonly ActionRefRunnable actionRef;

        /// <summary>
        ///     Initializes a new instance of the JobWorkItem class
        /// </summary>
        /// <param name="executor">action executor</param>
        /// <param name="actionRef">action reference</param>
        /// <param name="extensionProps">extension props</param>
        public JobWorkItem(
            IActionExecutor executor,
            ActionRefRunnable actionRef,
            IDictionary<string, IDictionary<string, string>> extensionProps)
        {
            this.actionRef = actionRef ?? throw new ArgumentNullException(nameof(actionRef));
            this.Executor = executor ?? throw new ArgumentNullException(nameof(executor));

            this.ExtensionProperties = 
                extensionProps ?? 
                new ReadOnlyDictionary<string, IDictionary<string, string>>(new Dictionary<string, IDictionary<string, string>>());
        }

        /// <summary>
        ///     Gets extension properties
        /// </summary>
        public IDictionary<string, IDictionary<string, string>> ExtensionProperties { get; }

        /// <summary>
        ///     Gets the action store
        /// </summary>
        public IActionExecutor Executor { get; }

        /// <summary>
        ///     Gets a reference to the action to execute
        /// </summary>
        public ActionRef ActionRef => this.actionRef;

        /// <summary>
        ///     Gets the task lease time
        /// </summary>
        public TimeSpan TaskLeaseTime => this.actionRef.MaxRuntime;

        /// <summary>
        ///     Gets the lock tag for this class
        /// </summary>
        public string RefId => this.actionRef.Id;

        /// <summary>
        ///     Gets a value indicating to perform verbose logging or not
        /// </summary>
        public bool EmitVerboseLogging => this.actionRef.EmitVerboseLogging;

        /// <summary>
        ///     Gets a value indicating whether this job should be run as a simulation
        /// </summary>
        public bool IsSimulation => this.actionRef.IsSimulation;
    }
}
