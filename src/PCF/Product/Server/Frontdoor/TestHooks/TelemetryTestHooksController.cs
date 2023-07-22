namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.Telemetry;
    using Microsoft.PrivacyServices.CommandFeed.Service.Telemetry.BackgroundTasks;
    using Microsoft.PrivacyServices.CommandFeed.Service.Telemetry.Repository;

#if INCLUDE_TEST_HOOKS

    /// <summary>
    /// A collection of test hooks, used for helping our test automation out. These hooks are NOT compiled 
    /// for builds that go to production.
    /// </summary>
    [RoutePrefix("testhooks")]
    [ExcludeFromCodeCoverage]
    public class TelemetryTestHooksController : ApiController
    {
        private static readonly AgentId FakeAgentId = new AgentId("D6EE9E03-03DF-4105-A859-0C5447353F06");

        /// <summary>
        /// Initializes a new instance of the class <see cref="TelemetryTestHooksController" />.
        /// </summary>
        public TelemetryTestHooksController()
        {
            ProductionSafetyHelper.EnsureNotInProduction();
        }

        /// <summary>
        /// Verify if lifecycle event could be added to telemetry database.
        /// </summary>
        [HttpGet]
        [Route("telemetry/canaddlifecycleevent")]
        [IncomingRequestActionFilter("TestHooks", "CanAddLifecycleEvent", "1.0")]
        [DisallowPort80ActionFilter]
        public async Task<IHttpActionResult> CanAddLifecycleEventAsync()
        {
            await CommandFeedGlobals.ServiceAuthorizer.CheckAuthorizedAsync(this.Request, AuthenticationScope.TestHooks);

            var lifecycleEvent = GetRandomLifecycleEvent(agentid: FakeAgentId);

            await CommandFeedGlobals.KustoTelemetryRepository.AddAsync(new List<LifecycleEventTelemetry>() { lifecycleEvent });

            return this.Ok();
        }

        /// <summary>
        /// Verify if baseline record could be added to telemetry database.
        /// </summary>
        [HttpGet]
        [Route("telemetry/canaddbaseline")]
        [IncomingRequestActionFilter("TestHooks", "CanAddBaselineAsync", "1.0")]
        [DisallowPort80ActionFilter]
        public async Task<IHttpActionResult> CanAddBaselineAsync()
        {
            await CommandFeedGlobals.ServiceAuthorizer.CheckAuthorizedAsync(this.Request, AuthenticationScope.TestHooks);

            var baseline = GetFakeQueueDepthWorkItem(agentid: FakeAgentId);

            await CommandFeedGlobals.KustoTelemetryRepository.AddBaselineAsync(baseline);

            return this.Ok();
        }

        /// <summary>
        /// Verify if agent azure storage queue depth could be added to telemetry database.
        /// </summary>
        [HttpGet]
        [Route("telemetry/canaddazurestoragequeuedepth")]
        [IncomingRequestActionFilter("TestHooks", "CanAddAzureStorageQueueDepthAsync", "1.0")]
        [DisallowPort80ActionFilter]
        public async Task<IHttpActionResult> CanAddAzureStorageQueueDepthAsync()
        {
            await CommandFeedGlobals.ServiceAuthorizer.CheckAuthorizedAsync(this.Request, AuthenticationScope.TestHooks);

            var queueStats = new List<AgentQueueStatistics>()
            {
                GetFakeAgentQueueStatistics(agentid: FakeAgentId),
                GetFakeAgentQueueStatistics(agentid: FakeAgentId)
            };

            await CommandFeedGlobals.KustoTelemetryRepository.AddAzureStorageQueueDepthAsync(queueStats);

            return this.Ok();
        }

        /// <summary>
        /// Verify if baseline task could be added to telemetry database.
        /// </summary>
        [HttpGet]
        [Route("telemetry/canaddtask")]
        [IncomingRequestActionFilter("TestHooks", "CanAddTaskAsync", "1.0")]
        [DisallowPort80ActionFilter]
        public async Task<IHttpActionResult> CanAddTaskAsync()
        {
            await CommandFeedGlobals.ServiceAuthorizer.CheckAuthorizedAsync(this.Request, AuthenticationScope.TestHooks);

            var baseline = GetFakeQueueDepthWorkItem(agentid: FakeAgentId);

            await CommandFeedGlobals.KustoTelemetryRepository.AddTaskAsync(baseline, TaskActionName.Create);
            await CommandFeedGlobals.KustoTelemetryRepository.AddTaskAsync(baseline, TaskActionName.Complete);

            return this.Ok();
        }

        /// <summary>
        /// Verify if baseline task could be added to telemetry database.
        /// </summary>
        [HttpGet]
        [Route("telemetry/canaddtasks")]
        [IncomingRequestActionFilter("TestHooks", "CanAddTasksAsync", "1.0")]
        [DisallowPort80ActionFilter]
        public async Task<IHttpActionResult> CanAddTasksAsync()
        {
            await CommandFeedGlobals.ServiceAuthorizer.CheckAuthorizedAsync(this.Request, AuthenticationScope.TestHooks);

            var baseline = new List<QueueDepthWorkItem>()
            {
                GetFakeQueueDepthWorkItem(agentid: FakeAgentId),
                GetFakeQueueDepthWorkItem(agentid: FakeAgentId),
                GetFakeQueueDepthWorkItem(agentid: FakeAgentId)
            };

            await CommandFeedGlobals.KustoTelemetryRepository.AddTasksAsync(baseline, TaskActionName.Create);
            await CommandFeedGlobals.KustoTelemetryRepository.AddTasksAsync(baseline, TaskActionName.Complete);

            return this.Ok();
        }

        /// <summary>
        /// Verify if can run agent aggregation query.
        /// </summary>
        [HttpPost]
        [Route("telemetry/canappendagentstat")]
        [IncomingRequestActionFilter("TestHooks", "CanAppendAgentStatAsync", "1.0")]
        [DisallowPort80ActionFilter]
        public async Task<IHttpActionResult> CanAppendAgentStatAsync()
        {
            await CommandFeedGlobals.ServiceAuthorizer.CheckAuthorizedAsync(this.Request, AuthenticationScope.TestHooks);
            await CommandFeedGlobals.KustoTelemetryRepository.AppendAgentStatAsync(FakeAgentId);
            return this.Ok();
        }

        /// <summary>
        /// Verify if can run interpolation query.
        /// </summary>
        [HttpPost]
        [Route("telemetry/canruninterpolationquery")]
        [IncomingRequestActionFilter("TestHooks", "CanRunInterpolationQueryAsync", "1.0")]
        [DisallowPort80ActionFilter]
        public async Task<IHttpActionResult> CanRunInterpolationQueryAsync()
        {
            await CommandFeedGlobals.ServiceAuthorizer.CheckAuthorizedAsync(this.Request, AuthenticationScope.TestHooks);
            await CommandFeedGlobals.KustoTelemetryRepository.InterpolateAgentStatAsync(FakeAgentId);
            return this.Ok();
        }

        /// <summary>
        /// Verifies if we can call StartQueueDepthBaseline debug API
        /// </summary>
        [HttpPost]
        [Route("telemetry/canstartqdbaseline")]
        [IncomingRequestActionFilter("TestHooks", "CanStartQueueDepthBaseline", "1.0")]
        [DisallowPort80ActionFilter]
        public IHttpActionResult CanStartQueueDepthBaseline()
        {
            var dataAgentMap = CommandFeedGlobals.DataAgentMapFactory.GetDataAgentMap();
            var dataAgentInfo = dataAgentMap[dataAgentMap.GetAgentIds().First()];

            return new StartQueueDepthBaselineActionResult(
                dataAgentInfo.AgentId,
                this.Request,
                dataAgentInfo,
                CommandFeedGlobals.ServiceAuthorizer,
                AuthenticationScope.DebugApis);
        }

        private static LifecycleEventTelemetry GetRandomLifecycleEvent(
            AgentId agentid = null, 
            AssetGroupId assetGroupId = null,
            CommandId commandId = null,
            PrivacyCommandType commandType = PrivacyCommandType.AccountClose,
            LifecycleEventType eventType = LifecycleEventType.CommandCompletedEvent)
        {
            return new LifecycleEventTelemetry()
            {
                AgentId = agentid ?? new AgentId(Guid.NewGuid()),
                AssetGroupId = assetGroupId ?? new AssetGroupId(Guid.NewGuid()),
                CommandId = commandId ?? new CommandId(Guid.NewGuid()),
                CommandType = commandType,
                EventType = eventType,
                Timestamp = DateTimeOffset.UtcNow,
                Count = 1
            };
        }

        private static AgentQueueStatistics GetFakeAgentQueueStatistics(
            AgentId agentid = null,
            AssetGroupId assetGroupId = null,
            PrivacyCommandType commandType = PrivacyCommandType.AgeOut,
            SubjectType subjectType = SubjectType.Msa,
            string dbMoniker = "FakeMoniker")
        {
            return new AgentQueueStatistics()
            {
                AgentId = agentid ?? new AgentId(Guid.NewGuid()),
                AssetGroupId = assetGroupId ?? new AssetGroupId(Guid.NewGuid()),
                CommandType = commandType,
                SubjectType = subjectType,
                DbMoniker = dbMoniker,
                QueryDate = DateTimeOffset.UtcNow.DateTime,
                PendingCommandCount = 1
            };
        }

        private static QueueDepthWorkItem GetFakeQueueDepthWorkItem(
            AgentId agentid = null,
            AssetGroupId assetGroupId = null,
            SubjectType subjectType = SubjectType.Aad,
            DateTimeOffset? startTime = null)
        {
            return new QueueDepthWorkItem()
            {
                TaskId = Guid.NewGuid(),
                CreateTime = startTime?.UtcDateTime ?? DateTimeOffset.UtcNow,
                AgentId = agentid ?? new AgentId(Guid.NewGuid()),
                AssetGroupId = assetGroupId ?? new AssetGroupId(Guid.NewGuid()),
                CommandTypeCountDictionary = new Dictionary<PrivacyCommandType, int>()
                                {
                                    { PrivacyCommandType.AccountClose, 0 },
                                    { PrivacyCommandType.Delete, 0 },
                                    { PrivacyCommandType.Export, 0 },
                                },
                ContinuationToken = null,
                DatabaseId = "DatabaseId",
                MaxItemsCount = Config.Instance.Telemetry.MaxItemCount,
                SubjectType = subjectType,
                CollectionId = CosmosDbQueueCollection.GetQueueCollectionId(subjectType),
                DbMoniker = "DbMoniker",
                BatchSize = 1000,
            };
        }
    }

#endif
}