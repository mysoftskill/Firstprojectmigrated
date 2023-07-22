// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.PrivacyVsoWorker.Processor
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Worker;
    using Microsoft.Membership.MemberServices.Privacy.PrivacyVsoWorker.Utility;

    using Newtonsoft.Json.Linq;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <inheritdoc />
    /// <summary>
    ///     ItemProcessor
    /// </summary>
    public class ItemProcessor : BackgroundWorker
    {
        private const string ClassName = nameof(ItemProcessor);

        private readonly ICounterFactory counterFactory;

        private readonly IFileSystemProcessor fileSystemProcessor;

        private readonly short intervalInDays;

        private readonly IKustoDataHelper kustoDataHelper;

        private readonly ILogger logger;

        private readonly IVsoHelper vsoHelper;

        /// <summary>
        ///     Creates a new instance of <see cref="ItemProcessor" />
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="counterFactory">The counter factory for making perf counters</param>
        /// <param name="kustoDataHelper">Kusto data helper</param>
        /// <param name="fileSystemProcessor">File system processor</param>
        /// <param name="vsoHelper">Helper to invoke VSO</param>
        /// <param name="intervalInDays">Interval in Days to execute processor</param>
        public ItemProcessor(
            ILogger logger,
            ICounterFactory counterFactory,
            IKustoDataHelper kustoDataHelper,
            IFileSystemProcessor fileSystemProcessor,
            IVsoHelper vsoHelper,
            short intervalInDays)
        {
            if (intervalInDays <= 0)
                throw new ArgumentOutOfRangeException(nameof(intervalInDays));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.counterFactory = counterFactory ?? throw new ArgumentNullException(nameof(counterFactory));
            this.kustoDataHelper = kustoDataHelper ?? throw new ArgumentNullException(nameof(kustoDataHelper));
            this.fileSystemProcessor = fileSystemProcessor ?? throw new ArgumentNullException(nameof(fileSystemProcessor));
            this.vsoHelper = vsoHelper ?? throw new ArgumentNullException(nameof(vsoHelper));
            this.intervalInDays = intervalInDays;
        }

        /// <summary>
        ///     Process all the agents with no ICM connectors
        /// </summary>
        /// <returns>
        ///     <c>true</c> if there were events to process, otherwise <c>false</c>
        /// </returns>
        public override async Task<bool> DoWorkAsync()
        {
            this.DoLog(nameof(ItemProcessor), "Doing work.");

            try
            {
                List<Agent> listOfAgents = this.TransformAgentsGcEmail(await this.kustoDataHelper.GetAgentsWithNoConnectorIdAsync().ConfigureAwait(false));
                this.DoLog(nameof(ItemProcessor), $"No. of items to process {listOfAgents.Count}");
                IncrementCounter(this.counterFactory, "Agents With No ICM Connector", 1);

                // Avoid creating more than 25 work items.
                // This would avoid dumping too many items into Vso, if Kusto Query is return way too many records
                int count = 0;
                
                // Process all agents with no ICM Connector ID and create work items if not already present
                foreach (Agent agent in listOfAgents)
                {
                    if (count < 25)
                    {
                        JObject workItem = await this.vsoHelper.CreateVsoWorkItemIfNotExistsAsync(agent).ConfigureAwait(false);

                        if (workItem == null)
                        {
                            this.DoLog(
                                nameof(ItemProcessor),
                                $"Item already exist for agent id: {agent.AgentId}");
                        }
                        else
                        {
                            count++;

                            this.DoLog(
                                nameof(ItemProcessor),
                                $"Successfully Created work item for agent id: {agent.AgentId}");
                            IncrementCounter(this.counterFactory, "Work Item Created", 1);
                        }
                    }
                    else
                    {
                        this.DoLog(
                            nameof(ItemProcessor), 
                            $"Skipping work due to high expected volume: {listOfAgents.Count}");
                        break;
                    }
                }

                this.DoLog(
                    nameof(ItemProcessor),
                    $"Next schedule (UTC): {DateTime.UtcNow.AddMilliseconds(TimeSpan.FromDays(this.intervalInDays).TotalMilliseconds)}");
                Thread.Sleep(TimeSpan.FromDays(this.intervalInDays));
                return true;
            }
            catch (OperationCanceledException e)
            {
                this.logger.Error(nameof(ItemProcessor), e, $"{nameof(this.DoWorkAsync)} was cancelled.");
                Trace.TraceError(nameof(ItemProcessor), e, $"{nameof(this.DoWorkAsync)} was cancelled.");
                return false;
            }
            catch (Exception e)
            {
                this.logger.Error(nameof(ItemProcessor), e, "An unhandled exception occurred.");
                Trace.TraceError(nameof(ItemProcessor), e, "An unhandled exception occurred.");
                return false;
            }
        }

        private List<Agent> TransformAgentsGcEmail(List<Agent> agents)
        {
            var newAgentsList = new List<Agent>();
            foreach (Agent agent in agents)
            {
                var emailId = agent.AlertContacts;
                if (!string.IsNullOrWhiteSpace(emailId))
                {
                    string fullEmailId = emailId.Split('<', '>')[1];
                    agent.AlertContacts = fullEmailId.Split('@')[0].Contains(".") ? null : fullEmailId;
                }
                newAgentsList.Add(agent);
            }

            return newAgentsList;
        }

        private static void IncrementCounter(ICounterFactory counterFactory, string counterName, ulong value)
        {
            ICounter counter = counterFactory.GetCounter(CounterCategoryNames.VortexDeviceDelete, counterName, CounterType.Rate);
            counter.IncrementBy(value);
        }

        /// <summary>
        /// This is temporary code to write to tracelogs including the xpert logs, since for some reason the xpert logging is not working
        /// Want to investigate first why the worker is not functioning
        /// </summary>
        /// <param name="source">Log source</param>
        /// <param name="message">Log message</param>
        private void DoLog(string source, string message)
        {
            Trace.TraceInformation($"{ClassName}.{source}: {message}");

            this.logger.Information(source, message);
        }
    }
}
