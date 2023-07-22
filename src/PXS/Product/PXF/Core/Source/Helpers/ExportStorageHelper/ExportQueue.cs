// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.ExportStorageHelper
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.DataContracts.ExportTypes;
    using Microsoft.Azure.Storage.Queue;

    using Newtonsoft.Json;

    /// <summary>
    ///     Queuing mechanism which schedules export data tasks and cosmos reader tasks and retries them until they expire or are complete
    ///     This class is a singleton shared across threads and can be acquired via the Export Storage Provider
    /// </summary>
    public class ExportQueue : IExportQueue
    {
        private const int InitialVisibilityTries = 2;

        private readonly CloudQueue exportStatusQueue;

        private TimeSpan initialVisibility;

        private TimeSpan subsequentVisibility;

        private TimeSpan timeToLive;

        /// <summary>
        ///     Construct the Export Queue
        /// </summary>
        /// <param name="queueClient"></param>
        /// <param name="queueName"></param>
        public ExportQueue(CloudQueueClient queueClient, string queueName)
        {
            this.exportStatusQueue = queueClient.GetQueueReference(queueName);
        }

        /// <summary>
        ///     Add a Message to the Export Queue
        /// </summary>
        public Task AddMessageAsync(BaseQueueMessage baseQueueMsg, TimeSpan? visibilityDelay = null)
        {
            string strMsg = JsonConvert.SerializeObject(baseQueueMsg);
            var msg = new CloudQueueMessage(strMsg);
            return this.exportStatusQueue.AddMessageAsync(msg, this.timeToLive, visibilityDelay, null, null);
        }

        /// <summary>
        ///     deletes all messages from the queue
        /// </summary>
        /// <returns></returns>
        public Task ClearMessagesAsync()
        {
            return this.exportStatusQueue.ClearAsync();
        }

        /// <summary>
        ///     Complete the message
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public Task CompleteMessageAsync(BaseQueueMessage msg)
        {
            return this.exportStatusQueue.DeleteMessageAsync(msg.MessageId, msg.PopRecipt);
        }

        /// <summary>
        ///     Get a Message from the Queue
        /// </summary>
        /// <returns></returns>
        public async Task<BaseQueueMessage> GetMessageAsync()
        {
            BaseQueueMessage msg = null;
            CloudQueueMessage retrievedMessage = await this.exportStatusQueue.GetMessageAsync(this.initialVisibility, null, null).ConfigureAwait(false);
            if (retrievedMessage != null)
            {
                msg = JsonConvert.DeserializeObject<BaseQueueMessage>(retrievedMessage.AsString);
                msg.MessageId = retrievedMessage.Id;
                msg.PopRecipt = retrievedMessage.PopReceipt;
                msg.DequeueCount = retrievedMessage.DequeueCount;
                msg.InsertionTime = retrievedMessage.InsertionTime;
                msg.NextVisibleTime = retrievedMessage.NextVisibleTime;

                if (retrievedMessage.DequeueCount > InitialVisibilityTries)
                {
                    await this.exportStatusQueue.UpdateMessageAsync(retrievedMessage, this.subsequentVisibility, MessageUpdateFields.Visibility).ConfigureAwait(false);

                    // If we update the message, we have a new pop receipt
                    msg.PopRecipt = retrievedMessage.PopReceipt;
                }
            }

            return msg;
        }

        /// <summary>
        ///     Initialize the export queue
        /// </summary>
        /// <param name="ttl">time to live for the message before it is automatically deleted</param>
        /// <param name="initialVisibilityTime">initial time the queue gives the client to complete the message before making it available to be dequeued again</param>
        /// <param name="subsequentVisibilityTime">intended to be a longer period the initial invisibility for messages that fail a couple times</param>
        /// <returns></returns>
        public async Task InitializeAsync(TimeSpan ttl, TimeSpan initialVisibilityTime, TimeSpan subsequentVisibilityTime)
        {
            await this.exportStatusQueue.CreateIfNotExistsAsync().ConfigureAwait(false);
            this.timeToLive = ttl;
            this.initialVisibility = initialVisibilityTime;
            this.subsequentVisibility = subsequentVisibilityTime;
        }

        /// <summary>
        ///     See the next message from the queue without changing its state
        /// </summary>
        /// <returns></returns>
        public async Task<BaseQueueMessage> PeekMessageAsync()
        {
            BaseQueueMessage msg = null;
            CloudQueueMessage retrievedMessage = await this.exportStatusQueue.PeekMessageAsync().ConfigureAwait(false);
            if (retrievedMessage != null)
            {
                msg = JsonConvert.DeserializeObject<BaseQueueMessage>(retrievedMessage.AsString);
                msg.MessageId = retrievedMessage.Id;
                msg.PopRecipt = retrievedMessage.PopReceipt;
                msg.DequeueCount = retrievedMessage.DequeueCount;
                msg.InsertionTime = retrievedMessage.InsertionTime;
                msg.NextVisibleTime = retrievedMessage.NextVisibleTime;
            }
            return msg;
        }
    }
}
