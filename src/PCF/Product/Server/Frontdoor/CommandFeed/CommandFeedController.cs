namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Web.Http;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// The Command Feed controller. This is the entry point for PCF clients.
    /// </summary>
    [RoutePrefix("pcf/v1")]
    [ExcludeFromCodeCoverage]
    public partial class CommandFeedController : ApiController
    {
        /// <summary>
        /// Gets the next batch of commands for the given agent.
        /// </summary>        
        /// <url>https://pcf.privacy.microsoft.com/pcf/v1/{agentId}/commands</url>
        /// <verb>get</verb>
        /// <group>Command Feed</group>
        /// <param in="path" name="agentId" cref="string">The agent ID.</param>
        /// <response code="200"><see cref="Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts.GetCommandsResponse"/></response>
        [HttpGet]
        [Route("{agentId}/commands")]
        [IncomingRequestActionFilter("API", "GetCommands", "1.0")]
        [ClientVersionFilter]
        [DisallowPort80ActionFilter]
        public IHttpActionResult GetCommands(string agentId)
        {
            if (!AgentId.TryParse(agentId, out AgentId id))
            {
                return this.BadRequest("Invalid agent ID.");
            }
            var clientVersion = ClientVersionHelper.GetClientVersionHeader(this.Request);
            DualLogger.Instance.Information("clientVersion",$"clientVersion for agent {agentId} is: {clientVersion}");
            
            var authorizer = CommandFeedGlobals.ServiceAuthorizer;

            return new GetCommandsActionResult(
                id,
                this.Request,
                CommandFeedGlobals.CommandQueueFactory,
                CommandFeedGlobals.DataAgentMapFactory.GetDataAgentMap(),
                CommandFeedGlobals.EventPublisher,
                authorizer,
                CommandFeedGlobals.ApiTrafficHandler);
        }

        /// <summary>
        /// Updates the status of a single command for an agent.
        /// </summary>        
        /// <url>https://pcf.privacy.microsoft.com/pcf/v1/{agentId}/checkpoint</url>
        /// <verb>post</verb>
        /// <group>Command Feed</group>
        /// <param in="path" name="agentId" cref="string">The agent ID.</param>
        /// <response code="200"><see cref="Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts.CheckpointResponse"/></response>
        [HttpPost]
        [Route("{agentId}/checkpoint")]
        [IncomingRequestActionFilter("API", "PostCheckpoint", "1.0")]
        [ClientVersionFilter]
        [DisallowPort80ActionFilter]
        public IHttpActionResult Checkpoint(string agentId)
        {
            if (!AgentId.TryParse(agentId, out AgentId id))
            {
                return this.BadRequest("Invalid agent ID.");
            }
            var clientVersion = ClientVersionHelper.GetClientVersionHeader(this.Request);
            DualLogger.Instance.Information("clientVersion", $"clientVersion for agent {agentId} is: {clientVersion}");
            var map = CommandFeedGlobals.DataAgentMapFactory.GetDataAgentMap();

            var queue = new DataAgentCommandQueue(
                id,
                CommandFeedGlobals.CommandQueueFactory,
                map);

            return new CheckpointActionResult(
                id,
                CommandFeedGlobals.ServiceAuthorizer,
                queue,
                map,
                CommandFeedGlobals.EventPublisher,
                CommandFeedGlobals.DeleteFromQueuePublisher,
                this.Request,
                Logger.Instance,
                CommandFeedGlobals.CommandHistory,
                CommandFeedGlobals.ApiTrafficHandler);
        }

        /// <summary>
        /// Completes a batch of commands for an agent.
        /// </summary>        
        /// <url>https://pcf.privacy.microsoft.com/pcf/v1/{agentId}/batchcomplete</url>
        /// <verb>post</verb>
        /// <group>Command Feed</group>
        /// <param in="path" name="agentId" cref="string">The agent ID.</param>
        /// <response code="200"></response>
        [HttpPost]
        [Route("{agentId}/batchcomplete")]
        [IncomingRequestActionFilter("API", "PostBatchCompleteCheckpoint", "1.0")]
        [ClientVersionFilter]
        [DisallowPort80ActionFilter]
        public IHttpActionResult BatchCompleteCheckpoint(string agentId)
        {
            if (!AgentId.TryParse(agentId, out AgentId id))
            {
                return this.BadRequest("Invalid agent ID.");
            }
            var clientVersion = ClientVersionHelper.GetClientVersionHeader(this.Request);
            DualLogger.Instance.Information("clientVersion", $"clientVersion for agent {agentId} is: {clientVersion}");
            IncomingEvent.Current?.SetProperty("BatchCompleteCheckpointStart", DateTimeOffset.UtcNow.ToString());

            var map = CommandFeedGlobals.DataAgentMapFactory.GetDataAgentMap();

            var queue = new DataAgentCommandQueue(
                id,
                CommandFeedGlobals.CommandQueueFactory,
                map);

            return new BatchCheckpointCompleteActionResult(
                id,
                CommandFeedGlobals.ServiceAuthorizer,
                queue,
                map,
                CommandFeedGlobals.EventPublisher,
                CommandFeedGlobals.BatchCheckpointCompleteQueuePublisher,
                this.Request,
                Logger.Instance,
                CommandFeedGlobals.CommandHistory,
                CommandFeedGlobals.ApiTrafficHandler);
        }

        /// <summary>
        /// Gets the details about a specific Command for an agent
        /// </summary>        
        /// <url>https://pcf.privacy.microsoft.com/pcf/v1/{agentId}/command</url>
        /// <verb>post</verb>
        /// <group>Command Feed</group>
        /// <param in="path" name="agentId" cref="string">The agent ID.</param>
        /// <response code="200"><see cref="Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts.QueryCommandResponse"/></response>
        [HttpPost]
        [Route("{agentId}/command")]
        [IncomingRequestActionFilter("API", "QueryCommand", "1.0")]
        [ClientVersionFilter]
        [DisallowPort80ActionFilter]
        public IHttpActionResult GetCommand(string agentId)
        {
            if (!AgentId.TryParse(agentId, out AgentId id))
            {
                return this.BadRequest("Invalid agent ID.");
            }
            var clientVersion = ClientVersionHelper.GetClientVersionHeader(this.Request);
            DualLogger.Instance.Information("clientVersion", $"clientVersion for agent {agentId} is: {clientVersion}");
            var map = CommandFeedGlobals.DataAgentMapFactory.GetDataAgentMap();

            var queue = new DataAgentCommandQueue(
                id,
                CommandFeedGlobals.CommandQueueFactory,
                map);

            return new QueryCommandActionResult(
                id,
                this.Request,
                queue,
                CommandFeedGlobals.CommandHistory,
                map,
                CommandFeedGlobals.EventPublisher,
                CommandFeedGlobals.ServiceAuthorizer);
        }

        /// <summary>
        /// Gets the stats on an agent queue
        /// </summary>        
        /// <url>https://pcf.privacy.microsoft.com/pcf/v1/{agentId}/queuestats</url>
        /// <verb>post</verb>
        /// <group>Command Feed</group>
        /// <param in="path" name="agentId" cref="string">The agent ID.</param>
        /// <response code="200"><see cref="Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts.QueueStatsResponse"/></response>
        [HttpPost]
        [Route("{agentId}/queuestats")]
        [IncomingRequestActionFilter("API", "GetQueueStats", "1.0")]
        [ClientVersionFilter]
        [DisallowPort80ActionFilter]
        public IHttpActionResult GetQueueStats(string agentId)
        {
            if (!AgentId.TryParse(agentId, out AgentId id))
            {
                return this.BadRequest("Invalid agent ID.");
            }
            var clientVersion = ClientVersionHelper.GetClientVersionHeader(this.Request);
            DualLogger.Instance.Information("clientVersion", $"clientVersion for agent {agentId} is: {clientVersion}");
            return new GetQueueStatsActionResult(
                id,
                this.Request,
                CommandFeedGlobals.DataAgentMapFactory.GetDataAgentMap(),
                CommandFeedGlobals.ServiceAuthorizer,
                CommandFeedGlobals.KustoTelemetryRepository);
        }

        /// <summary>
        /// Insert command replay request
        /// </summary>
        /// <url>https://pcf.privacy.microsoft.com/pcf/v1/{agentId}/replaycommands</url>
        /// <verb>post</verb>
        /// <group>Command Feed</group>
        /// <param in="path" name="agentId" cref="string">The agent ID.</param>
        /// <response code="200"></response>
        [HttpPost]
        [Route("{agentId}/replaycommands")]
        [IncomingRequestActionFilter("API", "ReplayCommands", "1.0")]
        [ClientVersionFilter]
        [DisallowPort80ActionFilter]
        public IHttpActionResult ReplayCommands(string agentId)
        {
            if (!AgentId.TryParse(agentId, out AgentId id))
            {
                return this.BadRequest("Invalid agent ID.");
            }
            var clientVersion = ClientVersionHelper.GetClientVersionHeader(this.Request);
            DualLogger.Instance.Information("clientVersion", $"clientVersion for agent {agentId} is: {clientVersion}");
            return new ReplayCommandsActionResult(
                id,
                this.Request,
                CommandFeedGlobals.DataAgentMapFactory.GetDataAgentMap(),
                CommandFeedGlobals.ServiceAuthorizer,
                AuthenticationScope.Agent,
                CommandFeedGlobals.InsertReplayRequestWorkItemPublisher,
                CommandFeedGlobals.EnqueueReplayCommandsWorkItemPublisher,
                CommandFeedGlobals.CommandHistory,
                CommandFeedGlobals.AppConfiguration);
        }

        /// <summary>
        /// Logs exported file size by agent and command
        /// </summary>        
        /// <url>https://pcf.privacy.microsoft.com/pcf/v1/{agentId}/postexportedfilesize</url>
        /// <verb>post</verb>
        /// <group>Command Feed</group>
        /// <param in="path" name="agentId" cref="string">The agent ID.</param>
        /// <response code="200"></response>
        [HttpPost]
        [Route("{agentId}/postexportedfilesize")]
        [IncomingRequestActionFilter("API", "PostExportedFileSize", "1.0")]
        [ClientVersionFilter]
        [DisallowPort80ActionFilter]
        public IHttpActionResult PostExportedFileSize(string agentId)
        {
            if (!AgentId.TryParse(agentId, out AgentId id))
            {
                return this.BadRequest("Invalid agent ID.");
            }
            var clientVersion = ClientVersionHelper.GetClientVersionHeader(this.Request);
            DualLogger.Instance.Information("clientVersion", $"clientVersion for agent {agentId} is: {clientVersion}");
            return new PostExportedFileSizeActionResult(
                id,
                this.Request,
                CommandFeedGlobals.DataAgentMapFactory.GetDataAgentMap(),
                CommandFeedGlobals.ServiceAuthorizer,
                Logger.Instance);
        }
    }
}
