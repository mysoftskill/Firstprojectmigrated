// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts;
    using Microsoft.PrivacyServices.CommandFeed.Client.Commands;
    using Microsoft.PrivacyServices.CommandFeed.Validator.Configuration;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     command feed client wrapper
    /// </summary>
    public class CommandFeedClientRetry : CommandFeedClientNotImplemented, ICommandClient
    {
        private const uint DefaultMaxAttemps = 3;

        private const ulong DefaultWaitBaseMs = 100;

        private const ulong DefaultWaitIncrementMs = 100;

        private readonly ICommandClient inner;

        private readonly ILogger logger;

        private readonly uint maxAttempts;

        private readonly TimeSpan waitBase;

        private readonly TimeSpan waitIncrement;

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
        /// <param name="retryConfig">retry configuration</param>
        /// <param name="inner">actual client</param>
        /// <param name="logger">Geneva logger</param>
        public CommandFeedClientRetry(
            IRetryStrategyConfiguration retryConfig,
            ICommandClient inner,
            ILogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.inner = inner ?? throw new ArgumentNullException(nameof(inner));

            if (retryConfig?.RetryMode == RetryMode.FixedInterval)
            {
                IFixedIntervalRetryConfiguration cfg = retryConfig.FixedIntervalRetryConfiguration;

                this.waitIncrement = TimeSpan.Zero;

                this.waitBase =
                    TimeSpan.FromMilliseconds(cfg?.RetryIntervalInMilliseconds ?? DefaultWaitBaseMs);

                this.maxAttempts = cfg?.RetryCount ?? DefaultMaxAttemps;
            }
            else if (retryConfig?.RetryMode == RetryMode.IncrementInterval)
            {
                IIncrementIntervalRetryConfiguration cfg = retryConfig.IncrementIntervalRetryConfiguration;

                this.maxAttempts = cfg?.RetryCount ?? DefaultMaxAttemps;

                this.waitIncrement =
                    TimeSpan.FromMilliseconds(cfg?.IntervalIncrementInMilliseconds ?? DefaultWaitIncrementMs);

                this.waitBase =
                    TimeSpan.FromMilliseconds(cfg?.InitialIntervalInMilliseconds ?? DefaultWaitBaseMs);
            }
            else if (retryConfig?.RetryMode == RetryMode.ExponentialBackOff)
            {
                IExponentialBackOffRetryConfiguration cfg = retryConfig.ExponentialBackOffRetryConfiguration;

                this.waitIncrement = TimeSpan.FromMilliseconds(DefaultWaitIncrementMs);
                this.waitBase = TimeSpan.FromMilliseconds(DefaultWaitBaseMs);

                this.maxAttempts = cfg?.RetryCount ?? DefaultMaxAttemps;
            }
            else
            {
                this.waitIncrement = TimeSpan.FromMilliseconds(DefaultWaitIncrementMs);
                this.waitBase = TimeSpan.FromMilliseconds(DefaultWaitBaseMs);
                this.maxAttempts = DefaultMaxAttemps;
            }
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
                $"set checkpoint {commandId} to {commandStatus}",
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
                $"set checkpoint {command.CommandId} to {commandStatus} via command object",
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
            return this.ExecuteAsync("get commands", () => this.inner.GetCommandsAsync(cancellationToken));
        }

        /// <summary>
        ///     Determines whether the specified command is synthetic or not
        /// </summary>
        /// <param name="commandId">command id</param>
        /// <returns>true if it is a test command, false if it is not a test command, null if no value could be determined</returns>
        public async Task<bool?> IsTestCommandAsync(string commandId)
        {
            TimeSpan wait;
            int pass;

            for (pass = 1, wait = this.waitBase;; ++pass, wait = wait.Add(this.waitIncrement))
            {
                try
                {
                    return await this.inner.IsTestCommandAsync(commandId).ConfigureAwait(false);
                }
                catch (Exception e)
                    when (e is OperationCanceledException || e is HttpRequestException || e is InvalidOperationException)
                {
                    if (pass < this.maxAttempts)
                    {
                        this.logger.Warning(
                            nameof(CommandFeedClientWrapper),
                            "Failed to fetch command {0} from command feed on pass {1} and will retry ({2}: {3})",
                            commandId,
                            pass,
                            e.GetType().FullName,
                            e.Message);

                        await Task.Delay(wait).ConfigureAwait(false);
                    }
                    else
                    {
                        this.logger.Error(
                            nameof(CommandFeedClientWrapper),
                            "Failed to fetch command {0} from command feed (pass {1}). Assuming non-test command: {2}",
                            commandId,
                            pass,
                            e.ToString());

                        // we ignore failures here because the worse case is that we think a command is real when it is actually
                        //  a test command. This results in tests perhaps blocking , but the alternative is to cause processing of
                        //  real data files to stop until PCF hands us the command. Even worse, if it takes long enough to get 
                        //  the command again, we'll dead-letter or abandon the data. 
                        // so the lesser evil is to negatively affect tests while still allowing real commands to go through
                        return null;
                    }
                }
            }
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
                "query for command " + (commandId ?? string.Empty) + " by lease receipt",
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
                $"query for command [{commandId}] by asset group [{assetGroupId}] and agent [{agentId}]",
                () => this.inner.QueryCommandAsync(agentId, assetGroupId, commandId, cancellationToken));
        }

        /// <summary>
        ///     Executes the provided command, calling the other provided method for error handling
        /// </summary>
        /// <typeparam name="T">type of return value</typeparam>
        /// <param name="tag">command tag</param>
        /// <param name="func">function to execute</param>
        /// <returns>resulting value</returns>
        private async Task<T> ExecuteAsync<T>(
            string tag,
            Func<Task<T>> func)
        {
            TimeSpan wait;
            int pass;

            for (pass = 1, wait = this.waitBase;; ++pass, wait = wait.Add(this.waitIncrement))
            {
                try
                {
                    return await func().ConfigureAwait(false);
                }
                catch (Exception e) when (e is OperationCanceledException || e is HttpRequestException)
                {
                    if (pass < this.maxAttempts)
                    {
                        string exMsg = e.GetMessageAndInnerMessages();

                        this.logger.Warning(
                            nameof(CommandFeedClientWrapper),
                            $"Failed to {tag} from command feed (pass {pass}) and will retry ({e.GetType().FullName}: {exMsg})");

                        await Task.Delay(wait).ConfigureAwait(false);
                    }
                    else
                    {
                        this.logger.Error(
                            nameof(CommandFeedClientWrapper),
                            $"Failed to {tag} from command feed (pass {pass}): {e}");

                        throw;
                    }
                }
            }
        }
    }
}
