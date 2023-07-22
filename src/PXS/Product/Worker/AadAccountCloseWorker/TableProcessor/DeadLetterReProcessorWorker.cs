// <copyright company="Microsoft Corporation">
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

using Microsoft.Azure.ComplianceServices.Common;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Membership.MemberServices.Common.Worker;
using Microsoft.Membership.MemberServices.Configuration;
using Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.QueueProcessor;
using Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.TableStorage;
using Microsoft.Membership.MemberServices.Privacy.Core.AadAccountClose;
using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.AzureStorageHelper;
using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Queues;
using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.WorkerTasks.Tables;
using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
using Microsoft.PrivacyServices.Common.Azure;
using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Membership.MemberServices.Privacy.AadAccountCloseWorker.DeadLetterReProcessor
{
    public class DeadLetterReProcessorWorker : BackgroundWorker
    {
        private readonly ILogger logger;
        private readonly TimeSpan MaxTimeoutGetMessages = TimeSpan.FromSeconds(10);
        private readonly TimeSpan MaxTimeoutInsertMessages = TimeSpan.FromSeconds(60);
        private readonly IAadAccountCloseService pcfProxyService;
        private readonly IAccountCloseQueueManager queueManager;
        private readonly IAppConfiguration appConfiguration;
        private readonly IAadAccountCloseQueueProccessorConfiguration config;
        private readonly ITable<AccountCloseDeadLetterStorage> deadLetterTable;
        private readonly AzureStorageProvider storage;
        private readonly string DateFormat = "yyyy-MM-dd";
        private readonly string LockContainer = "locks";
        private readonly string DeadLetterContentDumpContainer = "deadlettercontentdumpcontainer"; 
        private readonly Guid Id = Guid.NewGuid();
        private string lastLeaseId = "";
        private int totalProcessedSoFar = 0;
        private DateTime startTime = DateTime.UtcNow;
        private List<string> partitionKeyList = null;
        private int delayInSeconds = 30;

        /// <summary>
        ///     Creates a new instance of <see cref="DeadLetterReProcessorWorker" />
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="configuration">The queue processor configuration</param>
        /// <param name="queueManager">The queue</param>
        /// <param name="aadAccountCloseService">The aad account close service.</param>
        /// <param name="deadLetterTable">The dead letter table for items that were not processable.</param>
        /// <param name="appConfiguration">The Azure App Configuration instance</param>
        /// <param name="storage">storage Account containing the dead letter table</param>
        public DeadLetterReProcessorWorker(
            ILogger logger,
            IAadAccountCloseQueueProccessorConfiguration configuration,
            IAccountCloseQueueManager queueManager,
            IAadAccountCloseService aadAccountCloseService,
            ITable<AccountCloseDeadLetterStorage> deadLetterTable,
            IAppConfiguration appConfiguration,
            AzureStorageProvider storage)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.queueManager = queueManager ?? throw new ArgumentNullException(nameof(queueManager));
            this.pcfProxyService = aadAccountCloseService ?? throw new ArgumentNullException(nameof(aadAccountCloseService));
            this.deadLetterTable = deadLetterTable ?? throw new ArgumentNullException(nameof(deadLetterTable));
            this.appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));

            this.config = configuration;
            this.storage = storage;
        }

        /// <inheritdoc />
        public override void Start()
        {
            base.Start(TimeSpan.FromSeconds(delayInSeconds));
        }

        /// <inheritdoc />
        public override async Task<bool> DoWorkAsync()
        {
            var totalProcessedInCurrentRun = 0;
            logger.Information(nameof(DeadLetterReProcessorWorker), "Doing work.");
            try
            {
                // check if we need to work.
                if (await appConfiguration.IsFeatureFlagEnabledAsync(FeatureNames.PXS.DeadLetterReProcessingEnabled,
                        CustomOperatorContextFactory.CreateDefaultStringComparisonContext(this.storage.AccountName), true))
                {
                    logger.Information(nameof(DeadLetterReProcessorWorker), $"Acquiring lock {this.Id}");
                    bool leaseAcquired = await AcquireOrExtendLockLeaseAsync();
                    if (leaseAcquired)
                    {
                        logger.Information(nameof(DeadLetterReProcessorWorker), $"Acquired lock {this.Id}");

                        // check if batch size has bee changed.
                        var configString = appConfiguration.GetConfigValue(ConfigNames.PXS.AadAccountCloseWorker_DeadLetterTableReProcessingConfig, defaultValue: "");

                        if (!string.IsNullOrEmpty(configString))
                        {
                            // Time window needs to be supplied through configs.
                            var processorConfig = JsonConvert.DeserializeObject<DeadLetterReProcessorConfig>(configString);

                            DateTime startDate = DateTime.ParseExact(processorConfig.startDate, DateFormat, CultureInfo.InvariantCulture);
                            DateTime endDate = DateTime.ParseExact(processorConfig.endDate, DateFormat, CultureInfo.InvariantCulture);

                            string partitionKey = null;

                            // Get latest partition key to work with.
                            this.partitionKeyList = this.partitionKeyList ?? await FetchAllPartitionKeysAsync();

                            DateTime startTime = DateTime.UtcNow;

                            bool shouldWait = false;

                            // invocation per minute * 60 minutes 
                            var maxInvocationPerHour = 60 * (60 / delayInSeconds);

                            // wait time is in minutes, and batch size is per hour
                            var batchSizePerInvocation = processorConfig.batchSize / maxInvocationPerHour;

                            while (!shouldWait)
                            {
                                if (this.partitionKeyList != null && this.partitionKeyList.Count > 0)
                                {
                                    partitionKey = this.partitionKeyList[0];
                                    logger.Information(nameof(DeadLetterReProcessorWorker), $"total partitionKeys Remaining = {this.partitionKeyList.Count}");
                                }

                                if (partitionKey == null)
                                {
                                    logger.Information(nameof(DeadLetterReProcessorWorker), $"partitionKey == null, nothing to process");
                                    // nothing to process.
                                    return false;
                                }

                                string filter = TableQuery.CombineFilters(
                                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey),
                                    TableOperators.And,
                                    TableQuery.CombineFilters(
                                    TableQuery.GenerateFilterConditionForDate("Timestamp", QueryComparisons.GreaterThanOrEqual, DateTime.SpecifyKind(startDate, DateTimeKind.Utc)),
                                    TableOperators.And,
                                    TableQuery.GenerateFilterConditionForDate("Timestamp", QueryComparisons.LessThan, DateTime.SpecifyKind(endDate, DateTimeKind.Utc))));

                                logger.Information(nameof(DeadLetterReProcessorWorker), $"Filter string={filter}");

                                // BatchSize is per hour, the worker is run on a 5 minute cadence, divide by 30 to get approx batch for each 2 mins run.
                                ICollection<AccountCloseDeadLetterStorage> deadLtrEntries = await deadLetterTable.QueryAsync(filter, batchSizePerInvocation, null);

                                logger.Information(nameof(DeadLetterReProcessorWorker), $"Query fetched {deadLtrEntries.Count} entries");

                                // If the count is 0, move to next item in the list.
                                if (deadLtrEntries.Count == 0)
                                {
                                    logger.Information(nameof(DeadLetterReProcessorWorker), $"no more entries in the table for partition={partitionKey}");
                                    // Remove the first element.
                                    this.partitionKeyList.RemoveAt(0);
                                }
                                else
                                {
                                    var shouldPushToQueue = await appConfiguration.IsFeatureFlagEnabledAsync(FeatureNames.PXS.DeadLetterRePushingToQueueEnabled, false);

                                    if (shouldPushToQueue)
                                    {
                                        List<List<AccountCloseDeadLetterStorage>> listOfList = deadLtrEntries
                                                                                                .Select((x, i) => new { Index = i, Value = x })
                                                                                                .GroupBy(x => x.Index / 32)
                                                                                                .Select(x => x.Select(v => v.Value).ToList())
                                                                                                .ToList();

                                        foreach (var list in listOfList)
                                        {
                                            // Set some delay for visibility.
                                            await this.queueManager.EnqueueAsync(deadLtrEntries.Select(x => x.DataActual).ToList(), TimeSpan.FromMinutes(30), new CancellationTokenSource().Token);

                                            // we have some entries that were processed, delete those from table.
                                            await deadLetterTable.DeleteBatchAsync(list);

                                            // increment the processed count;
                                            totalProcessedInCurrentRun += list.Count;
                                        }
                                    }
                                    else
                                    {
                                        List<IQueueItem<AccountCloseRequest>> queueItemsWrapped = new List<IQueueItem<AccountCloseRequest>>();

                                        var result = deadLtrEntries.Select(x => new AzureQueueItem<AccountCloseRequest>(null, null, x.DataActual)).ToList();

                                        queueItemsWrapped.AddRange(result);

                                        // send to aadrvs in batch of 32 items
                                        List<List<IQueueItem<AccountCloseRequest>>> listOfList = queueItemsWrapped
                                                                                                .Select((x, i) => new { Index = i, Value = x })
                                                                                                .GroupBy(x => x.Index / 32)
                                                                                                .Select(x => x.Select(v => v.Value).ToList())
                                                                                                .ToList();

                                        foreach (var list in listOfList)
                                        {
                                            IList<ServiceResponse<IQueueItem<AccountCloseRequest>>> responses = await this.pcfProxyService.PostBatchAccountCloseAsync(list).ConfigureAwait(false);
                                            IList<ServiceResponse<IQueueItem<AccountCloseRequest>>> responsesProcessedSuccesfully = new List<ServiceResponse<IQueueItem<AccountCloseRequest>>>();
                                            var totalFailed = 0;
                                            var totalPass = 0;
                                            foreach (ServiceResponse<IQueueItem<AccountCloseRequest>> serviceResponse in responses)
                                            {
                                                if (serviceResponse.IsSuccess)
                                                {
                                                    totalPass++;
                                                    responsesProcessedSuccesfully.Add(serviceResponse);
                                                }
                                                else
                                                {
                                                    totalFailed++;
                                                    this.logger.Error(nameof(DeadLetterReProcessorWorker), $"Error processing AccountClose for Tenant={partitionKey}, Error={serviceResponse?.Error?.ToString()}");
                                                }
                                            }

                                            totalProcessedInCurrentRun += totalPass;
                                            totalProcessedSoFar += totalPass;

                                            if (totalFailed > 0)
                                            {
                                                logger.Error(nameof(DeadLetterReProcessorWorker), $"Result Success={totalPass}, failed = {totalFailed}  total={responses.Count}");
                                            }

                                            if (totalPass > 0)
                                            {
                                                List<AccountCloseDeadLetterStorage> cleanup = new List<AccountCloseDeadLetterStorage>();
                                                foreach (var entry in responsesProcessedSuccesfully)
                                                {
                                                    var tableItemToDelete = deadLtrEntries.FirstOrDefault(y => y.DataActual == entry.Result.Data);
                                                    if (tableItemToDelete != default)
                                                    {
                                                        cleanup.Add(tableItemToDelete);
                                                    }
                                                    else
                                                    {
                                                        logger.Error(nameof(DeadLetterReProcessorWorker), $"Could not match the accountCloseRequest back to a table item, Quiting!!");
                                                        return false;
                                                    }
                                                }
                                                // we have some entries that were processed, delete those from table.
                                                await deadLetterTable.DeleteBatchAsync(cleanup);
                                            }
                                            else
                                            {
                                                logger.Error(nameof(DeadLetterReProcessorWorker), $"Nothing processed in current run, Skipping tenant={partitionKey} !");
                                                this.partitionKeyList.RemoveAt(0);
                                            }
                                        }
                                    }


                                    // if we processed enough, break out and wait for next run to respect per hour batch size
                                    if (totalProcessedInCurrentRun >= batchSizePerInvocation)
                                    {
                                        shouldWait = true;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    logger.Information(nameof(DeadLetterReProcessorWorker), "feature flag is disabled for deadletterprocessing, Quiting!!!");
                }
            }
            catch (TaskCanceledException e)
            {
                // PostBatchAccountCloseAsync is throwing a TaskCancelledException for some cases in invalid_grant errors.
                // we want to skip those too.
                if (this.partitionKeyList != null && this.partitionKeyList.Count > 0)
                {
                    var partitionKey = this.partitionKeyList[0];
                    this.partitionKeyList.RemoveAt(0);
                    logger.Error(nameof(DeadLetterReProcessorWorker), e, $"Nothing processed in current run, Skipping tenant={partitionKey} !");
                }
                else
                {
                    logger.Error(nameof(DeadLetterReProcessorWorker), e, $"{nameof(this.DoWorkAsync)} was canceled, Quiting");
                }
            }
            catch (Exception e)
            {
                logger.Error(nameof(DeadLetterReProcessorWorker), e, $"{nameof(this.DoWorkAsync)} ran into errors, Quiting");
            }

            if (totalProcessedInCurrentRun > 0)
            {
                logger.Information(nameof(DeadLetterReProcessorWorker), $" WorkerId= {Id} Progress totalProcessedSoFar={totalProcessedSoFar}, totalProcessedInCurrentRun = {totalProcessedInCurrentRun}, timeSpentInHours = {DateTime.UtcNow.Subtract(startTime).TotalHours} Minutes = {DateTime.UtcNow.Subtract(startTime).Minutes}");
            }

            logger.Information(nameof(DeadLetterReProcessorWorker), $" Will be run again");
            // Schedule a re run
            return false;
        }

        private async Task<bool> AcquireOrExtendLockLeaseAsync()
        {
            try
            {
                var blobClient = storage.CreateCloudBlobClient();
                await blobClient.GetContainerReference(LockContainer).CreateIfNotExistsAsync();
                var blobReference = blobClient.GetBlobReferenceFromServer(new Uri($"https://{storage.AccountName}.blob.core.windows.net/{LockContainer}/deadLetterLock.json"));

                // UpdateLock.
                using (MemoryStream stream = new MemoryStream())
                {
                    await DownloadLockContentAsync(stream, blobReference);
                    stream.Position = 0;
                    StreamReader reader = new StreamReader(stream);
                    string text = reader.ReadToEnd();
                    logger.Warning(nameof(DeadLetterReProcessorWorker), $"lockContent {text}");
                    if (!string.IsNullOrEmpty(text))
                    {
                        DeadLetterLockContent content = JsonConvert.DeserializeObject<DeadLetterLockContent>(text);
                        if (!string.IsNullOrEmpty(content.Id) && content.Id != this.Id.ToString())
                        {
                            DateTime time = DateTime.UtcNow;
                            if (time.Subtract(DateTime.Parse(content.acquireDateTime)) < TimeSpan.FromHours(2))
                            {
                                logger.Warning(nameof(DeadLetterReProcessorWorker), "do no own lock, will wait and try again after 30 mins");
                                return false;
                            }
                        }
                    }

                    await AcquireOrRenewBlobLeaseAsync(blobReference);

                    var jsonContent = JsonConvert.SerializeObject(new DeadLetterLockContent()
                            {
                                Id = this.Id.ToString(),
                                acquireDateTime = DateTime.UtcNow.ToString()
                            });

                    byte[] byteArray = Encoding.UTF8.GetBytes(jsonContent);
                    using (var inputstream = new MemoryStream(byteArray))
                    {
                        await blobReference.UploadFromStreamAsync(inputstream, AccessCondition.GenerateLeaseCondition(this.lastLeaseId), null, null);
                    }
                }
            } 
            catch (Exception ex)
            {
                logger.Error(nameof(DeadLetterReProcessorWorker), "failed to acquire lock", ex);
                return false;
            }

            return true;
        }

        private async Task DownloadLockContentAsync(Stream stream, ICloudBlob blobReference)
        {
            if (this.lastLeaseId != null)
            {
                try
                {
                    await blobReference.DownloadToStreamAsync(stream, AccessCondition.GenerateLeaseCondition(this.lastLeaseId), null, null);

                }
                catch (Azure.Storage.StorageException _)
                {
                    // Lease could have expired, try downloading without lease
                    await blobReference.DownloadToStreamAsync(stream);
                    // reset lease
                    this.lastLeaseId = null;
                }
            }
            else
            {
                await blobReference.DownloadToStreamAsync(stream);
            }
        }


        private async Task AcquireOrRenewBlobLeaseAsync(ICloudBlob blobReference)
        {
            if (this.lastLeaseId != null)
            {
                try
                {
                    // We own the lease, extend it
                    await blobReference.RenewLeaseAsync(AccessCondition.GenerateLeaseCondition(this.lastLeaseId));
                    return;
                }
                catch (Exception ex)
                {
                    logger.Error(nameof(DeadLetterReProcessorWorker), "failed to renew lease", ex);
                }
            }

            this.lastLeaseId = await blobReference.AcquireLeaseAsync(TimeSpan.FromSeconds(50));
        }

        private async Task<List<string>> FetchAllPartitionKeysAsync()
        {
            try
            {
                var blobClient = storage.CreateCloudBlobClient();
                var blobReference = blobClient.GetBlobReferenceFromServer(new Uri($"https://{storage.AccountName}.blob.core.windows.net/{DeadLetterContentDumpContainer}/deadLetterTableContents.csv"));

                var indexForPartitionKey = 0;
                var partitionKeySet = new HashSet<string>();

                // read csv file
                using (MemoryStream stream = new MemoryStream())
                {
                    await blobReference.DownloadToStreamAsync(stream);
                    stream.Position = 0;
                    StreamReader reader = new StreamReader(stream);
                    string text = reader.ReadLine();

                    string[] headers = text.Split(',');
                    for (var i = 0; i < headers.Length; i++)
                    {
                        if (headers[i] == "PartitionKey")
                        {
                            indexForPartitionKey = i;
                            break;
                        }
                    }

                    while ((text = reader.ReadLine()) != null)
                    {
                        partitionKeySet.Add(text.Split(',')[indexForPartitionKey]);
                    }
                }

                return partitionKeySet.ToList();
            }
            catch (Exception ex)
            {
                logger.Error(nameof(DeadLetterReProcessorWorker), ex, "failed to parse csv file");
                return null;
            }
        }
    }

    public struct DeadLetterReProcessorConfig
    {
        public int batchSize;
        public string startDate;
        public string endDate;
        public List<string> partitionKeys;
    }

    public struct DeadLetterLockContent
    {
        public string Id;
        public string acquireDateTime;
    }
}
