namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.PXS.Command.CommandStatus;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

    using Newtonsoft.Json;

    internal abstract class GetCommandStatusActionResult : BaseHttpActionResult
    {
        /// <summary>
        /// Parts of the response have "redacted" substituted when processing this on behalf of a low-trust caller.
        /// </summary>
        public const string RedactedReplacementString = "Redacted";

        private readonly HttpRequestMessage request;
        private readonly IAuthorizer authorizer;
        private readonly ICommandHistoryRepository repository;
        private readonly IDataAgentMap dataAgentMap;
        private readonly IExportStorageManager exportStorageManager;
        private readonly AuthenticationScope authenticationScope;

        protected GetCommandStatusActionResult(
            HttpRequestMessage request,
            ICommandHistoryRepository repository, 
            IExportStorageManager exportStorageManager,
            IDataAgentMap dataAgentMap,
            IAuthorizer authorizer,
            AuthenticationScope authenticationScope)
        {
            this.dataAgentMap = dataAgentMap;
            this.exportStorageManager = exportStorageManager;
            this.request = request;
            this.repository = repository;
            this.authorizer = authorizer;
            this.authenticationScope = authenticationScope;

            // Default is true to prevent accidental leaks, scenarios can explicitly opt out if they want.
            this.RedactConfidentialFields = true;
        }

        /// <summary>
        /// Indicates if we should expect the AssetGroupStatuses property as a result of query async.
        /// </summary>
        protected abstract bool ReturnAssetGroupStatuses { get; }

        /// <summary>
        /// Indicates if we should expect multiple matches as a result of query async and should return a list.
        /// </summary>
        protected abstract bool ReturnMultiple { get; }

        /// <summary>
        /// Indicates that confidential/private fields are suppressed in the output.
        /// </summary>
        public bool RedactConfidentialFields { get; set; }

        protected sealed override async Task<HttpResponseMessage> ExecuteInnerAsync(CancellationToken cancellationToken)
        {
            var authContext = await this.authorizer.CheckAuthorizedAsync(this.request, this.authenticationScope);
            StressRequestForwarder.Instance.SendForwardedRequest(authContext, this.request, null);

            CommandHistoryFragmentTypes fragmentsToRead = CommandHistoryFragmentTypes.Core | CommandHistoryFragmentTypes.Status;

            // Audit fragment is only used when returning asset group statuses.
            if (this.ReturnAssetGroupStatuses)
            {
                fragmentsToRead |= CommandHistoryFragmentTypes.Audit;
            }

            IEnumerable<CommandHistoryRecord> records = (await this.QueryAsync(this.repository, fragmentsToRead))?.ToList();

            if (records == null || !records.Any())
            {
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            if (this.ReturnMultiple)
            {
                var items = new List<CommandStatusResponse>();
                items.AddRange(records.Select(this.ConvertToResponse));

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new PushStreamContent(
                        (stream, content, context) =>
                        {
                            using (var sw = new StreamWriter(stream, Encoding.UTF8))
                            using (var jtw = new JsonTextWriter(sw))
                            {
                                var ser = new JsonSerializer();
                                ser.Serialize(jtw, items);
                            }
                        },
                        "application/json")
                };
            }

            CommandStatusResponse item = this.ConvertToResponse(records.Single());
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new JsonContent(item)
            };
        }

        protected abstract Task<IEnumerable<CommandHistoryRecord>> QueryAsync(ICommandHistoryRepository commandHistoryRepository, CommandHistoryFragmentTypes fragmentsToRead);

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        protected CommandStatusResponse ConvertToResponse(CommandHistoryRecord record)
        {
            try
            {
                List<AssetGroupCommandStatus> assetGroupStatuses = new List<AssetGroupCommandStatus>();

                // Get the set of common keys between audit map and status map.
                var keys = new HashSet<(AgentId agentId, AssetGroupId assetGroupId)>();
                foreach (var item in record.AuditMap)
                {
                    keys.Add(item.Key);
                }

                foreach (var item in record.StatusMap)
                {
                    keys.Add(item.Key);
                }

                foreach (var key in keys)
                {
                    record.AuditMap.TryGetValue(key, out CommandIngestionAuditRecord auditRecord);
                    record.StatusMap.TryGetValue(key, out CommandHistoryAssetGroupStatusRecord statusRecord);

                    if((key.agentId == null 
                        || key.agentId.GuidValue == Guid.Empty) || (key.assetGroupId == null
                        || key.assetGroupId.GuidValue == Guid.Empty))
                    {
                        DualLogger.Instance.Warning(nameof(GetCommandStatusActionResult), $"Ignoring unexpected data agentId={key.agentId?.GuidValue ?? Guid.Empty} and assetgroupid={key.assetGroupId?.GuidValue ?? Guid.Empty}");
                        continue;
                    }

                    AssetGroupCommandStatus status = new AssetGroupCommandStatus
                    {
                        AgentId = key.agentId?.GuidValue ?? Guid.Empty,
                        AssetGroupId = key.assetGroupId.GuidValue,
                        AssetGroupQualifier = this.GetAssetGroupQualifier(key.agentId, key.assetGroupId),
                        CompletedTime = statusRecord?.CompletedTime,
                        IngestionTime = statusRecord?.IngestionTime,
                        SoftDeleteTime = statusRecord?.SoftDeleteTime,
                        ForceCompleted = statusRecord?.ForceCompleted ?? false,
                        AffectedRows = statusRecord?.AffectedRows,

                        IngestionActionTaken = (auditRecord?.IngestionStatus ?? CommandIngestionStatus.Unknown).ToString(),
                        IngestionDebugText = auditRecord?.DebugText,

                        // Logic can be removed (Task 16191499)
                        IngestionAssemblyVersion = record.Core?.IngestionAssemblyVersion ?? "(unknown)",
                        IngestionDataSetVersion = record.Core?.IngestionDataSetVersion ?? -1,
                    };

                    assetGroupStatuses.Add(status);
                }

                var forceCompletedCount = assetGroupStatuses.Count(ag => ag.ForceCompleted);

                CommandStatusResponse response = new CommandStatusResponse
                {
                    CommandId = record.CommandId.GuidValue,
                    CommandType = record.Core.CommandType.ToString(),
                    Context = RedactedReplacementString,
                    Requester = RedactedReplacementString,
                    FinalExportDestinationUri = new Uri($"https://{RedactedReplacementString}"),
                    IsGloballyComplete = record.Core.IsGloballyComplete,
                    CompletedTime = record.Core.CompletedTime ?? DateTimeOffset.MinValue,
                    AssetGroupStatuses = this.ReturnAssetGroupStatuses ? assetGroupStatuses : new List<AssetGroupCommandStatus>(),
                    IsSyntheticCommand = record.Core.IsSynthetic,
                    SubjectType = record.Core.Subject?.GetSubjectType().ToString(),
                    IngestionAssemblyVersion = record.Core.IngestionAssemblyVersion,
                    ExportArchivesDeleteStatus = record.Core.ExportArchivesDeleteStatus,
                    IngestionDataSetVersion = record.Core.IngestionDataSetVersion,
                    TotalCommandCount = record.Core.TotalCommandCount,
                    CreatedTime = record.Core.CreatedTime,
                    DataTypes = GetDataTypes(record.Core.RawPxsCommand),
                    CompletionSuccessRate = GetCompletionSuccessRate(record.Core.CompletedCommandCount, forceCompletedCount, record.Core.TotalCommandCount)
                };

                if (!this.RedactConfidentialFields)
                {
                    response.Subject = record.Core.Subject;
                    response.FinalExportDestinationUri = this.exportStorageManager.GetReadOnlyContainerUri(record.Core.FinalExportDestinationUri);
                    response.Requester = record.Core.Requester;
                    response.Context = record.Core.Context;
                }

                return response;
            }
            catch(Exception ex)
            {
                DualLogger.Instance.Error(nameof(GetCommandStatusActionResult), ex, $"parsing command with ID={record.CommandId} failed");
                throw;
            }
        }

        /// <summary>
        /// Gets the ratio between the number of asset groups for which agents reported completion of this command and
        /// the total number of asset groups for which this command is applicable.
        /// A valid success rate is a number between 0 and 1. A success rate calculation error is represented as a negative number such as -1.
        /// A negative number is used for indicating failure due to serialization issues caused by double.NaN.
        /// </summary>
        internal static double GetCompletionSuccessRate(long? completedCount, long forceCompletedCount, long? totalCommandCount)
        {
            if (completedCount.HasValue && totalCommandCount.HasValue && totalCommandCount.Value != 0)
            {
                return (double)(completedCount.Value - forceCompletedCount) / totalCommandCount.Value;
            }

            // cannot divide by null or 0
            return -1;
        }

        private string GetAssetGroupQualifier(AgentId agentId, AssetGroupId assetGroupId)
        {
            string assetGroupQualifier = string.Empty;

            if (agentId != null &&
                this.dataAgentMap.TryGetAgent(agentId, out IDataAgentInfo agentInfo) &&
                agentInfo.TryGetAssetGroupInfo(assetGroupId, out IAssetGroupInfo assetGroupInfo))
            {
                assetGroupQualifier = assetGroupInfo.AssetGroupQualifier;
            }

            return assetGroupQualifier;
        }

        private static IEnumerable<string> GetDataTypes(string rawPxsCommand)
        {
            if (string.IsNullOrWhiteSpace(rawPxsCommand))
            {
                return Enumerable.Empty<string>();
            }

            // This seems expensive. Is there a better way?
            IEnumerable<string> dataTypes = null;
            PrivacyRequest command = JsonConvert.DeserializeObject<PrivacyRequest>(rawPxsCommand);
            switch (command?.RequestType)
            {
                case RequestType.Export:
                    dataTypes = JsonConvert.DeserializeObject<ExportRequest>(rawPxsCommand)?.PrivacyDataTypes;
                    break;
                case RequestType.Delete:
                    dataTypes = new[] { JsonConvert.DeserializeObject<DeleteRequest>(rawPxsCommand)?.PrivacyDataType };
                    break;
            }

            return dataTypes ?? Enumerable.Empty<string>();
        }
    }
}
