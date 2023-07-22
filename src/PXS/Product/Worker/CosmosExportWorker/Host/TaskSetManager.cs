// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks;
    using Microsoft.Membership.MemberServices.PrivacyHost;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     Implements the task manager to start and stop the worker's tasks
    /// </summary>
    public class TaskSetManager : HostDecorator
    {
        private readonly ICosmosExportAgentConfig appConfig;

        private readonly IDependencyManager resolver;

        private readonly ILogger logger;

        /// <summary>
        ///     Initializes a new instance of the TaskSetManager class
        /// </summary>
        /// <param name="config">task configuration</param>
        /// <param name="resolver">type resolver</param>
        /// <param name="logger">Geneva trace logger</param>
        public TaskSetManager(
            ICosmosExportAgentConfig config,
            IDependencyManager resolver,
            ILogger logger)
        {
            this.appConfig = config ?? throw new ArgumentNullException(nameof(config));
            this.resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        ///     Executes this instance
        /// </summary>
        /// <returns>resulting value</returns>
        public override ConsoleSpecialKey? Execute()
        {
            this.logger.Information(nameof(TaskSetManager), "Cosmos export worker task manager starting");
            
            try
            {
                ICollection<IBackgroundTask> tasks = this.appConfig.Tasks.Values.Select(this.CreateAndStartTask).ToList();

                ConsoleSpecialKey? result = base.Execute();

                Task.WhenAll(tasks.Select(o => o.StopAsync())).GetAwaiter().GetResult();

                return result;
            }
            finally
            {
                this.logger.Information(nameof(TaskSetManager), "Cosmos export worker task manager terminating");
            }
        }

        /// <summary>
        ///     Creates the task and starts it
        /// </summary>
        /// <param name="config">task config</param>
        /// <returns>resulting value</returns>
        private IBackgroundTask CreateAndStartTask(ITaskConfig config)
        {
            IBackgroundTask task = this.resolver.GetType<IBackgroundTask>(config.Tag);

            task.Start();

            return task;
        }
    }
}
