// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Tasks
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     monitors PCF to obtain leases and mark signals as complete
    /// </summary>
    public class CommandMonitor : MultiInstanceTask<ICommandMonitorConfig>
    {
        private readonly ICommandObjectFactory factory;

        /// <summary>
        ///     Initializes a new instance of the CommandMonitor class
        /// </summary>
        /// <param name="commandFactory">privacy command object factory</param>
        /// <param name="config">task config</param>
        /// <param name="counterFactory">performance counter factory</param>
        /// <param name="logger">Geneva trace logger</param>
        public CommandMonitor(
            ICommandObjectFactory commandFactory,
            ICommandMonitorConfig config,
            ICounterFactory counterFactory,
            ILogger logger) :
            base(config, counterFactory, logger)
        {
            this.factory = commandFactory ?? throw new ArgumentNullException(nameof(commandFactory));
        }

        /// <summary>
        ///     Runs the task
        /// </summary>
        /// <param name="ctx">operation context</param>
        /// <returns>resulting value</returns>
        protected override async Task<TimeSpan?> RunOnceAsync(OperationContext ctx)
        {
            ICommandReceiver recevier = this.factory.CreateCommandReceiver(ctx.TaskId);

            ctx.Item = "PCF client";
            ctx.Op = "running PCF client library";

            this.TraceInfo(
                "Starting command feed client to receive commands [AgentId: {0}][Auth mode: {1}][Environment: {2}]",
                this.Config.AgentId,
                this.Config.AuthMode,
                this.Config.StockEndpointType);

            await recevier.BeginReceivingAsync(this.CancelToken).ConfigureAwait(false);

            return null;
        }
    }
}
