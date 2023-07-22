// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.EventHubProcessor
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using Microsoft.ServiceBus.Messaging;

    public static class PartitionContextExtensions
    {
        private const string DependencyType = "AzureEventHub";

        private const string PartnerId = "AzureEventHub";

        private const string Version = "1";

        public static Task InstrumentedCheckpointAsync(this PartitionContext context)
        {
            return InstrumentOutgoingCallAsync(nameof(context.CheckpointAsync), HttpMethod.Post, () => context.CheckpointAsync());
        }

        public static Task InstrumentedCheckpointAsync(this PartitionContext context, EventData data)
        {
            return InstrumentOutgoingCallAsync(nameof(context.CheckpointAsync), HttpMethod.Post, () => context.CheckpointAsync(data));
        }

        public static async Task InstrumentOutgoingCallAsync(string operationName, HttpMethod httpMethod, Func<Task> method)
        {
            OutgoingApiEventWrapper eventWrapper = OutgoingApiEventWrapper.CreateBasicOutgoingEvent(
                PartnerId,
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
