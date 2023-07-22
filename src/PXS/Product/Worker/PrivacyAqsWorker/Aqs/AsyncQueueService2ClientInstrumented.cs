// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.Aqs
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.Threading.Tasks;

    using Live.Mesh.Service.AsyncQueueService.Interface;

    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;

    /// <summary>
    ///     Instrumented client for interacting with AQS
    /// </summary>
    /// <seealso cref="AsyncQueueService2Client" />
    /// <seealso cref="IAsyncQueueService2" />
    public class AsyncQueueService2ClientInstrumented : IAsyncQueueService2
    {
        private const string DependencyType = "AQS";

        private readonly IAsyncQueueService2 client;

        private readonly string PartnerId;

        private const string Version = "1";

        /// <summary>
        ///     Initializes a new instance of the <see cref="AsyncQueueService2ClientInstrumented" /> class.
        /// </summary>
        /// <param name="client">Queue service client.</param>
        /// <param name="partnerId">Partner id.</param>
        public AsyncQueueService2ClientInstrumented(IAsyncQueueService2 client, string partnerId)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            this.PartnerId = partnerId ?? throw new ArgumentNullException(nameof(partnerId));
        }

        public virtual Dictionary<string, string> AddWork(string queueName, WorkItem workItem)
        {
            return this.InstrumentOutgoingAqsCall(nameof(this.AddWork), queueName, HttpMethod.Post, () => this.client.AddWork(queueName, workItem));
        }

        public virtual Task<Dictionary<string, string>> AddWorkAsync(string queueName, WorkItem workItem)
        {
            return this.InstrumentOutgoingAqsCallAsync(nameof(this.AddWork), queueName, HttpMethod.Post, () => this.client.AddWorkAsync(queueName, workItem));
        }

        public virtual void CompleteWork(string queueName, string aggregationGroupId)
        {
            this.InstrumentOutgoingAqsCall(nameof(this.CompleteWork), queueName, HttpMethod.Post, () => this.client.CompleteWork(queueName, aggregationGroupId));
        }

        public virtual Task CompleteWorkAsync(string queueName, string aggregationGroupId)
        {
            return this.InstrumentOutgoingAqsCallAsync(nameof(this.CompleteWork), queueName, HttpMethod.Post, () => this.client.CompleteWorkAsync(queueName, aggregationGroupId));
        }

        public virtual void DeleteWorkItems(string queueName, string aggregationKey, string workItemId)
        {
            this.InstrumentOutgoingAqsCall(nameof(this.DeleteWorkItems), queueName, HttpMethod.Post, () => this.client.DeleteWorkItems(queueName, aggregationKey, workItemId));
        }

        public virtual Task DeleteWorkItemsAsync(string queueName, string aggregationKey, string workItemId)
        {
            return this.InstrumentOutgoingAqsCallAsync(nameof(this.DeleteWorkItems), queueName, HttpMethod.Post, () => this.client.DeleteWorkItemsAsync(queueName, aggregationKey, workItemId));
        }

        public virtual void ExtendLease(string queueName, string aggregationGroupId, int leaseExtensionSeconds)
        {
            this.InstrumentOutgoingAqsCall(nameof(this.ExtendLease), queueName, HttpMethod.Post, () => this.client.ExtendLease(queueName, aggregationGroupId, leaseExtensionSeconds));
        }

        public virtual Task ExtendLeaseAsync(string queueName, string aggregationGroupId, int leaseExtensionSeconds)
        {
            return this.InstrumentOutgoingAqsCallAsync(
                nameof(this.ExtendLease),
                queueName,
                HttpMethod.Post,
                () => this.client.ExtendLeaseAsync(queueName, aggregationGroupId, leaseExtensionSeconds));
        }

        public virtual WorkItem GetWorkItem(string queueName, string workItemId)
        {
            return this.InstrumentOutgoingAqsCall(nameof(this.GetWorkItem), queueName, HttpMethod.Get, () => this.client.GetWorkItem(queueName, workItemId));
        }

        public virtual Task<WorkItem> GetWorkItemAsync(string queueName, string workItemId)
        {
            return this.InstrumentOutgoingAqsCallAsync(nameof(this.GetWorkItemAsync), queueName, HttpMethod.Get, () => this.client.GetWorkItemAsync(queueName, workItemId));
        }

        public virtual WorkItem[] GetWorkItems(string queueName, string aggregationKey, string workItemId)
        {
            return this.InstrumentOutgoingAqsCall(nameof(this.GetWorkItems), queueName, HttpMethod.Get, () => this.client.GetWorkItems(queueName, aggregationKey, workItemId));
        }

        public virtual Task<WorkItem[]> GetWorkItemsAsync(string queueName, string aggregationKey, string workItemId)
        {
            return this.InstrumentOutgoingAqsCallAsync(nameof(this.GetWorkItems), queueName, HttpMethod.Get, () => this.client.GetWorkItemsAsync(queueName, aggregationKey, workItemId));
        }

        public virtual void ReleaseWork(string queueName, string aggregationGroupId, int waitIntervalSeconds, string debugInfo)
        {
            this.InstrumentOutgoingAqsCall(nameof(this.ReleaseWork), queueName, HttpMethod.Post, () => this.client.ReleaseWork(queueName, aggregationGroupId, waitIntervalSeconds, debugInfo));
        }

        public virtual Task ReleaseWorkAsync(string queueName, string aggregationGroupId, int waitIntervalSeconds, string debugInfo)
        {
            return this.InstrumentOutgoingAqsCallAsync(
                nameof(this.ReleaseWork),
                queueName,
                HttpMethod.Post,
                () => this.client.ReleaseWorkAsync(queueName, aggregationGroupId, waitIntervalSeconds, debugInfo));
        }

        public virtual AggregationGroup[] TakeWork(string queueName, short aggregationGroupsToTake, int leaseTimeoutSeconds) =>
            this.TakeWorkAsync(queueName, aggregationGroupsToTake, leaseTimeoutSeconds).Result;

        public virtual async Task<AggregationGroup[]> TakeWorkAsync(string queueName, short aggregationGroupsToTake, int leaseTimeoutSeconds)
        {
            OutgoingApiEventWrapper eventWrapper = OutgoingApiEventWrapper.CreateBasicOutgoingEvent(
                PartnerId,
                $"{nameof(this.TakeWork)}.{queueName}",
                Version,
                queueName,
                HttpMethod.Get,
                DependencyType);

            eventWrapper.ExtraData["GroupsToTake"] = $"{aggregationGroupsToTake}";
            eventWrapper.ExtraData["LeaseTimeout"] = $"{leaseTimeoutSeconds}";

            eventWrapper.Start();
            try
            {
                AggregationGroup[] result = await this.client.TakeWorkAsync(queueName, aggregationGroupsToTake, leaseTimeoutSeconds).ConfigureAwait(false);
                eventWrapper.ExtraData["GroupsReceived"] = $"{result.Length}";
                eventWrapper.Success = true;

                return result;
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

        private TResult InstrumentOutgoingAqsCall<TResult>(string operationName, string queueName, HttpMethod httpMethod, Func<TResult> method)
        {
            OutgoingApiEventWrapper eventWrapper = OutgoingApiEventWrapper.CreateBasicOutgoingEvent(
                PartnerId,
                $"{operationName}.{queueName}",
                Version,
                queueName,
                httpMethod,
                DependencyType);

            eventWrapper.Start();
            try
            {
                TResult result = method();
                eventWrapper.Success = true;

                return result;
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

        private void InstrumentOutgoingAqsCall(string operationName, string queueName, HttpMethod httpMethod, Action method)
        {
            OutgoingApiEventWrapper eventWrapper = OutgoingApiEventWrapper.CreateBasicOutgoingEvent(
                PartnerId,
                $"{operationName}.{queueName}",
                Version,
                queueName,
                httpMethod,
                DependencyType);

            eventWrapper.Start();
            try
            {
                method();
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

        private async Task InstrumentOutgoingAqsCallAsync(string operationName, string queueName, HttpMethod httpMethod, Func<Task> method)
        {
            OutgoingApiEventWrapper eventWrapper = OutgoingApiEventWrapper.CreateBasicOutgoingEvent(
                PartnerId,
                $"{operationName}.{queueName}",
                Version,
                queueName,
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

        private async Task<TResult> InstrumentOutgoingAqsCallAsync<TResult>(string operationName, string queueName, HttpMethod httpMethod, Func<Task<TResult>> method)
        {
            OutgoingApiEventWrapper eventWrapper = OutgoingApiEventWrapper.CreateBasicOutgoingEvent(
                PartnerId,
                $"{operationName}.{queueName}",
                Version,
                queueName,
                httpMethod,
                DependencyType);

            eventWrapper.Start();
            try
            {
                TResult result = await method().ConfigureAwait(false);
                eventWrapper.Success = true;

                return result;
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
