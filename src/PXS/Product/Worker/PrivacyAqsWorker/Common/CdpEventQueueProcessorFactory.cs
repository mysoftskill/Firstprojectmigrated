// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.Common
{
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Worker;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.EventProcessors;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.TableStorage;
    using Microsoft.Membership.MemberServices.Privacy.Core.AQS;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;

    using Microsoft.PrivacyServices.Common.Azure;

    internal class CdpEventQueueProcessorFactory : ICdpEventQueueProcessorFactory
    {
        /// <summary>
        ///     Creates an instance of the <see cref="T:Microsoft.Membership.MemberServices.Privacy.AqsWorker.Common.CdpEventQueueProcessor" />
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="queueConfig">The queue configuration.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="createEventProcessor">The create event processor.</param>
        /// <param name="deleteEventProcessor">The delete event processor.</param>
        /// <param name="accountCreateWriter">The mapping writer.</param>
        /// <param name="accountDeleteWriter">The delete writer</param>
        /// <param name="counterFactory">The counter factory.</param>
        /// <param name="deadLetterTable">The dead letter table.</param>
        /// <returns>
        ///     A new instance of <see cref="T:Microsoft.Membership.MemberServices.Privacy.AqsWorker.Common.CdpEventQueueProcessor" />
        /// </returns>
        /// <inheritdoc />
        public IWorker Create(
            IAsyncQueueService2 client,
            IAqsConfiguration queueConfig,
            ILogger logger,
            IUserCreateEventProcessor createEventProcessor,
            IUserDeleteEventProcessor deleteEventProcessor,
            IAccountCreateWriter accountCreateWriter,
            IAccountDeleteWriter accountDeleteWriter,
            ICounterFactory counterFactory,
            ITable<MsaDeadLetterStorage> deadLetterTable)
        {
            return new CdpEventQueueProcessor(
                client,
                queueConfig.AqsQueueProcessorConfiguration,
                logger,
                createEventProcessor,
                deleteEventProcessor,
                accountCreateWriter,
                accountDeleteWriter,
                counterFactory,
                deadLetterTable);
        }
    }
}
