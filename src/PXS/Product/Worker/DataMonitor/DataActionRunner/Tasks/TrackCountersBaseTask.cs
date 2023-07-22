// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.Runner.Tasks
{
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks;
    using Microsoft.PrivacyServices.DataMonitor.Runner.Utility;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     base class that emits the counter category
    /// </summary>
    public abstract class TrackCountersBaseTask<TIConfig> : MultiInstanceTask<TIConfig>
        where TIConfig : class, ITaskConfig
    {
        /// <summary>
        ///     Initializes a new instance of the TrackCountersBaseTask class
        /// </summary>
        /// <param name="config">task config</param>
        /// <param name="counterFactory">performance counter factory</param>
        /// <param name="logger">Geneva trace logger</param>
        protected TrackCountersBaseTask(
            TIConfig config, 
            ICounterFactory counterFactory, 
            ILogger logger) : 
            base(config, counterFactory, logger)
        {
        }

        /// <summary>
        ///     Gets the task error counter category
        /// </summary>
        protected override string TaskCounterCategory => Constants.CounterCategory;

        /// <summary>
        ///      Gets the counter
        /// </summary>
        /// <param name="name">name</param>
        /// <param name="type">type</param>
        /// <returns>resulting value</returns>
        protected ICounter GetCounter(
            string name,
            CounterType type)
        {
            return this.CounterFactory.GetCounter(this.TaskCounterCategory, name, type);
        }
        /// <summary>
        ///      Gets the counter
        /// </summary>
        /// <param name="name">name</param>
        /// <returns>resulting value</returns>
        protected ICounter GetCounter(string name)
        {
            return this.CounterFactory.GetCounter(this.TaskCounterCategory, name, CounterType.Number);
        }
    }
}
