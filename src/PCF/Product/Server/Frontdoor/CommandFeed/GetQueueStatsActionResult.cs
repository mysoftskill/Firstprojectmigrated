namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Telemetry.Repository;
    using Microsoft.PrivacyServices.Identity;
    using Newtonsoft.Json;

    /// <summary>
    /// The GetQueueStats Api
    /// 
    /// Queries the Kusto Telemetry repository to get the queue stats for the agent
    /// If the AssetGroupId and/or subject type is provided, the queue depth is more tailored.
    /// </summary>
    internal class GetQueueStatsActionResult : BaseHttpActionResult
    {
        private AgentId agentId;
        private HttpRequestMessage requestMessage;
        private IDataAgentMap dataAgentMap;
        private IAuthorizer authorizer;
        private ITelemetryRepository kustoTelemetryRepository;

        public GetQueueStatsActionResult(
            AgentId agentId,
            HttpRequestMessage requestMessage,
            IDataAgentMap dataAgentMap,
            IAuthorizer authorizer,
            ITelemetryRepository kustoTelemetryRepository)
        {
            this.agentId = agentId;
            this.requestMessage = requestMessage;
            this.dataAgentMap = dataAgentMap;
            this.authorizer = authorizer;
            this.kustoTelemetryRepository = kustoTelemetryRepository;
        }

        protected override async Task<HttpResponseMessage> ExecuteInnerAsync(CancellationToken cancellationToken)
        {
            IncomingEvent.Current?.SetProperty("AgentId", this.agentId.Value);

            var authContext = await this.authorizer.CheckAuthorizedAsync(this.requestMessage, this.agentId);

            if (!FlightingUtilities.IsAgentIdEnabled(FlightingNames.AllowedAgentIdForQueueStatsApi, this.agentId))
            {
                IncomingEvent.Current?.SetProperty(FlightingNames.AllowedAgentIdForQueueStatsApi, "false");
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new JsonContent(new QueueStatsError(QueueStatsErrorCode.AgentNotAllowed))
                };
            }

            AssetGroupId assetGroupId = null;
            PrivacyCommandType privacyCommandType = PrivacyCommandType.None;
            string requestBody = await this.requestMessage.Content.ReadAsStringAsync();
            HttpContent stressForwardedBody = null;

            if (!string.IsNullOrWhiteSpace(requestBody))
            {
                stressForwardedBody = new StringContent(requestBody);
                QueueStatsRequest queueStatsRequest;
                try
                {
                    queueStatsRequest = JsonConvert.DeserializeObject<QueueStatsRequest>(requestBody);
                }
                catch (JsonException)
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new JsonContent(new QueueStatsError(QueueStatsErrorCode.MalformedRequest))
                    };
                }

                var assetQualifierFromRequest = queueStatsRequest.AssetGroupQualifier;
                var commandType = queueStatsRequest.CommandType;

                IncomingEvent.Current?.SetProperty("AssetQualifier", assetQualifierFromRequest);

                IDataAgentInfo agentInfo = this.dataAgentMap[this.agentId];

                if (!string.IsNullOrWhiteSpace(assetQualifierFromRequest))
                {
                    AssetQualifier assetQualifier;
                    try
                    {
                        assetQualifier = AssetQualifier.Parse(assetQualifierFromRequest);
                    }
                    catch
                    {
                        return new HttpResponseMessage(HttpStatusCode.BadRequest)
                        {
                            Content = new JsonContent(new QueueStatsError(QueueStatsErrorCode.MalformedAssetQualifier))
                        };
                    }

                    IAssetGroupInfo assetGroup;
                    try
                    {
                        assetGroup = agentInfo.AssetGroupInfos.Single(x => x.AssetGroupQualifier == assetQualifier.Value);
                        IncomingEvent.Current?.SetProperty("AssetGroupId", assetGroup.AssetGroupId.Value);
                        assetGroupId = assetGroup.AssetGroupId;
                    }
                    catch (InvalidOperationException)
                    {
                        return new HttpResponseMessage(HttpStatusCode.BadRequest)
                        {
                            Content = new JsonContent(new QueueStatsError(QueueStatsErrorCode.AssetQualifierNotFound))
                        };
                    }
                }

                var commandTypeFromRequest = queueStatsRequest.CommandType;
                IncomingEvent.Current?.SetProperty("CommandType", commandTypeFromRequest);
                if (!string.IsNullOrWhiteSpace(commandTypeFromRequest))
                {
                    if (!Enum.TryParse(commandTypeFromRequest, out privacyCommandType))
                    {
                        return new HttpResponseMessage(HttpStatusCode.BadRequest)
                        {
                            Content = new JsonContent(new QueueStatsError(QueueStatsErrorCode.MalformedCommandType))
                        };
                    }
                }
            }

            StressRequestForwarder.Instance.SendForwardedRequest(authContext, this.requestMessage, stressForwardedBody, this.agentId);

            QueueStatsResponse response = new QueueStatsResponse();
            try
            {
                var queueStats = await this.kustoTelemetryRepository.GetAgentStats(this.dataAgentMap, this.agentId, assetGroupId, privacyCommandType);
                response.QueueStats = queueStats;

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new JsonContent(response),
                };
            }
            catch (Exception ex)
            {
                Logger.Instance.UnexpectedException(ex);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new JsonContent(new QueueStatsError(QueueStatsErrorCode.UnexpectedException))
                };
            }
        }

        internal enum QueueStatsErrorCode
        {
            MalformedAssetQualifier = 2,
            AssetQualifierNotFound = 3,
            AgentNotAllowed = 4,
            MalformedCommandType = 5,
            GetQueueStatsApiDisabled = 13,
            MalformedRequest = 15,
            UnexpectedException = 25
        }

        internal class QueueStatsError
        {
            public QueueStatsError(QueueStatsErrorCode errorCode)
            {
                this.Message = errorCode.ToString();
                this.ErrorCode = errorCode;
            }

            public string Message { get; set; }

            public QueueStatsErrorCode ErrorCode { get; set; }
        }
    }
}