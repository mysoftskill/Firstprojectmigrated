namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the Privacy Command Receiver. This is the top-level entry point into the client library.
    /// </summary>
    public class PrivacyCommandReceiver
    {
        private const int MaxBackoffSeconds = 30;

        private readonly ICommandFeedClient commandFeedClient;
        private readonly IPrivacyDataAgent agent;
        private readonly CommandFeedLogger logger;

        private int concurrencyLimit = 50;

        /// <summary>
        /// Initializes a new <see cref="PrivacyCommandReceiver"/>. The PrivacyCommandReceiver instance receives commands from
        /// the Privacy Command Feed and invokes the processing methods on the given <see cref="IPrivacyDataAgent"/>.
        /// </summary>
        /// <param name="dataAgent">The data agent implementation to invoke when a command is received.</param>
        /// <param name="commandFeedClient">The command feed client.</param>
        /// <param name="logger">The command feed logger.</param>
        public PrivacyCommandReceiver(
            IPrivacyDataAgent dataAgent,
            ICommandFeedClient commandFeedClient,
            CommandFeedLogger logger)
        {
            if (dataAgent == null)
            {
                throw new ArgumentNullException(nameof(dataAgent));
            }

            if (commandFeedClient == null)
            {
                throw new ArgumentNullException(nameof(commandFeedClient));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            this.logger = logger;
            this.commandFeedClient = commandFeedClient;
            this.agent = dataAgent;
        }
        
        /// <summary>
        /// Gets or sets the current currency limit for the receiver. This controls the number of simultaneous commands that will
        /// be executed by this receiver.
        /// </summary>
        public int ConcurrencyLimit
        {
            get
            {
                return this.concurrencyLimit;
            }

            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Value must be a nonnegative number");
                }

                // Enforce a logical minimum of 10. Throwing an exception here would be a breaking change, but we are free to ignore any too-low settings.
                if (value < 10)
                {
                    value = 10;
                }

                this.concurrencyLimit = value;
            }
        }

        /// <summary>
        /// The amount of time for which leases are acquired.
        /// Please set a value between 15 minutes and a day.
        /// </summary>
        public TimeSpan? RequestedLeaseDuration { get; set; }

        /// <summary>
        /// Initializes a new <see cref="PrivacyCommandReceiver"/>. The PrivacyCommandReceiver instance receives commands from
        /// the Privacy Command Feed and invokes the processing methods on the given <see cref="IPrivacyDataAgent"/>.
        /// </summary>
        /// <param name="dataAgent">The agent implementation to invoke when a command is received.</param>
        /// <param name="siteId">The MSA Site ID.</param>
        /// <param name="clientCertificate">The client certificate.</param>
        /// <param name="agentId">The agent ID obtained from PDMS registration.</param>
        /// <param name="logger">The command feed logger.</param>
        /// <param name="environment">Used to override the command feed server, optionally.</param>
        /// <param name="httpClientFactory">A custom HTTP Client factory, optionally.</param>
        public PrivacyCommandReceiver(
            IPrivacyDataAgent dataAgent,
            long siteId,
            X509Certificate2 clientCertificate,
            Guid agentId,
            CommandFeedLogger logger,
            CommandFeedEndpointConfiguration environment = null,
            IHttpClientFactory httpClientFactory = null)
        {
            if (dataAgent == null)
            {
                throw new ArgumentNullException(nameof(dataAgent));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            this.logger = logger;
            this.agent = dataAgent;

            IHttpClientFactory factory = httpClientFactory ?? new DefaultHttpClientFactory();
            this.commandFeedClient = new CommandFeedClient(
                agentId,
                siteId,
                clientCertificate,
                logger,
                factory,
                environment)
            {
                RequestedLeaseDuration = this.RequestedLeaseDuration
            };
        }

        /// <summary>
        /// Starts a background operation that receives Privacy Commands. The resulting task runs indefinitely,
        /// and only completes once the cancellation token has requested a cancellation.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token, used to stop the operation.</param>
        /// <returns>The task that represents the operation. The task runs until canceled.</returns>
        public async Task BeginReceivingAsync(CancellationToken cancellationToken)
        {
            TimeSpan nextDelay = TimeSpan.Zero;

            // Create a subtask with a unique cancellation source. This allows us to
            // be sure that we've stopped adding new commands before the sub-task has
            // stopped processing new commands. We use a concurrent queue to buffer
            // between the consumer and producer. We could use BlockingCollection{T},
            // but that hijacks a threadpool thread, and that's quite rude.
            CancellationTokenSource processTaskCancellationSource = new CancellationTokenSource();
            ConcurrentQueue<IPrivacyCommand> commandQueue = new ConcurrentQueue<IPrivacyCommand>();
            Task processCommandsTask = this.ProcessCommandsAsync(processTaskCancellationSource.Token, commandQueue);
                
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(nextDelay, cancellationToken).ConfigureAwait(false);

                    if (commandQueue.Count < this.ConcurrencyLimit)
                    {
                        // If we're buffering less than the concurrency limit, then we're making progress.
                        // Fetch a new batch of items.
                        nextDelay = TimeSpan.FromMilliseconds(500);

                        List<IPrivacyCommand> commands = await this.commandFeedClient.GetCommandsAsync(cancellationToken).ConfigureAwait(false);
                        
                        if (commands != null && commands.Count > 0)
                        {
                            nextDelay = TimeSpan.Zero;
                            foreach (var command in commands)
                            {
                                commandQueue.Enqueue(command);
                            }
                        }
                    }
                    else
                    {
                        // Our queue is already full enough. Don't fetch anything new yet.
                        nextDelay = TimeSpan.FromMilliseconds(100);
                    }
                }
                catch (Exception ex)
                {
                    // Shutdown if cancellation is requested.
                    if (cancellationToken.IsCancellationRequested)
                    {
                        this.logger.CancellationException(ex);
                        break;
                    }

                    // After a failure, increase the backoff up to maximum.
                    nextDelay += TimeSpan.FromSeconds(1);
                    if (nextDelay >= TimeSpan.FromSeconds(MaxBackoffSeconds))
                    {
                        nextDelay = TimeSpan.FromSeconds(MaxBackoffSeconds);
                    }
                    
                    this.logger.UnhandledException(ex);
                }
            }

            // We've officially stopped adding to the queue. Set the cancellation for the process task.
            processTaskCancellationSource.Cancel();
            await processCommandsTask.ConfigureAwait(false);
        }
        
        /// <summary>
        /// Pulls commands from the given queue and processes them according to the defined concurrency limit.
        /// </summary>
        private async Task ProcessCommandsAsync(CancellationToken cancellationToken, ConcurrentQueue<IPrivacyCommand> queue)
        {
            // Make sure we actually go async at some point.
            await Task.Yield();

            IPrivacyCommand command;
            List<Task> currentCommands = new List<Task>();

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    while (currentCommands.Count >= this.ConcurrencyLimit)
                    {
                        Task completedTask = await Task.WhenAny(currentCommands).ConfigureAwait(false);
                        currentCommands.Remove(completedTask);
                    }

                    // Start the tasks and add them to a list of things that are currently processing, up to the concurrency limit
                    while (currentCommands.Count < this.ConcurrencyLimit && queue.TryDequeue(out command))
                    {
                        currentCommands.Add(this.ProcessSingleCommandAsync(command));
                    }

                    if (queue.IsEmpty)
                    {
                        // If the queue is empty, then wait a little bit before checking again.
                        // We could use signaling here, but 100ms never hurt anyone.
                        await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    // Shouldn't happen, but let's not stop processing if it does.
                    this.logger.UnhandledException(ex);
                }
            }

            // Process whatever we have (ignoring concurrency limits)
            while (queue.TryDequeue(out command))
            {
                currentCommands.Add(this.ProcessSingleCommandAsync(command));
            }

            await Task.WhenAll(currentCommands).ConfigureAwait(false);
        }

        private Task ProcessSingleCommandAsync(IPrivacyCommand command)
        {
            if (command.ApproximateLeaseExpiration <= DateTimeOffset.UtcNow)
            {
                return Task.FromResult(true);
            }

            switch (command)
            {
                case IDeleteCommand deleteCommand:
                    return this.ProcessAsync(deleteCommand, this.agent.ProcessDeleteAsync);

                case IExportCommand exportCommand:
                    return this.ProcessAsync(exportCommand, this.agent.ProcessExportAsync);

                case IAccountCloseCommand accountCloseCommand:
                    return this.ProcessAsync(accountCloseCommand, this.agent.ProcessAccountClosedAsync);

                case IAgeOutCommand ageOutCommand:
                    return this.ProcessAsync(ageOutCommand, this.agent.ProcessAgeOutAsync);

                default:
                    this.logger.UnrecognizedCommandType(command.CorrelationVector,  command.CommandId, command.GetType().FullName);
                    break;
            }

            return Task.FromResult(true);
        }

        private async Task ProcessAsync<T>(T command, Func<T, Task> callback) where T : IPrivacyCommand
        {
            await Task.Yield();

            bool hadError = false;

            // Store a copy of the old lease receipt. This will change if the agent has successfully checkpointed.
            string oldLeaseReceipt = command.LeaseReceipt;

            try
            {
                // Force the task to be run asynchronously to prevent blocking behavior from bad implementations of IDataAgent.
                await Task.Run(() => callback(command)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                hadError = true;
                this.logger.UnhandledException(ex);
            }

            // Use a separate try/catch to checkpoint. We don't want to throw from inside a catch block!
            // We only do this if the lease receipt has not changed: that is, the agent did not successfully issue a checkpoint.
            if (hadError && command.LeaseReceipt == oldLeaseReceipt)
            {
                try
                {
                    await command.CheckpointAsync(CommandStatus.Failed, 0).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    this.logger.UnhandledException(ex);
                }
            }
        }
    }
}
