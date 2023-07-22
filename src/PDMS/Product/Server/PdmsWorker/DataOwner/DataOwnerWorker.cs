namespace Microsoft.PrivacyServices.DataManagement.Worker.DataOwnerWorker
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Tracing;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.DataManagement.Client;
    using Microsoft.PrivacyServices.DataManagement.Common;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Reader;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Scheduler;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Writer;
    using Microsoft.PrivacyServices.DataManagement.Models.Filters;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;
    using Microsoft.PrivacyServices.DataManagement.Worker.DataOwner;
    using Microsoft.PrivacyServices.DataManagement.Worker.Scheduler;
    using ST = Microsoft.PrivacyServices.DataManagement.Client.ServiceTree;

    /// <summary>
    /// The data owner worker fetches all data owners from CosmosDB and compares its service tree information from ServiceTree API.
    /// DataOwner is updated in case of a mismatch.
    /// </summary>
    public class DataOwnerWorker : LockWorker<DataOwnerWorkerLockState>, IInitializer
    {
        private readonly IEventWriterFactory eventWriterFactory;
        private readonly IPrivacyDataStorageReader storageReader;
        private readonly IPrivacyDataStorageWriter storageWriter;
        private readonly ST.IServiceTreeClient serviceTreeClient;
        private readonly IAuthenticationProvider authenticationProvider;
        private readonly int MaxPageSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataOwnerWorker"/> class.
        /// </summary>
        /// <param name="id">Worker id.</param>
        /// <param name="dateFactory">Date factory instance.</param>
        /// <param name="lockConfig">Lock configuration.</param>
        /// <param name="dataAccess">Lock data access.</param>
        /// <param name="sessionFactory">The session factory.</param>
        /// <param name="eventWriterFactory">The event writer factory.</param>
        /// <param name="storageReader">The storage reader.</param>
        /// <param name="storageWriter">The storage writer.</param>
        /// <param name="serviceTreeClient">The service tree client.</param>
        /// <param name="authenticationProviderFactory">The authentication provider factory.</param>
        /// <param name="coreConfiguration">The core configuration.</param>
        public DataOwnerWorker(
            Guid id,
            IDateFactory dateFactory,
            ILockConfig lockConfig,
            ILockDataAccess<DataOwnerWorkerLockState> dataAccess,
            ISessionFactory sessionFactory,
            IEventWriterFactory eventWriterFactory,
            IPrivacyDataStorageReader storageReader,
            IPrivacyDataStorageWriter storageWriter,
            ST.IServiceTreeClient serviceTreeClient,
            IAuthenticationProviderFactory authenticationProviderFactory,
            ICoreConfiguration coreConfiguration)
            : base(id, dateFactory, dataAccess, sessionFactory)
        {
            this.eventWriterFactory = eventWriterFactory;
            this.storageReader = storageReader;
            this.storageWriter = storageWriter;
            this.EnableAcquireLock = lockConfig.EnableDataOwnerWorkerLock;
            this.serviceTreeClient = serviceTreeClient;
            this.authenticationProvider = authenticationProviderFactory.CreateForClient();
            this.MaxPageSize = coreConfiguration.MaxPageSize;
            this.LockName = "DataOwnerWorkerLock";
        }

        // Increase idle time between calls to 30mins.
        public override int IdleTimeBetweenCallsInMilliseconds => 30 * 60 * 1000;

        public override bool EnableAcquireLock { get; set; }

        public override string LockName { get; set; }

        // Set the lock time to 8 hours, we can tweak this expiry time later
        public override double LockExpiryTimeInMilliseconds => 8 * 60 * 60 * 1000;

        public override int LockMaxFailureCountPerInstance => 10;

        private readonly string UpdatedByName = "DataOwnerUpdater";

        private readonly int BatchSize = 200;

        private readonly int MaxRetries = 5;

        private readonly string TaskCompleted = "Completed";

        // Worker initializer.
        public Task InitializeAsync()
        {
            string workerName = this.GetType().Name;

            eventWriterFactory.Trace(workerName, $"Beginning {workerName} initialization.");

            return Task.CompletedTask;
        }

        /// <summary>
        /// Update Data Owner information by reading from Service Tree everyday.
        /// </summary>
        /// <param name="lockStatus">Lock status.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Null string when host process should invoke DoWorkAsync immediately, otherwise a string message indicating why it should back off.</returns>
        public override async Task<string> DoLockWorkAsync(Lock<DataOwnerWorkerLockState> lockStatus, CancellationToken cancellationToken)
        {

            var timer = Stopwatch.StartNew();

            try
            {
                // Take the current date.
                var currentDate = this.GetCurrentDate();

                // If this is the first time the worker has ever run, or we are currently not processing it.
                if (lockStatus.State == null || !lockStatus.State.InProgress)
                {
                    var previousDate = lockStatus.State?.LastSyncTime ?? DateTimeOffset.MinValue;

                    if (previousDate < currentDate)
                    {
                        // Update lock with the current date and mark it so that we begin to process it.
                        lockStatus.State = new DataOwnerWorkerLockState
                        {
                            LastSyncTime = currentDate,
                            InProgress = true
                        };

                        await this.DataAccess.UpdateAsync(lockStatus).ConfigureAwait(false);

                        // Exit to begin processing.
                        return null;
                    }

                    // Return completed string since we already completed processing for the day.
                    return this.TaskCompleted;
                }
                else
                {
                    if (lockStatus.State.InProgress)
                    {
                        // Fetch DataOwners in batches, compare it against ServiceTree API, and update if needed.
                        await this.UpdateDataOwnerFromServiceTreeAsync().ConfigureAwait(false);

                        lockStatus.State.LastSyncTime = currentDate;
                        lockStatus.State.InProgress = false;

                        await this.DataAccess.UpdateAsync(lockStatus).ConfigureAwait(false);
                    }

                    // Return completed string, so that next processing can start after IdleTimeBetweenCallsInMilliseconds: 30mins.
                    return this.TaskCompleted;
                }
            }
            catch (Exception ex)
            {
                string lockStatusMsg = string.Empty;
                if (lockStatus.State != null)
                {
                    lockStatusMsg = $"State: InProgress = {lockStatus.State.InProgress}  LastSyncTime: {lockStatus.State.LastSyncTime}";
                }

                this.eventWriterFactory.WriteEvent(this.LockName, $"DoLockWorkAsync: LockStatus: {lockStatusMsg} Exception: {ex.ToString()}", EventLevel.Error);
                throw;
            }
            finally
            {
                timer.Stop();
                this.eventWriterFactory.Trace(this.LockName, $"DoLockWorkAsync finished. Total time used {timer.ElapsedMilliseconds / 1000} seconds");
            }
        }

        /// <summary>
        /// Get all exisiting dataOwners from cosmosDb, compare it against information from ServiceTree, and update it if required.
        /// </summary>
        /// <returns>The task to execute the work asynchronously.</returns>
        private async Task UpdateDataOwnerFromServiceTreeAsync()
        {
            var currentIndex = 0;
            var totalRowsReturned = 0;
            var totalDataOwnerUpdated = 0;
            var totalRowsProcessed = 0;
            var failedDataOwners = new List<string>();
            
            do
            {
                var filterCriteria = new DataOwnerFilterCriteria()
                {
                    IsDeleted = false,
                    Count = BatchSize,
                    Index = currentIndex
                };

                // Get all existing data owners.
                var existingDataOwners = await this.storageReader.GetDataOwnersAsync(filterCriteria, true, true).ConfigureAwait(false);

                // Iterate over existing data owner in batches.
                foreach (var existingDataOwner in existingDataOwners.Values)
                {
                    // Call service tree to get the latest information
                    // Compare exisiting dataOwner.ServiceTree information with ServiceTree and perforrm update if anything changed
                    totalDataOwnerUpdated += await this.CompareAndPerformRealTimeRefreshAsync(existingDataOwner, failedDataOwners).ConfigureAwait(false) ? 1 : 0;
                }

                totalRowsReturned = existingDataOwners.Values.Count();
                currentIndex += BatchSize;
                totalRowsProcessed += totalRowsReturned;

                this.eventWriterFactory.Trace(nameof(DataOwnerWorker), $"Total rows processed: {totalRowsProcessed}");

            } while (BatchSize == totalRowsReturned);

            this.eventWriterFactory.Trace(nameof(DataOwnerWorker), $"Total Data Owners updated: {totalDataOwnerUpdated}");

            if (failedDataOwners.Count > 0)
            {
                StringBuilder failedDataOwnersList = new StringBuilder();

                // Combining dataOwnerIds failed due to TaskCanceledException despite retrying and logging them at once.
                failedDataOwners.ForEach(dataOwner => failedDataOwnersList.Append($"{dataOwner},"));
                this.eventWriterFactory.WriteEvent(nameof(DataOwnerWorker), $"TaskCanceledException caused failure to update data owners for: [{failedDataOwnersList}]", EventLevel.Error);
            }
        }

        /// <summary>
        /// Compares existing dataOwner.serviceTree information against serviceTree and updates it, if needed.
        /// </summary>
        /// <param name="existingDataOwner">Existing data owner.</param>
        /// <param name="failedDataOwners">Failed data owners list.</param>
        /// <returns>The task to execute the work asynchronously.</returns>
        private async Task<bool> CompareAndPerformRealTimeRefreshAsync(DataOwner existingDataOwner, List<string> failedDataOwners)
        {
            if (existingDataOwner.ServiceTree == null)
            {
                return false;
            }

            var existingServiceTree = existingDataOwner.ServiceTree;
            bool needRealTimeUpdate = false;

            var requestContext = new RequestContext()
            {
                AuthenticationProvider = authenticationProvider
            };

            for(var delay = 1; delay <= MaxRetries; delay++)
            {
                try
                {
                    if (existingServiceTree.Level == ServiceTreeLevel.Service && !string.IsNullOrEmpty(existingServiceTree.ServiceId))
                    {
                        this.eventWriterFactory.Trace(nameof(DataOwnerWorker), $"ReadServiceWithExtendedProperties({existingServiceTree.ServiceId}) for DataOwner.Id: {existingDataOwner.Id}");

                        var response = await this.serviceTreeClient.ReadServiceWithExtendedProperties(Guid.Parse(existingServiceTree.ServiceId), requestContext).ConfigureAwait(false);
                        var updatedValue = response.Response;

                        if (!this.CompareService(existingServiceTree, updatedValue))
                        {
                            this.UpdateService(existingServiceTree, updatedValue);

                            existingDataOwner.Name = DataOwnerWriter.CreateServiceName(existingServiceTree.ServiceName);

                            existingDataOwner.Description = updatedValue.Description;
                            needRealTimeUpdate = true;
                        }
                    }
                    else if (existingServiceTree.Level == ServiceTreeLevel.TeamGroup && !string.IsNullOrEmpty(existingServiceTree.TeamGroupId))
                    {
                        this.eventWriterFactory.Trace(nameof(DataOwnerWorker), $"ReadTeamGroupWithExtendedProperties({existingServiceTree.TeamGroupId}) for DataOwner.Id: {existingDataOwner.Id}");

                        var response = await this.serviceTreeClient.ReadTeamGroupWithExtendedProperties(Guid.Parse(existingServiceTree.TeamGroupId), requestContext).ConfigureAwait(false);
                        var updatedValue = response.Response;

                        if (!this.CompareTeamGroup(existingServiceTree, updatedValue))
                        {
                            this.UpdateTeamGroup(existingServiceTree, updatedValue);

                            existingDataOwner.Name = DataOwnerWriter.CreateTeamGroupName(existingServiceTree.TeamGroupName);
                            existingDataOwner.Description = updatedValue.Description;
                            needRealTimeUpdate = true;
                        }
                    }
                    else if (existingServiceTree.Level == ServiceTreeLevel.ServiceGroup && !string.IsNullOrEmpty(existingServiceTree.ServiceGroupId))
                    {
                        this.eventWriterFactory.Trace(nameof(DataOwnerWorker), $"ReadServiceGroupWithExtendedProperties({existingServiceTree.ServiceGroupId}) for DataOwner.Id: {existingDataOwner.Id}");

                        var response = await this.serviceTreeClient.ReadServiceGroupWithExtendedProperties(Guid.Parse(existingServiceTree.ServiceGroupId), requestContext).ConfigureAwait(false);
                        var updatedValue = response.Response;

                        if (!this.CompareServiceGroup(existingServiceTree, updatedValue))
                        {
                            this.UpdateServiceGroup(existingServiceTree, updatedValue);

                            existingDataOwner.Name = DataOwnerWriter.CreateServiceGroupName(existingServiceTree.ServiceGroupName);
                            existingDataOwner.Description = updatedValue.Description;
                            needRealTimeUpdate = true;
                        }
                    }

                    if (needRealTimeUpdate)
                    {
                        this.UpdateTrackingDetails(existingDataOwner);
                        await this.storageWriter.UpdateDataOwnerAsync(existingDataOwner).ConfigureAwait(false);

                        this.eventWriterFactory.Trace(nameof(DataOwnerWorker), $"Updated DataOwner.Id: {existingDataOwner.Id}");
                    }

                    return needRealTimeUpdate;
                }
                catch (ST.NotFoundError)
                {
                    this.eventWriterFactory.Trace(nameof(DataOwnerWorker), $"Service tree entry not found. Assuming orphaned. DataOwner.Id: {existingDataOwner.Id}");
                    return needRealTimeUpdate;
                }
                catch (TaskCanceledException t)
                {
                    if (delay < MaxRetries)
                    {
                        this.eventWriterFactory.Trace(nameof(DataOwnerWorker), $"TaskCanceledException, delay for: {delay} mins, before retrying");
                        await Task.Delay(TimeSpan.FromSeconds(delay)).ConfigureAwait(false);
                    }
                    else
                    {
                        this.eventWriterFactory.Trace(nameof(DataOwnerWorker), $"Max retries reached for TaskCanceledException: {t.Message}, DataOwner.Id: {existingDataOwner.Id}, consuming exception to complete the update cycle.");
                        failedDataOwners.Add(existingDataOwner.Id.ToString());
                    }
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("throttled") && delay < MaxRetries)
                    {
                        // Service tree accepts only 200 request/min/principal as per SLA
                        await Task.Delay(TimeSpan.FromMinutes(delay)).ConfigureAwait(false);
                        this.eventWriterFactory.Trace(nameof(DataOwnerWorker), $"Throttling worker for: {delay} mins, before calling ServiceTree");
                    }
                    else
                    {
                        this.eventWriterFactory.WriteEvent(nameof(DataOwnerWorker), $"Exception: {ex.Message}", EventLevel.Error);
                        throw;
                    }
                }
            }
            return needRealTimeUpdate;
        }

        /// <summary>
        /// Generic method for updating the tracking details of any entity.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="entity">The entity object.</param>
        /// <returns>The updated entity.</returns>
        private TEntity UpdateTrackingDetails<TEntity>(TEntity entity) where TEntity : Entity
        {
            var currentTime = this.DateFactory.GetCurrentTime();
            var name = this.UpdatedByName;

            if (entity.TrackingDetails == null)
            {
                entity.TrackingDetails = new TrackingDetails
                {
                    CreatedBy = name,
                    CreatedOn = currentTime,
                    Version = 0 // Version should start at 1, but we use 0 because it is incremented below.
                };
            }

            entity.TrackingDetails.UpdatedBy = name;
            entity.TrackingDetails.UpdatedOn = currentTime;
            entity.TrackingDetails.EgressedOn = currentTime;
            entity.TrackingDetails.Version += 1;

            return entity;
        }

        private void UpdateService(ServiceTree existingServiceTree, ST.Service updatedValue)
        {
            this.UpdateBaseGroup(existingServiceTree, updatedValue);

            existingServiceTree.ServiceGroupId = updatedValue.ServiceGroupId.ToString();
            existingServiceTree.ServiceGroupName = updatedValue.ServiceGroupName;
            existingServiceTree.TeamGroupId = updatedValue.TeamGroupId.ToString();
            existingServiceTree.TeamGroupName = updatedValue.TeamGroupName;
            existingServiceTree.ServiceId = updatedValue.Id.ToString();
            existingServiceTree.ServiceName = updatedValue.Name;
        }

        private bool CompareService(ServiceTree existingServiceTree, ST.Service updatedValue)
        {
            return this.CompareBaseGroup(existingServiceTree, updatedValue) &&
                existingServiceTree.ServiceGroupId == updatedValue.ServiceGroupId?.ToString() &&
                existingServiceTree.ServiceGroupName == updatedValue.ServiceGroupName?.ToString() &&
                existingServiceTree.TeamGroupId == updatedValue.TeamGroupId.ToString() &&
                existingServiceTree.TeamGroupName == updatedValue.TeamGroupName &&
                existingServiceTree.ServiceId == updatedValue.Id.ToString() &&
                existingServiceTree.ServiceName == updatedValue.Name;
        }

        private void UpdateTeamGroup(ServiceTree existingServiceTree, ST.TeamGroup updatedValue)
        {
            this.UpdateBaseGroup(existingServiceTree, updatedValue);

            existingServiceTree.ServiceGroupId = updatedValue.ServiceGroupId.ToString();
            existingServiceTree.ServiceGroupName = updatedValue.ServiceGroupName;
            existingServiceTree.TeamGroupId = updatedValue.Id.ToString();
            existingServiceTree.TeamGroupName = updatedValue.Name;
            existingServiceTree.ServiceId = null;
            existingServiceTree.ServiceName = null;
        }

        private void UpdateServiceGroup(ServiceTree existingServiceTree, ST.ServiceGroup updatedValue)
        {
            this.UpdateBaseGroup(existingServiceTree, updatedValue);

            existingServiceTree.ServiceGroupId = updatedValue.Id.ToString();
            existingServiceTree.ServiceGroupName = updatedValue.Name;
            existingServiceTree.TeamGroupId = null;
            existingServiceTree.TeamGroupName = null;
            existingServiceTree.ServiceId = null;
            existingServiceTree.ServiceName = null;
        }

        private void UpdateBaseGroup(ServiceTree existingServiceTree, ST.ServiceTreeNode updatedValue)
        {
            existingServiceTree.ServiceAdmins = updatedValue.AdminUserNames;
            existingServiceTree.DivisionId = updatedValue.DivisionId?.ToString();
            existingServiceTree.DivisionName = updatedValue.DivisionName?.ToString();
            existingServiceTree.OrganizationId = updatedValue.OrganizationId?.ToString();
            existingServiceTree.OrganizationName = updatedValue.OrganizationName?.ToString();
        }

        private bool CompareBaseGroup(ServiceTree existingServiceTree, ST.ServiceTreeNode updatedValue)
        {
            return this.CompareServiceAdminNames(existingServiceTree.ServiceAdmins, updatedValue.AdminUserNames) &&
                existingServiceTree.DivisionId == updatedValue.DivisionId?.ToString() &&
                existingServiceTree.DivisionName == updatedValue.DivisionName?.ToString() &&
                existingServiceTree.OrganizationId == updatedValue.OrganizationId?.ToString() &&
                existingServiceTree.OrganizationName == updatedValue.OrganizationName?.ToString();       
        }

        /// <summary>
        /// Compare DataOwner ServiceAdmins against ServiceAdmins from ServiceTree.
        /// </summary>
        /// <param name="existingServiceAdmins">Data Owner Service Admins.</param>
        /// <param name="updatedServiceAdmins">Service Tree Service Admins.</param>
        /// <returns>Whether need to update or not.</returns>
        private bool CompareServiceAdminNames(IEnumerable<string> existingServiceAdmins, IEnumerable<string> updatedServiceAdmins)
        {
            if (existingServiceAdmins == null && updatedServiceAdmins == null)
            {
                return true;
            }
            else if (existingServiceAdmins == null || updatedServiceAdmins == null)
            {
                return false;
            }
            else
            {
                return existingServiceAdmins.Count() == updatedServiceAdmins.Count() && (!existingServiceAdmins.Except(updatedServiceAdmins).Any() || !updatedServiceAdmins.Except(existingServiceAdmins).Any());
            }
        }

        private bool CompareServiceGroup(ServiceTree existingServiceTree, ST.ServiceGroup updatedValue)
        {
            return this.CompareBaseGroup(existingServiceTree, updatedValue) &&
                existingServiceTree.ServiceGroupId == updatedValue.Id.ToString() &&
                existingServiceTree.ServiceGroupName == updatedValue.Name;
        }

        private bool CompareTeamGroup(ServiceTree existingServiceTree, ST.TeamGroup updatedValue)
        {
            return this.CompareBaseGroup(existingServiceTree, updatedValue) &&
                existingServiceTree.ServiceGroupId == updatedValue.ServiceGroupId?.ToString() &&
                existingServiceTree.ServiceGroupName == updatedValue.ServiceGroupName?.ToString() &&
                existingServiceTree.TeamGroupId == updatedValue.Id.ToString() &&
                existingServiceTree.TeamGroupName == updatedValue.Name;
        }
        
        private DateTimeOffset GetCurrentDate()
        {
            var date = this.DateFactory.GetCurrentTime().ToUniversalTime();
            return new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, TimeSpan.Zero);
        }
    }
}
