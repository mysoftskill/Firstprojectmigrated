using Castle.Core.Internal;
using Microsoft.Azure.ComplianceServices.Common;
using Microsoft.PrivacyServices.DataManagement.Client;
using Microsoft.PrivacyServices.DataManagement.Client.ServiceTree;
using Microsoft.PrivacyServices.DataManagement.Common;
using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
using Microsoft.PrivacyServices.DataManagement.DataAccess.Kusto;
using Microsoft.PrivacyServices.DataManagement.DataAccess.Reader;
using Microsoft.PrivacyServices.DataManagement.DataAccess.Scheduler;
using Microsoft.PrivacyServices.DataManagement.Models.V2;
using Microsoft.PrivacyServices.DataManagement.Worker.Scheduler;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ST = Microsoft.PrivacyServices.DataManagement.Client.ServiceTree;

namespace Microsoft.PrivacyServices.DataManagement.Worker.ServiceTreeMetadata
{
    public class ServiceTreeMetadataWorker : LockWorker<ServiceTreeMetadataWorkerLockState>, IInitializer
    {
        private IEventWriterFactory eventWriterFactory { get; set; }

        private IPrivacyDataStorageReader storageReader { get; set; }

        private IServiceTreeKustoClient serviceTreeKustoClient { get; set; }

        public override string LockName { get; set; }

        public override double LockExpiryTimeInMilliseconds => 8 * 60 * 60 * 1000;

        public override bool EnableAcquireLock { get; set; }

        private IServiceTreeClient serviceTreeClient { get; set; }

        private IAuthenticationProvider authenticationProvider { get; set; }

        public override int LockMaxFailureCountPerInstance => 10;

        // 30 mins
        public override int IdleTimeBetweenCallsInMilliseconds => 30 * 60 * 1000;

        private readonly string TaskCompleted = "Completed";

        private IKustoClient ngpKustoClient { get; set; }

        private IKustoClientConfig ngpKustoClientConfig { get; set; }

        private List<string> ServiceTreeWhiteListLookup = new List<string>();

        private List<string> ServiceTreeBlackListLookup = new List<string>();

        private IAppConfiguration appConfiguration { get; set; }
        public int TimeIntervalInMilliSeconds { get; private set; }

        public ServiceTreeMetadataWorker(
            Guid id,
            IDateFactory dateFactory,
            ILockConfig lockConfig,
            ILockDataAccess<ServiceTreeMetadataWorkerLockState> dataAccess,
            ISessionFactory sessionFactory,
            IEventWriterFactory eventWriterFactory,
            IPrivacyDataStorageReader storageReader,
            ST.IServiceTreeClient serviceTreeClient,
            IAuthenticationProviderFactory authenticationProviderFactory,
            IServiceTreeKustoClient serviceTreeKustoClient,
            IKustoClient ngpKustoClient,
            IKustoClientConfig ngpKustoClientConfig,
            IAppConfiguration appConfiguration)
            : base(id, dateFactory, dataAccess, sessionFactory)
        {
            this.eventWriterFactory = eventWriterFactory;
            this.storageReader = storageReader;
            this.EnableAcquireLock = lockConfig.EnableServiceTreeMetadataWorkerLock;
            this.serviceTreeClient = serviceTreeClient;
            this.serviceTreeKustoClient = serviceTreeKustoClient;
            this.authenticationProvider = authenticationProviderFactory.CreateForClient();
            this.LockName = "ServiceTreeMetadataWorkerLock";
            this.ngpKustoClientConfig = ngpKustoClientConfig;
            this.ngpKustoClient = ngpKustoClient;
            this.appConfiguration = appConfiguration;
            this.TimeIntervalInMilliSeconds = this.appConfiguration.GetConfigValue<int>(ConfigNames.PDMS.ServiceTreeMetadataWorker_Frequency, 24) * 60 * 60 * 1000;
            ServiceTreeMetadataWorkerConstants.ServiceTreeServicesWithMetadataQuery = this.appConfiguration.GetConfigValue(ConfigNames.PDMS.ServiceTreeMetadataWorker_GetServicesWithMetadataQuery, defaultValue: ServiceTreeMetadataWorkerConstants.ServiceTreeServicesWithMetadataQuery);
            ServiceTreeMetadataWorkerConstants.ServiceTreeServicesUnderDivisionQuery = this.appConfiguration.GetConfigValue(ConfigNames.PDMS.ServiceTreeMetadataWorker_GetServicesUnderDivisionQuery, defaultValue: ServiceTreeMetadataWorkerConstants.ServiceTreeServicesUnderDivisionQuery);
            ServiceTreeMetadataWorkerConstants.NGPPowerBIUrlTemplate = this.appConfiguration.GetConfigValue(ConfigNames.PDMS.NGPPowerBIUrlTemplate, defaultValue: ServiceTreeMetadataWorkerConstants.NGPPowerBIUrlTemplate);
            ServiceTreeMetadataWorkerConstants.PrivacyComplianceDashboardTemplate = this.appConfiguration.GetConfigValue(ConfigNames.PDMS.PrivacyComplianceDashboardTemplate, defaultValue: ServiceTreeMetadataWorkerConstants.PrivacyComplianceDashboardTemplate);
        }

        /// <summary>
        /// Update ST information by reading from PDMS everyday.
        /// </summary>
        /// <param name="lockStatus">Lock status.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Null string when host process should invoke DoWorkAsync immediately, otherwise a string message indicating why it should back off.</returns>

        public override async Task<string> DoLockWorkAsync(Lock<ServiceTreeMetadataWorkerLockState> lockStatus, CancellationToken cancellationToken)
        {
            var featureFlag = this.appConfiguration.GetConfigValue<bool>(ConfigNames.PDMS.ServiceTreeMetadataWorker_Enabled, false);
            var timer = Stopwatch.StartNew();

            try
            {
                // Take the current date.
                var currentDate = this.GetCurrentDate();

                // If this is the first time the worker has ever run, or we are currently not processing it.
                if (lockStatus.State == null || !lockStatus.State.InProgress)
                {
                    var previousDate = lockStatus.State?.LastSyncTime ?? DateTimeOffset.MinValue;

                    if (previousDate.ToUnixTimeMilliseconds()+this.TimeIntervalInMilliSeconds <= currentDate.ToUnixTimeMilliseconds())
                    {
                        // Update lock with the current date and mark it so that we begin to process it.
                        lockStatus.State = new ServiceTreeMetadataWorkerLockState
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
                    if (lockStatus.State.InProgress && featureFlag)
                    {
                        // Fetch DataOwners, compare it against ServiceTree, and update if needed.
                        await this.RunUpdates().ConfigureAwait(false);

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
        /// Get all exisiting dataOwners from PDMS & ST, compare it against information from PDMS & ServiceTree , and update it if required.
        /// Updation Logic:
        ///     Whitelisted NGP Service     Whitelisted ST Service    Blacklist     Action
        ///                 T                           T                 T         Delete
        ///                 T                           T                 F         Update
        ///                 T                           F                 T           NA
        ///                 T                           F                 F          Add
        ///                 F                           T                 T         Delete
        ///                 F                           T                 F         Delete
        ///                 F                           F                 T           NA
        ///                 F                           F                 F           NA
        /// </summary>
        /// <returns>The task to execute the work asynchronously.</returns>

        private async Task RunUpdates()
        {
            
            var timer = Stopwatch.StartNew();
            this.eventWriterFactory.Trace(this.LockName, "RunUpdates: Starting RunUpdates at:"+this.GetCurrentDate().ToString());
            // Build the whitelisted service lookup
            await this.BuildLookups().ConfigureAwait(false);
            
            List<ST.Models.ServiceTreeMetadata> STServices = await this.GetServiceTreeServices().ConfigureAwait(false);
            Dictionary<string, Models.V2.DataOwner> PDMSServices = await this.GetPDMSServicesHashMapAsync().ConfigureAwait(false);

            this.eventWriterFactory.Trace(this.LockName, $"RunUpdates: GetServiceTreeServices and GetPDMSServicesHashMapAsync took {timer.ElapsedMilliseconds / 1000} seconds");
            
            List<ST.Models.ServiceTreeMetadata> ItemsToBeUpdated = new List<ST.Models.ServiceTreeMetadata>();
            List<ST.Models.ServiceTreeMetadata> ItemsToBeDeleted = new List<ST.Models.ServiceTreeMetadata>();
            List<Models.V2.DataOwner> ItemsToBeCreated = new List<Models.V2.DataOwner>();

            foreach(var service in STServices)
            {
                if(this.ServiceTreeBlackListLookup.Contains(service.Id))
                {
                    ItemsToBeDeleted.Add(service);
                    if(PDMSServices.ContainsKey(service.Id))
                    {
                        PDMSServices.Remove(service.Id);
                    }
                }
                else
                {
                    if (PDMSServices.ContainsKey(service.Id))
                    {
                        var dataOwner = PDMSServices[service.Id];
                        var metadata = this.BuildServiceTreeMetadataValue(dataOwner);
                        if (service.Value != metadata)
                        {
                            ItemsToBeUpdated.Add(service);
                        }
                        PDMSServices.Remove(service.Id);
                    }
                    else
                    {
                        ItemsToBeDeleted.Add(service);
                    }
                }
            }

            foreach(var item in PDMSServices)
            {
                if(!ServiceTreeBlackListLookup.Contains(item.Key))
                {
                    ItemsToBeCreated.Add(item.Value);
                }
            }

            await this.AddItems(ItemsToBeCreated).ConfigureAwait(false);
            
            await this.UpdateItems(ItemsToBeUpdated).ConfigureAwait(false);
            
            await this.DeleteItems(ItemsToBeDeleted).ConfigureAwait(false);

            timer.Stop();
            this.eventWriterFactory.Trace(this.LockName, $"RunUpdates: Total time used {timer.ElapsedMilliseconds / 1000} seconds at:"+this.GetCurrentDate());
        }

        /// <summary>
        /// Builds the Services Lookup.
        /// </summary>
        /// <returns>The task to execute the work asynchronously.</returns>
        private async Task BuildLookups()
        {
            ServiceTreeWhiteListLookup = new List<string>();
            ServiceTreeBlackListLookup = new List<string>();

            IHttpResult<KustoResponse> httpResult;

            var whitelistedDivisions = this.appConfiguration.GetConfigValue(ConfigNames.PDMS.ServiceTreeMetadataWorker_WhiteListedServices_Divisions, defaultValue: "");
            var whitelistedServices = this.appConfiguration.GetConfigValue(ConfigNames.PDMS.ServiceTreeMetadataWorker_WhiteListedServices_Services, defaultValue: "");
            var whitelistQuery = ServiceTreeMetadataWorkerConstants.ServiceTreeServicesUnderDivisionQuery.Replace("<DivisionIds>", whitelistedDivisions);
            httpResult = await this.serviceTreeKustoClient.QueryAsync(whitelistQuery).ConfigureAwait(false);
            foreach (var item in httpResult.Response.Rows)
            {
                ServiceTreeWhiteListLookup.Add((string)item[0]);
            }
            if (!whitelistedServices.IsNullOrEmpty())
            {
                ServiceTreeWhiteListLookup = ServiceTreeWhiteListLookup.Concat(whitelistedServices.Split(',')).ToList();
            }

            var blacklistedServices = this.appConfiguration.GetConfigValue(ConfigNames.PDMS.ServiceTreeMetadataWorker_BlackListedServices_Services, defaultValue: "");
            if (!blacklistedServices.IsNullOrEmpty())
            {
                ServiceTreeBlackListLookup = blacklistedServices.Split(',').ToList();
            }
        }

        /// <summary>
        /// Builds ServiceTree Metadata Value from team id
        /// </summary>
        /// <returns>The task to execute the work asynchronously.</returns>
        private ST.Models.ServiceTreeMetadataValue BuildServiceTreeMetadataValue(Models.V2.DataOwner owner)
        {
            ST.Models.ServiceTreeMetadataValue response = new ST.Models.ServiceTreeMetadataValue ();
            response.NGPPowerBIUrl = ServiceTreeMetadataWorkerConstants.NGPPowerBIUrlTemplate.Replace("<ServiceId>",owner.ServiceTree.ServiceId);
            response.PrivacyComplianceDashboard = ServiceTreeMetadataWorkerConstants.PrivacyComplianceDashboardTemplate + owner.Id;
            return response;
        }

        /// <summary>
        /// Add metadata to ServiceTree.
        /// </summary>
        /// <returns>The task to execute the work asynchronously.</returns>
        private async Task AddItems(List<Models.V2.DataOwner> PDMSServices)
        {
            RequestContext requestContext = new RequestContext()
            {
                AuthenticationProvider = authenticationProvider,
            };

            foreach (Models.V2.DataOwner item in PDMSServices )
            {
                ST.Models.ServiceTreeMetadata serviceTreeMetadata = new ST.Models.ServiceTreeMetadata();
                serviceTreeMetadata.Id = item.ServiceTree.ServiceId;
                serviceTreeMetadata.Name = item.Name;
                serviceTreeMetadata.Value = BuildServiceTreeMetadataValue(item);
                try
                {
                    var response = await this.serviceTreeClient.CreateMetadata(new Guid(item.ServiceTree.ServiceId), serviceTreeMetadata, requestContext).ConfigureAwait(false);

                    if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
                    {
                        await this.LogEvent($"Failed to create metadata for service: {item.ServiceTree.ServiceId}.\n Error: {response.ResponseContent}");
                    }
                    else
                    {
                        await this.LogEvent($"Successfully created metadata for service: {item.ServiceTree.ServiceId}.");
                    }
                }
                catch(Exception ex)
                {
                    await this.LogEvent($"Failed to create metadata for service: {item.ServiceTree.ServiceId}.\n Error: {ex.ToString()}");
                }
            }
        }


        /// <summary>
        /// Delete metadata from ServiceTree.
        /// </summary>
        /// <returns>The task to execute the work asynchronously.</returns>
        private async Task DeleteItems(IEnumerable<ST.Models.ServiceTreeMetadata> itemsToBeDeleted)
        {
            RequestContext requestContext = new RequestContext()
            {
                AuthenticationProvider = authenticationProvider,
            };

            foreach (ST.Models.ServiceTreeMetadata item in itemsToBeDeleted) 
            {
                try
                {
                    var response = await this.serviceTreeClient.DeleteMetadata(new Guid(item.Id), requestContext).ConfigureAwait(false);
                    if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
                    {
                        await this.LogEvent($"Failed to delete metadata for service: {item.Id}.\n Error: {response.ResponseContent}");
                    }
                    else
                    {
                        await this.LogEvent($"Successfully deleted metadata for service: {item.Id}.");
                    }
                }
                catch(Exception ex)
                {
                    await this.LogEvent($"Failed to delete metadata for service: {item.Id}.\n Error: {ex.ToString()}");
                }
            }
        }

        /// <summary>
        /// Updates metadata in ServiceTree.
        /// </summary>
        /// <returns>The task to execute the work asynchronously.</returns>

        private async Task UpdateItems(IEnumerable<ST.Models.ServiceTreeMetadata> itemsToBeUpdated)
        {
            RequestContext requestContext = new RequestContext()
            {
                AuthenticationProvider = authenticationProvider,
            };

            foreach (ST.Models.ServiceTreeMetadata item in itemsToBeUpdated)
            {
                try
                {
                    var response = await this.serviceTreeClient.UpdateMetadata(new Guid(item.Id), item, requestContext).ConfigureAwait(false);
                    if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
                    {
                        await this.LogEvent($"Failed to update metadata for service: {item.Id}.\n Error: {response.ResponseContent}");
                    }
                    else
                    {
                        await this.LogEvent($"Successfully updated metadata for service: {item.Id}.");
                    }
                }
                catch(Exception ex)
                {
                    await this.LogEvent($"Failed to update metadata for service: {item.Id}.\n Error: {ex.ToString()}");
                }
            }
        }

        /// <summary>
        /// Build PDMS services hashmap by fetching all the pdms services from NGP Kusto and then merging it with the services registered in PDMS
        /// </summary>
        /// <returns>The task to execute the work asynchronously.</returns>
        private async Task<Dictionary<string, Models.V2.DataOwner>> GetPDMSServicesHashMapAsync()
        {
            var existingDataOwners = await this.GetDataOwnersFromPDMS().ConfigureAwait(false);
            
            var kustoResponse = await this.ngpKustoClient.QueryAsync($"{this.ngpKustoClientConfig.KustoFunctionListNGPAssetsAndAgents}()").ConfigureAwait(false);
            var result = kustoResponse.Response;
            
            Dictionary<string, Models.V2.DataOwner> PDMSServices = new Dictionary<string, Models.V2.DataOwner>();
            
            // Building hashmap from the Kusto query result
            foreach (var item in result.Rows)
            {
                string serviceId = (string)item.First();
                if(ServiceTreeWhiteListLookup.Contains(serviceId))
                {
                    Models.V2.DataOwner dataOwner = existingDataOwners.FirstOrDefault(x => x.ServiceTree?.ServiceId == serviceId);
                    if(dataOwner != null)
                    {
                        PDMSServices.Add(serviceId, dataOwner);
                        existingDataOwners.Remove(dataOwner);
                    }
                    else
                    {
                        PDMSServices.Add(serviceId, new Models.V2.DataOwner());
                    }
                }
            }

            Dictionary<string, Models.V2.DataOwner> restOfPDMSServices = existingDataOwners.Where(x => ServiceTreeWhiteListLookup.Contains(x.ServiceTree?.ServiceId)).ToDictionary(keySelector: obj => obj.ServiceTree.ServiceId, elementSelector: obj => obj);

            PDMSServices = PDMSServices.Concat(restOfPDMSServices).ToDictionary(x => x.Key, x => x.Value);

            return PDMSServices;
        }

        //Fetch all services in PDMS
        private async Task<List<Models.V2.DataOwner>> GetDataOwnersFromPDMS()
        {
            var currentIndex = 0;
            var totalRowsReturned = 0;
            var BatchSize = 13000;
            List<Models.V2.DataOwner> result = new List<Models.V2.DataOwner>();
            try
            {
                var timer = Stopwatch.StartNew();
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

                    result = result.Concat(existingDataOwners.Values).ToList();

                    totalRowsReturned = existingDataOwners.Values.Count();
                    currentIndex += BatchSize;

                } while (BatchSize == totalRowsReturned);
                timer.Stop();
                this.eventWriterFactory.Trace(nameof(ServiceTreeMetadataWorker), $"Fetched {result.Count} records from PDMS in :" + timer.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                await this.LogEvent("Failed to fetch services from PDMS.\nError:"+ex.ToString());
            }
            return result;
        }

        /// <summary>
        /// Build ST services List
        /// </summary>
        /// <returns>The task to execute the work asynchronously.</returns>
        private async Task<List<ST.Models.ServiceTreeMetadata>> GetServiceTreeServices()
        {
            List<ST.Models.ServiceTreeMetadata> serviceTreeServices = new List<ST.Models.ServiceTreeMetadata> { };

            try
            {
                var httpResult = await this.serviceTreeKustoClient.QueryAsync(ServiceTreeMetadataWorkerConstants.ServiceTreeServicesWithMetadataQuery).ConfigureAwait(false);
                var response = httpResult.Response;

                foreach (var item in response.Rows)
                {
                    ST.Models.ServiceTreeMetadata serviceTreeMetadata = new ST.Models.ServiceTreeMetadata();

                    serviceTreeMetadata.Id = (string)item[0];
                    serviceTreeMetadata.Name = (string)item[1];
                    var metadataString = (string)item[2];
                    serviceTreeMetadata.Value = JsonConvert.DeserializeObject<ST.Models.ServiceTreeMetadataValue>(metadataString);

                    if (ServiceTreeWhiteListLookup.Contains(serviceTreeMetadata.Id))
                    {
                        serviceTreeServices.Add(serviceTreeMetadata);
                    }
                }
            }
            catch (Exception ex)
            {
                await this.LogEvent($"Failed to fetch services from ServiceTree.\n Error: {ex.ToString()}");
            }

            return serviceTreeServices;
        }

        private Task LogEvent(string log)
        {
            string workerName = this.GetType().Name;

            this.eventWriterFactory.Trace(workerName, log);

            return Task.CompletedTask;
        }

        public Task InitializeAsync()
        {
            string workerName = this.GetType().Name;

            this.eventWriterFactory.Trace(workerName, $"Beginning {workerName} initialization.");

            return Task.CompletedTask;
        }

        private DateTimeOffset GetCurrentDate()
        {
            var date = this.DateFactory.GetCurrentTime().ToUniversalTime();
            return new DateTimeOffset(date.Year, date.Month, date.Day, date.Hour, date.Minute, 0, TimeSpan.Zero);
        }
    }
}
