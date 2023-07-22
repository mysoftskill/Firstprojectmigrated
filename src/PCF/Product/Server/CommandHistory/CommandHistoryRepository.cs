namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandHistory
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Azure.Documents;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.Common.Azure;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// DocDB implementation of command cold storage.
    /// </summary>
    public class CommandHistoryRepository : ICommandHistoryRepository
    {
        private readonly ICommandHistoryBlobClient blobClient;
        private readonly ICommandHistoryDocDbClient docDbClient;
        private readonly ICommandQueueFactory commandQueueFactory;
        private readonly IAppConfiguration appConfiguration;

        internal CommandHistoryRepository(ICommandHistoryDocDbClient docDbClient,
                                          ICommandHistoryBlobClient blobClient,
                                          ICommandQueueFactory commandQueueFactory,
                                          IAppConfiguration appConfiguration)
        {
            this.docDbClient = docDbClient;
            this.blobClient = blobClient;
            this.commandQueueFactory = commandQueueFactory;
            this.appConfiguration = appConfiguration;
        }

        public static async Task<CommandHistoryRepository> CreateAsync(ICommandQueueFactory commandQueueFactory, 
            IAppConfiguration appConfiguration)
        {
            var docDbClient = new CommandHistoryDocDbClient();
            var blobClient = new CommandHistoryBlobClient();

            await docDbClient.InitializeAsync();

            return new CommandHistoryRepository(docDbClient, blobClient, commandQueueFactory, appConfiguration);
        }

        public async Task<CommandHistoryRecord> QueryAsync(
            CommandId commandId,
            CommandHistoryFragmentTypes fragmentsToRead)
        {
            if (fragmentsToRead == CommandHistoryFragmentTypes.None)
            {
                throw new InvalidOperationException("Why are we reading no fragments?");
            }

            var coreDocument = await this.docDbClient.PointQueryAsync(commandId);

            // Query the other requested fragments.
            var result = await this.QueryFragmentsAsync(coreDocument, fragmentsToRead);
            return result;
        }

        public async Task<IEnumerable<CommandHistoryRecord>> QueryAsync(
            IPrivacySubject subject,
            string requester,
            IList<PrivacyCommandType> commandTypes,
            DateTimeOffset oldestRecord,
            CommandHistoryFragmentTypes fragmentsToRead)
        {
            var whereClause = new StringBuilder();
            var parameterCollection = new SqlParameterCollection();

            BuildWhereClause(whereClause, parameterCollection, subject, requester, commandTypes, oldestRecord);

            var sqlQuery = new SqlQuerySpec($"SELECT * FROM c WHERE {whereClause}", parameterCollection);

            List<Task<CommandHistoryRecord>> readRecordTasks = new List<Task<CommandHistoryRecord>>();
            string continuation = null;
            do
            {
                var (coreDocuments, nextContinuation) = await this.docDbClient.MaxParallelismCrossPartitionQueryAsync(sqlQuery, continuation);
                continuation = nextContinuation;
                readRecordTasks.AddRange(coreDocuments.Select(x => this.QueryFragmentsAsync(x, fragmentsToRead)));

                if (readRecordTasks.Count >= 5000)
                {
                    // If we get here, we bail out of the loop. This is a safety measure to not spend a lifetime fetching thousands of documents
                    // for a single query, and the result is a truncated list. We need to log this because this means we are not giving complete
                    // results.
                    Logger.Instance.CommandHistoryQueryTooLarge(subject, requester, commandTypes, oldestRecord, fragmentsToRead);
                    throw new CommandFeedException("Too many results.") { ErrorCode = CommandFeedInternalErrorCode.Throttle, IsExpected = false };
                }
            }
            while (continuation != null);

            var allRecords = await Task.WhenAll(readRecordTasks);
            return allRecords;
        }

        public async Task<(IEnumerable<CommandHistoryRecord> records, string nextContinuationToken)> QueryPartiallyIngestedCommandsAsync(
            DateTimeOffset oldestRecordTimestamp,
            DateTimeOffset newestRecordTimestamp,
            int maxItemCount,
            bool exportOnly,
            bool nonExportOnly,
            string continuationToken = null)
        {
            var sqlParameterCollection = new SqlParameterCollection
            {
                new SqlParameter { Name = "@oldestRecord", Value = oldestRecordTimestamp.ToUnixTimeSeconds() },
                new SqlParameter { Name = "@newestRecord", Value = newestRecordTimestamp.ToUnixTimeSeconds() }
            };

            // Find records for which the total command count and ingested command count are different given a certain creation time window.
            string queryText = "SELECT * FROM c WHERE c.tcc != c.icc AND c.c = false AND (c.crt BETWEEN @oldestRecord AND @newestRecord)";

            if (exportOnly)
            {
                queryText += $" AND c.ct ={(int)PrivacyCommandType.Export}";
            }
            else if(nonExportOnly)
            {
                queryText += $" AND c.ct !={(int)PrivacyCommandType.Export}";
            }

            var sqlQuery = new SqlQuerySpec(
                queryText,
                sqlParameterCollection);

            var readRecordTasks = new List<Task<CommandHistoryRecord>>();
            var (coreDocuments, nextContinuation) = await this.docDbClient.CrossPartitionQueryAsync(sqlQuery, continuationToken, maxItemCount);
            readRecordTasks.AddRange(coreDocuments.Select(x => this.QueryFragmentsAsync(x, CommandHistoryFragmentTypes.Core | CommandHistoryFragmentTypes.Status)));

            CommandHistoryRecord[] allRecords = await Task.WhenAll(readRecordTasks);

            return (allRecords, nextContinuation);
        }

        public async Task<IEnumerable<CommandHistoryRecord>> QueryIncompleteExportsAsync(
            DateTimeOffset oldestRecordTimestamp,
            DateTimeOffset newestRecordTimestamp,
            bool aadOnly,
            CommandHistoryFragmentTypes fragmentsToRead)
        {
            if (oldestRecordTimestamp > newestRecordTimestamp)
            {
                throw new ArgumentOutOfRangeException(nameof(oldestRecordTimestamp), $"{nameof(oldestRecordTimestamp)} must be less than or equal to {nameof(newestRecordTimestamp)}");
            }

            var sqlParameterCollection = new SqlParameterCollection
            {
                new SqlParameter { Name = "@oldestRecord", Value = oldestRecordTimestamp.ToUnixTimeSeconds() },
                new SqlParameter { Name = "@newestRecord", Value = newestRecordTimestamp.ToUnixTimeSeconds() },
                new SqlParameter { Name = "@commandType", Value = (int)PrivacyCommandType.Export },
                new SqlParameter { Name = "@subject", Value = "aad" },
            };

            SqlQuerySpec sqlQuery;
            if (aadOnly)
            {
                sqlQuery = new SqlQuerySpec(
                    "SELECT * FROM c WHERE c.ct = @commandType AND (c.crt BETWEEN @oldestRecord AND @newestRecord) AND c.c = false AND c.s.type = @subject",
                    sqlParameterCollection);
            }
            else
            {
                sqlQuery = new SqlQuerySpec(
                    "SELECT * FROM c WHERE c.ct = @commandType AND (c.crt BETWEEN @oldestRecord AND @newestRecord) AND c.c = false AND c.s.type != @subject",
                    sqlParameterCollection);
            }

            List<Task<CommandHistoryRecord>> readRecordTasks = new List<Task<CommandHistoryRecord>>();
            string continuation = null;
            do
            {
                var (coreDocuments, nextContinuation) = await this.docDbClient.CrossPartitionQueryAsync(sqlQuery, continuation);
                continuation = nextContinuation;
                readRecordTasks.AddRange(coreDocuments.Select(x => this.QueryFragmentsAsync(x, fragmentsToRead)));
            }
            while (continuation != null);

            var allRecords = await Task.WhenAll(readRecordTasks);
            return allRecords;
        }

        private async Task<CommandHistoryRecord> QueryFragmentsAsync(CoreCommandDocument coreDocument, CommandHistoryFragmentTypes fragmentsToRead)
        {
            if (coreDocument == null)
            {
                return null;
            }

            CommandId commandId = new CommandId(coreDocument.Id);

            var auditBlobTask = Task.FromResult<(AssetGroupAuditDocument[], string)>((null, null));
            var statusBlobTask = Task.FromResult<(AssetGroupStatusDocument[], string)>((null, null));
            var exportBlobTask = Task.FromResult<(ExportDestinationDocument[], string)>((null, null));

            if (fragmentsToRead.HasFlag(CommandHistoryFragmentTypes.Audit))
            {
                auditBlobTask = this.blobClient.ReadBlobAsync<AssetGroupAuditDocument[]>(coreDocument.AuditBlobPointer);
            }

            if (fragmentsToRead.HasFlag(CommandHistoryFragmentTypes.Status))
            {
                statusBlobTask = this.blobClient.ReadBlobAsync<AssetGroupStatusDocument[]>(coreDocument.StatusBlobPointer);
            }

            if (fragmentsToRead.HasFlag(CommandHistoryFragmentTypes.ExportDestinations))
            {
                exportBlobTask = this.blobClient.ReadBlobAsync<ExportDestinationDocument[]>(coreDocument.ExportDestinationBlobPointer);
            }

            var (auditDocuments, auditEtag) = await auditBlobTask;
            var (statusDocuments, statusEtag) = await statusBlobTask;
            var (exportDocuments, exportEtag) = await exportBlobTask;

            CommandHistoryOperationContext operationContext = new CommandHistoryOperationContext(fragmentsToRead, commandId, coreDocument.ETag)
            {
                AuditBlobEtag = auditEtag,
                AuditBlobPointer = coreDocument.AuditBlobPointer,
                ExportDestinationBlobEtag = exportEtag,
                ExportDestinationBlobPointer = coreDocument.ExportDestinationBlobPointer,
                StatusBlobEtag = statusEtag,
                StatusBlobPointer = coreDocument.StatusBlobPointer,
            };

            return new CommandHistoryRecord(
                commandId,
                coreDocument.ToRecord(),
                auditDocuments?.ToDictionary(x => (x.AgentId, x.AssetGroupId), x => x.AuditRecord),
                statusDocuments?.ToDictionary(x => (x.AgentId, x.AssetGroupId), x => x.ToRecord()),
                exportDocuments?.ToDictionary(x => (x.AgentId, x.AssetGroupId), x => x.ToRecord()),
                operationContext);
        }

        public async Task<(IEnumerable<JObject> pxsCommands, string continuationToken)> GetCommandsForReplayAsync(
            DateTimeOffset startTime,
            DateTimeOffset endTime,
            string subjectType,
            bool includeExportCommands,
            string continuationToken,
            int maxItemCount = 1000)
        {
            string query;

            if (includeExportCommands && await appConfiguration.IsFeatureFlagEnabledAsync(FeatureNames.PCF.EnableExportCommandReplay).ConfigureAwait(false))
            {
                query = "SELECT * FROM c WHERE c.crt >= @startTime AND c.crt < @endTime";
            }
            else
            {
                // Note: ct is CommandType, 2 is Export
                query = "SELECT * FROM c WHERE c.ct != 2 AND c.crt >= @startTime AND c.crt < @endTime";
            }

            if (!string.IsNullOrWhiteSpace(subjectType))
            {
                query += $" AND c.s.type = \"{subjectType}\"";
            }

            var sqlQuery = new SqlQuerySpec(
                query,
                new SqlParameterCollection
                {
                    new SqlParameter { Name = "@startTime", Value = startTime.ToUnixTimeSeconds() },
                    new SqlParameter { Name = "@endTime", Value = endTime.ToUnixTimeSeconds() }
                });

            var (coreDocuments, newContinuation) = await this.docDbClient.CrossPartitionQueryAsync(sqlQuery, continuationToken, maxItemCount);
            return (coreDocuments.Select(x => x.PxsCommand), newContinuation);
        }

        public async Task ReplaceAsync(
            CommandHistoryRecord record,
            CommandHistoryFragmentTypes fragmentsToModify)
        {
            CommandHistoryOperationContext operationContext = record?.ReadContext as CommandHistoryOperationContext;

            if (operationContext == null)
            {
                throw new ArgumentException("Attempting to use context generated by a different ICommandHistoryRepository.");
            }

            if (operationContext.CommandId != record.CommandId)
            {
                throw new ArgumentException("Operation context issued for different command id.");
            }

            // Check that the writer's actual changes match what they intended to change.
            if (record.GetChangedFragments() != fragmentsToModify)
            {
                throw new InvalidOperationException($"Record had modified fragments '{record.GetChangedFragments()}, but write only wanted to change '{fragmentsToModify}'");
            }

            // Check to make sure what we are trying to write is a subset of what we read.
            if ((~operationContext.FragmentTypesRead & fragmentsToModify) != CommandHistoryFragmentTypes.None)
            {
                throw new InvalidOperationException($"Read operation read fragments '{operationContext.FragmentTypesRead}', but attempted to modifiy fragments '{fragmentsToModify}'");
            }

            List<Task> updateTasks = new List<Task>();

            if (fragmentsToModify.HasFlag(CommandHistoryFragmentTypes.Core))
            {
                var document = new CoreCommandDocument(record.Core)
                {
                    TimeToLive = DateTimeHelper.GetTimeToLiveSeconds(record.Core.CreatedTime.AddDays(Config.Instance.CommandHistory.DefaultTimeToLiveDays)),
                    AuditBlobPointer = operationContext.AuditBlobPointer,
                    StatusBlobPointer = operationContext.StatusBlobPointer,
                    ExportDestinationBlobPointer = operationContext.ExportDestinationBlobPointer,
                };

                updateTasks.Add(this.docDbClient.ReplaceAsync(document, operationContext.CoreDocumentEtag));
            }

            if (fragmentsToModify.HasFlag(CommandHistoryFragmentTypes.Audit))
            {
                var auditData = SerializeAuditData(record);
                updateTasks.Add(this.blobClient.ReplaceBlobAsync(operationContext.AuditBlobPointer, auditData, operationContext.AuditBlobEtag));
            }

            if (fragmentsToModify.HasFlag(CommandHistoryFragmentTypes.Status))
            {
                var statusData = SerializeStatusData(record);
                updateTasks.Add(this.blobClient.ReplaceBlobAsync(operationContext.StatusBlobPointer, statusData, operationContext.StatusBlobEtag));
            }

            if (fragmentsToModify.HasFlag(CommandHistoryFragmentTypes.ExportDestinations))
            {
                var exportDestinations = SerializeExportData(record);
                updateTasks.Add(this.blobClient.ReplaceBlobAsync(operationContext.ExportDestinationBlobPointer, exportDestinations, operationContext.ExportDestinationBlobEtag));
            }

            await Task.WhenAll(updateTasks);
        }

        public async Task<bool> TryInsertAsync(CommandHistoryRecord record)
        {
            if (record.AuditMap == null ||
                record.StatusMap == null ||
                record.ExportDestinations == null ||
                record.Core == null)
            {
                throw new InvalidOperationException("Insert must specify all record components");
            }

            if (record.ReadContext != null)
            {
                throw new InvalidOperationException("Insert must be a blob that has not been read before.");
            }

            // Order of operations is important for transactionality here.
            // We must first insert all of the parts to Azure blobs in random locations,
            // then finally insert into DocDb. Once the record is in DocDb it is considered committed
            // and the blobs will be stable from that point forward.
            // However, it is possible to have duplicate blobs in the event of an insert failing before the final
            // commit to DocDb.
            Task<BlobPointer> createAuditTask = this.blobClient.CreateBlobAsync(SerializeAuditData(record));
            Task<BlobPointer> createStatusTask = this.blobClient.CreateBlobAsync(SerializeStatusData(record));
            Task<BlobPointer> createExportTask = this.blobClient.CreateBlobAsync(SerializeExportData(record));

            try
            {
                await Task.WhenAll(createAuditTask, createStatusTask, createExportTask);

                CoreCommandDocument coreDocument = new CoreCommandDocument(record.Core)
                {
                    AuditBlobPointer = createAuditTask.Result,
                    StatusBlobPointer = createStatusTask.Result,
                    ExportDestinationBlobPointer = createExportTask.Result,
                    TimeToLive = DateTimeHelper.GetTimeToLiveSeconds(record.Core.CreatedTime.AddDays(Config.Instance.CommandHistory.DefaultTimeToLiveDays))
                };

                await this.docDbClient.InsertAsync(coreDocument);
                return true;
            }
            catch (CommandFeedException ex) when (ex.ErrorCode == CommandFeedInternalErrorCode.Conflict)
            {
                return false;
            }
        }

        public async Task<PrivacyCommand> QueryPrivacyCommandAsync(LeaseReceipt leaseReceipt)
        {
            var fragments = CommandHistoryFragmentTypes.Core;

            // optionally, get export destinations
            if (leaseReceipt.CommandType == PrivacyCommandType.Export)
            {
                fragments |= CommandHistoryFragmentTypes.ExportDestinations;
            }

            CommandHistoryRecord coreCommand = await this.QueryAsync(leaseReceipt.CommandId, fragments);

            if (coreCommand == null || string.IsNullOrWhiteSpace(coreCommand.Core?.RawPxsCommand))
            {
                return null;
            }

            var parser = new PxsCommandParser(
                leaseReceipt.AgentId,
                leaseReceipt.AssetGroupId,
                leaseReceipt.AssetGroupQualifier,
                leaseReceipt.QueueStorageType);

            (PrivacyCommand pcfCommand, _) = parser.Process(JObject.Parse(coreCommand.Core.RawPxsCommand));
            pcfCommand.NextVisibleTime = leaseReceipt.ApproximateExpirationTime;

            LeaseReceipt currentLeaseReceipt = new LeaseReceipt(leaseReceipt.DatabaseMoniker, leaseReceipt.Token, pcfCommand, leaseReceipt.QueueStorageType);
            pcfCommand.LeaseReceipt = currentLeaseReceipt;

            if (pcfCommand is ExportCommand exportCommand)
            {
                if (coreCommand.ExportDestinations != null)
                {
                    if (coreCommand.ExportDestinations.TryGetValue((leaseReceipt.AgentId, leaseReceipt.AssetGroupId), out CommandHistoryExportDestinationRecord exportDestination))
                    {
                        exportCommand.AzureBlobContainerTargetUri = exportDestination?.ExportDestinationUri;
                        exportCommand.AzureBlobContainerPath = exportDestination?.ExportDestinationPath;
                    }
                }

                // If a command is globally complete (could be force-completed) then the container shouldn't be available.
                if (exportCommand.AzureBlobContainerTargetUri == null && !coreCommand.Core.IsGloballyComplete)
                {
                    if (commandQueueFactory != null && FlightingUtilities.IsEnabled(FlightingNames.RePopulateExportDestinationFromQueues))
                    {
                        // Try reading the url from command queue.
                        var queue = this.commandQueueFactory.CreateQueue(leaseReceipt.AgentId, leaseReceipt.AssetGroupId, leaseReceipt.SubjectType, leaseReceipt.QueueStorageType);

                        if (queue.SupportsLeaseReceipt(leaseReceipt))
                        {
                            var command = await queue.QueryCommandAsync(leaseReceipt);
                            if (command is ExportCommand exportCommandFromQueue)
                            {
                                exportCommand.AzureBlobContainerTargetUri = exportCommandFromQueue.AzureBlobContainerTargetUri;
                                exportCommand.AzureBlobContainerPath = exportCommandFromQueue.AzureBlobContainerPath;

                                coreCommand.ExportDestinations[(leaseReceipt.AgentId, leaseReceipt.AssetGroupId)] = new CommandHistoryExportDestinationRecord(leaseReceipt.AgentId, leaseReceipt.AssetGroupId, exportCommand.AzureBlobContainerTargetUri, exportCommand.AzureBlobContainerPath);
                                await ReplaceAsync(coreCommand, CommandHistoryFragmentTypes.ExportDestinations);
                            }
                        }

                        // Add some logs if we could find the container.
                        if (exportCommand.AzureBlobContainerTargetUri != null)
                        {
                            DualLogger.Instance.Warning(nameof(CommandHistoryRepository), $"export destination was missing is missing for AgentId: {leaseReceipt.AgentId.Value}, AssetGroupId: {leaseReceipt.AssetGroupId.Value}, commandId: {leaseReceipt.CommandId.Value} in commandHistory, Added by reading from commandQueues");
                        }
                        else
                        {
                            throw new CommandFeedException(
                            $"{nameof(exportCommand.AzureBlobContainerTargetUri)} is missing for AgentId: {leaseReceipt.AgentId.Value}, AssetGroupId: {leaseReceipt.AssetGroupId.Value}, commandId: {leaseReceipt.CommandId.Value}");
                        }
                    }
                    else
                    {
                        throw new CommandFeedException(
                        $"{nameof(exportCommand.AzureBlobContainerTargetUri)} is missing for AgentId: {leaseReceipt.AgentId.Value}, AssetGroupId: {leaseReceipt.AssetGroupId.Value}, commandId: {leaseReceipt.CommandId.Value}");   
                    }
                }
            }

            return pcfCommand;
        }

        /// <inheritdoc />
        public async Task<bool> QueryIsCompleteByAgentAsync(LeaseReceipt leaseReceipt)
        {
            CommandHistoryRecord coreCommand = await this.QueryAsync(leaseReceipt.CommandId, CommandHistoryFragmentTypes.Status);

            if (coreCommand?.StatusMap == null)
            {
                return false;
            }

            if (coreCommand.StatusMap.TryGetValue((leaseReceipt.AgentId, leaseReceipt.AssetGroupId), out CommandHistoryAssetGroupStatusRecord status))
            {
                if (status == null)
                {
                    return false;
                }

                if (status.CompletedTime != null && status.CompletedTime != default(DateTimeOffset))
                {
                    return true;
                }
            }

            return false;
        }

        private static AssetGroupAuditDocument[] SerializeAuditData(CommandHistoryRecord record)
        {
            return record.AuditMap.Select(x => new AssetGroupAuditDocument(x.Key.agentId, x.Key.assetGroupId, x.Value)).ToArray();
        }

        private static AssetGroupStatusDocument[] SerializeStatusData(CommandHistoryRecord record)
        {
            return record.StatusMap.Select(x => new AssetGroupStatusDocument(x.Value)).ToArray();
        }

        private static ExportDestinationDocument[] SerializeExportData(CommandHistoryRecord record)
        {
            return record.ExportDestinations.Select(x => new ExportDestinationDocument(x.Value)).ToArray();
        }

        /// <summary>
        /// Build where clause for query command.
        /// </summary>
        private static void BuildWhereClause(
            StringBuilder whereClause,
            SqlParameterCollection parameterCollection,
            IPrivacySubject subject,
            string requester,
            IList<PrivacyCommandType> commandTypes,
            DateTimeOffset oldestRecord)
        {
            // Subject portion
            if (subject != null)
            {
                switch (subject)
                {
                    case MsaSubject msaSubject:
                        whereClause.Append(whereClause.Length > 0 ? " AND " : string.Empty);
                        whereClause.Append("c.s.puid = @puid");
                        parameterCollection.Add(new SqlParameter { Name = "@puid", Value = msaSubject.Puid.ToString() });
                        break;

                    case AadSubject aadSubject:
                        whereClause.Append(whereClause.Length > 0 ? " AND " : string.Empty);
                        whereClause.Append("c.s.objectId = @objectId");
                        parameterCollection.Add(new SqlParameter { Name = "@objectId", Value = aadSubject.ObjectId });
                        break;
                }
            }

            // Requester portion
            if (requester != null)
            {
                
                var allConfiguredRequestors = FlightingUtilities.GetConfigValue<string>("PCF.CONFIGURED_PCD_APP_IDS", String.Empty); // Get the comma separated appIds from app configuration
                var requestorSetCaseInsensitive = allConfiguredRequestors.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.InvariantCultureIgnoreCase); //Converting to HashSet so that we can check if the requestor exists in the collection in O(1) time
                if(requestorSetCaseInsensitive.Contains(requester)) //Check if the requester is from PCD
                {
                    StringBuilder requestorClause = new StringBuilder(whereClause.Length > 0 ? " AND c.r IN ({0})" : " c.r IN ({0})");
                    var loopIndex = 0;
                    var namedParameters = new List<string>();
                    // Loop through the list of PCD appIds as configured and build the query dynamically.
                    foreach (var appId in requestorSetCaseInsensitive)
                    {
                        var paramName = "@requester_" + loopIndex;
                        namedParameters.Add(paramName);

                        var newSqlParameter = new SqlParameter(paramName, appId);
                        parameterCollection.Add(newSqlParameter);
                        loopIndex++;
                    }
                    if(namedParameters.Count > 0)
                    {
                        whereClause.Append(string.Format(requestorClause.ToString(), string.Join(",", namedParameters)));
                    }
                }else // If requester is not from PCD, proceed with the actual requester
                {
                    whereClause.Append(whereClause.Length > 0 ? " AND " : string.Empty);
                    whereClause.Append("c.r = @requester");
                    parameterCollection.Add(new SqlParameter { Name = "@requester", Value = requester });
                }           
            }

            // Command types portion
            if (commandTypes != null)
            {
                StringBuilder commandTypeClause = new StringBuilder();
                for (int i = 0; i < commandTypes.Count; i++)
                {
                    commandTypeClause.Append(commandTypeClause.Length > 0 ? " OR " : string.Empty);
                    commandTypeClause.Append($"c.ct = @commandType{i}");
                    parameterCollection.Add(new SqlParameter { Name = $"@commandType{i}", Value = (int)commandTypes[i] });
                }

                if (commandTypeClause.Length > 0)
                {
                    whereClause.Append(whereClause.Length > 0 ? " AND " : string.Empty);
                    whereClause.Append($"({commandTypeClause})");
                }
            }

            // Max age portion
            oldestRecord = new DateTimeOffset(
                Math.Max(oldestRecord.UtcTicks, DateTimeOffset.UtcNow.AddDays(-Config.Instance.CommandHistory.MaxAgeInDaysForQuery).UtcTicks),
                TimeSpan.Zero);

            whereClause.Append(whereClause.Length > 0 ? " AND " : string.Empty);
            whereClause.Append("c.crt >= @createdTime");
            parameterCollection.Add(new SqlParameter { Name = "@createdTime", Value = oldestRecord.ToUnixTimeSeconds() });
        }
    }
}
