namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Web.Http;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.PXS.Command.CommandStatus;

    /// <summary>
    /// A collection of test hooks, used for helping our test automation out. These hooks are NOT compiled 
    /// for builds that go to production.
    /// </summary>
    [RoutePrefix("debug")]
    [ExcludeFromCodeCoverage]
    public class DebugUtilitiesController : ApiController
    {
        /// <summary>
        /// Gets the cold storage record for the given command ID.
        /// </summary>        
        /// <url>https://pcf.privacy.microsoft.com/debug/status/commandid/{commandId}</url>
        /// <verb>get</verb>
        /// <group>Debug Utilities</group>
        /// <param in="path" name="commandId" cref="string">The command ID.</param>
        /// <response code="200"><see cref="CommandStatusResponse"/></response>
        [HttpGet]
        [Route("status/commandid/{commandId}")]
        [IncomingRequestActionFilter("DebugUtilities", "GetCommandStatusById", "1.0")]
        [DisallowPort80ActionFilter]
        public IHttpActionResult GetCommandStatus(string commandId)
        {
            if (!CommandId.TryParse(commandId, out CommandId id))
            {
                return this.BadRequest("Invalid command ID.");
            }

            var actionResult = new GetCommandStatusByCommandIdActionResult(
                this.Request,
                id,
                CommandFeedGlobals.CommandHistory,
                ExportStorageManager.Instance,
                CommandFeedGlobals.DataAgentMapFactory.GetDataAgentMap(),
                CommandFeedGlobals.ServiceAuthorizer,
                AuthenticationScope.DebugApis);

            actionResult.RedactConfidentialFields = true;

            return actionResult;
        }

        /// <summary>
        /// Gets the PDMS data set, applying filtering both by agent ID and by data set version as query string parameters.
        /// </summary>
        /// <url>https://pcf.privacy.microsoft.com/debug/dataagentmap</url>
        /// <verb>get</verb>
        /// <group>Debug Utilities</group>
        /// <param in="query" name="agent" cref="string" reqiured="false">The agent ID.</param>
        /// <param in="query" name="version" cref="long" required="false">The data set version.</param>
        /// <response code="200"><see cref="Newtonsoft.Json.Linq.JObject"/></response>
        [HttpGet]
        [Route("dataagentmap")]
        [IncomingRequestActionFilter("DebugUtilities", "GetPdmsInfoByAgentId", "1.0")]
        [DisallowPort80ActionFilter]
        public IHttpActionResult GetPdmsInfo([FromUri] string agent = null, [FromUri] long? version = null)
        {
            AgentId agentId = null;
            if (!string.IsNullOrEmpty(agent) && !AgentId.TryParse(agent, out agentId))
            {
                return this.BadRequest("Malformed agent ID.");
            }

            GetDebugPdmsInfoActionResult result = new GetDebugPdmsInfoActionResult(
                this.Request,
                agentId,
                version,
                CommandFeedGlobals.AssetGroupInfoReader,
                CommandFeedGlobals.ServiceAuthorizer,
                AuthenticationScope.DebugApis);

            return result;
        }

        /// <summary>
        /// Gets the PDMS data set, applying filtering both by agent ID and by data set version as query string parameters.
        /// </summary>       
        /// <url>https://pcf.privacy.microsoft.com/debug/queuestats/{agentIdString}/</url>
        /// <verb>get</verb>
        /// <group>Debug Utilities</group>
        /// <param in="path" name="agentIdString" cref="string">The agent ID.</param>
        /// <param in="query" name="detailed" cref="bool"><c>true</c> for getting detailed statistics.</param>
        /// <response code="200"><see cref="PXS.Command.CommandStatus.AgentQueueStatisticsResponse"/></response>
        [HttpGet]
        [Route("queuestats/{agentIdString}/")]
        [IncomingRequestActionFilter("DebugUtilities", "GetQueueStatsForAgent", "1.0")]
        [DisallowPort80ActionFilter]
        [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "string")]
        public IHttpActionResult GetQueueStatsForAgent(
            string agentIdString, 
            [FromUri] bool detailed = true)
        {
            AgentId agentId = null;
            if (!AgentId.TryParse(agentIdString, out agentId))
            {
                return this.BadRequest("Malformed agent ID.");
            }

            ICommandQueue commandQueue = new DataAgentCommandQueue(
                agentId, 
                CommandFeedGlobals.CommandQueueFactory, 
                CommandFeedGlobals.DataAgentMapFactory.GetDataAgentMap());
            
            GetQueueStatsForAgentActionResult result = new GetQueueStatsForAgentActionResult(
                this.Request,
                commandQueue,
                CommandFeedGlobals.DataAgentMapFactory.GetDataAgentMap(),
                detailed,
                CommandFeedGlobals.ServiceAuthorizer,
                AuthenticationScope.DebugApis);

            return result;
        }

        /// <summary>Force completes a command.</summary>       
        /// <url>https://pcf.privacy.microsoft.com/debug/completecommand/{cid}</url>
        /// <verb>get</verb>
        /// <group>Debug Utilities</group>
        /// <param in="path" name="cid" cref="string">The command ID.</param>
        /// <response code="200"></response>
        [HttpGet]
        [Route("completecommand/{cid}")]
        [IncomingRequestActionFilter("DebugUtilities", "ForceCompleteCommand", "1.0")]
        [DisallowPort80ActionFilter]
        public IHttpActionResult ForceCompleteCommand(string cid)
        {
            if (!CommandId.TryParse(cid, out CommandId commandId))
            {
                return this.BadRequest("Malformed command ID.");
            }

            ForceCompleteCommandActionResult actionResult = new ForceCompleteCommandActionResult(
                this.Request,
                commandId,
                CommandFeedGlobals.CommandHistory,
                CommandFeedGlobals.CosmosDbClientFactory,
                CommandFeedGlobals.AppConfiguration,
                CommandFeedGlobals.EventPublisher,
                CommandFeedGlobals.ServiceAuthorizer,
                AuthenticationScope.DebugApis);

            return actionResult;
        }

        /// <summary>Resets the next visible time for a command id associated with the given agent.</summary>        
        /// <url>https://pcf.privacy.microsoft.com/debug/resetnextvisibletime/{agentId}/{commandId}</url>
        /// <verb>get</verb>
        /// <group>Debug Utilities</group>
        /// <param in="path" name="agentId" cref="string">The agent ID.</param>
        /// <param in="path" name="assetGroupId" cref="string">The asset group ID.</param>
        /// <param in="path" name="commandId" cref="string">The command ID.</param>
        /// <response code="200"></response>
        [HttpGet]
        [Route("resetnextvisibletime/{agentId}/{assetGroupId}/{commandId}")]
        [IncomingRequestActionFilter("DebugUtilities", "ResetNextVisibleTime", "1.0")]
        public IHttpActionResult ResetNextVisibleTime(string agentId, string assetGroupId, string commandId)
        {
            if (!AgentId.TryParse(agentId, out AgentId agentIdParsed))
            {
                return this.BadRequest("Invalid agent ID.");
            }

            if (!AssetGroupId.TryParse(assetGroupId, out AssetGroupId assetGroupIdParsed))
            {
                return this.BadRequest("Invalid asset group ID.");
            }

            if (!CommandId.TryParse(commandId, out CommandId commandIdParsed))
            {
                return this.BadRequest("Malformed command ID.");
            }

            ResetNextVisibleTimeActionResult actionResult = new ResetNextVisibleTimeActionResult(
                this.Request,
                commandIdParsed,
                agentIdParsed,
                assetGroupIdParsed,
                CommandFeedGlobals.DataAgentMapFactory.GetDataAgentMap(),
                CommandFeedGlobals.CommandHistory,
                CommandFeedGlobals.ServiceAuthorizer,
                AuthenticationScope.DebugApis,
                CommandFeedGlobals.CommandQueueFactory);

            return actionResult;
        }

        /// <summary>
        /// Flushes a given agent's queue, clearing all commands issued until the flushdate
        /// </summary>
        /// <url>https://pcf.privacy.microsoft.com/debug/flushqueue</url>
        /// <verb>get</verb>
        /// <group>Debug Utilities</group>
        /// <param in="query" name="agent" cref="string" required="true">The agent ID.</param>
        /// <param in="query" name="flushDate" cref="string" required="false">The flush date.</param>
        /// <response code="200"></response>
        [HttpGet]
        [Route("flushqueue")]
        [IncomingRequestActionFilter("DebugUtilities", "FlushAgentQueue", "1.0")]
        [DisallowPort80ActionFilter]
        public IHttpActionResult FlushAgentQueue([FromUri] string agent, [FromUri] string flushDate = null)
        {
            FlushAgentQueueActionResult actionResult = new FlushAgentQueueActionResult(
                this.Request,
                agent,
                flushDate,
                CommandFeedGlobals.ServiceAuthorizer,
                AuthenticationScope.DebugApis,
                CommandFeedGlobals.AgentQueueFlushWorkItemPublisher);

            return actionResult;
        }

        /// <summary>
        /// Replay selected days of commands for a given agent 
        /// </summary>        
        /// <url>https://pcf.privacy.microsoft.com/debug/replaycommands/{agentId}</url>
        /// <verb>post</verb>
        /// <group>Debug Utilities</group>
        /// <param in="path" name="agentId" cref="string">The agent ID.</param>
        /// <response code="200"></response>
        [HttpPost]
        [Route("replaycommands/{agentId}")]
        [IncomingRequestActionFilter("DebugUtilities", "ReplayCommands", "1.0")]
        [DisallowPort80ActionFilter]
        public IHttpActionResult ReplayCommands(string agentId)
        {
            if (!AgentId.TryParse(agentId, out AgentId id))
            {
                return this.BadRequest("Invalid agent ID.");
            }

            return new ReplayCommandsActionResult(
                id,
                this.Request,
                CommandFeedGlobals.DataAgentMapFactory.GetDataAgentMap(),
                CommandFeedGlobals.ServiceAuthorizer,
                AuthenticationScope.DebugApis,
                CommandFeedGlobals.InsertReplayRequestWorkItemPublisher,
                CommandFeedGlobals.EnqueueReplayCommandsWorkItemPublisher,
                CommandFeedGlobals.CommandHistory,
                CommandFeedGlobals.AppConfiguration);
        }

        /// <summary>
        /// Run command queue depth baseline for the agent. 
        /// </summary>        
        /// <url>https://pcf.privacy.microsoft.com/debug/startqdbaseline/{agentId}</url>
        /// <verb>post</verb>
        /// <group>Debug Utilities</group>
        /// <param in="path" name="agentId" cref="string">The agent ID.</param>
        /// <response code="200"></response>
        [HttpPost]
        [Route("startqdbaseline/{agentId}")]
        [IncomingRequestActionFilter("DebugUtilities", "StartQueueDepthBaseline", "1.0")]
        [DisallowPort80ActionFilter]
        public IHttpActionResult StartQueueDepthBaseline(string agentId)
        {
            if (!AgentId.TryParse(agentId, out AgentId id))
            {
                return this.BadRequest("Invalid agent ID.");
            }

            var dataAgentMap = CommandFeedGlobals.DataAgentMapFactory.GetDataAgentMap();
            if (!dataAgentMap.TryGetAgent(id, out var dataAgentInfo))
            {
                return this.BadRequest($"Agent ID {agentId} not found.");
            }

            return new StartQueueDepthBaselineActionResult(
                id,
                this.Request,
                dataAgentInfo,
                CommandFeedGlobals.ServiceAuthorizer,
                AuthenticationScope.DebugApis);
        }

        /// <summary>
        /// Run ingestion pipeline for a given time window. 
        /// </summary>        
        /// <url>https://pcf.privacy.microsoft.com/debug/ingestionRecovery/{startDate}/{endDate}</url>
        /// <verb>post</verb>
        /// <group>Debug Utilities</group>
        /// <param in="path" name="startDate" cref="string">StartDate in yyyyMMdd format.</param>
        /// <param in="path" name="endDate" cref="string">endDate in yyyyMMdd format.</param>
        /// /// <param in="path" name="exportOnly" cref="string">true/false to limit ingestion recovery to export only or not.</param>
        /// <response code="200"></response>
        [HttpPost]
        [Route("ingestionrecovery/{startDate}/{endDate}/{exportOnly}/{nonExportOnly}")]
        [IncomingRequestActionFilter("DebugUtilities", "StartIngestionRecovery", "1.0")]
        [DisallowPort80ActionFilter]
        public IHttpActionResult StartIngestionRecovery(string startDate, string endDate, string exportOnly, string nonExportOnly)
        {
            // Date format strings.
            var format = "yyyyMMdd";
            var windowSizeInDays = FlightingUtilities.GetConfigValue<int>(ConfigNames.PCF.IngestionRecoveryWindowSizeInDays, 29);

            if (DateTime.TryParseExact(startDate, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime startDateParsed))
            {
                if (DateTime.TryParseExact(endDate, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime endDateParsed))
                {
                    if (endDateParsed <= startDateParsed)
                    {
                        return this.BadRequest($"End Date must be > startDate.");
                    }
                    else if (endDateParsed - startDateParsed > TimeSpan.FromDays(windowSizeInDays))
                    {
                        return this.BadRequest($"EndDate - StartDate should be less than {windowSizeInDays} days");
                    }
                    else if (DateTime.UtcNow - startDateParsed > TimeSpan.FromDays(29))
                    {
                        return this.BadRequest("startDate cannot be more than 29 days in the past");
                    }
                    else
                    {
                        return new IngestionRecoveryActionResult(
                        startDateParsed,
                        endDateParsed,
                        this.Request,
                        CommandFeedGlobals.ServiceAuthorizer,
                        AuthenticationScope.DebugApis,
                        new AzureWorkItemQueue<IngestionRecoveryWorkItem>(),
                        bool.Parse(exportOnly),
                        bool.Parse(nonExportOnly));
                    }
                }
            }

            return this.BadRequest($"Data validation failed.");
        }
    }
}
