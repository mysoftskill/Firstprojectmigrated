// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.EventHubProcessor
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using Microsoft.ServiceBus.Messaging;

    /// <summary>
    ///     Class for instrumenting implementations of <see cref="IEventProcessor"/>
    /// </summary>
    public class InstrumentedEventProcessor : IEventProcessor
    {
        private const string Version = "1";

        private const string DependencyType = nameof(IEventProcessor);

        private readonly IEventProcessor eventProcessor;

        private readonly string underlyingClassName;

        private string PartnerId => this.underlyingClassName;

        public InstrumentedEventProcessor(IEventProcessor eventProcessor)
        {
            this.eventProcessor = eventProcessor ?? throw new ArgumentNullException(nameof(eventProcessor));
            this.underlyingClassName = eventProcessor.GetType().Name;
        }

        public async Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            OutgoingApiEventWrapper eventWrapper = OutgoingApiEventWrapper.CreateBasicOutgoingEvent(
                this.PartnerId,
                nameof(this.CloseAsync),
                Version,
                "",
                HttpMethod.Get,
                DependencyType);

            eventWrapper.Start();
            eventWrapper.ExtraData.Add(nameof(CloseReason), reason.ToString());

            try
            {
                await this.eventProcessor.CloseAsync(context, reason).ConfigureAwait(false);
                eventWrapper.Success = true;
            }
            catch (Exception e)
            {
                eventWrapper.Success = false;
                eventWrapper.ErrorMessage = e.ToString();
                throw;
            }
            finally
            {
                eventWrapper.Finish();
            }
        }

        public Task OpenAsync(PartitionContext context)
        {
            return this.InstrumentCallAsync(nameof(this.OpenAsync), HttpMethod.Get, context, () => this.eventProcessor.OpenAsync(context));
        }

        public Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            return this.InstrumentCallAsync(nameof(this.ProcessEventsAsync), HttpMethod.Get, context, () => this.eventProcessor.ProcessEventsAsync(context, messages));
        }

        private async Task InstrumentCallAsync(string operationName, HttpMethod httpMethod, PartitionContext context, Func<Task> method)
        {
            OutgoingApiEventWrapper eventWrapper = OutgoingApiEventWrapper.CreateBasicOutgoingEvent(
                this.PartnerId,
                operationName,
                Version,
                "",
                httpMethod,
                DependencyType);

            eventWrapper.Start();

            try
            {
                await method().ConfigureAwait(false);
                eventWrapper.Success = true;
            }
            catch (Exception e)
            {
                eventWrapper.Success = false;
                eventWrapper.ErrorMessage = e.ToString();
                throw;
            }
            finally
            {
                eventWrapper.Finish();
            }
        }
    }
}
