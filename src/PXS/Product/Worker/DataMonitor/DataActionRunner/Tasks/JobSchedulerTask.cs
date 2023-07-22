// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.PrivacyServices.DataMonitor.Runner.Tasks
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.Exceptions;
    using Microsoft.PrivacyServices.DataMonitor.Runner.Data;
    using Microsoft.PrivacyServices.DataMonitor.Runner.Storage;

    /// <summary>
    ///     task to periodically queue work items for processing
    /// </summary>
    public class JobSchedulerTask : TrackCountersBaseTask<IDataActionJobSchedulerConfig>
    {
        private readonly IActionManagerFactory actionMgrFactory;

        private readonly IContextFactory parseCtxFactory;

        private readonly TimeSpan runFreq;

        private readonly IQueue<JobWorkItem> workerQueue;

        private readonly IAppConfiguration appConfiguration;

        /// <summary>
        ///     Initializes a new instance of the JobSchedulerTask class
        /// </summary>
        /// <param name="config">task config</param>
        /// <param name="actionManagerFactory">action manager factory</param>
        /// <param name="workerQueue">worker queue</param>
        /// <param name="parseCtxFactory">parse context factory</param>
        /// <param name="counterFactory">perf counter factory</param>
        /// <param name="logger">Geneva trace logger</param>
        /// <param name="appConfiguration">Azure App configuration isntance</param>
        public JobSchedulerTask(
            IDataActionJobSchedulerConfig config,
            IActionManagerFactory actionManagerFactory,
            IQueue<JobWorkItem> workerQueue,
            IContextFactory parseCtxFactory,
            ICounterFactory counterFactory,
            ILogger logger,
            IAppConfiguration appConfiguration)
            :
            base(config, counterFactory, logger)
        {
            this.actionMgrFactory = actionManagerFactory ?? throw new ArgumentNullException(nameof(actionManagerFactory));
            this.parseCtxFactory = parseCtxFactory ?? throw new ArgumentNullException(nameof(parseCtxFactory));
            this.workerQueue = workerQueue ?? throw new ArgumentNullException(nameof(workerQueue));
            this.appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));

            this.runFreq = TimeSpan.FromSeconds(config.RunFrequencySeconds);
        }

        /// <summary>
        ///     Starts up the set of tasks used by the task to execute work
        /// </summary>
        /// <param name="ctx">operation context</param>
        /// <returns>resulting value</returns>
        protected override async Task<TimeSpan?> RunOnceAsync(OperationContext ctx)
        {
            this.TraceInfo("Initializing action manager");

            ctx.Item = "LoadAndScheduleActions";

            try
            {
                if (!appConfiguration.GetConfigValue(ConfigNames.PXS.DataActionRunner_EnableJobScheduler, defaultValue: true))
                {
                    return this.runFreq;
                }

                IActionAccessor accessor = this.actionMgrFactory.CreateStoreManager();
                IParseContext parseContext = this.parseCtxFactory.Create<IParseContext>(this.TaskCounterCategory);
                ICounter parseAttemptsCounter = this.GetCounter("Action Parse Attempts", CounterType.Number);

                parseAttemptsCounter.Increment();

                ctx.Op = "ActionTemplateLoad";

                parseContext.OnActionStart(ActionType.Parse, ctx.Op);

                await accessor.InitializeAndRetrieveActionsAsync(parseContext, false).ConfigureAwait(false);

                parseContext.OnActionEnd();

                ctx.Op = "ReportingParseResults";

                if (parseContext.HasErrors)
                {
                    ICounter parseErrorCounter = this.GetCounter("Action Parse Errors", CounterType.Number);

                    parseErrorCounter.Increment();

                    this.TraceError(parseContext.GetLogs(EntryTypes.All));

                    string errorDetails = "Failed to parse actions: " + parseContext.GetLogs(EntryTypes.Error);
                    ErrorEvent errorEvent = new ErrorEvent
                    {
                        ComponentName = nameof(JobSchedulerTask),
                        ErrorMethod = nameof(this.RunOnceAsync),
                        ErrorCode = "FailedToParseActions",
                        ErrorDetails = errorDetails,
                        ErrorType = "Parsing"
                    };
                    errorEvent.LogError();

                    throw new ActionParseException(errorDetails);
                }
                else if (this.Config.ForceVerboseLogOnSuccess)
                {
                    this.TraceInfo(parseContext.GetLogs(EntryTypes.All));
                }
                else
                {
                    this.TraceInfo(parseContext.GetLogs(EntryTypes.Title));
                }

                ctx.Op = "EnqueueActions";

                parseContext.OnActionStart(ActionType.Parse, ctx.Op);

                await accessor.EnqueueActionsToExecuteAsync(this.workerQueue, this.CancelToken).ConfigureAwait(false);

                parseContext.OnActionEnd();
            }
            finally
            {
                this.TraceInfo("Completed pass to fetch from store");
            }

            return this.runFreq;
        }
    }
}
