// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.PrivacyServices.MsaAgeOutFakeCommandWorker
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Common.Worker;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.PcfAdapter;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     This is a simple msa age out queue processor that a) grabs message from an azure queue and b) sends them to pcf
    /// </summary>
    /// <remarks>This is intended for NON-production use only</remarks>
    public class MsaAgeOutQueueProcessor : BackgroundWorker
    {
        private readonly TimeSpan doWorkDelay = TimeSpan.FromSeconds(30);

        private readonly TimeSpan emptyQueueDelay = TimeSpan.FromMinutes(15);

        private readonly ILogger logger;

        private readonly IPcfAdapter pcfAdapter;

        private readonly IMsaAgeOutQueue queue;

        private readonly ThreadSafeRandom random = new ThreadSafeRandom();

        private readonly TimeSpan renewLeaseTimeSpanMax = TimeSpan.FromHours(12);

        private readonly TimeSpan renewLeaseTimeSpanMin = TimeSpan.FromMinutes(15);

        public MsaAgeOutQueueProcessor(IMsaAgeOutQueue queue, IPcfAdapter pcfAdapter, ILogger logger)
        {
            this.queue = queue;
            this.pcfAdapter = pcfAdapter;
            this.logger = logger;
        }

        /// <inheritdoc />
        public override async Task<bool> DoWorkAsync()
        {
            // This worker follows this pattern:
            // 1. Get messages from queue
            // 2. If nothing in queue -> nothing to do (return).
            // 3. If a message exists in queue -> send it to PCF
            // 4. If sending to PCF was successful -> extend the visibility time in the queue so we don't see it again for a while
            // 5. If sending to PCF was NOT successful -> do nothing with the message, it will get retried again when invisibility time is up

            this.logger.Verbose(nameof(MsaAgeOutQueueProcessor), $"{DateTimeOffset.UtcNow}");

            try
            {
                // read from queue
                IList<IQueueItem<AgeOutRequest>> messages = await this.queue.GetMessagesAsync(32, CancellationToken.None).ConfigureAwait(false);

                if (messages.Count == 0)
                {
                    // no work to do!
                    this.logger.Information(nameof(MsaAgeOutQueueProcessor), $"Nothing in queue. Delaying for {this.emptyQueueDelay.TotalMinutes} minutes.");
                    await Task.Delay(this.emptyQueueDelay).ConfigureAwait(false);
                    return false;
                }

                // send to pcf with new command ids
                List<AgeOutRequest> commands = messages.Select(c => c.Data).ToList();
                foreach (AgeOutRequest command in commands)
                {
                    // make them unique
                    command.RequestId = Guid.NewGuid();
                    command.CorrelationVector = new CorrelationVector().Value;
                    command.RequestGuid = Guid.NewGuid();

                    // keep moving the timestamp to current time, but keep the 'age' of the generated command the same
                    if (command.LastActive.HasValue)
                    {
                        TimeSpan age = command.Timestamp - command.LastActive.Value;
                        DateTimeOffset now = DateTimeOffset.UtcNow;
                        command.Timestamp = now;
                        command.LastActive = now - age;
                    }
                }

                AdapterResponse response = await this.pcfAdapter.PostCommandsAsync(commands.Cast<PrivacyRequest>().ToList()).ConfigureAwait(false);

                if (response.IsSuccess)
                {
                    // extend invisibility time so we don't see it again for a while. we aren't deleting them from the queue.
                    foreach (IQueueItem<AgeOutRequest> queueItem in messages)
                    {
                        await queueItem.RenewLeaseAsync(this.GetRandomInvisibilityTimeSpan()).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception e)
            {
                this.logger.Error(nameof(MsaAgeOutQueueProcessor), e, "An unhandled exception occurred");

                // return false so we don't do work again right away
                return false;
            }

            return true;
        }

        /// <inheritdoc />
        public override void Start()
        {
            base.Start(this.doWorkDelay);
        }

        private TimeSpan GetRandomInvisibilityTimeSpan()
        {
            // use a random timespan so traffic gets spread out
            return TimeSpan.FromMinutes(this.random.Next((int)this.renewLeaseTimeSpanMin.TotalMinutes, (int)this.renewLeaseTimeSpanMax.TotalMinutes));
        }
    }
}
