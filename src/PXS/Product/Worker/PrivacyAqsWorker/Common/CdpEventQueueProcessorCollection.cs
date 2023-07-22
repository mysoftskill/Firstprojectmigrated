// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Common.Worker;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.Aqs;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.EventProcessors;
    using Microsoft.Membership.MemberServices.Privacy.AqsWorker.TableStorage;
    using Microsoft.Membership.MemberServices.Privacy.Core.AQS;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;

    using Microsoft.PrivacyServices.Common.Azure;

    internal class CdpEventQueueProcessorCollection : IWorker
    {
        /// <summary>
        ///     Sub processors that were created
        /// </summary>
        private readonly List<IWorker> processors;

        /// <summary>
        ///     Create a new instance of the <see cref="CdpEventQueueProcessorCollection" /> class.
        /// </summary>
        public CdpEventQueueProcessorCollection(
            IPrivacyConfigurationManager config,
            IAsyncQueueService2ClientFactory clientFactory,
            ICdpEventQueueProcessorFactory eventQueueFactory,
            ILogger logger,
            IUserCreateEventProcessorFactory cepFactory,
            IUserDeleteEventProcessorFactory depFactory,
            IAccountCreateWriter accountCreateWriter,
            IAccountDeleteWriter accountDeleteWriter,
            ICounterFactory counterFactory,
            ITable<MsaDeadLetterStorage> deadLetterTable)
        {
            this.processors = new List<IWorker>();

            foreach (IAqsConfiguration queueConfig in config.AqsWorkerConfiguration.AqsConfiguration)
            {
                int processorCount = queueConfig.ProcessorCount;

                IAsyncQueueService2 client = clientFactory.Create(queueConfig, logger);

                IUserCreateEventProcessor createEventProcessor = cepFactory.Create(queueConfig.AqsQueueProcessorConfiguration);
                IUserDeleteEventProcessor deleteEventProcessor = depFactory.Create(queueConfig.AqsQueueProcessorConfiguration);

                for (int x = 0; x < processorCount; ++x)
                {
                    this.processors.Add(
                        eventQueueFactory.Create(
                            client,
                            queueConfig,
                            logger,
                            createEventProcessor,
                            deleteEventProcessor,
                            accountCreateWriter,
                            accountDeleteWriter,
                            counterFactory,
                            deadLetterTable));
                }
            }
        }

        /// <inheritdoc />
        /// <summary>
        ///     Starts the instance for processing events
        /// </summary>
        public void Start() => this.processors.ForEach(p => p.Start());

        /// <inheritdoc />
        /// <summary>
        ///     Starts the instance for processing events
        /// </summary>
        public void Start(TimeSpan delay) => this.processors.ForEach(p => p.Start(delay));

        /// <inheritdoc />
        /// <summary>
        ///     Stops the instance
        /// </summary>
        /// <returns>A task to wait on for the instance to stop</returns>
        public Task StopAsync() => Task.WhenAll(this.processors.Select(p => p.StopAsync()));
    }
}
