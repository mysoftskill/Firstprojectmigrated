// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyMockService.Controllers.Aqs
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http;
    using System.Xml.Serialization;

    using Live.Mesh.Service.AsyncQueueService.Interface;

    using Microsoft.Membership.MemberServices.Common.Configuration.Privacy;
    using Microsoft.Membership.MemberServices.Common.SecretStore;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Privacy.Core.AQS;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.Azure.Storage.Queue;
    using Microsoft.Azure.Storage.RetryPolicies;

    using Newtonsoft.Json;

    internal class AggregationId
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("receipt")]
        public string Receipt { get; set; }
    }

    public class UserItem
    {
        [JsonProperty("eventData")]
        public EventData EventData { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("isClose")]
        public bool IsClose { get; set; }
    }

    [RoutePrefix("aqs")]
    public class AqsController : ApiController
    {
        private static AzureStorageProvider azureStorageProvider;

        readonly ConcurrentDictionary<string, ICloudQueue> queues = new ConcurrentDictionary<string, ICloudQueue>();

        public AqsController()
        {
            this.InitializeStorageAsync().GetAwaiter().GetResult();
        }

        [HttpPost]
        [Route("AddMultipleWork")]
        public async Task<IHttpActionResult> AddMultipleWork(string queueName, [FromBody] UserItem[] data)
        {
            var queue = await this.GetQueueAsync(queueName);
            IEnumerable<Task> tasks = data.Select(item => queue.EnqueueAsync(new CloudQueueMessage(JsonConvert.SerializeObject(item))));
            await Task.WhenAll(tasks);

            return this.Ok();
        }

        [HttpPost]
        [Route("AddWork")]
        public async Task<IHttpActionResult> AddWorkAsync(string queueName, long puid, bool isClose, [FromBody] EventData eventData)
        {
            var queue = await this.GetQueueAsync(queueName);
            await queue.EnqueueAsync(
                new CloudQueueMessage(
                    JsonConvert.SerializeObject(
                        new UserItem
                        {
                            Id = puid,
                            IsClose = isClose,
                            EventData = eventData
                        })));

            return this.Ok();
        }

        [HttpPost]
        [Route("CompleteWork")]
        public async Task<IHttpActionResult> CompleteWorkAsync(string queueName, string aggregationGroupId)
        {
            var queue = await this.GetQueueAsync(queueName);
            var receipt = DecodeAggregationId(aggregationGroupId);
            if (await queue.CompleteItemAsync(receipt.Id, receipt.Receipt))
            {
                return this.Ok();
            }

            return this.Conflict();
        }

        [HttpPost]
        [Route("ReleaseWork")]
        public async Task<IHttpActionResult> ReleaseWorkAsync(string queueName, string aggregationGroupId, int waitIntervalSeconds, string debugInfo)
        {
            // todo: need to see if we can update using the id and pop receipt, appears to only be complete that supports this
            var receipt = await Task.Run(() => DecodeAggregationId(aggregationGroupId));
            return this.Ok();
        }

        [HttpPost]
        [Route("TakeWork")]
        public async Task<IHttpActionResult> TakeWorkAsync(string queueName, short aggregationGroupsToTake, int leaseTimeoutSeconds)
        {
            var queue = await this.GetQueueAsync(queueName);
            var groups = new List<AggregationGroup>();
            var xmlSerializer = new XmlSerializer(typeof(CDPEvent2));
            while (aggregationGroupsToTake > 0)
            {
                short toTake = Math.Min(aggregationGroupsToTake, (short)32);
                var items = await queue.DequeueAsync(toTake, TimeSpan.FromSeconds(leaseTimeoutSeconds), TimeSpan.FromSeconds(10), new NoRetry(), CancellationToken.None);
                foreach (var item in items)
                {
                    var aggId = new AggregationId
                    {
                        Id = item.Id,
                        Receipt = item.PopReceipt
                    };

                    var userItem = JsonConvert.DeserializeObject<UserItem>(item.AsString);
                    using (var xmlContent = new MemoryStream())
                    {
                        var evt = new CDPEvent2
                        {
                            AggregationKey = userItem.Id.ToString("X16")
                        };

                        if (userItem.IsClose)
                        {
                            evt.EventData = new UserDelete
                            {
                                Property = userItem.EventData?.Property
                            };
                        }
                        else
                        {
                            evt.EventData = new UserCreate
                            {
                                Property = userItem.EventData?.Property
                            };
                        }

                        xmlSerializer.Serialize(xmlContent, evt);
                        groups.Add(
                            new AggregationGroup
                            {
                                LeaseExpirationTime = item.ExpirationTime?.DateTime ?? DateTime.UtcNow.AddSeconds(leaseTimeoutSeconds),
                                QueueName = queueName,
                                TakenTime = DateTime.UtcNow,
                                Id = EncodingAggregationId(aggId),
                                WorkItems = new[]
                                {
                                    new WorkItem
                                    {
                                        SubmissionTime = item.InsertionTime?.DateTime ?? DateTime.MinValue,
                                        Id = userItem.Id.ToString("X16"),
                                        Payload = xmlContent.ToArray()
                                    }
                                }
                            });
                    }
                }

                aggregationGroupsToTake -= toTake;
            }

            return this.Ok(groups.ToArray());
        }

        private ILogger Logger { get; } = IfxTraceLogger.Instance;

        internal async Task InitializeStorageAsync()
        {
            if (azureStorageProvider == null)
            {
                azureStorageProvider = new AzureStorageProvider(
                    this.Logger,
                    new AzureKeyVaultReader(
                        Program.PartnerMockConfigurations,
                        new Clock(),
                        this.Logger));

                await azureStorageProvider.InitializeAsync(Program.PartnerMockConfigurations.PartnerMockConfiguration.PrivacyCommandAzureStorageConfiguration).ConfigureAwait(false);
            }
        }

        private async Task<ICloudQueue> GetQueueAsync(string queueName, CancellationToken token = default)
        {
            queueName = queueName.ToLowerInvariant();
            if (!this.queues.TryGetValue(queueName, out var queue))
            {
                queue = await azureStorageProvider.GetCloudQueueAsync(queueName, token);
                this.queues.TryAdd(queueName, queue);
            }

            return queue;
        }

        private static AggregationId DecodeAggregationId(string aggregationGroupId)
        {
            return JsonConvert.DeserializeObject<AggregationId>(Encoding.UTF8.GetString(Convert.FromBase64String(WebUtility.UrlDecode(aggregationGroupId))));
        }

        private static string EncodingAggregationId(AggregationId id)
        {
            return WebUtility.UrlEncode(Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(id))));
        }
    }
}
