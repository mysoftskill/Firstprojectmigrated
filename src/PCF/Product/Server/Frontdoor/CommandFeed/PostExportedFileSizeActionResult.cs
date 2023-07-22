namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    using Newtonsoft.Json;

    /// <summary>
    /// Implements the PostExportedFileSize API.
    /// </summary>
    internal class PostExportedFileSizeActionResult : BaseHttpActionResult
    {
        private readonly IAuthorizer authorizer;
        private readonly HttpRequestMessage requestMessage;
        private readonly IDataAgentMap dataAgentMap;
        private readonly AgentId agentId;
        private readonly AgentType agentType;
        private readonly ILogger logger;

        public PostExportedFileSizeActionResult(
            AgentId agentId,
            HttpRequestMessage requestMessage,
            IDataAgentMap dataAgentMap,
            IAuthorizer authorizer,
            ILogger logger)
        {
            this.requestMessage = requestMessage;
            this.authorizer = authorizer;
            this.dataAgentMap = dataAgentMap;
            this.agentId = agentId;
            this.logger = logger;

            this.agentType = this.agentId.GuidValue == Config.Instance.Frontdoor.CosmosExporterAgentId ? AgentType.Cosmos : AgentType.NonCosmos;
        }

        protected override async Task<HttpResponseMessage> ExecuteInnerAsync(CancellationToken cancellationToken)
        {
            var authContext = await this.authorizer.CheckAuthorizedAsync(this.requestMessage, this.agentId);
            string requestBody = await this.requestMessage.Content.ReadAsStringAsync();

            PostExportedFileSizeRequest request = JsonConvert.DeserializeObject<PostExportedFileSizeRequest>(requestBody);

            if (!LeaseReceipt.TryParse(request.LeaseReceipt, out LeaseReceipt leaseReceipt))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new JsonContent(
                        new PostExportedFileSizeError(PostExportedFileSizeErrorCode.MalformedLeaseReceipt))
                };
            }

            StressRequestForwarder.Instance.SendForwardedRequest(authContext, this.requestMessage, new StringContent(requestBody), leaseReceipt.AgentId, leaseReceipt.AssetGroupId, leaseReceipt.CommandId);

            if (leaseReceipt.AgentId != this.agentId)
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new JsonContent(
                        new PostExportedFileSizeError(PostExportedFileSizeErrorCode.LeaseReceiptAgentIdMismatch))
                };
            }

            if (!this.dataAgentMap[this.agentId].TryGetAssetGroupInfo(leaseReceipt.AssetGroupId, out _))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new JsonContent(
                        new PostExportedFileSizeError(PostExportedFileSizeErrorCode.LeaseReceiptAssetGroupIdInvalid))
                };
            }

            this.logger.LogExportFileSizeEvent(
                this.agentId,
                leaseReceipt.AssetGroupId,
                leaseReceipt.CommandId,
                request.FileName,
                request.OriginalSize,
                request.CompressedSize,
                request.IsCompressed,
                leaseReceipt.SubjectType,
                this.agentType,
                leaseReceipt.CloudInstance);

            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        internal enum PostExportedFileSizeErrorCode
        {
            LeaseReceiptAgentIdMismatch = 2,
            LeaseReceiptAssetGroupIdInvalid = 6,
            MalformedLeaseReceipt = 9,
        }

        internal class PostExportedFileSizeError
        {
            public PostExportedFileSizeError(PostExportedFileSizeErrorCode errorCode)
            {
                this.Message = errorCode.ToString();
                this.ErrorCode = errorCode;
            }

            public string Message { get; set; }

            public PostExportedFileSizeErrorCode ErrorCode { get; set; }
        }
    }
}
