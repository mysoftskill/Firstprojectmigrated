namespace Microsoft.PrivacyServices.CommandFeed.Service.Instrumentation
{
    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.CommonSchema.Services;
    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.Telemetry;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.PrivacyServices.SignalApplicability;
    using Microsoft.Telemetry;
    using Ms.Qos;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.Tracing;
    using System.Linq;
    using System.Net;

    /// <summary>
    /// SLL-based implementation of ILogger
    /// </summary>
    public class SllLogger : Common.ILogger, IDisposable
    {
        private static readonly string AppVer = EnvironmentInfo.AssemblyVersion;
        private static readonly string CloudLocation = Environment.GetEnvironmentVariable("MONITORING_DATACENTER");

        // Store the SLL policy registration
        private readonly IDisposable sllContextRegister;
        private readonly IDisposable sllServiceFabric;

        // For api log sampling. Using ConcurrentDictionary for thread safety
        // Sampling rate mean 1/rate event will be actually fired. Set rate to 0 will disable sampling
        private long incomingApiSamplingRate = 0;
        private readonly ConcurrentDictionary<string, int> incomingApiSamplingMap = new ConcurrentDictionary<string, int>();
        private long outgoingApiSamplingRate = 0;
        private readonly ConcurrentDictionary<string, int> outgoingApiSamplingMap = new ConcurrentDictionary<string, int>();

        public SllLogger(long? maxFileSizeBytes = null, int? maxFileCount = null)
        {
            if (!EnvironmentInfo.IsUnitTest)
            {
                this.sllContextRegister = Policies.Owin.Register();
                this.sllServiceFabric = Policies.ServiceFabric.Register();

                InitializeSampling(
                    Config.Instance.SllLogger.IncomingApiSamplingRate,
                    Config.Instance.SllLogger.IncomingApiSamplingList,
                    Config.Instance.SllLogger.OutgoingApiSamplingRate,
                    Config.Instance.SllLogger.OutgoingApiSamplingList);
            }
        }

        ~SllLogger()
        {
            this.Dispose(false);
            this.sllContextRegister.Dispose();
            this.sllServiceFabric.Dispose();
        }

        /// <summary>
        /// Implements dispose pattern
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gets the current CV.
        /// </summary>
        public string CorrelationVector => Sll.Context.Vector?.Value;

        /// <summary>
        /// Sets the CV to the given value.
        /// </summary>
        public void SetCorrelationVector(string vector)
        {
            Sll.Context.Vector = CommonSchema.Services.Logging.CorrelationVector.Parse(vector);
        }

        /// <summary>
        /// Ensures that a CV is created.
        /// </summary>
        public void EnsureCorrelationVector()
        {
            if (Sll.Context.Vector == null)
            {
                Sll.Context.Vector = new CorrelationVector();
            }
        }

        /// <summary>
        /// Logs the given incoming request event.
        /// </summary>
        public void Log(IncomingEvent incomingEvent)
        {
            if (this.incomingApiSamplingRate > 0 && this.incomingApiSamplingMap.TryGetValue(incomingEvent.OperationName, out var currentCount))
            {
                if (currentCount < this.incomingApiSamplingRate - 1)
                {
                    // Skip logging if sampling rate has not been reached
                    this.incomingApiSamplingMap[incomingEvent.OperationName] = currentCount + 1;
                    return;
                }
                else
                {
                    // Continue logging, reset sampling counter
                    this.incomingApiSamplingMap[incomingEvent.OperationName] = 0;
                    incomingEvent["Sampled"] = "1";
                }
            }

            ClassifyRequest(incomingEvent.StatusCode, out EventLevel eventLevel, out ServiceRequestStatus requestStatus);

            bool succeeded = incomingEvent.OperationStatus == OperationStatus.Succeeded ? true : false;

            if (incomingEvent.ForceReportAsFailed)
            {
                eventLevel = EventLevel.Error;
                requestStatus = ServiceRequestStatus.ServiceError;
                succeeded = false;
            }

            var sllEvent = new IncomingServiceRequest
            {
                baseData =
                {
                    callerIpAddress = incomingEvent.CallerIpAddress,
                    callerName = incomingEvent.CallerName,
                    latencyMs = (int)incomingEvent.ElapsedTime.TotalMilliseconds,
                    operationName = incomingEvent.OperationName,
                    protocolStatusCode = ((int)incomingEvent.StatusCode).ToString(),
                    requestMethod = incomingEvent.RequestMethod,
                    serviceErrorCode = (int)incomingEvent.StatusCode,
                    targetUri = incomingEvent.TargetUri,
                    requestStatus = requestStatus,
                    succeeded = succeeded
                },
                ErrorDetails = CreateErrorDetails(incomingEvent.Exception),

                // Pack the properties into one field to reduce the number of columns
                // created by the log flattener.
                PackedProperties = string.Join("|", incomingEvent.Properties.Select(x => $"{x.Key}={x.Value}"))
            };

            Sll.Context?.CorrelationContext?.FillIncomingServiceRequest(sllEvent.baseData);

            sllEvent.Log(eventLevel, FillEnvelope);
        }

        /// <summary>
        /// Logs the commands we returned for a given request.
        /// </summary>
        public void CommandsReturned(
            AgentId agentId,
            AssetGroupId assetGroupId,
            string assetGroupQualifier,
            IEnumerable<CommandId> commandIds,
            Dictionary<PrivacyCommandType, int> commandCounts)
        {
            int commandCount;

            new GetCommandsEvent
            {
                AgentId = agentId.Value,
                AssetGroupId = assetGroupId.Value,
                AssetGroupQualifier = assetGroupQualifier,
                CommandIds = string.Join(";", commandIds),
                AccountCloseCommandCount = commandCounts.TryGetValue(PrivacyCommandType.AccountClose, out commandCount) ? commandCount : 0,
                DeleteCommandCount = commandCounts.TryGetValue(PrivacyCommandType.Delete, out commandCount) ? commandCount : 0,
                ExportCommandCount = commandCounts.TryGetValue(PrivacyCommandType.Export, out commandCount) ? commandCount : 0
            }.LogInformational(FillEnvelope);
        }

        /// <summary>
        /// Logs the command we ingested to Agent queue.
        /// </summary>
        public void CommandIngested(PrivacyCommand command)
        {
            new AddCommandEvent
            {
                CommandId = command.CommandId.Value,
                AgentId = command.AgentId.Value,
                AssetGroupId = command.AssetGroupId.Value,
                CommandType = command.CommandType.ToString(),
                SubjectType = command.Subject.GetSubjectType().ToString()
            }.LogInformational(FillEnvelope);
        }

        /// <summary>
        /// Logs the given outgoing event.
        /// </summary>
        public void Log(OutgoingEvent outgoingEvent)
        {
            if (this.outgoingApiSamplingRate > 0 && this.outgoingApiSamplingMap.TryGetValue(outgoingEvent.OperationName, out var currentCount))
            {
                if (currentCount < this.outgoingApiSamplingRate - 1)
                {
                    // Skip logging if sampling rate has not been reached
                    this.outgoingApiSamplingMap[outgoingEvent.OperationName] = currentCount + 1;
                    return;
                }
                else
                {
                    // Continue logging, reset sampling counter
                    this.outgoingApiSamplingMap[outgoingEvent.OperationName] = 0;
                    outgoingEvent["Sampled"] = "1";
                }
            }

            FillCommonAndLog(outgoingEvent, new OutgoingServiceRequest());
        }

        /// <summary>
        /// Logs the given CosmosDB outgoing event.
        /// </summary>
        public void Log(CosmosDbOutgoingEvent cosmosDbEvent)
        {
            if (this.outgoingApiSamplingRate > 0 && this.outgoingApiSamplingMap.TryGetValue(cosmosDbEvent.OperationName, out var currentCount))
            {
                if (currentCount < this.outgoingApiSamplingRate - 1)
                {
                    // Skip logging if sampling rate has not been reached
                    this.outgoingApiSamplingMap[cosmosDbEvent.OperationName] = currentCount + 1;
                    return;
                }
                else
                {
                    // Continue logging, reset sampling counter
                    this.outgoingApiSamplingMap[cosmosDbEvent.OperationName] = 0;
                    cosmosDbEvent["Sampled"] = "1";
                }
            }

            var outgoingRequest = new CosmosDbOutgoingServiceRequest
            {
                Collection = cosmosDbEvent.CollectionId,
                Moniker = cosmosDbEvent.Moniker,
                RequestCharge = cosmosDbEvent.RequestCharge,
                RowCount = cosmosDbEvent.RowCount,
                PartitionKey = cosmosDbEvent.PartitionKey,
                CommandIds = cosmosDbEvent.CommandIds,
                Throttled = cosmosDbEvent.IsThrottled
            };

            FillCommonAndLog(cosmosDbEvent, outgoingRequest);
        }

        /// <summary>
        /// Logs the given operation.
        /// </summary>
        public void Log(OperationEvent operationEvent)
        {
            ClassifyRequest(operationEvent.OperationStatus, out EventLevel eventLevel, out ServiceRequestStatus requestStatus, out bool succeeded);

            new InternalOperationEvent
            {
                ErrorDetails = CreateErrorDetails(operationEvent.Exception),
                LatencyMs = (int)operationEvent.ElapsedTime.TotalMilliseconds,
                OperationName = operationEvent.OperationName,
                Succeeded = succeeded
            }.Log(eventLevel, FillEnvelope);
        }

        /// <inheritdoc />
        public void LogDataAgentValidatorError(string cv, string commandId, Exception ex)
        {
            var sllEvent = new SllDataAgentValidationErrorEvent
            {
                Cv = cv,
                CommandId = commandId,
                ErrorDetails = CreateErrorDetails(ex)
            };

            sllEvent.LogError(FillEnvelope);
        }

        /// <inheritdoc />
        public void LogDataAgentUnrecognizedDataType(string cv, string commandId, string dataType)
        {
            var sllEvent = new SllDataAgentUnrecognizedDataType
            {
                Cv = cv,
                CommandId = commandId,
                DataType = dataType
            };

            sllEvent.LogError(FillEnvelope);
        }

        /// <inheritdoc />
        public void LogDataAgentUnrecognizedCommandType(string cv, string commandId, string commandType)
        {
            var sllEvent = new SllDataAgentUnrecognizedCommandType
            {
                Cv = cv,
                CommandId = commandId,
                CommandType = commandType
            };

            sllEvent.LogError(FillEnvelope);
        }

        /// <summary>
        /// Log an exception
        /// </summary>
        public void UnexpectedException(Exception ex)
        {
            if (ex is CommandFeedException commandFeedException)
            {
                if (commandFeedException.IsExpected)
                {
                    // This wasn't really an unexpected exception, so we just suppress it here.
                    return;
                }
            }

            DualLogger.Instance.Error(nameof(SllLogger), ex, "Unexpected exception!");
            PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "UnexpectedExceptions").Increment();

            if (ex == null)
            {
                ex = new ArgumentNullException(nameof(ex), "This indicate a logging issue that passed in a Null Exception");
            }

            var exception = new UnexpectedExceptionEvent
            {
                ErrorDetails = CreateErrorDetails(ex)
            };

            exception.Log(EventLevel.Error, FillEnvelope);
        }

        /// <inheritdoc />
        public void DistributedLockAcquiredEvent(string lockName, TimeSpan duration)
        {
            new DistributedLockAcquiredEvent
            {
                DurationMs = (int)duration.TotalMilliseconds,
                LockName = lockName
            }.LogInformational(FillEnvelope);
        }

        /// <inheritdoc />
        public void AgentQueueStatisticsEvent(AgentQueueStatistics agentQueueStatistics)
        {
            new AgentQueueStatisticsEvent
            {
                AgentId = agentQueueStatistics.AgentId.Value,
                AssetGroupId = agentQueueStatistics.AssetGroupId.Value,
                SubjectType = agentQueueStatistics.SubjectType.ToString(),
                DbMoniker = agentQueueStatistics.DbMoniker,
                MinPendingCommandCreationTime = agentQueueStatistics.MinPendingCommandCreationTime?.ToString() ?? string.Empty,
                MinLeaseAvailableTime = agentQueueStatistics.MinLeaseAvailableTime?.ToString() ?? string.Empty
            }.LogInformational(FillEnvelope);
        }

        /// <inheritdoc />
        public void NullPxsTimeRangePredicate(CommandId commandId)
        {
            new NullTimeRangePredicateEvent
            {
                CommandId = commandId.Value
            }.LogError(FillEnvelope);
        }

        /// <inheritdoc />
        public void LogPdmsDataSetAgeEvent(string assetGroupInfoStream, string variantInfoStream, DateTimeOffset createdTime, long version)
        {
            new PdmsDataSetAgeEvent
            {
                AgeInHours = (int)(DateTimeOffset.UtcNow - createdTime).TotalHours,
                Version = version,
                SourceStream = assetGroupInfoStream,
                VariantInfoStream = variantInfoStream
            }.LogWarning(FillEnvelope);
        }

        /// <inheritdoc />
        public void InvalidVerifierReceived(CommandId commandId, AgentId agentId, Exception ex)
        {
            new CommandReceivedInvalidVerifierEvent
            {
                AgentId = agentId.Value,
                CommandId = commandId.Value,
                ErrorDetails = CreateErrorDetails(ex)
            }.LogCritical(FillEnvelope);
        }

        /// <inheritdoc />
        public void AzureWorkerQueueDepth(string storageAccountName, string queueName, long depth)
        {
            new AzureQueueDepthEvent
            {
                Depth = depth,
                QueueName = queueName,
                StorageAccountName = storageAccountName
            }.LogInformational(FillEnvelope);
        }

        /// <inheritdoc />
        public void LogExportFileSizeEvent(
            AgentId agentId,
            AssetGroupId assetGroupId,
            CommandId commandId,
            string fileName,
            long sizeInBytes,
            long compressedSizeInBytes,
            bool isSourceCompressed,
            Common.SubjectType subjectType,
            AgentType agentType,
            string cloudInstance)
        {
            new ExportFileSizeEvent
            {
                AgentId = agentId.Value,
                AssetGroupId = assetGroupId.Value,
                Path = fileName,
                CommandId = commandId.Value,
                Length = sizeInBytes,
                CompressedLength = compressedSizeInBytes,
                IsSourceCompressed = isSourceCompressed,
                SubjectType = subjectType.ToString(),
                AgentType = agentType.ToString(),
                CloudInstance = cloudInstance
            }.LogInformational(FillEnvelope);
        }

        /// <inheritdoc />
        public void LogQueueDepth(CollectionQueueDepth queueDepth)
        {
            new SllCommandQueueDepthBaseline
            {
                BaselineVersion = queueDepth.BaselineVersion,
                DbMoniker = queueDepth.DbMoniker,
                CollectionId = queueDepth.CollectionId,
                AgentId = queueDepth.AgentId.Value,
                AssetGroupId = queueDepth.AssetGroupId.Value,
                CommandType = queueDepth.CommandType.ToString(),
                QueueDepth = queueDepth.CommandsCount,
                RequestCharge = (int)queueDepth.RequestCharge,
                DurationSeconds = (int)queueDepth.DurationSeconds
            }.LogInformational(FillEnvelope);
        }

        /// <inheritdoc />
        public void LogForceCompleteCommandEvent(
            CommandId commandId,
            AgentId agentId,
            AssetGroupId assetGroupId,
            string forceCompletedReason,
            PrivacyCommandType commandType,
            Common.SubjectType? subjectType)
        {
            new ForceCompleteCommandEvent
            {
                CommandId = commandId.Value,
                AgentId = agentId.Value,
                AssetGroupId = assetGroupId.Value,
                ForceCompleteReason = forceCompletedReason,
                CommandType = commandType.ToString(),
                SubjectType = subjectType.ToString()
            }.LogInformational(FillEnvelope);
        }

        /// <inheritdoc />
        public void LogNotReceivedForceCompleteCommandEvent(
            CommandId commandId,
            AgentId agentId,
            AssetGroupId assetGroupId,
            PrivacyCommandType commandType,
            Common.SubjectType? subjectType)
        {
            new NotReceivedForceCompletedCommandEvent
            {
                CommandId = commandId.Value,
                AgentId = agentId.Value,
                AssetGroupId = assetGroupId.Value,
                CommandType = commandType.ToString(),
                SubjectType = subjectType.ToString()
            }.LogInformational(FillEnvelope);
        }

        /// <inheritdoc />
        public void CommandsTransferred(int commandCount, string agentId, string assetGroupId, string transferPoint)
        {
            new CommandsTransferredEvent
            {
                AgentId = agentId,
                AssetGroupId = assetGroupId,
                TransferPoint = transferPoint,
                CommandCount = commandCount
            }.LogInformational(FillEnvelope);
        }

        /// <inheritdoc />
        public void IcmConnectorNotRegistered(string agentId, string assetGroupId, string eventName)
        {
            new IcmConnectorDetailsNotRegisteredEvent
            {
                AgentId = agentId,
                AssetGroupId = assetGroupId,
                EventName = eventName
            }.LogWarning(FillEnvelope);
        }

        /// <inheritdoc />
        public void LogLeaseReceiptFailedToParse(string leaseReceipt)
        {
            new LeaseReceiptFailedToParse()
            {
                LeaseReceipt = leaseReceipt
            }.LogError(FillEnvelope);
        }

        /// <inheritdoc />
        public void LogTelemetryLifecycleCheckpointInfo(TelemetryLifecycleCheckpointInfo eventInfo)
        {
            new SllTelemetryLifecycleCheckpointInfo()
            {
                CheckpointFrequency = eventInfo.CheckpointFrequency.ToString("c"),
                LastCheckpointTime = eventInfo.LastCheckpointTime.ToString("o"),
                EventsCount = eventInfo.EventsCount,
            }.LogInformational(FillEnvelope);
        }

        /// <summary>
        /// Logs the source location and reason for requesting a restart of the process.
        /// </summary>
        public void RestartRequested(string memberName, string fileName, int lineNumber, string reason)
        {
            new RestartRequestedEvent
            {
                MemberName = memberName,
                FileName = fileName,
                LineNumber = lineNumber,
                Reason = reason,
            }.LogInformational(FillEnvelope);
        }

        public void CommandFiltered(
            bool sentToAgent, 
            ApplicabilityReasonCode applicabilityCode, 
            IEnumerable<string> variantsApplied, 
            IEnumerable<DataTypeId> dataTypes, 
            IEnumerable<string> commandLifecycleEventNames, 
            Common.SubjectType subjectType, 
            PrivacyCommandType commandType, 
            bool isWhatIfMode, 
            string cloudInstance, 
            string salVersion, 
            string pdmsVersion, 
            AgentId agentId, 
            AssetGroupId assetGroupId, 
            CommandId commandId,
            DateTimeOffset commandCreationTimestamp)
        {
            dataTypes = dataTypes ?? Enumerable.Empty<DataTypeId>();
            variantsApplied = variantsApplied ?? Enumerable.Empty<string>();
            commandLifecycleEventNames = commandLifecycleEventNames ?? Enumerable.Empty<string>();

            cloudInstance = DefaultIfNullOrEmpty(cloudInstance);
            pdmsVersion = DefaultIfNullOrEmpty(pdmsVersion);
            salVersion = DefaultIfNullOrEmpty(salVersion);
            string dataTypeString = DefaultIfNullOrEmpty(string.Join(";", dataTypes.Select(x => x.Value).OrderBy(x => x)));
            string variantString = DefaultIfNullOrEmpty(string.Join(";", variantsApplied.OrderBy(x => x)));
            string lifecycleEventNames = DefaultIfNullOrEmpty(string.Join(";", commandLifecycleEventNames.OrderBy(x => x)));
            string environmentName = DefaultIfNullOrEmpty(EnvironmentInfo.EnvironmentName);

            new CommandFilteredEvent
            {
                AgentId = agentId.Value,
                AssetGroupId = assetGroupId.Value,
                CommandId = commandId.Value,
                CloudInstance = cloudInstance,
                CommandType = commandType.ToString(),
                SubjectType = subjectType.ToString(),
                DataTypes = dataTypeString,
                VariantsApplied = variantString,
                LifecycleEventNames = lifecycleEventNames,
                ApplicabilityCode = applicabilityCode.ToString(),
                SentToAgent = sentToAgent.ToString(),
                IsWhatIfMode = isWhatIfMode.ToString(),
                PDMSVersion = pdmsVersion,
                SALVersion = salVersion,
                EnvironmentName = environmentName,
                commandCreationTimestamp = commandCreationTimestamp.ToString("o"),
            }.LogInformational(FillEnvelope);

            string DefaultIfNullOrEmpty(string s)
            {
                if (string.IsNullOrEmpty(s))
                {
                    return "(not set)";
                }

                return s;
            }
        }

        /// <summary>
        /// Logs a query truncated event
        /// </summary>
        public void CommandHistoryQueryTooLarge(IPrivacySubject subject, string requester, IList<PrivacyCommandType> commandTypes, DateTimeOffset oldestRecord, CommandHistoryFragmentTypes fragmentsToRead)
        {
            string commandTypesString =string.Join(";", commandTypes?.Select(x => x.ToString()).OrderBy(x => x) ?? Enumerable.Empty<string>());

            new CommandHistoryQueryTooLarge
            {
                SubjectType = subject?.GetSubjectType().ToString(),
                Requester = requester,
                CommandTypes = commandTypesString,
                OldestRecord = oldestRecord.ToString("O"),
                FragmentsToRead = fragmentsToRead.ToString(),
            }.LogInformational(FillEnvelope);
        }

        /// <summary>
        /// Fills the common outgoing fields.
        /// </summary>
        private static void FillCommonAndLog<TOutgoingEvent, TSllEvent>(TOutgoingEvent outgoingEvent, TSllEvent sllEvent)
            where TSllEvent : OutgoingServiceRequest
            where TOutgoingEvent : OutgoingEvent
        {
            // Increment the CV before logging.
            Sll.Context?.Vector?.Increment();

            ClassifyRequest(outgoingEvent.OperationStatus, out EventLevel eventLevel, out ServiceRequestStatus requestStatus, out bool succeeded);

            Ms.Qos.OutgoingServiceRequest baseData = sllEvent.baseData;
            Sll.Context?.CorrelationContext?.FillOutgoingServiceRequest(baseData);

            baseData.latencyMs = (int)outgoingEvent.ElapsedTime.TotalMilliseconds;
            baseData.dependencyName = outgoingEvent.IncomingOperationName;
            baseData.dependencyOperationName = outgoingEvent.OperationName;
            baseData.succeeded = succeeded;
            baseData.requestStatus = requestStatus;

            baseData.protocolStatusCode = outgoingEvent.StatusCode;

            if (succeeded && string.IsNullOrEmpty(outgoingEvent.StatusCode))
            {
                baseData.protocolStatusCode = "OK";
            }

            if (!succeeded && string.IsNullOrEmpty(outgoingEvent.StatusCode))
            {
                baseData.protocolStatusCode = outgoingEvent.Exception?.GetType().ToString() ?? "Unknown Error";
            }

            // Pack the properties into one field to reduce the number of columns
            // created by the log flattener.
            sllEvent.PackedProperties = string.Join("|", outgoingEvent.Properties.Select(x => $"{x.Key}={x.Value}"));

            sllEvent.ErrorDetails = CreateErrorDetails(outgoingEvent.Exception);
            sllEvent.Log(eventLevel, FillEnvelope);
        }

        private static ErrorDetails CreateErrorDetails(Exception ex)
        {
            if (ex == null)
            {
                return new ErrorDetails();
            }

            var details = new ErrorDetails
            {
                ErrorMessage = ex.Message,
                Hresult = ex.HResult,
                ExceptionType = ex.GetType().FullName,
                StackTrace = ex.StackTrace
            };

            // Include the inner exception details, if they exist
            details.InnerExceptionDetails = (ex.InnerException != null) ? ex.InnerException.ToString() : string.Empty;

            return details;
        }

        private static void ClassifyRequest(OperationStatus operationStatus, out EventLevel eventLevel, out ServiceRequestStatus requestStatus, out bool succeeded)
        {
            switch (operationStatus)
            {
                case OperationStatus.Succeeded:
                    eventLevel = EventLevel.Informational;
                    requestStatus = ServiceRequestStatus.Success;
                    succeeded = true;
                    break;
                case OperationStatus.ExpectedFailure:
                    eventLevel = EventLevel.Informational;
                    requestStatus = ServiceRequestStatus.CallerError;
                    succeeded = false;
                    break;
                default:
                    eventLevel = EventLevel.Error;
                    requestStatus = ServiceRequestStatus.ServiceError;
                    succeeded = false;
                    break;
            }
        }

        private static void ClassifyRequest(HttpStatusCode code, out EventLevel eventLevel, out ServiceRequestStatus requestStatus)
        {
            requestStatus = ServiceRequestStatus.ServiceError;
            eventLevel = EventLevel.Error;

            if ((int)code >= 200
                && (int)code <= 299)
            {
                requestStatus = ServiceRequestStatus.Success;
                eventLevel = EventLevel.Informational;
            }

            if ((int)code >= 400
                && (int)code <= 499)
            {
                requestStatus = ServiceRequestStatus.CallerError;
                eventLevel = EventLevel.Informational;
            }
        }

        private static void FillEnvelope(Envelope envelope)
        {
            // It's best to ensure that CV is *always* set. This helps avoid data-skew issues
            // in cosmos when things are partitioned/grouped by CV.
            if (string.IsNullOrEmpty(envelope.cV))
            {
                PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "SllCvNotSupplied").Increment();
                envelope.cV = new CorrelationVector().Value;
            }

            if (string.IsNullOrWhiteSpace(envelope.appId))
            {
                envelope.appId = PrivacyApplication.Instance?.ServiceName;
            }

            if (string.IsNullOrWhiteSpace(envelope.appVer))
            {
                envelope.appVer = AppVer;
            }

            envelope.SafeCloud().location = CloudLocation;
        }

        private void InitializeSampling(long incomingApiSamplingRate, IEnumerable<string> incomingApiSamplingList, long outgoingApiSamplingRate, IEnumerable<string> outgoingApiSamplingList)
        {
            this.incomingApiSamplingRate = incomingApiSamplingRate;
            foreach (var api in incomingApiSamplingList)
            {
                incomingApiSamplingMap[api] = 0;
            }

            this.outgoingApiSamplingRate = outgoingApiSamplingRate;
            foreach (var api in outgoingApiSamplingList)
            {
                outgoingApiSamplingMap[api] = 0;
            }
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.sllServiceFabric?.Dispose();
            }
        }
    }
}
