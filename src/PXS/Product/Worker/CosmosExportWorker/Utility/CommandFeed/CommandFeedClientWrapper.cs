// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.PcfAdapter;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts;
    using Microsoft.PrivacyServices.CommandFeed.Client.Commands;
    using Microsoft.PrivacyServices.CommandFeed.Client.SharedCommandFeedContracts.Partials;
    using Microsoft.PrivacyServices.CommandFeed.Validator.Configuration;
    using Microsoft.PrivacyServices.PXS.Command.CommandStatus;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     command feed client wrapper
    /// </summary>
    public class CommandFeedClientWrapper : CommandFeedClientNotImplemented, ICommandClient
    {
        private readonly ICommandFeedClient commandClient;

        private readonly ILogger logger;

        private readonly IPcfAdapter pcfClient;

        private readonly bool suppressIsTestCommandFailures;

        /// <summary>
        ///     The amount of time for which leases are acquired
        /// </summary>
        public TimeSpan? RequestedLeaseDuration
        {
            get => this.commandClient.RequestedLeaseDuration;
            set => this.commandClient.RequestedLeaseDuration = value;
        }

        /// <summary>
        ///     Gets or sets the list of supported sovereign cloud configurations
        /// </summary>
        public List<KeyDiscoveryConfiguration> SovereignCloudConfigurations
        {
            get => this.commandClient.SovereignCloudConfigurations;
            set => this.commandClient.SovereignCloudConfigurations = value;
        }

        /// <summary>
        ///     Initializes a new instance of the CommandFeedClientWrapper class
        /// </summary>
        /// <param name="commandMonitorConfig">command monitor configuration</param>
        /// <param name="commandClient">actual client</param>
        /// <param name="pcfClient">PCF client</param>
        /// <param name="logger">Geneva logger</param>
        public CommandFeedClientWrapper(
            ICommandMonitorConfig commandMonitorConfig,
            ICommandFeedClient commandClient,
            IPcfAdapter pcfClient,
            ILogger logger)
        {
            this.suppressIsTestCommandFailures = commandMonitorConfig.SuppressIsTestCommandFailures;
            this.commandClient = commandClient;
            this.pcfClient = pcfClient;
            this.logger = logger;
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
        /// <param name="exportedFileSizeDetails">Exported File Size details</param>
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
            IEnumerable<ExportedFileSizeDetails> exportedFileSizeDetails = null)
        {
            return this.commandClient.CheckpointAsync(
                commandId,
                agentState,
                commandStatus,
                affectedRowCount,
                leaseReceipt,
                leaseExtension,
                variantIds,
                nonTransientFailures,
                exportedFileSizeDetails);
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
            return command.CheckpointAsync(commandStatus, affectedRowCount, leaseExtension, null, nonTransientFailures, exportedFileSizeDetails);
        }

        /// <summary>
        ///     Retrieves details about a Command previously received from GetCommandsAsync()
        /// </summary>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>list of commands</returns>
        public Task<List<IPrivacyCommand>> GetCommandsAsync(CancellationToken cancellationToken)
        {
            return this.commandClient.GetCommandsAsync(cancellationToken);
        }

        /// <summary>
        ///     Determines whether the specified command is synthetic or not
        /// </summary>
        /// <param name="commandId">command id</param>
        /// <returns>true if it is a test command, false if it is not a test command, null if no value could be determined</returns>
        public async Task<bool?> IsTestCommandAsync(string commandId)
        {
            bool ShouldSuppress(Exception e)
            {
                return this.suppressIsTestCommandFailures &&
                       (e is OperationCanceledException || e is HttpRequestException || e is InvalidOperationException);
            }

            Guid commandIdActual;

            if (this.pcfClient != null && Guid.TryParse(commandId, out commandIdActual))
            {
                try
                {
                    AdapterResponse<CommandStatusResponse> job =
                        await this.pcfClient.GetRequestByIdAsync(commandIdActual, false).ConfigureAwait(false);

                    if (job?.Result == null || job.IsSuccess == false)
                    {
                        string msg = "Failed to fetch command " + commandId + " from command feed. Assuming real command";

                        if (job?.Error != null)
                        {
                            msg +=
                                $" [Code: {job.Error.Code}.{job.Error.StatusCode}][Message: {job.Error.Message ?? "<Unknown>"}]";
                        }

                        this.logger.Error(nameof(CommandFeedClientWrapper), msg);

                        return false;
                    }

                    return job.Result.IsSyntheticCommand;
                }
                catch (Exception e) when (ShouldSuppress(e))
                {
                    const string Fmt =
                        "Failed to fetch command {0} from command feed as operation was canceled. Assuming command is non-test " +
                        "command";

                    // we ignore failures here because the worse case is that we think a command is real when it is actually
                    //  a test command. This results in tests perhaps blocking, but the alternative is to cause processing of
                    //  real data files to stop until PCF hands us the command. Even worse, if it takes long enough to get 
                    //  the command again, we'll dead-letter or abandon the data. 
                    // so the lesser evil is to negatively affect tests while still allowing real commands to go through

                    this.logger.Error(nameof(CommandFeedClientWrapper), Fmt, commandId);

                    return null;
                }
            }

            return false;
        }

        /// <summary>
        ///     Retrieves details about a Command previously received from GetCommandsAsync()
        /// </summary>
        /// <param name="commandId">command id</param>
        /// <param name="leaseReceipt">lease receipt</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>full command or null if command could not be found</returns>
        public async Task<IPrivacyCommand> QueryCommandAsync(
            string commandId,
            string leaseReceipt,
            CancellationToken cancellationToken)
        {
            try
            {
                return await this.commandClient.QueryCommandAsync(leaseReceipt, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException e)
            {
                int? code = Utility.ExtractStatusCodeFromHttpRequestExceptionData(e);
                if (code.HasValue && (code.Value == (int)HttpStatusCode.NotFound || code.Value == (int)HttpStatusCode.BadRequest))
                {
                    this.logger.Warning(
                        nameof(CommandFeedClientWrapper),
                        $"CommandFeed returned {code.Value} fetching command with id {commandId}. Treating as missing: {e.Message}");

                    return null;
                }

                throw;
            }
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
        public async Task<QueryCommandResult> QueryCommandAsync(
            string agentId,
            string assetGroupId,
            string commandId,
            CancellationToken cancellationToken)
        {
            AdapterResponse<QueryCommandByIdResult> response;
            QueryCommandResult result;

            response = await this
                .pcfClient.QueryCommandByCommandIdAsync(agentId, assetGroupId, commandId, cancellationToken)
                .ConfigureAwait(false);

            if (response?.Result == null || response.IsSuccess == false)
            {
                AdapterError err = response?.Error;

                string msg =
                    "Failed to fetch command [{0}] from command feed when querying by agent [{1}] & asset group id [{2}]"
                        .FormatInvariant(commandId, agentId, assetGroupId);

                if (err != null)
                {
                    msg += $" [Code: {err.Code}.{err.StatusCode}][Message: {err.Message ?? "<Unknown>"}]";
                }

                this.logger.Error(nameof(CommandFeedClientWrapper), msg);

                throw new CommandOperationException(msg);
            }

            result = new QueryCommandResult { ResponseCode = response.Result.ResponseCode };

            if (response.Result.Command != null)
            {
                result.Command = PrivacyCommandFeedParser.ParseObject(response.Result.Command);
            }

            return result;
        }
    }
}
