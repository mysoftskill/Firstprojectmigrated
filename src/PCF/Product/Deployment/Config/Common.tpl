<Configuration xmlns:cg="http://memdmconfiggen">
  <cg:Meta>
    <cg:Language>CSharp</cg:Language>
    <cg:Namespace>Microsoft.PrivacyServices.CommandFeed.Service.Common.ConfigGen</cg:Namespace>
    <cg:Import>System.Security.Cryptography.X509Certificates</cg:Import>
    <cg:Import>System</cg:Import>
    <cg:InferTypes>true</cg:InferTypes>
    <cg:CommonInterface>IConfigurationNode</cg:CommonInterface>
  </cg:Meta>
  
  <PPEHack>
    <Enabled>%PPEHackEnabled%</Enabled>
    <FixedAssetGroupId cg:Type="AssetGroupId">00000000-0000-0000-0000-000000000000</FixedAssetGroupId>
  </PPEHack>

  <Common>
    <DataFolder>Data\%DataFolderName%</DataFolder>
    <LocalLogDir>%LocalLogDir%</LocalLogDir>
    <ServiceToServiceSiteId>%ServiceToServiceSiteId%</ServiceToServiceSiteId>
    <ServiceToServiceCertificate cg:Type="X509Certificate2" keyVault="%CertificateKeyVaultOverride%">%CertificateName%</ServiceToServiceCertificate>
    <AzureActiveDirectoryTenant>microsoft.onmicrosoft.com</AzureActiveDirectoryTenant>
    <AzureActiveDirectoryValidAudience>%AzureActiveDirectoryValidAudience%</AzureActiveDirectoryValidAudience>
    <AzureActiveDirectoryValidAudienceAME>%AzureActiveDirectoryValidAudienceAME%</AzureActiveDirectoryValidAudienceAME>
    <ApplyTestDataAgentMapDecorator>%ApplyTestDataAgentMapDecorator%</ApplyTestDataAgentMapDecorator>
    <IsProductionEnvironment>%IsProductionEnvironment%</IsProductionEnvironment>
    <IsTestEnvironment>%IsTestEnvironment%</IsTestEnvironment>
    <IsStressEnvironment>%IsStressEnvironment%</IsStressEnvironment>
    <IsPreProdEnvironment>%IsPreProdEnvironment%</IsPreProdEnvironment>
    <ExportInitialVisibilityDelayMinutes>%ExportInitialVisibilityDelayMinutes%</ExportInitialVisibilityDelayMinutes>
  </Common>
  
  <AzureKeyVault>
    <!-- The name of the key vault -->
    <BaseUrl oneboxsecret="%UseOneboxSecrets%">%AzureKeyVaultBaseUrl%</BaseUrl>
  </AzureKeyVault>

  <AzureStorageAccounts cg:Collection="true">
     %AzureStorageAccounts%
  </AzureStorageAccounts>

  <Kusto>
    <ClusterName>%KustoClusterName%</ClusterName>
    <DatabaseName>%KustoDatabaseName%</DatabaseName>

    <!-- Corp tenant -->
    <Authority>72f988bf-86f1-41af-91ab-2d7cd011db47</Authority>
  </Kusto>

  <Flighting>
    <EnvironmentName>%CarbonFlightingEnvironmentName%</EnvironmentName>
  </Flighting>

  <!-- Defines configuration for Azure Management resources, such as event grids, storage accounts, etc -->
  <AzureManagement>
    <!-- All access is scoped to the given resource group -->
    <ResourceGroupName>%AzureManagementResourceGroupName%</ResourceGroupName>

    <!-- The app ID of we use to authenticate -->
    <ApplicationId>%AzureManagementApplicationId%</ApplicationId>

    <!-- The subscription ID we're operating under -->
    <SubscriptionId>%AzureManagementSubscriptionId%</SubscriptionId>

    <!-- The User Assigned Managed Identity subscription ID we're operating under -->
    <UAMISubscriptionId>%AzureManagementUAMISubscriptionId%</UAMISubscriptionId>

    <!-- The tenant ID to authenicate as -->
    <TenantId>33e01921-4d64-4f8c-a055-5bdaffd5e33d</TenantId>

    <!-- Azure Redis Cache Settings -->
    <RedisCacheEndpoint>%AzureRedisCacheEndpoint%</RedisCacheEndpoint>
    <RedisCachePort>6380</RedisCachePort>
    <RedisCachePassword kvsecret="%UseKvSecretsForRedisPassword%">redis-password</RedisCachePassword>
  </AzureManagement>

  <DistributedLocks>
    <BlobConnectionString kvsecret="%UseKvSecretsForStorageConnectionString%">%AzureBlobConnectionString%</BlobConnectionString>
    <BlobContainerName>DistributedLocks</BlobContainerName>
  </DistributedLocks>

  <PCFV2Storage>
    <StorageAccountName>%PCFV2AzureStorageAccountName%</StorageAccountName>
    <BlobContainerName>expectationruntime</BlobContainerName>
    <BlobName>EndTime.txt</BlobName>
  </PCFV2Storage>

  <Frontdoor>
    <SyntheticCommandInsertionEnabled>%SytheticCommandInsertionEnabled%</SyntheticCommandInsertionEnabled>
    <FrontdoorShutdownTimeoutSecs>60</FrontdoorShutdownTimeoutSecs>
    <PdmsCacheRefreshTimeoutSecs>300</PdmsCacheRefreshTimeoutSecs>
    <AllowSdkWithoutVerifier>%AllowSdkWithoutVerifier%</AllowSdkWithoutVerifier>

    <Checkpoint>
      <FailedCommandReplayTimeSecs>%FailedCommandReplayTimeSecs%</FailedCommandReplayTimeSecs>
      <UnexpectedCommandReplayTimeSecs>86400</UnexpectedCommandReplayTimeSecs>
      <VerificationFailedReplayTimeSecs>86400</VerificationFailedReplayTimeSecs>
      <UnexpectedVerificationFailureReplayTimeSecs>10800</UnexpectedVerificationFailureReplayTimeSecs>
      <SafeVisibilityDelayNonExportSlaThresholdDays>21</SafeVisibilityDelayNonExportSlaThresholdDays>
      <SafeVisibilityDelayExportSlaThresholdDays>14</SafeVisibilityDelayExportSlaThresholdDays>
      <SafeVisibilityDelayAadExportSlaThresholdDays>21</SafeVisibilityDelayAadExportSlaThresholdDays>
    </Checkpoint>

    <AuthenticatedCallers>
      <KnownCallers cg:Collection="true">
        %KnownCallersList%
      </KnownCallers>
      <GetFullCommandStatus cg:Collection="true" cg:Type="System.Int64">
        %GetFullCommandStatusCallerList%
      </GetFullCommandStatus>
      <GetFullCommandStatusWithAadAuth cg:Collection="true" cg:Type="Guid">
        %GetFullCommandStatusAadCallerList%
      </GetFullCommandStatusWithAadAuth>
      <ExportStorageGetAccounts cg:Collection="true" cg:Type="System.Int64">
        %ExportStorageGetAccountsCallerList%
      </ExportStorageGetAccounts>
      <ExportStorageGetAccountsWithAadAuth cg:Collection="true" cg:Type="Guid">
        %ExportStorageGetAccountsAadCallerList%
      </ExportStorageGetAccountsWithAadAuth>
      <DebugApis cg:Collection="true">
        %DebugApisCallerList%
      </DebugApis>
      <DebugApisWithAadAuth cg:Collection="true" cg:Type="Guid">
        %DebugApisAadCallerList%
      </DebugApisWithAadAuth>
      <TestHooks cg:Collection="true">
          %TestHooksCallerList%
      </TestHooks>
      <TestHooksWithAadAuth cg:Collection="true" cg:Type="Guid">
          %TestHooksAadCallerList%
      </TestHooksWithAadAuth>
      <PxsPostCommand cg:Collection="true">
          %PxsPostCommandCallerList%
      </PxsPostCommand>
      <PxsPostCommandWithAadAuth cg:Collection="true" cg:Type="Guid">
        %PxsPostCommandAadCallerList%
      </PxsPostCommandWithAadAuth>
    </AuthenticatedCallers>

    <CosmosExporterAgentId cg:Type="Guid">%CosmosExporterAgentId%</CosmosExporterAgentId>

    <SyntheticAgents cg:Collection="true">
      %SyntheticAgentsList%
    </SyntheticAgents>

    <AllowedAkamaiCert>
      <Subject>%ClientCertSubject%</Subject>
    </AllowedAkamaiCert>
  </Frontdoor>

  <CosmosDBQueues>
    <DefaultLeaseDurationSeconds>900</DefaultLeaseDurationSeconds>
    <DefaultLeaseOverrides cg:Collection="true">
      <LeaseOverride>
        <AgentId>c814ff83c1324f809ff3d7fa05a821f7</AgentId>

        <!-- 2.5 hours -->
        <LeaseDurationSeconds>9000</LeaseDurationSeconds>
      </LeaseOverride>
      <LeaseOverride>
        <AgentId>a0359da5201543958c72381572c79bb4</AgentId>

        <!-- 2.5 hours -->
        <LeaseDurationSeconds>9000</LeaseDurationSeconds>
      </LeaseOverride>
    </DefaultLeaseOverrides>
    <MaxLeaseDurationSeconds cg:Type="System.Int32">%CosmosDbQueueMaxLeaseDurationSecs%</MaxLeaseDurationSeconds>
    <MinLeaseDurationSeconds cg:Type="System.Int32">%CosmosDbQueueMinLeaseDurationSecs%</MinLeaseDurationSeconds>
    <DefaultRUProvisioning cg:Type="System.Int32">%CosmosDbDefaultRuPerSecond%</DefaultRUProvisioning>
    <Instances cg:Collection="true">%CosmosDBList%</Instances>
    <TimeToLiveDays cg:Type="Int32">%CosmosDBQueueTimeToLiveDays%</TimeToLiveDays>
  </CosmosDBQueues>

  <AgentAzureQueues>
    <StorageAccounts cg:Collection="true">
        %AgentAzureQueuesStorageAccounts%
    </StorageAccounts>
  </AgentAzureQueues>
  
  <CommandHistory>
    <Uri oneboxsecret="%UseOneboxSecrets%">%CommandHistoryCosmosDbUri%</Uri>
    <Key kvsecret="%UseKvSecrets%">%CommandHistoryCosmosDbKey%</Key>
    <DefaultRUProvisioning cg:Type="System.Int32">%CosmosDbDefaultRuPerSecond%</DefaultRUProvisioning>
    <DefaultTimeToLiveDays cg:Type="Int32">60</DefaultTimeToLiveDays>
    <MaxAgeInDaysForQuery>60</MaxAgeInDaysForQuery>
  </CommandHistory>

  <CommandLifecycle>
    <!-- Two event hub connection strings. These are equally weighted -->
    <EventHub>
      <Instances cg:Collection="true">%EventHubList%</Instances>
    </EventHub>

    <EnablePeriodicNoOpEvents>%EventHubEnablePeriodicNoOpEvents%</EnablePeriodicNoOpEvents>
  </CommandLifecycle>

  <ExportStorage>
    <!-- TODO: This will eventually be more dynamic, for now, just have two storage accounts -->
    <ConnectionStringA kvsecret="%UseKvSecretsForStorageConnectionString%">%ExportStorageConnectionStringA%</ConnectionStringA>
    <ConnectionStringB kvsecret="%UseKvSecretsForStorageConnectionString%">%ExportStorageConnectionStringB%</ConnectionStringB>
  </ExportStorage>
  
  <PdmsCache>
    <Cosmos>
      <MaxCosmosStreamAgeDays>30</MaxCosmosStreamAgeDays>
      <AssetGroupCosmosStreamTemplate cg:Type="System.String">%AssetGroupCosmosStreamTemplate%</AssetGroupCosmosStreamTemplate>
      <AssetGroupHourlyCosmosStreamTemplate cg:Type="System.String">%AssetGroupHourlyCosmosStreamTemplate%</AssetGroupHourlyCosmosStreamTemplate>
      <AssetGroupVariantInfoCosmosStreamTemplate cg:Type="System.String">%AssetGroupVariantInfoCosmosStreamTemplate%</AssetGroupVariantInfoCosmosStreamTemplate>
    </Cosmos>
    <DocumentDb>
      <DatabaseId cg:Type="System.String">Pdms</DatabaseId>
      <CollectionId cg:Type="System.String">PdmsCache</CollectionId>
      <DatabaseUri cg:Type="System.Uri">%PdmsDatabaseUri%</DatabaseUri>
      <DatabaseKey cg:Type="System.String" kvsecret="%UseKvSecrets%">%PdmsDatabaseKey%</DatabaseKey>
    </DocumentDb>
    <OnDiskCache>
      <Directory>%PdmsOnDiskCacheDirectory%</Directory>
    </OnDiskCache>
  </PdmsCache>

  <Cosmos>
    <CosmosVcPath cg:Type="System.String">%CosmosVcPath%</CosmosVcPath>
    <Streams>
      <AuditLog>
        <StreamFormat cg:Type="System.String">%AuditLogStreamFormat%</StreamFormat>
        <CompletionSignalStreamFormat cg:Type="System.String">%AuditLogCompletionSignalStreamFormat%</CompletionSignalStreamFormat>
        <StreamExpirationDays>%AuditLogStreamExpirationDays%</StreamExpirationDays>
        <MaxCheckpointQueueSizeBytes>%AuditLogMaxCheckpointQueueSizeBytes%</MaxCheckpointQueueSizeBytes>
        <MaxCheckpointIntervalSecs>%AuditLogMaxCheckpointIntervalSecs%</MaxCheckpointIntervalSecs>
      </AuditLog>
      <PrivacyCommand>
        <StreamFormat cg:Type="System.String">%PrivacyCommandStreamFormat%</StreamFormat>
        <CompletionSignalStreamFormat cg:Type="System.String">%PrivacyCommandCompletionSignalStreamFormat%</CompletionSignalStreamFormat>
        <StreamExpirationDays>%PrivacyCommandStreamExpirationDays%</StreamExpirationDays>
        <MaxCheckpointQueueSizeBytes>%PrivacyCommandMaxCheckpointQueueSizeBytes%</MaxCheckpointQueueSizeBytes>
        <MaxCheckpointIntervalSecs>%PrivacyCommandMaxCheckpointIntervalSecs%</MaxCheckpointIntervalSecs>
      </PrivacyCommand>
    </Streams>
  </Cosmos>

  <Worker>
      <Tasks>
          <!-- This task pulls NonWindowsDevice Delete Events from EventHub and creates Delete Events Azure Queue workitems -->
          <NonWindowsDeviceWorker>
              <EventHubCheckpointExpirationSeconds cg:Type="Int32">%EventHubCheckpointExpirationSeconds%</EventHubCheckpointExpirationSeconds>
              <DeviceIdExpirationMinutes cg:Type="Int32">%DeviceIdExpirationMinutes%</DeviceIdExpirationMinutes>
              <EventHubConfig cg:Interface="IEventHubConfig">
                  %NonWindowsDeviceEventHubConfig%
              </EventHubConfig>
              <CommonConfig cg:Interface="ITaskConfiguration">
                  <Enabled>true</Enabled>
                  <!-- How long to acquire / extend the lock for -->
                  <LockDurationMinutes>5</LockDurationMinutes>
                  <!-- Extend the lock when we have less than this time remaining -->
                  <LockExtensionThresholdMinutes>1</LockExtensionThresholdMinutes>
                  <!-- The minimum amount of time to sleep before checking to see if it's time to run -->
                  <MinSleepTimeSeconds cg:Type="Int32">10</MinSleepTimeSeconds>
                  <!-- The maximum amount of time to sleep before checking to see if it's time to run -->
                  <MaxSleepTimeSeconds cg:Type="Int32">60</MaxSleepTimeSeconds>
                  <!-- The number of concurrent tasks -->
                  <BatchSize>1</BatchSize>
              </CommonConfig>
          </NonWindowsDeviceWorker>
          <!-- The task that periodically pulls new data out of cosmos for the PDMS cache -->
          <PdmsCosmosRefresh>
              <!-- Should be run roughly this often -->
              <FrequencyHours cg:Type="Int32">1</FrequencyHours>
              <CommonConfig cg:Interface="ITaskConfiguration">
                  <Enabled>%EnablePdmsDataRefresh%</Enabled>

                  <!-- How long to aquire / extend the lock for -->
                  <LockDurationMinutes>45</LockDurationMinutes>

                  <!-- Extend the lock when we have less than this time remaining -->
                  <LockExtensionThresholdMinutes>2</LockExtensionThresholdMinutes>

                  <!-- The minimum amount of time to sleep before checking to see if it's time to run -->
                  <MinSleepTimeSeconds cg:Type="Int32">300</MinSleepTimeSeconds>

                  <!-- The maximum amount of time to sleep before checking to see if it's time to run -->
                  <MaxSleepTimeSeconds cg:Type="Int32">3600</MaxSleepTimeSeconds>

                  <!-- The number of concurrent requests to event grid -->
                  <BatchSize>1</BatchSize>
              </CommonConfig>
          </PdmsCosmosRefresh>

          <CosmosStreamHourlyCompletion>
              <!-- This value should never be less than 61.(1 hour to wait until this hour to finish, +1 min to avoid clock skew issue) -->
              <MinutesToWaitToCloseCurrentHour cg:Type="Int32">61</MinutesToWaitToCloseCurrentHour>
              <StartDateOffsetFromCurrentTime cg:Type="Int32">-5</StartDateOffsetFromCurrentTime>
              <CommonConfig cg:Interface="ITaskConfiguration">
                  <Enabled>true</Enabled>
                  <LockDurationMinutes>5</LockDurationMinutes>
                  <LockExtensionThresholdMinutes>2</LockExtensionThresholdMinutes>
                  <MinSleepTimeSeconds cg:Type="Int32">30</MinSleepTimeSeconds>
                  <MaxSleepTimeSeconds cg:Type="Int32">60</MaxSleepTimeSeconds>
                  <BatchSize>1</BatchSize>
              </CommonConfig>
          </CosmosStreamHourlyCompletion>

          <ForceCompleteOldExportCommandsTask>
              <!-- The Daily Hour (UTC) when task should start (9am in Redmond) -->
              <TaskStartHour>1</TaskStartHour>
              <CommonConfig cg:Interface="ITaskConfiguration">
                  <Enabled>%OldExportForceCompleteTaskEnabled%</Enabled>
                  <LockDurationMinutes>15</LockDurationMinutes>
                  <LockExtensionThresholdMinutes>5</LockExtensionThresholdMinutes>
                  <MinSleepTimeSeconds cg:Type="Int32">900</MinSleepTimeSeconds>
                  <MaxSleepTimeSeconds cg:Type="Int32">1800</MaxSleepTimeSeconds>
                  <BatchSize>1</BatchSize>
              </CommonConfig>

              <!-- The earliest record to mark as force-completed -->
              <MinCompletionAgeDays>%OldExportForceCompleteMinAgeDays%</MinCompletionAgeDays>
              <!-- The latest record to mark as force completed. For non-test, this will be the TTL of the command. -->
              <MaxCompletionAgeDays>%OldExportForceCompleteMaxAgeDays%</MaxCompletionAgeDays>
              <!-- The earliest AAD record to mark as force-completed -->
              <MinCompletionAgeDaysAad>%MinCompletionAgeDaysAad%</MinCompletionAgeDaysAad>
              <!-- The latest AAD record to mark as force completed. For non-test, this will be the TTL of the command. -->
              <MaxCompletionAgeDaysAad>%MaxCompletionAgeDaysAad%</MaxCompletionAgeDaysAad>
          </ForceCompleteOldExportCommandsTask>

          <!-- The task that runs daily to cleanup expired export storage containers -->
          <ExportStorageCleanupTask>
              <!-- The Daily Hour (UTC) when task should start -->
              <TaskStartHour>1</TaskStartHour>
              <MaxAgeDays cg:Type="Int32">60</MaxAgeDays>
              <CommonConfig cg:Interface="ITaskConfiguration">
                  <Enabled>true</Enabled>
                  <LockDurationMinutes>60</LockDurationMinutes>
                  <LockExtensionThresholdMinutes>5</LockExtensionThresholdMinutes>
                  <MinSleepTimeSeconds cg:Type="Int32">900</MinSleepTimeSeconds>
                  <MaxSleepTimeSeconds cg:Type="Int32">1800</MaxSleepTimeSeconds>
                  <BatchSize>1</BatchSize>
              </CommonConfig>
          </ExportStorageCleanupTask>

          <!-- Baseline queue depth calculation  -->
          <BaselineQueueDepthTaskScheduler>
              <FrequencyMinutes cg:Type="Int32">%QueueDepthBaselineFrequencyMinutes%</FrequencyMinutes>
              <CommonConfig cg:Interface="ITaskConfiguration">
                  <Enabled>true</Enabled>
                  <LockDurationMinutes>5</LockDurationMinutes>
                  <LockExtensionThresholdMinutes>2</LockExtensionThresholdMinutes>
                  <MinSleepTimeSeconds cg:Type="Int32">300</MinSleepTimeSeconds>
                  <MaxSleepTimeSeconds cg:Type="Int32">300</MaxSleepTimeSeconds>
                  <BatchSize>2</BatchSize>
              </CommonConfig>
          </BaselineQueueDepthTaskScheduler>

          <AzureQueueCommandQueueDepth>
              <FrequencyMinutes cg:Type="Int32">3</FrequencyMinutes>
              <CommonConfig cg:Interface="ITaskConfiguration">
                  <Enabled>%AzureQueueCommandQueueDepthCheckerEnabled%</Enabled>
                  <LockDurationMinutes>5</LockDurationMinutes>
                  <LockExtensionThresholdMinutes>1</LockExtensionThresholdMinutes>
                  <MinSleepTimeSeconds cg:Type="Int32">20</MinSleepTimeSeconds>
                  <MaxSleepTimeSeconds cg:Type="Int32">40</MaxSleepTimeSeconds>
                  <BatchSize>1</BatchSize>
              </CommonConfig>
          </AzureQueueCommandQueueDepth>

          <KustoAggregationWorker>
              <CommonConfig cg:Interface="ITaskConfiguration">
                  <Enabled>true</Enabled>
                  <LockDurationMinutes>5</LockDurationMinutes>
                  <LockExtensionThresholdMinutes>2</LockExtensionThresholdMinutes>
                  <MinSleepTimeSeconds cg:Type="Int32">60</MinSleepTimeSeconds>
                  <MaxSleepTimeSeconds cg:Type="Int32">300</MaxSleepTimeSeconds>
                  <BatchSize>2</BatchSize>
              </CommonConfig>
          </KustoAggregationWorker>

          <IngestionRecoveryTask>
              <!-- The Daily Hour (UTC) when task should start (10am PST) -->
              <TaskStartHour>18</TaskStartHour>
              <CommonConfig cg:Interface="ITaskConfiguration">
                  <Enabled>%IngestionRecoveryTaskEnabled%</Enabled>
                  <LockDurationMinutes>5</LockDurationMinutes>
                  <LockExtensionThresholdMinutes>2</LockExtensionThresholdMinutes>
                  <MinSleepTimeSeconds cg:Type="Int32">3600</MinSleepTimeSeconds>
                  <MaxSleepTimeSeconds cg:Type="Int32">4200</MaxSleepTimeSeconds>
                  <BatchSize>1</BatchSize>
              </CommonConfig>

              <!-- The earliest record to check for partial insertion -->
              <MinCreatedAgeDays cg:Type="Int32">%PartiallyIngestedMinCreatedAgeDays%</MinCreatedAgeDays>
              <!-- The latest record to check for partial insertion -->
              <MaxCreatedAgeDays cg:Type="Int32">%PartiallyIngestedMaxCreatedAgeDays%</MaxCreatedAgeDays>
          </IngestionRecoveryTask>

          <CosmosDbPartitionSizeWorker>
              <WorkerFrequencyMinutes cg:Type="Int32">60</WorkerFrequencyMinutes>  <!-- This is how often the worker should wake up and check the lock -->
              <GlobalFrequencyMinutes cg:Type="Int32">120</GlobalFrequencyMinutes>  <!-- This is how often the worker should actually do the work -->
              <AzureLogAnalyticsWorkspaceId>%AzureLogAnalyticsWorkspaceId%</AzureLogAnalyticsWorkspaceId>
              <CosmosDbResourceGroup>%CosmosDbResourceGroup%</CosmosDbResourceGroup>
              <CommonConfig cg:Interface="ITaskConfiguration">
                  <Enabled>%CosmosDbPartitionSizeWorkerEnabled%</Enabled>
                  <LockDurationMinutes>5</LockDurationMinutes>
                  <LockExtensionThresholdMinutes>1</LockExtensionThresholdMinutes>
                  <MinSleepTimeSeconds cg:Type="Int32">10</MinSleepTimeSeconds>
                  <MaxSleepTimeSeconds cg:Type="Int32">600</MaxSleepTimeSeconds>
                  <BatchSize>1</BatchSize>
              </CommonConfig>
          </CosmosDbPartitionSizeWorker>
      </Tasks>

    <!-- The amount of time to wait (in minutes) before marking a command as being globally complete and notifying PXS -->
    <ColdStorageCompletionBackoffSeconds>%ColdStorageCompletionBackoffSeconds%</ColdStorageCompletionBackoffSeconds>

    <!-- How frequently we should checkpoint our cold storage event hubs. This influences the amount of aggregation we get, as well as the number of items we need to handle at once -->
    <CommandHistoryEventHubCheckpointFrequencySeconds>%CommandHistoryEventHubCheckpointFrequencySeconds%</CommandHistoryEventHubCheckpointFrequencySeconds>

    <!-- the maximum amount of time between successive polls of an Azure queue -->
    <AzureQueueMaxBackoffMs cg:Type="Int32">%AzureQueueMaxBackoffMs%</AzureQueueMaxBackoffMs>

    <CosmosPipelineEnabled>%CosmosPipelineEnabled%</CosmosPipelineEnabled>

    <DefenderPipelineEnabled>%DefenderPipelineEnabled%</DefenderPipelineEnabled>
    <DefenderFileMetaDataServiceUri>%DefenderFileMetaDataServiceUri%</DefenderFileMetaDataServiceUri>
    <DefenderAPIKey kvsecret="%UseKvSecrets%">defender-api-key</DefenderAPIKey>
    <DefenderThresholdHoursToSkipScan>1</DefenderThresholdHoursToSkipScan>
    <DefenderFileDownloadTempDirectory>%DefenderFileDownloadTempDirectory%</DefenderFileDownloadTempDirectory>
    
    <!-- Gets whether serialization to CSV format is enabled for export -->
    <ExportToCSVEnabled>%ExportToCSVEnabled%</ExportToCSVEnabled>

    <!-- The maximum amount of time to delay ingesting an account close command. Used for load smoothing -->
    <AccountCloseCommandMaxIngestionDelaySeconds cg:Type="Int32">%AccountCloseCommandMaxIngestionDelaySeconds%</AccountCloseCommandMaxIngestionDelaySeconds>
  </Worker>

  <FrontdoorWatchdog>
    <!-- Maximum service working set in MB -->
    <MaxWorkingSetMb  cg:Type="System.Int32">%MaxWorkingSetMb%</MaxWorkingSetMb>
    <!-- True to stop the service if its working set exceed given threshold -->
    <StopService>%StopService%</StopService>
  </FrontdoorWatchdog>

  <!-- A fake PPE export test agent. Contains many different asset groups designed to simulate a 5GB export -->
  <PpeExportStressAgent>
    <AgentId cg:Type="AgentId">7A2D9D32-D070-49E0-8031-0050831F1882</AgentId>
    <MsaPuids cg:Collection="true" cg:Type="UInt64">
      <Item>985156848376132</Item>
    </MsaPuids>
    <AssetGroups cg:Collection="true">
      <AssetGroup>
        <Id cg:Type="AssetGroupId">E5FF245C-22D4-46D1-A77C-7ABB20D97582</Id>
        <ExportDataSizeMb>10</ExportDataSizeMb>
      </AssetGroup>
      <AssetGroup>
        <Id>1761cf6b7184411a9459f730b9124c57</Id>
        <ExportDataSizeMb>10</ExportDataSizeMb>
      </AssetGroup>
      <AssetGroup>
        <Id>509a21e4f94a4ce5923e962addd8dbbf</Id>
        <ExportDataSizeMb>10</ExportDataSizeMb>
      </AssetGroup>
      <AssetGroup>
        <Id>7f47371d8a0749aaaf486ce2de7ccd50</Id>
        <ExportDataSizeMb>10</ExportDataSizeMb>
      </AssetGroup>
      <AssetGroup>
        <Id>0824514c25de4db8bfd7ffe3f53009c7</Id>
        <ExportDataSizeMb>10</ExportDataSizeMb>
      </AssetGroup>
      <AssetGroup>
        <Id>62ece84fe32642539d1a8d3f4b1e4c09</Id>
        <ExportDataSizeMb>10</ExportDataSizeMb>
      </AssetGroup>
      <AssetGroup>
        <Id>c7198cbb22d444e7bf7cf70b351287d4</Id>
        <ExportDataSizeMb>10</ExportDataSizeMb>
      </AssetGroup>
      <AssetGroup>
        <Id>2532318dca5945259b76da209482b995</Id>
        <ExportDataSizeMb>10</ExportDataSizeMb>
      </AssetGroup>
      <AssetGroup>
        <Id>73af939409384af7bcce80369b65978a</Id>
        <ExportDataSizeMb>10</ExportDataSizeMb>
      </AssetGroup>
      <AssetGroup>
        <Id>85d883f805d94ca18398773caa0e3d1c</Id>
        <ExportDataSizeMb>10</ExportDataSizeMb>
      </AssetGroup>
      <AssetGroup>
        <Id>692f18130d3040d8ad65b25d15b59441</Id>
        <ExportDataSizeMb>10</ExportDataSizeMb>
      </AssetGroup>
      <AssetGroup>
        <Id>f96730a153214b109d5c4d130282200d</Id>
        <ExportDataSizeMb>100</ExportDataSizeMb>
      </AssetGroup>
      <AssetGroup>
        <Id>e5d2994d79dd49f78baa3da81297fec8</Id>
        <ExportDataSizeMb>100</ExportDataSizeMb>
      </AssetGroup>
      <AssetGroup>
        <Id>664a24dbb94e406db88d620a5ee05e54</Id>
        <ExportDataSizeMb>100</ExportDataSizeMb>
      </AssetGroup>
      <AssetGroup>
        <Id>5b1cab4bcb01488aafc97faabd1115ad</Id>
        <ExportDataSizeMb>100</ExportDataSizeMb>
      </AssetGroup>
      <AssetGroup>
        <Id>18a7c7cf393f4af1b07da813379a7fb9</Id>
        <ExportDataSizeMb>100</ExportDataSizeMb>
      </AssetGroup>
      <AssetGroup>
        <Id>9f490ffd8f6c474a9f15413b7c1b39ee</Id>
        <ExportDataSizeMb>100</ExportDataSizeMb>
      </AssetGroup>
      <AssetGroup>
        <Id>3d63f24c178643fa9980a7bc01632e42</Id>
        <ExportDataSizeMb>100</ExportDataSizeMb>
      </AssetGroup>
      <AssetGroup>
        <Id>ff96c0b3ba624b2c8dea12359c0b0bab</Id>
        <ExportDataSizeMb>100</ExportDataSizeMb>
      </AssetGroup>
      <AssetGroup>
        <Id>2389ea171230460c949e164144619c90</Id>
        <ExportDataSizeMb>100</ExportDataSizeMb>
      </AssetGroup>
      <AssetGroup>
        <Id>3fd2a843d2e04ff9b277a0d8ccab7e01</Id>
        <ExportDataSizeMb>250</ExportDataSizeMb>
      </AssetGroup>
      <AssetGroup>
        <Id>5576d16acf7d49ec92ea4e9209af7c32</Id>
        <ExportDataSizeMb>250</ExportDataSizeMb>
      </AssetGroup>
      <AssetGroup>
        <Id>ba402e68f75b448391f4fd15586aeff1</Id>
        <ExportDataSizeMb>500</ExportDataSizeMb>
      </AssetGroup>
      <AssetGroup>
        <Id>ff6affd28b2648b4b591ae9a17b44403</Id>
        <ExportDataSizeMb>1000</ExportDataSizeMb>
      </AssetGroup>
      <AssetGroup>
        <Id>43df980da3bc4447829d49dabf1285ee</Id>
        <ExportDataSizeMb>2000</ExportDataSizeMb>
      </AssetGroup>
    </AssetGroups>
  </PpeExportStressAgent>

  <CommandReplay>
    <MaxReplayDays cg:Type="Int32">45</MaxReplayDays>
    <ReplayProcessDelayEnabled>%ReplayProcessDelayEnabled%</ReplayProcessDelayEnabled>
    <MinReplayWorkerSleepSeconds cg:Type="Int32">%MinReplayWorkerSleepSeconds%</MinReplayWorkerSleepSeconds>
    <MaxReplayWorkerSleepSeconds cg:Type="Int32">%MaxReplayWorkerSleepSeconds%</MaxReplayWorkerSleepSeconds>
    <Repository>
      <Uri oneboxsecret="%UseOneboxSecrets%">%CommandReplayDocDBUri%</Uri>
      <Key kvsecret="%UseKvSecrets%">%CommandReplayDocDBKey%</Key>
      <DefaultRUProvisioning cg:Type="System.Int32">%CosmosDbDefaultRuPerSecond%</DefaultRUProvisioning>
      <TimeToLiveDays cg:Type="Int32">180</TimeToLiveDays>
    </Repository>
  </CommandReplay>

  <Telemetry>
    <TelemetryTimeToLiveDays cg:Type="Int32">%TelemetryTimeToLiveDays%</TelemetryTimeToLiveDays >
    <MaxItemCount cg:Type="Int32">%TelemetryMaxItemCount%</MaxItemCount>
    <MaxRetries cg:Type="Int32">10</MaxRetries>
    <RetryDelayMillisecs cg:Type="Int32">500</RetryDelayMillisecs>
  </Telemetry>

  <StressForwarding>
    <Enabled>%IsProductionEnvironment%</Enabled>
    <ForwardingThreadCount>10</ForwardingThreadCount>
    <ForwardingTargetHostName>40.90.216.83</ForwardingTargetHostName>
  </StressForwarding>

  <CosmosDbAutoscaler cg:Interface="ICosmosDbAutoscalerConfig">
    <Enabled>%CosmosDbAutoscalerEnabled%</Enabled>

    <!-- As a real number, the success rate the autoscaler should aim for. -->
    <TargetSuccessRate cg:Type="Double">0.998</TargetSuccessRate>

    <!-- The maximum amount to increase by at each step. This is a real number, so a value of 1 indicates a max increase of 100 percent (ie, double). -->
    <MaxIncrementalIncreasePercent cg:Type="Double">0.5</MaxIncrementalIncreasePercent>

    <!-- The maximum amount to decrease by at each step. This is a real number, so a value of .25 indicates a max decrease of 25 percent (ie, 75 percent of the original). -->
    <MaxIncrementalDecreasePercent cg:Type="Double">0.01</MaxIncrementalDecreasePercent>

    <!-- When increasing RUs, increase by at least this much. This 
         keeps the autoscaler from creeping up to its target too slowly. -->
    <MinRuIncrease cg:Type="Int32">100</MinRuIncrease>
  </CosmosDbAutoscaler>

  <SllLogger>
    <!-- SamplingRate indicates how many events received before one can be fired. e.g. 10 means we will fire 10th, 20th, 30th, ... event. 
         Set to 0 will disable sampling -->
    <IncomingApiSamplingRate>%IncomingApiSamplingRate%</IncomingApiSamplingRate>
    <IncomingApiSamplingList cg:Collection="true" cg:Type="System.String">
        %IncomingApiSamplingList%
    </IncomingApiSamplingList>

    <OutgoingApiSamplingRate>%OutgoingApiSamplingRate%</OutgoingApiSamplingRate>
    <OutgoingApiSamplingList cg:Collection="true" cg:Type="System.String">
        %OutgoingApiSamplingList%
    </OutgoingApiSamplingList>
  </SllLogger>

  <AzureAppConfigEndpoint>%AzureAppConfigEndpoint%</AzureAppConfigEndpoint>

  <Adls>
    <AccountName cg:Type="System.String">%AdlsAccountName%</AccountName>
    <TenantId cg:Type="System.String">%AdlsTenantId%</TenantId>
    <AccountSuffix cg:Type="System.String">%AdlsAccountSuffix%</AccountSuffix>
    <ClientAppId cg:Type="System.String">%AdlsClientAppId%</ClientAppId>
    <ClientAppCertificateName cg:Type="System.String">%AdlsClientAppCertificateName%</ClientAppCertificateName>
  </Adls>

  <PCFv2>
    <CosmosDbExportExpectationsContainerName cg:Type="System.String">%PCFv2CosmosDbExportExpectationsContainerName%</CosmosDbExportExpectationsContainerName>
    <CosmosDbCompletedCommandsContainerName cg:Type="System.String">%PCFv2CosmosDbCompletedCommandsContainerName%</CosmosDbCompletedCommandsContainerName>
    <CosmosDbDatabaseName cg:Type="System.String">%PCFv2CosmosDbDatabaseName%</CosmosDbDatabaseName>
    <CosmosDbEndpointName cg:Type="System.String">%PCFv2CosmosDbEndpointName%</CosmosDbEndpointName>
  </PCFv2>

</Configuration>