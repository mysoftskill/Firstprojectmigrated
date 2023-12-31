namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache;

    [ExcludeFromCodeCoverage] // Justification: not product code
    internal class GetDebugPdmsInfoActionResult : BaseHttpActionResult
    {
        private readonly HttpRequestMessage request;
        private readonly IAuthorizer authorizer;
        private readonly IAssetGroupInfoReader assetGroupInfoReader;
        private readonly AuthenticationScope authenticationScope;

        private readonly AgentId agentId;
        private readonly long? dataSetVersion;

        public GetDebugPdmsInfoActionResult(
            HttpRequestMessage requestMessage,
            AgentId agentId,
            long? dataSetVersion,
            IAssetGroupInfoReader assetGroupInfoReader,
            IAuthorizer authorizer,
            AuthenticationScope authenticationScope)
        {
            this.request = requestMessage;
            this.agentId = agentId;
            this.dataSetVersion = dataSetVersion;
            this.assetGroupInfoReader = assetGroupInfoReader;
            this.authorizer = authorizer;
            this.authenticationScope = authenticationScope;
        }

        protected override async Task<HttpResponseMessage> ExecuteInnerAsync(CancellationToken cancellationToken)
        {
            await this.authorizer.CheckAuthorizedAsync(this.request, this.authenticationScope);

            AssetGroupInfoCollectionReadResult readResult;
            if (this.dataSetVersion == null)
            {
                // Grab current version
                readResult = await this.assetGroupInfoReader.ReadAsync();
            }
            else
            {
                readResult = await this.assetGroupInfoReader.ReadVersionAsync(this.dataSetVersion.Value);
            }

            IEnumerable<AssetGroupInfoDocument> items = readResult.AssetGroupInfos;

            // Filter down to agent if applicable.
            if (this.agentId != null)
            {
                items = items.Where(x => x.AgentId == this.agentId);
            }

            var responseObject = new
            {
                CreatedTime = readResult.CreatedTime,
                DataSource = readResult.AssetGroupInfoStream,
                VariantSource = readResult.VariantInfoStream,
                Version = readResult.DataVersion,
                Items = items.ToList(),
            };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new JsonContent(responseObject)
            };
        }
    }
}
