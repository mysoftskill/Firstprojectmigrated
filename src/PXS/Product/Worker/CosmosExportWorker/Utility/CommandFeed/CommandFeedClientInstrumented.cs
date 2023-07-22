// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts;
    using Microsoft.PrivacyServices.CommandFeed.Client.Commands;
    using Microsoft.PrivacyServices.CommandFeed.Validator.Configuration;

    /// <summary>
    ///     command feed client wrapper
    /// </summary>
    public class CommandFeedClientInstrumented : CommandFeedClientNotImplemented, ICommandClient
    {
        private const string PartnerId = "CommandFeedClient";

        private readonly ICounterFactory counterFactory;

        private readonly ICommandClient inner;

        /// <summary>
        ///     The amount of time for which leases are acquired
        /// </summary>
        public TimeSpan? RequestedLeaseDuration
        {
            get => this.inner.RequestedLeaseDuration;
            set => this.inner.RequestedLeaseDuration = value;
        }

        /// <summary>
        ///     Gets or sets the list of supported sovereign cloud configurations
        /// </summary>
        public List<KeyDiscoveryConfiguration> SovereignCloudConfigurations
        {
            get => this.inner.SovereignCloudConfigurations;
            set => this.inner.SovereignCloudConfigurations = value;
        }

        /// <summary>
        ///     Initializes a new instance of the CommandFeedClientWrapper class
        /// </summary>
        /// <param name="counterFactory">counter factory</param>
        /// <param name="inner">actual client</param>
        public CommandFeedClientInstrumented(
            ICounterFactory counterFactory,
            ICommandClient inner)
        {
            this.counterFactory = counterFactory ?? throw new ArgumentNullException(nameof(counterFactory));
            this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        /// <summary>
        ///     Updates the status of the given command
        /// </summary>
        /// <param name="commandId">command id</param>
        /// <param name="agentState">agent state</param>
        /// <param name="commandStatus">command status</param>
        /// <param name="affectedRowCount">affected row count</param>
        /// <param name="leaseReceipt">lease receipt</param>
        /// <param name="leaseExtension">lease extension</param>
        /// <param name="variantIds">variant ids</param>
        /// <param name="nonTransientFailures">non-transient failures</param>
        /// <param name="exportedFileSizeDetails">Exported File Size Details</param>
        /// <returns>new lease receipt for the command</returns>
        public Task<string> CheckpointAsync(
            string commandId,
            string agentState,
            CommandStatus commandStatus,
            int affectedRowCount,
            string leaseReceipt,
            TimeSpan? leaseExtension,
            IEnumerable<string> variantIds,
            IEnumerable<string> nonTransientFailures,
            IEnumerable<ExportedFileSizeDetails> exportedFileSizeDetails)
        {
            return this.ExecuteAsync(
                "Checkpoint",
                HttpMethod.Post,
                () =>
                    this.inner.CheckpointAsync(
                        commandId,
                        agentState,
                        commandStatus,
                        affectedRowCount,
                        leaseReceipt,
                        leaseExtension,
                        variantIds,
                        nonTransientFailures,
                        exportedFileSizeDetails));
        }

        /// <summary>
        ///     Updates the status of the given command
        /// </summary>
        /// <param name="command">command</param>
        /// <param name="commandStatus">command status</param>
        /// <param name="affectedRowCount">affected row count</param>
        /// <param name="leaseExtension">lease extension</param>
        /// <param name="nonTransientFailures">non-transient failures</param>
        /// <param name="exportedFileSizeDetails">Exported File Size Details</param>
        /// <returns>new lease receipt for the command</returns>
        public Task CheckpointAsync(
            IPrivacyCommand command,
            CommandStatus commandStatus,
            TimeSpan? leaseExtension,
            int affectedRowCount,
            IEnumerable<string> nonTransientFailures,
            IEnumerable<ExportedFileSizeDetails> exportedFileSizeDetails)
        {
            return this.ExecuteAsync(
                "CheckpointViaCommand",
                HttpMethod.Post,
                async () =>
                {
                    await this.inner
                        .CheckpointAsync(command, commandStatus, leaseExtension, affectedRowCount, nonTransientFailures, exportedFileSizeDetails)
                        .ConfigureAwait(false);
                    return string.Empty;
                });
        }

        /// <summary>
        ///     Retrieves details about a Command previously received from GetCommandsAsync()
        /// </summary>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>list of commands</returns>
        public Task<List<IPrivacyCommand>> GetCommandsAsync(CancellationToken cancellationToken)
        {
            return this.ExecuteAsync(
                "GetCommands",
                HttpMethod.Get,
                () => this.inner.GetCommandsAsync(cancellationToken));
        }

        /// <summary>
        ///     Determines whether the specified command is synthetic or not
        /// </summary>
        /// <param name="commandId">command id</param>
        /// <returns>true if it is a test command, false if it is not a test command, null if no value could be determined</returns>
        public Task<bool?> IsTestCommandAsync(string commandId)
        {
            return this.ExecuteAsync(
                "IsTestCommand",
                HttpMethod.Get,
                () => this.inner.IsTestCommandAsync(commandId));
        }

        /// <summary>
        ///     Retrieves details about a Command previously received from GetCommandsAsync()
        /// </summary>
        /// <param name="commandId">command id</param>
        /// <param name="leaseReceipt">lease receipt</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>full command</returns>
        public Task<IPrivacyCommand> QueryCommandAsync(
            string commandId,
            string leaseReceipt,
            CancellationToken cancellationToken)
        {
            return this.ExecuteAsync(
                "QueryCommand",
                HttpMethod.Get,
                () => this.inner.QueryCommandAsync(leaseReceipt, cancellationToken));
        }

        /// <summary>
        ///     Retrieves details about a Command previously received from GetCommandsAsync()
        /// </summary>
        /// <param name="leaseReceipt">lease receipt</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>full command or null if command could not be found</returns>
        public Task<IPrivacyCommand> QueryCommandAsync(
            string leaseReceipt,
            CancellationToken cancellationToken)
        {
            return this.QueryCommandAsync(null, leaseReceipt, cancellationToken);
        }

        /// <summary>
        ///     Retrieves details about a Command previously received from GetCommandsAsync()
        /// </summary>
        /// <param name="agentId">agent id</param>
        /// <param name="assetGroupId">asset group id</param>
        /// <param name="commandId">command id</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>command object and status details</returns>
        public Task<QueryCommandResult> QueryCommandAsync(
            string agentId,
            string assetGroupId,
            string commandId,
            CancellationToken cancellationToken)
        {
            return this.ExecuteAsync(
                "QueryCommandByAgentAssetGroupCommand",
                HttpMethod.Get,
                () => this.inner.QueryCommandAsync(agentId, assetGroupId, commandId, cancellationToken));
        }

        /// <summary>
        ///     Executes the provided command, wrapping the calls in outgoing API instrumentation
        /// </summary>
        /// <typeparam name="T">type of return value</typeparam>
        /// <param name="methodName">method name</param>
        /// <param name="httpMethod">HTTP method</param>
        /// <param name="func">function to execute</param>
        /// <returns>resulting value</returns>
        private async Task<T> ExecuteAsync<T>(
            string methodName,
            HttpMethod httpMethod,
            Func<Task<T>> func)
        {
            OutgoingApiEventWrapper outgoingEvent;
            Stopwatch timer = new Stopwatch();
            ulong elapsed = 0;
            bool success = true;
            bool is400Err = false;
            bool is500Err = false;

            outgoingEvent = OutgoingApiEventWrapper.CreateBasicOutgoingEvent(
                PartnerId,
                methodName,
                "1",
                "pcf://" + methodName + "/", // TODO: determie actual URI
                httpMethod,
                "CommandFeedClient");

            outgoingEvent.Start();

            timer.Start();

            try
            {
                T result = await func().ConfigureAwait(false);

                elapsed = Convert.ToUInt64(timer.ElapsedMilliseconds);

                return result;
            }
            catch (Exception e)
            {
                (bool, bool) GetErrType(int code) => (code >= 400 && code < 500, code >= 500 && code < 600);

                elapsed = Convert.ToUInt64(timer.ElapsedMilliseconds);
                success = false;

                outgoingEvent.ErrorMessage = e.ToString();

                if (e is HttpResponseException e2)
                {
                    (is400Err, is500Err) = GetErrType((int)e2.Response.StatusCode);
                }
                else if (e is HttpRequestException e3)
                {
                    // 0 is never going to be a 4xx or 5xx error, so just pass that in the case where we don't have a status code
                    (is400Err, is500Err) = GetErrType(Utility.ExtractStatusCodeFromHttpRequestExceptionData(e3) ?? 0);
                }

                throw;
            }
            finally
            {
                outgoingEvent.Finish();

                PerfCounterHelper.UpdatePerfCounters(
                    this.counterFactory,
                    PartnerId,
                    methodName,
                    success,
                    elapsed);

                if (is400Err)
                {
                    PerfCounterHelper.UpdateHttpFourXXCounter(
                        this.counterFactory,
                        PartnerId,
                        methodName);
                }

                if (is500Err)
                {
                    PerfCounterHelper.UpdateHttpFiveXXCounter(
                        this.counterFactory,
                        PartnerId,
                        methodName);
                }
            }
        }
    }
}
