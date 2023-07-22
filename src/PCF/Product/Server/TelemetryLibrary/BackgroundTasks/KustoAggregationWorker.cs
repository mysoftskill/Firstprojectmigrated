namespace Microsoft.PrivacyServices.CommandFeed.Service.Telemetry.BackgroundTasks
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Threading.Tasks;
    using Kusto.Data.Exceptions;
    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.ConfigGen;
    using Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache;
    using Microsoft.PrivacyServices.CommandFeed.Service.Telemetry.Repository;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// A periodic task that create queue depth calculation tasks in Azure Queue.
    /// </summary>
    [ExcludeFromCodeCoverage] // Justification: runs as a periodic background operation. Not really coverable.
    public class KustoAggregationWorker : WorkerTaskBase<object, KustoAggregationWorker.LockState>
    {
        private readonly TimeSpan frequency;

        /// <summary>
        /// Create background worker
        /// </summary>
        public KustoAggregationWorker(
            string taskName,
            ITaskConfiguration taskConfig,
            TimeSpan frequency)
            : base(taskConfig, taskName, null)
        {
            this.frequency = frequency;
        }

        /// <summary>
        /// Telemetry repository.
        /// </summary>
        public ITelemetryRepository TelemetryRepository { get; set; }

        /// <summary>
        /// Data agents.
        /// </summary>
        public IDataAgentMapFactory DataAgentMapFactory { get; set; }

        /// <summary>
        /// Create worker tasks
        /// </summary>
        protected override IEnumerable<Func<Task>> GetTasksAsync(
            LockState state,
            object parameters)
        {
            List<Func<Task>> tasks = new List<Func<Task>>();
            IDataAgentMap dataAgentMap = this.DataAgentMapFactory.GetDataAgentMap();

            foreach (var agentId in dataAgentMap.GetAgentIds())
            {
                tasks.Add(() => this.RunKustoAggregationAsync(agentId));
            }

            DualLogger.Instance.Information(nameof(AzureQueueCommandQueueDepthChecker), $"TaskName={this.TaskName} scheduled {tasks.Count} tasks.");

            return tasks;
        }

        /// <summary>
        /// Run Kusto aggregation query.
        /// </summary>
        public async Task RunKustoAggregationAsync(AgentId agentId)
        {
            DualLogger.Instance.Verbose(nameof(AzureQueueCommandQueueDepthChecker), $"AgentId={agentId}");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                await this.TelemetryRepository.AppendAgentStatAsync(agentId);
                await this.TelemetryRepository.InterpolateAgentStatAsync(agentId);
            }
            catch (Exception ex)
            {
                // unexpected exception
                DualLogger.Instance.Error(nameof(AzureQueueCommandQueueDepthChecker), ex, $"AgentId={agentId}");
            }
            finally
            {
                DualLogger.Instance.Information(nameof(AzureQueueCommandQueueDepthChecker), $"Elapsed={stopwatch.Elapsed}, AgentId={agentId}");
            }
        }

        protected override bool ShouldRun(LockState lockState)
        {
            DateTimeOffset nextTimeToRun = lockState?.NextStartTime ?? DateTimeOffset.MinValue;
            DualLogger.Instance.Information(nameof(AzureQueueCommandQueueDepthChecker), $"TaskName={this.TaskName}, NextTimeToRun={nextTimeToRun.ToString("o")}");

            return nextTimeToRun <= DateTimeOffset.UtcNow;
        }

        protected override LockState GetFinalLockState(out TimeSpan leaseTime)
        {
            leaseTime = this.frequency;
            return new LockState { NextStartTime = DateTimeOffset.UtcNow.Add(leaseTime) };
        }

        // State stored along with the lock.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public class LockState
        {
            public DateTimeOffset NextStartTime { get; set; }
        }
    }
}
