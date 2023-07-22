namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts;
    using Newtonsoft.Json;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class PrivacyCommandReceiverV2
    {
        private const int MaxBackoffSeconds = 30;

        private readonly ICommandFeedClient commandFeedClient;
        private readonly IPrivacyDataAgentV2 agent;
        private readonly CommandFeedLogger logger;

        public PrivacyCommandReceiverV2(IPrivacyDataAgentV2 dataAgent, ICommandFeedClient commandFeedClient, CommandFeedLogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.commandFeedClient = commandFeedClient ?? throw new ArgumentNullException(nameof(commandFeedClient));
            this.agent = dataAgent ?? throw new ArgumentNullException(nameof(dataAgent));
        }

        public async Task BeginReceivingAsync(Guid assetGroupId = default, int leaseDuration = 900, bool returnOnlyTest = false, CancellationToken cancellationToken = default)
        {
            TimeSpan nextDelay = TimeSpan.Zero;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(nextDelay, cancellationToken).ConfigureAwait(false);

                    var workitem = await this.commandFeedClient.GetWorkitemAsync(assetGroupId, leaseDuration, returnOnlyTest, cancellationToken).ConfigureAwait(false);
                    if (workitem != null)
                    {
                        await agent.ProcessWorkitemAsync(this.commandFeedClient, workitem, cancellationToken);
                    }
                    else
                    {
                        // Command queue is currently empty, wait for 5 minutes
                        await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken).ConfigureAwait(false);
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

                    // After a failure, increase the back off up to maximum.
                    nextDelay += TimeSpan.FromSeconds(1);
                    if (nextDelay >= TimeSpan.FromSeconds(MaxBackoffSeconds))
                    {
                        nextDelay = TimeSpan.FromSeconds(MaxBackoffSeconds);
                    }

                    this.logger.UnhandledException(ex);
                }
            }
        }
    }
}
