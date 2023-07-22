namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Client.Commands;

    /// <summary>
    /// A polling receiver that completes commands in batches
    /// </summary>
    public class BatchCompleteReceiver
    {
        private const int MaxNumberOfCommandsInBatch = 100;
        private readonly CommandFeedClient client;
        private readonly ConcurrentQueue<IPrivacyCommand> commandProcessingQueue;

        /// <summary>
        /// A data agent that processes commands in batches.
        /// Retrieves commands using GetCommands, saves the commands in a queue in memory
        /// </summary>
        /// <param name="client">The command feed client.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        public BatchCompleteReceiver(CommandFeedClient client, CancellationToken cancellationToken)
        {
            this.client = client;
            this.commandProcessingQueue = new ConcurrentQueue<IPrivacyCommand>();

            Task.Run(() => this.BeginProcessingCommandsAsync(cancellationToken));
        }

        /// <summary>
        /// This loop polls the command feed and processes commands in parallel, enqueueing actionable commands for processing Completes.
        /// </summary>
        /// <param name="cancellationToken">The </param>
        public async Task BeginReceivingAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (FlightingUtilities.IsEnabled(FlightingNames.SyntheticAgentDisableBatchComplete))
                    {
                        await Task.Delay(5000, cancellationToken);
                    }
                    else
                    {
                        List<IPrivacyCommand> commands = await this.client.GetCommandsAsync(cancellationToken);

                        await Logger.InstrumentAsync(
                            new IncomingEvent(SourceLocation.Here()),
                            async ev =>
                            {
                                ev["CommandsCount"] = commands.Count.ToString();
                                ev["CommandsReceived"] = string.Join(",", commands.Select(c => c.CommandId));
                                ev.StatusCode = HttpStatusCode.OK;

                                await Task.Yield();
                            });
                        foreach (IPrivacyCommand command in commands)
                        {
                            if (FlightingUtilities.IsEnabled(FlightingNames.SyntheticAgentDisableCompleteCommands))
                            {
                                Task fireAndForget = command.CheckpointAsync(CommandStatus.Pending, 0, TimeSpan.FromDays(1));
                            }
                            else
                            {
                                this.commandProcessingQueue.Enqueue(command);
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // normal shutdown. Just let this pass.
            }
        }

        /// <summary>
        /// Processes a batch of 100 commands.
        /// </summary>
        private async Task BatchProcessCommandsAsync()
        {
            await Logger.InstrumentAsync(
                new OutgoingEvent(SourceLocation.Here()),
                async ev =>
                {
                    IPrivacyCommand[] commands = this.commandProcessingQueue.DequeueChunk(MaxNumberOfCommandsInBatch).ToArray();
                    ProcessedCommand[] processedCommands = commands.Select(c => new ProcessedCommand(c.CommandId, c.LeaseReceipt, 1)).ToArray();

                    try
                    {
                        await this.client.BatchCheckpointCompleteAsync(processedCommands);
                        ev["ProcessedCommandsCount"] = processedCommands.Length.ToString();
                        ev["CommandsCompleted"] = string.Join(",", processedCommands.Select(c => c.CommandId));
                    }
                    catch (Exception ex)
                    {
                        // Let the other agent instance process the commands individually.
                        ev.SetException(ex);
                    }
                });
        }

        private async Task BeginProcessingCommandsAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (this.commandProcessingQueue.Count >= MaxNumberOfCommandsInBatch)
                    {
                        await this.BatchProcessCommandsAsync();
                    }
                    else
                    {
                        await Task.Delay(3000, cancellationToken);
                    }
                }

                await this.DrainQueue();
            }
            catch (OperationCanceledException)
            {
                await this.DrainQueue();
            }
        }

        private async Task DrainQueue()
        {
            while (this.commandProcessingQueue.Any())
            {
                await this.BatchProcessCommandsAsync();
            }
        }
    }

    internal static class QueueExtensions
    {
        public static IEnumerable<T> DequeueChunk<T>(this ConcurrentQueue<T> queue, int chunkSize)
        {
            for (int i = 0; i < chunkSize && queue.Count > 0; i++)
            {
                queue.TryDequeue(out T value);
                yield return value;
            }
        }
    }
}
