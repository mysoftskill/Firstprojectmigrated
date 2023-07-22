namespace Microsoft.Membership.MemberServices.PrivacyMockService.Controllers.RecurringDeletes
{
    using global::Azure.Core;
    using global::Azure.Identity;
    using global::Azure.Storage.Blobs;

    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http;

    using Microsoft.Azure.ComplianceServices.Common.AzureServiceUrlValidator;
    using Microsoft.Azure.Storage;
    using Microsoft.ComplianceServices.Common.Queues;
    using Microsoft.Membership.MemberServices.Privacy.RecurrentDeleteWorker.LockPrimitives;
    using Microsoft.Membership.MemberServices.ScheduleDbClient.Model;
    using Microsoft.Membership.MemberServices.ScheduleDbClient;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.ScheduleDbClient.Exceptions;

    public abstract class RecurringDeleteBaseController : ApiController
    {
        private const string recurringDeleteQueueName = "recurringdeletequeue";

        private readonly ILogger logger;

        protected CloudQueue<RecurrentDeleteScheduleDbDocument> cloudQueue;

        protected readonly IPrivacyConfigurationManager configurationManager;

        protected readonly IScheduleDbClient scheduleDbClient;

        protected AzureBlockBlobPrimitives lockPrimitives;

        protected IAppConfiguration appConfiguration;

        protected readonly IRecurringDeleteWorkerConfiguration recurringDeleteWorkerConfiguration;

        public RecurringDeleteBaseController(
            ILogger logger,
            IPrivacyConfigurationManager configurationManager,
            IScheduleDbClient scheduleDbClient)
        {
            this.logger = logger;
            this.configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
            this.scheduleDbClient = scheduleDbClient ?? throw new ArgumentNullException(nameof(scheduleDbClient));
            this.appConfiguration = new AppConfiguration(@"local.settings.json");
            this.recurringDeleteWorkerConfiguration = configurationManager.RecurringDeleteWorkerConfiguration;
        }

        protected async Task CleanUpCloudQueue()
        {
            InitiaizeCloudQueue();

            while (await this.cloudQueue.GetQueueSizeAsync() > 0)
            {
                var messages = await this.cloudQueue.DequeueBatchAsync(
                     visibilityTimeout: TimeSpan.FromSeconds(1),
                     maxCount: 32,
                     cancellationToken: CancellationToken.None);

                if (messages.Any())
                {
                    foreach (var message in messages)
                    {
                        await message.DeleteAsync().ConfigureAwait(false);
                    }
                }
            }
        }

        protected void InitiaizeCloudQueue()
        {
            if (Program.PartnerMockConfigurations.EnvironmentConfiguration.EnvironmentType == MemberServices.Configuration.EnvironmentType.OneBox)
            {
                this.cloudQueue = new CloudQueue<RecurrentDeleteScheduleDbDocument>(recurringDeleteQueueName);
            }
            else
            {
                TokenCredential credential = new DefaultAzureCredential();
                this.cloudQueue = new CloudQueue<RecurrentDeleteScheduleDbDocument>(
                    Program.PartnerMockConfigurations.PartnerMockConfiguration.RecurringDeleteE2EConfiguration.StorageAccountName,
                    recurringDeleteQueueName,
                    credential);
            }
            this.cloudQueue.CreateIfNotExistsAsync().Wait();
        }

        protected void InitializeLockPrimitive()
        {
            if (lockPrimitives == null)
            {
                Uri uri;
                BlobContainerClient containerClient;

                try
                {
                    var blobContainerName = Program.PartnerMockConfigurations.PartnerMockConfiguration.RecurringDeleteE2EConfiguration.ContainerName;

                    if (Program.PartnerMockConfigurations.EnvironmentConfiguration.EnvironmentType == MemberServices.Configuration.EnvironmentType.OneBox)
                    {
                        containerClient = new BlobContainerClient("UseDevelopmentStorage=true", blobContainerName);
                    }
                    else
                    {
                        var blobAccountName = Program.PartnerMockConfigurations.PartnerMockConfiguration.RecurringDeleteE2EConfiguration.StorageAccountName;
                        TokenCredential credential = new DefaultAzureCredential();
                        
                        var result = StorageAccountValidator.IsValidStorageAccountName(blobAccountName);
                        if (!result.IsValid)
                        {
                            throw new StorageException($"Blob account name {blobAccountName} could not be validated. {result.Reason}");
                        }
                        
                        uri = new Uri($"https://{blobAccountName}.blob.core.windows.net/{blobContainerName}");
                        containerClient = new BlobContainerClient(uri, credential);
                    }
                }
                catch (Exception ex)
                {
                    this.logger.Error(nameof(RecurringDeleteWorkerController), $"Exception occurred. {ex.Message}");
                    throw;
                }

                var blobClient = containerClient.GetBlobClient("recurringdeletescannertest");
                lockPrimitives = new AzureBlockBlobPrimitives(blobClient);
            }
        }

        protected async Task<RecurrentDeleteScheduleDbDocument> CreateOrUpdateRecurringDeletesScheduleDbAsync(RecurrentDeleteScheduleDbDocument scheduleDbDoc)
        {
            RecurrentDeleteScheduleDbDocument scheduleDbDocument = null;
            try
            {
                scheduleDbDocument = await this.scheduleDbClient.CreateOrUpdateRecurringDeletesScheduleDbAsync(scheduleDbDoc).ConfigureAwait(false);
            }
            catch (ScheduleDbClientException ex)
            {
                this.logger.Error(nameof(RecurringDeleteWorkerController), $"Creating testing record in schedule db failed: {ex.Message}");
            }
            return scheduleDbDocument;
        }

        protected async Task<RecurrentDeleteScheduleDbDocument> GetRecurringDeletesScheduleDbDocumentAsync(RecurrentDeleteScheduleDbDocument scheduleDbDoc)
        {
            RecurrentDeleteScheduleDbDocument scheduleDbDocument = null;
            try
            {
                scheduleDbDocument = await this.scheduleDbClient.GetRecurringDeletesScheduleDbDocumentAsync(scheduleDbDoc.Puid, scheduleDbDoc.DataType, CancellationToken.None).ConfigureAwait(false);
            }
            catch (ScheduleDbClientException ex)
            {
                this.logger.Error(nameof(RecurringDeleteWorkerController), $"Retrieving testing record in schedule db failed: {ex.Message}");
            }
            return scheduleDbDocument;
        }

        protected async Task DeleteRecurringDeletesByPuidAsync(long puid)
        {
            try
            {
                await this.scheduleDbClient.DeleteRecurringDeletesByPuidScheduleDbAsync(puid, CancellationToken.None).ConfigureAwait(false);
            }
            catch (ScheduleDbClientException ex)
            {
                this.logger.Error(nameof(RecurringDeleteWorkerController), $"Cleaning up testing records by puid in schedule db failed: {ex.Message}");
            }
        }

        protected async Task DeleteRecurringDeletesScheduleDbAsync(RecurrentDeleteScheduleDbDocument scheduleDbDoc)
        {
            try
            {
                await this.scheduleDbClient.DeleteRecurringDeletesScheduleDbAsync(scheduleDbDoc.Puid, scheduleDbDoc.DataType, CancellationToken.None).ConfigureAwait(false);
            }
            catch (ScheduleDbClientException ex)
            {
                this.logger.Error(nameof(RecurringDeleteWorkerController), $"Cleaning up testing record in schedule db failed: {ex.Message}");
            }
        }
    }
}
