// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.Aqs
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web;

    using Live.Mesh.Service.AsyncQueueService.Interface;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Configuration;

    using Newtonsoft.Json;

    /// <summary>
    ///     A client that implements <see cref="IAsyncQueueService2" /> using REST.
    /// </summary>
    /// <seealso cref="AsyncQueueService2Client" />
    /// <seealso cref="IAsyncQueueService2" />
    public class AqsRestClient : IAsyncQueueService2, IDisposable
    {
        private readonly HttpClient client;

        private bool disposed;

        public AqsRestClient(IPrivacyConfigurationManager privacyConfig, Uri root)
        {
            switch (privacyConfig.EnvironmentConfiguration.EnvironmentType)
            {
                case EnvironmentType.ContinuousIntegration:
                    break;
                case EnvironmentType.OneBox:
                    break;
                default:
                    throw new InvalidOperationException($"{nameof(AqsRestClient)} cannot be used in production environments, please check the configuration.");
            }

            this.client = new HttpClient
            {
                BaseAddress = root
            };
        }

        public virtual Dictionary<string, string> AddWork(string queueName, WorkItem workItem)
        {
            throw new NotImplementedException();
        }

        public virtual Task<Dictionary<string, string>> AddWorkAsync(string queueName, WorkItem workItem)
        {
            throw new NotImplementedException();
        }

        public virtual void CompleteWork(string queueName, string aggregationGroupId)
        {
            this.CompleteWorkAsync(queueName, aggregationGroupId).GetAwaiter().GetResult();
        }

        public virtual async Task CompleteWorkAsync(string queueName, string aggregationGroupId)
        {
            HttpResponseMessage response = await this.client.PostAsync($"CompleteWork?queueName={queueName}&aggregationGroupId={aggregationGroupId}", null);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpException((int)response.StatusCode, response.ReasonPhrase);
            }
        }

        public virtual void DeleteWorkItems(string queueName, string aggregationKey, string workItemId)
        {
            throw new NotImplementedException();
        }

        public virtual Task DeleteWorkItemsAsync(string queueName, string aggregationKey, string workItemId)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual void ExtendLease(string queueName, string aggregationGroupId, int leaseExtensionSeconds)
        {
            throw new NotImplementedException();
        }

        public virtual Task ExtendLeaseAsync(string queueName, string aggregationGroupId, int leaseExtensionSeconds)
        {
            throw new NotImplementedException();
        }

        public virtual WorkItem GetWorkItem(string queueName, string workItemId)
        {
            throw new NotImplementedException();
        }

        public virtual Task<WorkItem> GetWorkItemAsync(string queueName, string workItemId)
        {
            throw new NotImplementedException();
        }

        public virtual WorkItem[] GetWorkItems(string queueName, string aggregationKey, string workItemId)
        {
            throw new NotImplementedException();
        }

        public virtual Task<WorkItem[]> GetWorkItemsAsync(string queueName, string aggregationKey, string workItemId)
        {
            throw new NotImplementedException();
        }

        public virtual void ReleaseWork(string queueName, string aggregationGroupId, int waitIntervalSeconds, string debugInfo)
        {
            this.ReleaseWorkAsync(queueName, aggregationGroupId, waitIntervalSeconds, debugInfo).GetAwaiter().GetResult();
        }

        public virtual async Task ReleaseWorkAsync(string queueName, string aggregationGroupId, int waitIntervalSeconds, string debugInfo)
        {
            HttpResponseMessage response = await this.client.PostAsync($"ReleaseWork?queueName={queueName}&aggregationGroupId={aggregationGroupId}&waitIntervalSeconds={waitIntervalSeconds}&debugInfo={debugInfo}", null);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpException((int)response.StatusCode, response.ReasonPhrase);
            }
        }

        public virtual AggregationGroup[] TakeWork(string queueName, short aggregationGroupsToTake, int leaseTimeoutSeconds)
        {
            return this.TakeWorkAsync(queueName, aggregationGroupsToTake, leaseTimeoutSeconds).GetAwaiter().GetResult();
        }

        public virtual async Task<AggregationGroup[]> TakeWorkAsync(string queueName, short aggregationGroupsToTake, int leaseTimeoutSeconds)
        {
            HttpResponseMessage response = await this.client.PostAsync($"TakeWork?queueName={queueName}&aggregationGroupsToTake={aggregationGroupsToTake}&leaseTimeoutSeconds={leaseTimeoutSeconds}", null);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpException((int)response.StatusCode, response.ReasonPhrase);
            }

            return JsonConvert.DeserializeObject<AggregationGroup[]>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            if (disposing)
            {
                this.client?.Dispose();
            }

            this.disposed = true;
        }

        ~AqsRestClient()
        {
            this.Dispose(false);
        }
    }
}
