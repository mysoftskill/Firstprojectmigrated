// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Tasks
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Data;
    using Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility;
    using Microsoft.PrivacyServices.CommandFeed.Client;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     creates command data writers
    /// </summary>
    public class CommandDataWriterFactory : ICommandDataWriterFactory
    {
        private readonly ICosmosExportPipelineFactory exportPipelineFactory;
        private readonly ICommandObjectFactory commandObjectFactory;
        private readonly ITable<CommandState> commandState;
        private readonly IFileSystemManager fileSystemManager;
        private readonly ILogger logger;

        private AsyncLocal<ICommandClient> commandClient = new AsyncLocal<ICommandClient>();

        /// <summary>
        ///    Initializes a new instance of the CommandDataWriterFactory class
        /// </summary>
        /// <param name="exportPipelineFactory">export pipeline factory</param>
        /// <param name="commandObjectFactory">command feed object factory</param>
        /// <param name="fileSystemManager">file system manager</param>
        /// <param name="commandState">command feed command state table</param>
        /// <param name="logger">Geneva trace logger</param>
        public CommandDataWriterFactory(
            ICosmosExportPipelineFactory exportPipelineFactory,
            ICommandObjectFactory commandObjectFactory,
            IFileSystemManager fileSystemManager,
            ITable<CommandState> commandState,
            ILogger logger)
        {
            this.exportPipelineFactory = exportPipelineFactory ?? throw new ArgumentNullException(nameof(exportPipelineFactory));
            this.commandObjectFactory = commandObjectFactory ?? throw new ArgumentNullException(nameof(commandObjectFactory));
            this.fileSystemManager = fileSystemManager ?? throw new ArgumentNullException(nameof(fileSystemManager));
            this.commandState = commandState ?? throw new ArgumentNullException(nameof(commandState));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        ///     Creates the specified agent id
        /// </summary>
        /// <param name="canceler">cancel token</param>
        /// <param name="agentId">agent id</param>
        /// <param name="commandId">command id</param>
        /// <param name="fileName">file name to write to</param>
        /// <returns>export writer or null if the command could not be found for the agent</returns>
        public async Task<ICommandDataWriter> CreateAsync(
            CancellationToken canceler,
            string agentId,
            string commandId,
            string fileName)
        {
            ArgumentCheck.ThrowIfNullEmptyOrWhiteSpace(commandId, nameof(commandId));
            ArgumentCheck.ThrowIfNullEmptyOrWhiteSpace(agentId, nameof(agentId));
            ArgumentCheck.ThrowIfNull(canceler, nameof(canceler));

            string errCtx = string.Empty;

            try
            {
                ICommandClient cmdFeedClient;
                IPrivacyCommand rawCmd;
                IExportCommand exportCmd;
                CommandState request;
                CommandState state;

                ArgumentCheck.ThrowIfNullEmptyOrWhiteSpace(commandId, nameof(commandId));
                ArgumentCheck.ThrowIfNullEmptyOrWhiteSpace(agentId, nameof(agentId));

                errCtx = "fetching PCF lease from command state table";

                request = new CommandState { AgentId = agentId, CommandId = commandId };

                state = await this.commandState.GetItemAsync(request.PartitionKey, request.RowKey).ConfigureAwait(false);

                // if we can't find a state object for this, just spin up a dead letter writer and we'll save it for later
                if (state == null)
                {
                    this.logger.Warning(
                        nameof(CommandDataWriter),
                        "Failed to find command state row for [agentId: {0}][commandId: {1}]. Sending to dead letter store.",
                        agentId,
                        commandId);

                    return new DeadLetterDataWriter(commandId, agentId, fileName, this.fileSystemManager.DeadLetter, this.logger);
                }

                if (state.NotApplicable)
                {
                    return new NoOpCommandDataWriter(commandId, fileName, WriterStatuses.AbandonedNotApplicable);
                }

                if (state.IsComplete)
                {
                    return new NoOpCommandDataWriter(commandId, fileName, WriterStatuses.AbandonedAlreadyComplete);
                }

                // if we're supposed to ignore this command, then just return the no-op writer 
                if (state.IgnoreCommand)
                {
                    return new NoOpCommandDataWriter(commandId, fileName, WriterStatuses.AbandonedTest);
                }
                
                cmdFeedClient = this.commandClient.Value;
                if (cmdFeedClient == null)
                {
                    this.commandClient.Value = cmdFeedClient = this.commandObjectFactory.CreateCommandFeedClient();
                }

                errCtx = "fetching PCF commnd object from PCF.";

                rawCmd = await cmdFeedClient.QueryCommandAsync(commandId, state.LeaseReceipt, canceler).ConfigureAwait(false);

                if (rawCmd == null)
                {
                    this.logger.Warning(
                        nameof(CommandDataWriter),
                        "Failed to find command in CommandFeed for [agentId: {0}][commandId: {1}]. Abandoning data.",
                        agentId,
                        commandId);

                    return new NoOpCommandDataWriter(commandId, fileName, WriterStatuses.AbandonedNoCommand);
                }

                exportCmd = rawCmd as IExportCommand;
                if (exportCmd == null)
                {
                    string expected = typeof(ExportCommand).Name;
                    string actual = rawCmd.GetType().Name;

                    throw new UnexpectedCommandException(
                        $"[Command: {commandId}] Got a {actual} command object when a {expected} command object was expected");
                }

                errCtx = "creating command export writer";

                return new CommandDataWriter(commandId, fileName, this.exportPipelineFactory.Create(commandId, exportCmd));
            }
            catch (Exception e) when ((e is OperationCanceledException) == false)
            {
                this.logger.Error(nameof(CommandDataWriterFactory), $"Failed to create new export writer while {errCtx}: {e}");
                throw;
            }
        }
    }
}
